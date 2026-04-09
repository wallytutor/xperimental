namespace LibraryXl.Slycke

open System.Threading.Tasks
open LibraryXl.Common

module Slycke =
    let private elements = ["C"; "N"; "Fe"]

    let getMassFractionToMolarFractionConverter () =
        let massToMole = Mixtures.makeMassFractionToMoleFractionConverter elements
        fun (y: float array) ->
            match y with
            | [| yc; yn |] -> massToMole [| yc; yn; 1.0 - yc - yn |]
            | _ -> invalidArg "y" "Expected [| yC; yN |] mass fractions."

    let getMolarFractionToMassFractionConverter () =
        let moleToMass = Mixtures.makeMoleFractionToMassFractionConverter elements
        fun (x: float array) ->
            match x with
            | [| xc; xn |] -> moleToMass [| xc; xn; 1.0 - xc - xn |]
            | _ -> invalidArg "x" "Expected [| xC; xN |] mole fractions."

    let FourierNumber (D: float) (tau: float) (L: float) : float =
        D * tau / (L * L)

    let SherwoodNumber (h: float) (L: float) (D: float) : float =
        h * L / D

    type DiffusionField1D =
        { Matrix: Tridiagonal.MatrixProblem
          Diffusivity: float array
          Concentration: float array }

        static member create (numPoints: int) =
            { Matrix = Tridiagonal.MatrixProblem.create numPoints
              Diffusivity = Array.zeroCreate numPoints
              Concentration = Array.zeroCreate numPoints }

        static member fromConcentration (concentration: float array) =
            let numPoints = concentration.Length

            { Matrix = Tridiagonal.MatrixProblem.create numPoints
              Diffusivity = Array.zeroCreate numPoints
              Concentration = Array.copy concentration }

    type ISlyckeUserInit =
        abstract member Relaxation: float
        abstract member MaxNonlinIter: int
        abstract member RelativeTolerance: float
        abstract member AbsoluteTolerance: float
        abstract member CellCenters: float array
        abstract member Spacing: float array
        abstract member TimePoints: float array
        abstract member TimeSteps: float array
        abstract member YcField: float array
        abstract member YnField: float array
        abstract member YInf: float -> float array
        abstract member HInf: float -> float array
        abstract member Temperature: float -> float

    type SlyckeDiffusivity = float -> float -> float

    type SlyckeData =
        { CarbonInfDiffusivity: float
          NitrogenInfDiffusivity: float
          CarbonActivationEnergy: float
          NitrogenActivationEnergy: float
          CoefCarbon: float
          CoefNitrogen: float
          ActivationEnergyBase: float
          CoefPreExpFactor: float }

        static member getDefaults () =
            { CarbonInfDiffusivity = 4.85e-05
              NitrogenInfDiffusivity = 9.10e-05
              CarbonActivationEnergy = 155_000.0
              NitrogenActivationEnergy = 168_600.0
              CoefCarbon = 1.0
              CoefNitrogen = 0.72
              ActivationEnergyBase = 570_000.0
              CoefPreExpFactor = 320.0 }

    type SlyckeModel =
        { Data: SlyckeData
          Conditions: ISlyckeUserInit }

        static member private geometricExclusionFactor (xa: float) (xb: float) =
           (1.0 - xb) / (1.0 - 5.0 * (xa + xb))

        member private self.compositionModifier (xc: float) (xn: float) =
            self.Data.CoefCarbon * xc + self.Data.CoefNitrogen * xn

        member private self.preExponentialFactor (xc: float) (xn: float) =
            let b = self.Data.CoefPreExpFactor * self.compositionModifier xc xn
            exp (-b / Constants.gasConstant)

        member private self.activationEnergy (Ea: float) (xc: float) (xn: float) =
            Ea - self.Data.ActivationEnergyBase * self.compositionModifier xc xn

        member _.massToMoleFraction (x: float array) : float array =
            let massToMole = getMassFractionToMolarFractionConverter ()
            massToMole x

        member _.moleToMassFraction (y: float array) : float array =
            let moleToMass = getMolarFractionToMassFractionConverter ()
            moleToMass y

        member self.carbonDiffusivity (xc: float) (xn: float) (t: float) =
            let a = SlyckeModel.geometricExclusionFactor xc xn * self.preExponentialFactor xc xn
            let e = self.activationEnergy self.Data.CarbonActivationEnergy xc xn
            self.Data.CarbonInfDiffusivity * Thermophysics.arrheniusFactor a e t

        member self.nitrogenDiffusivity (xc: float) (xn: float) (t: float) =
            let a = SlyckeModel.geometricExclusionFactor xn xc * self.preExponentialFactor xc xn
            let e = self.activationEnergy self.Data.NitrogenActivationEnergy xc xn
            self.Data.NitrogenInfDiffusivity * Thermophysics.arrheniusFactor a e t

        static member create (conditions: ISlyckeUserInit) =
            { Data = SlyckeData.getDefaults (); Conditions = conditions }

    type SlyckeManager =
        { Setup: ISlyckeUserInit
          Model: SlyckeModel
          CarbonField: DiffusionField1D
          NitrogenField: DiffusionField1D
          numPoints: int }

        static member create (init: ISlyckeUserInit) =
            if  init.YcField.Length <> init.YnField.Length then
                invalidArg "ynField" "Length of ynField must match length of ycField."

            let model = SlyckeModel.create init

            let xFun = fun yc yn -> model.massToMoleFraction [| yc; yn |]
            let xIni = Array.map2 xFun init.YcField init.YnField
            let cNow = xIni |> Array.map (fun xi -> xi.[0])
            let nNow = xIni |> Array.map (fun xi -> xi.[1])

            { Setup = init
              Model = model
              CarbonField = DiffusionField1D.fromConcentration cNow
              NitrogenField = DiffusionField1D.fromConcentration nNow
              numPoints = xIni.Length }

        member private self.updateElement
          (field: DiffusionField1D)
          (cInf: float)
          (hInf: float)
          (tau: float) : float array =
            // Aliases for better readability.
            let interp = Numerical.pairwiseHarmonic
            let delta = self.Setup.Spacing
            let cNow = field.Concentration
            let DNow = field.Diffusivity

            // Data is cell-based, so retrieving the coordinate of the first cell already
            // provides the half-length used for Sherwood number calculation; the distance
            // between first two cells gives the length for Fourier number.
            let dl0 = delta.[0]
            let dl1 = delta.[1]

            // West boundary with convective exchange at z = 0. Please note that we use
            // the diffusivity at the first interior point for the surface control volume
            // in the internal iteration, but in fact we should solve a nonlinear problem
            // to find the diffusivity at the surface control volume.
            // TODO loop until convergence?
            let shB = SherwoodNumber hInf dl0 DNow.[0]
            let cBw = (cNow.[0] + shB * cInf) / (1.0 + shB)

            let foB = FourierNumber DNow.[0] tau dl0
            let foE = FourierNumber (interp DNow.[0] DNow.[1]) tau dl1

            field.Matrix.b.[0] <- 1.0 + 2.0 * foB + foE
            field.Matrix.c.[0] <- -1.0 * foE
            field.Matrix.d.[0] <- cNow.[0] + 2.0 * foB * cBw

            // Interior control volumes.
            for i in 1 .. field.Matrix.n - 2 do
                let lw = delta[i + 0]
                let le = delta[i + 1]
                let lP = 0.5 * (lw + le)

                let foW = FourierNumber (interp DNow.[i - 1] DNow.[i]) tau (sqrt (lw * lP))
                let foE = FourierNumber (interp DNow.[i + 1] DNow.[i]) tau (sqrt (le * lP))

                field.Matrix.a.[i] <- -1.0 * foW
                field.Matrix.b.[i] <- 1.0 + foW + foE
                field.Matrix.c.[i] <- -1.0 * foE
                field.Matrix.d.[i] <- cNow.[i]

            // Back boundary with zero gradient at z = L.
            let n = field.Matrix.n - 1
            let lw = delta[n - 1]
            let lP = 2.0 * delta[n]
            let foW = FourierNumber (interp DNow.[n - 1] DNow.[n]) tau (sqrt (lw * lP))

            field.Matrix.a.[n] <- -1.0 * foW
            field.Matrix.b.[n] <- 1.0 + foW
            field.Matrix.d.[n] <- cNow.[n]

            field.Matrix.solve ()

        member private self.updateField
          (field: DiffusionField1D)
          (cInf: float)
          (hInf: float)
          (tau: float)
          (x: float array) : float * float =
            let xNew = self.updateElement field cInf hInf tau

            let small = self.Setup.AbsoluteTolerance
            let mutable absChange = 0.0
            let mutable relChange = 0.0

            for i = 0 to self.numPoints - 1 do
                let changeInc = self.Setup.Relaxation * (xNew.[i] - x.[i])
                let changeAbs = abs changeInc
                let changeRel = changeAbs / abs (x.[i] + small)

                let xUpdated = x.[i] + changeInc
                field.Concentration.[i] <- xUpdated

                if changeAbs > absChange then
                    absChange <- changeAbs

                if changeRel > relChange then
                    relChange <- changeRel

            absChange, relChange

        member private self.innerLoop
          (Dc: SlyckeDiffusivity)
          (Dn: SlyckeDiffusivity)
          (xInf: float array)
          (hInf: float array)
          (tau: float) : float * float =
            let xcNow = self.CarbonField.Concentration
            let xnNow = self.NitrogenField.Concentration

            let carbonTask =
                Task.Run (fun () ->
                    for i = 0 to self.numPoints - 1 do
                        self.CarbonField.Diffusivity.[i] <- Dc xcNow.[i] xnNow.[i]

                    self.updateField self.CarbonField xInf.[0] hInf.[0] tau xcNow
                )

            let nitrogenTask =
                Task.Run (fun () ->
                    for i = 0 to self.numPoints - 1 do
                        self.NitrogenField.Diffusivity.[i] <- Dn xcNow.[i] xnNow.[i]

                    self.updateField self.NitrogenField xInf.[1] hInf.[1] tau xnNow
                )

            Task.WaitAll [| carbonTask :> Task; nitrogenTask :> Task |]

            let maxAbsC, maxRelC = carbonTask.Result
            let maxAbsN, maxRelN = nitrogenTask.Result
            max maxAbsC maxAbsN, max maxRelC maxRelN

        member self.outerLoop
          (t: float)
          (tau: float) : int * float * float * bool =
            let temp = self.Setup.Temperature t
            let Dc xc xn = self.Model.carbonDiffusivity   xc xn temp
            let Dn xc xn = self.Model.nitrogenDiffusivity xc xn temp

            let xInf = self.Model.massToMoleFraction (self.Setup.YInf t)
            let hInf = self.Setup.HInf t

            let mutable converged = false
            let mutable iteration = 0
            let mutable absErr = 0.0
            let mutable relErr = 0.0

            while not converged && iteration < self.Setup.MaxNonlinIter do
                let residuals = self.innerLoop Dc Dn xInf hInf tau
                absErr <- residuals |> fst
                relErr <- residuals |> snd

                converged <- absErr < self.Setup.AbsoluteTolerance &&
                             relErr < self.Setup.RelativeTolerance

                iteration <- iteration + 1

            iteration, absErr, relErr, converged

        static member runSimulation (init: ISlyckeUserInit) : SlyckeManager=
            let mngr = SlyckeManager.create init

            let timePoints = mngr.Setup.TimePoints
            let timeSteps = mngr.Setup.TimeSteps

            for i in 0 .. timePoints.Length - 2 do
                let t = timePoints.[i]
                let stepOutputs = mngr.outerLoop t timeSteps.[i]
                let iteration, absErr, relErr, converged = stepOutputs

                printf  $"Step {i + 1}/{timePoints.Length - 1} (t = {timePoints.[i + 1]:F1} s) .. "
                printfn $"iters = {iteration:D2}, absErr = {absErr:E3}, relErr = {relErr:E3}"

            mngr
