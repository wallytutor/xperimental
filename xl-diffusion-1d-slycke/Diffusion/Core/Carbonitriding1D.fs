module Diffusion.Core.Carbonitriding1D

open System
open System.Threading.Tasks
open Diffusion.Numerics
open MixtureProperties
open FvmDomain1D
open DiffusionData1D

let private elements = ["C"; "N"; "Fe"]

// Even as small as this leads to performace improvements!
let private largeGridThreshold : int = 128

// A value above this threshold is considered fully implicit:
let private maxRelaxation : float = 0.999

type ICarbonitridingModel =
    // TODO return arrays, not tuples! -> REPLACE BY NEW MAKERS
    abstract member MoleFractionToConcentration: float -> float -> float * float
    abstract member ConcentrationToMoleFraction: float -> float -> float * float
    abstract member CarbonDiffusivity: float -> float -> float -> float
    abstract member NitrogenDiffusivity: float -> float -> float -> float

type SurfaceBoundaryCondition =
    { ExternalTemperature: float -> float
      ExternalCoefficients: float -> float array
      ExternalPotential: float -> float array }

type private SurfaceBoundaryState =
    { CarbonBoundary: float
      NitrogenBoundary: float
      CarbonDiffusivity: float
      NitrogenDiffusivity: float
      CarbonFlux: float
      NitrogenFlux: float }

// TODO refactor upper case member names
type CarbonitridingSolver =
    { model: ICarbonitridingModel
      grid: ImmersedNodeDomain1D
      bound: SurfaceBoundaryCondition
      carbonField: DiffusionField1D
      nitrogenField: DiffusionField1D

      numPoints: int
      timePoints: float array
      timeSteps: float array
      maxNonlinIter: int
      relaxationFactor: float
      absoluteTolerance: float
      relativeTolerance: float
      moleToMassConverter: float array -> float array -> float array * float array
      xcTmp: float array
      xnTmp: float array
      xcResults: float array array
      xnResults: float array array
      qcResults: float array
      qnResults: float array
      deltaW: float array
      deltaE: float array
      massIntake: float array }

    static member Create
      (init: IDiffusion1DSolverInit) (model: ICarbonitridingModel) : CarbonitridingSolver =
        if  init.YcField.Length <> init.YnField.Length then
            invalidArg "ynField" "Length of ynField must match length of ycField."
        let massToMole = makeMassFractionToMoleFractionConverter elements
        let moleToMass = makeMoleFractionToMassFractionConverter elements

        let mass2mole(y: float array) = massToMole [| yield! y; 1.0 - Array.sum y |]
        let mole2mass(x: float array) = moleToMass [| yield! x; 1.0 - Array.sum x |]
        let mole2conc = model.MoleFractionToConcentration

        let moleToMassConverter xcArr xnArr =
            let oneConv xc xn =
                let y = mole2mass [| xc; xn |]
                y.[0], y.[1]
            Array.map2 oneConv xcArr xnArr |> Array.unzip

        let initialComposition () =
            let xIni = Array.map2 (fun yc yn -> mass2mole [| yc; yn |]) init.YcField init.YnField
            let cNow = xIni |> Array.map (fun xi -> xi.[0])
            let nNow = xIni |> Array.map (fun xi -> xi.[1])
            Array.map2 mole2conc cNow nNow |> Array.unzip

        let timeDiscretization () =
            let tp = init.TimePoints
            let dt = tp |> Array.pairwise |> Array.map (fun (ti, tj) -> tj - ti)
            tp, dt

        let gridDistanceSq grid =
            let n = grid.Interior.Length
            let deltaW = Array.zeroCreate n
            let deltaE = Array.zeroCreate n

            // Notice that the first spacing is wrt the immersed node, not
            // applicable in this interpolation! Index is thus the one of the
            // node, so we need to stop at n-2 to access d_i out of bounds.
            let delta = grid.Spacing[1 .. grid.Spacing.Length - 2]

            // Upon inspection, the first cell has no west face, and the last
            // cell has no east face, so we only compute the internal faces.
            // The boundary faces are handled separately in the update functions.
            // The first internal face is between nodes 0 and 1 and its deltaE
            // is evaluated before the loop. The first element inside the loop
            // evaluates to deltaW.[1] = d_01 * L_1 and deltaE.[1] = d_12 * L_1
            // as expected by the FVM discretization.
            deltaE.[0] <- delta.[0] * grid.CellSizes.[0]
            deltaE.[n - 1] <- grid.CellSizes.[n - 1]**2 / 2.0

            deltaW.[0] <- grid.CellSizes.[0]**2 / 2.0
            deltaW.[n - 1] <- delta.[n - 2] * grid.CellSizes.[n - 2]

            for i in 1 .. n - 2 do
                let L_i = grid.CellSizes.[i]
                deltaW.[i] <- L_i * delta.[i - 1]
                deltaE.[i] <- L_i * delta.[i]

            deltaW, deltaE

        let cNow, nNow = initialComposition ()
        let tp, dt = timeDiscretization ()
        let deltaW, deltaE = gridDistanceSq init.Grid

        let bound =
            { ExternalTemperature  = init.Temperature
              ExternalCoefficients = init.HInf
              ExternalPotential    = fun t ->
                mass2mole (init.YInf t)
                |> fun y -> mole2conc y.[0] y.[1]
                |> fun (c, n) -> [| c; n |] }

        { model = model
          grid = init.Grid
          bound = bound

          carbonField = DiffusionField1D.FromConcentration cNow
          nitrogenField = DiffusionField1D.FromConcentration nNow

          numPoints = init.Grid.Interior.Length
          timePoints = tp
          timeSteps = dt

          maxNonlinIter = init.MaxNonlinIter
          relaxationFactor = init.Relaxation
          absoluteTolerance = init.AbsoluteTolerance
          relativeTolerance = init.RelativeTolerance

          moleToMassConverter = moleToMassConverter
          xcTmp = Array.copy cNow
          xnTmp = Array.copy nNow

          xcResults = Array.zeroCreate<float array> tp.Length
          xnResults = Array.zeroCreate<float array> tp.Length
          qcResults = Array.zeroCreate<float> tp.Length
          qnResults = Array.zeroCreate<float> tp.Length
          massIntake = Array.zeroCreate<float> tp.Length

          deltaW = deltaW
          deltaE = deltaE }

    member private self.UpdateFieldFvm
      (field: DiffusionField1D)
      (cBoundary: float)
      (boundaryDiffusivity: float)
      (tau: float)
      (xOld: float array) : float * float =
        // Aliases for better readability.
        let fo   = fourierNumberDeltaSq
        let grid = self.grid
        let mat  = field.Solver.Problem.Matrix
        let size = field.Solver.Problem.n

        let updateSurface () =
            // Notice that this is not affected by the arbitrary grid spacing,
            // as the `self.gridSpacing.[0]` is the distance from the first
            // node to the surface, which is the relevant length scale for the
            // Fourier number at the boundary, and `self.gridSpacing.[1]` is
            // the distance between the first and second nodes, which is the
            // relevant length scale for the Fourier number at the first face.
            let foB = fo boundaryDiffusivity       tau self.deltaW.[0]
            let foE = fo field.FaceDiffusivity.[0] tau self.deltaE.[0]
            mat.b.[0] <- 1.0 + 2.0 * foB + foE
            mat.c.[0] <- -1.0 * foE

            // XXX This is the only element of the RHS that is updated at each
            // inner iteration. Nothce that we use `field.Concentration.[0]` here,
            // which is the value from the previous time step, not the updated value
            // from the inner loop (updated in `xOld`).
            let rhs = field.Concentration.[0]
            field.Solver.Problem.d.[0] <- rhs + 2.0 * foB * cBoundary

        let updateInterior () =
            for i in 1 .. size - 2 do
                let Dw = field.FaceDiffusivity.[i - 1]
                let De = field.FaceDiffusivity.[i]

                let foW = fo Dw tau self.deltaW.[i]
                let foE = fo De tau self.deltaE.[i]

                mat.a.[i] <- -1.0 * foW
                mat.b.[i] <-  1.0 + foW + foE
                mat.c.[i] <- -1.0 * foE

        let updateSymmetry () =
            // Back boundary with zero gradient at z = L.
            let n = size - 1
            let foW = fo field.FaceDiffusivity.[n - 1] tau self.deltaE.[n - 1]

            mat.a.[n] <- -1.0 * foW
            mat.b.[n] <-  1.0 + foW

        let updateState () =
            let xNew = field.Solver.Solve ()
            let small = self.absoluteTolerance
            let mutable absChange = 0.0
            let mutable relChange = 0.0
            let mutable changeAbs = 0.0
            let mutable changeRel = 0.0

            for i = 0 to self.numPoints - 1 do
                if self.relaxationFactor < maxRelaxation then
                    let changeInc = self.relaxationFactor * (xNew.[i] - xOld.[i])
                    changeAbs <- abs changeInc
                    changeRel <- changeAbs / abs (xOld.[i] + small)
                    xNew.[i]  <- xOld.[i] + changeInc
                else
                    changeAbs <- abs (xNew.[i] - xOld.[i])
                    changeRel <- changeAbs / abs (xOld.[i] + small)

                // XXX does F# pass arrays by reference? YES, this modifies `self`!
                xOld.[i] <- xNew.[i]

                if changeAbs > absChange then absChange <- changeAbs
                if changeRel > relChange then relChange <- changeRel

            absChange, relChange

        updateSurface  ()
        updateInterior ()
        updateSymmetry ()
        updateState    ()

    member private self.OuterLoop
      (t: float)
      (tau: float) : int * float * float * bool =
        // Notice that evaluation is implicit, so the evaluation time for the
        // functions is t+tau, not t! This could eventually be parameterized.
        let tnow = t + tau
        let temp = self.bound.ExternalTemperature  tnow
        let xInf = self.bound.ExternalPotential    tnow
        let hInf = self.bound.ExternalCoefficients tnow

        let Dc xc xn = self.model.CarbonDiffusivity   xc xn temp
        let Dn xc xn = self.model.NitrogenDiffusivity xc xn temp

        let makeConcToDiff f =
            fun (cc: float) (cn: float) ->
                let xc, xn = self.model.ConcentrationToMoleFraction cc cn
                f xc xn

        let DcConc = makeConcToDiff Dc
        let DnConc = makeConcToDiff Dn

        let mutable converged = false
        let mutable iteration = 0
        let mutable absErr    = Double.NegativeInfinity
        let mutable relErr    = Double.NegativeInfinity

        let solveSurfaceBoundaryState () =
            // Solve the coupled surface boundary state before the element-wise
            // updates split.  Both surface concentrations are coupled because
            // both diffusivities depend on the local composition.
            let xc0 = self.xcTmp.[0]
            let xn0 = self.xnTmp.[0]

            // Data is node-based and stored only for interior nodes, but the grid
            // spacing includes the distance from the first node to the boundary.
            // So `delta.[0]` is the distance from the first node to the surface,
            // the value to be used for the Sherwood number calculation.
            let dl0 = self.grid.Spacing.[0]
            let tol = self.absoluteTolerance

            let mutable xcBoundary = xc0
            let mutable xnBoundary = xn0

            let mutable carbonDiffusivity   = DcConc xcBoundary xnBoundary
            let mutable nitrogenDiffusivity = DnConc xcBoundary xnBoundary
            let mutable deltaBoundary = Double.PositiveInfinity
            let mutable iteration = 0

            while deltaBoundary > tol && iteration < self.maxNonlinIter do
                carbonDiffusivity   <- DcConc xcBoundary xnBoundary
                nitrogenDiffusivity <- DnConc xcBoundary xnBoundary

                let shCarbon   = sherwoodNumber hInf.[0] dl0 carbonDiffusivity
                let shNitrogen = sherwoodNumber hInf.[1] dl0 nitrogenDiffusivity

                let xcNew = (xc0 + shCarbon   * xInf.[0]) / (1.0 + shCarbon)
                let xnNew = (xn0 + shNitrogen * xInf.[1]) / (1.0 + shNitrogen)

                let changeC = abs (xcNew - xcBoundary)
                let changeN = abs (xnNew - xnBoundary)

                deltaBoundary <- max changeC changeN
                xcBoundary    <- xcNew
                xnBoundary    <- xnNew
                iteration     <- iteration + 1

            carbonDiffusivity   <- DcConc xcBoundary xnBoundary
            nitrogenDiffusivity <- DnConc xcBoundary xnBoundary

            { CarbonBoundary      = xcBoundary
              NitrogenBoundary    = xnBoundary
              CarbonDiffusivity   = carbonDiffusivity
              NitrogenDiffusivity = nitrogenDiffusivity
              CarbonFlux          = hInf.[0] * (xcBoundary - xInf.[0])
              NitrogenFlux        = hInf.[1] * (xnBoundary - xInf.[1]) }

        let updateDiffusivities (xC: float array) (xN: float array) f field =
            // printfn $"xC[0] = {xC.[0]:E6}, xN[0] = {xN.[0]:E6}"

            for i = 0 to self.numPoints - 1 do
                field.Diffusivity.[i] <- f xC.[i] xN.[i]

                if i > 0 then
                    // General interpolation (replaces hamornic mean)
                    let d_i = self.grid.Spacing.[i - 1]
                    let d_j = self.grid.Spacing.[i]
                    let D_i = field.Diffusivity.[i - 1]
                    let D_j = field.Diffusivity.[i]
                    let num = d_i + d_j
                    let den = d_i / D_i + d_j / D_j
                    field.FaceDiffusivity.[i - 1] <- num / den

        let carbonSolver (xC: float array) (xN: float array) bState =
            fun () ->
                updateDiffusivities xC xN Dc self.carbonField
                let cB = bState.CarbonBoundary
                let Db = bState.CarbonDiffusivity
                self.UpdateFieldFvm self.carbonField cB Db tau self.xcTmp

        let nitrogenSolver (xC: float array) (xN: float array) bState =
            fun () ->
                updateDiffusivities xC xN Dn self.nitrogenField
                let cB = bState.NitrogenBoundary
                let Db = bState.NitrogenDiffusivity
                self.UpdateFieldFvm self.nitrogenField cB Db tau self.xnTmp

        let innerLoopSequential (xC: float array) (xN: float array) bState =
            let maxAbsC, maxRelC = carbonSolver xC xN bState ()
            let maxAbsN, maxRelN = nitrogenSolver xC xN bState ()
            max maxAbsC maxAbsN, max maxRelC maxRelN

        let innerLoopParallel (xC: float array) (xN: float array) bState =
            let carbonTask   = Task.Run (carbonSolver xC xN bState)
            let nitrogenTask = Task.Run (nitrogenSolver xC xN bState)
            Task.WaitAll [| carbonTask :> Task; nitrogenTask :> Task |]
            let maxAbsC, maxRelC = carbonTask.Result
            let maxAbsN, maxRelN = nitrogenTask.Result
            max maxAbsC maxAbsN, max maxRelC maxRelN

        let innerLoop =
            if self.numPoints < largeGridThreshold then
                innerLoopSequential
            else
                innerLoopParallel

        while not converged && iteration < self.maxNonlinIter do
            let xC, xN =
                let convert = self.model.ConcentrationToMoleFraction
                Array.map2 convert self.xcTmp self.xnTmp |> Array.unzip

            // printfn $"xC[0] = {xC.[0]:E6}, xN[0] = {xN.[0]:E6}"
            let bState = solveSurfaceBoundaryState ()
            self.carbonField.SurfaceFlux   <- bState.CarbonFlux
            self.nitrogenField.SurfaceFlux <- bState.NitrogenFlux
            let absNew, relNew = innerLoop xC xN bState

            absErr    <- absNew
            relErr    <- relNew
            iteration <- iteration + 1
            converged <- absErr < self.absoluteTolerance &&
                         relErr < self.relativeTolerance

        iteration, absErr, relErr, converged

    member self.Integrate
      (every: int) : unit=
        let updateIn () =
            // Update RHS only once before the inner loop, as it does not
            // change during the inner nonlinear iterations. Notice that this
            // requires a non-destroying version of the TDMA solver.
            let fc = self.carbonField
            let fn = self.nitrogenField

            for i = 0 to self.numPoints - 1 do
                self.xcTmp[i] <- fc.Concentration.[i]
                self.xnTmp[i] <- fn.Concentration.[i]

                fc.Solver.Problem.d[i] <- self.xcTmp.[i]
                fn.Solver.Problem.d[i] <- self.xnTmp.[i]

        let updateOut () =
            // Update the fields with the final solution of the inner loop,
            // thus preparing to the next time step.
            for i = 0 to self.numPoints - 1 do
                self.carbonField.Concentration.[i]   <- self.xcTmp.[i]
                self.nitrogenField.Concentration.[i] <- self.xnTmp.[i]

        let timePoints = self.timePoints
        let timeSteps  = self.timeSteps
        self.StoreState 0

        for i in 1 .. timePoints.Length - 1 do
            let t = timePoints.[i-1]
            let tau = timeSteps.[i-1]
            updateIn ()

            let stepOutputs = self.OuterLoop t tau
            let iteration, absErr, relErr, converged = stepOutputs

            updateOut ()
            self.StoreState i

            if not converged then
                printf  $"Warning: solver did not converge at time {t:E3} .. "
                printfn $"iters = {iteration:D2}, absErr = {absErr:E3}, relErr = {relErr:E3}"

            if i % every = 0 || i = timePoints.Length - 1 then
                printf  $"Step {i:D5}/{timePoints.Length-1:D5} (t = {timePoints.[i]:E3} s) .. "
                printfn $"iters = {iteration:D2}, absErr = {absErr:E3}, relErr = {relErr:E3}"

        let mutable sigmaF = 0.0
        let cC = self.carbonField.Concentration.[0]
        let cN = self.nitrogenField.Concentration.[0]
        let xc, xn = self.model.ConcentrationToMoleFraction cC cN

        for i in 1 .. self.massIntake.Length - 1 do
            let dm = 0.5 * (self.massIntake.[i - 1] + self.massIntake.[i])
            sigmaF <- sigmaF + dm * timeSteps.[i - 1]

        // XXX keep here until I am confident that this is right!
        // printfn $"{self.grid.PrintInfo ()}"
        // for i in 0 .. self.deltaE.Length - 1 do
        //     let dw = 1.0e+06 *sqrt self.deltaW.[i]
        //     let de = 1.0e+06 *sqrt self.deltaE.[i]
        //     printfn $"Node {i:D3}: W = {dw:F3} E = {de:F3}"

        printfn $"Integration complete, total mass intake: {sigmaF:F1} g/m^2"

    member self.GetReinitialization
      () : float array * float array =
        let xcFinal = self.carbonField.Concentration
        let xnFinal = self.nitrogenField.Concentration

        let xC, xN =
            let convert = self.model.ConcentrationToMoleFraction
            Array.map2 convert xcFinal xnFinal |> Array.unzip

        self.moleToMassConverter xC xN

    member self.StoreState
      (idx: int) : unit =
        // XXX; debug this should be g/m^2/s
        let mdotC = -12.0 * self.carbonField.SurfaceFlux
        let mdotN = -14.0 * self.nitrogenField.SurfaceFlux

        // TODO add surface/symmetry BC values to results.
        self.xcResults.[idx]  <- self.carbonField.Concentration
        self.xnResults.[idx]  <- self.nitrogenField.Concentration
        self.qcResults.[idx]  <- mdotC
        self.qnResults.[idx]  <- mdotN

        // TODO use only qResults in the future!
        self.massIntake.[idx] <- mdotC + mdotN

    member self.GetSurfaceFluxes
      (idx: int) : float * float =
        let numSteps = self.timePoints.Length
        if idx < 0 || idx >= numSteps then
            invalidArg "idx" $"Out-of-bounds index {idx} for {numSteps} time points."
        self.qcResults.[idx], self.qnResults.[idx]

    member self.GetStep
      (idx: int) =
        let numSteps = self.timePoints.Length

        if idx < 0 || idx >= numSteps-1 then
            invalidArg "idx" $"Out-of-bounds index {idx} for {numSteps} time points."

        let xc, xn = self.xcResults.[idx], self.xnResults.[idx]
        let yc, yn = self.moleToMassConverter xc xn
        {| z = self.grid.Interior; yc = yc; yn = yn |}

let runSimulation
  (init: IDiffusion1DSolverInit)
  (model: ICarbonitridingModel)
  (every: int option) : CarbonitridingSolver =
    let solver = CarbonitridingSolver.Create init model
    solver.Integrate (defaultArg every 10)
    solver
