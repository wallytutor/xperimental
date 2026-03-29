namespace LibraryXl.Slycke
open LibraryXl.Common

module Models =
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

    type Model =
        { Data: Data.Data }

        static member private geometricExclusionFactor (xa: float) (xb: float) =
           (1.0 - xb) / (1.0 - 5.0 * (xa + xb))

        member private self.compositionModifier (xc: float) (xn: float) =
            self.Data.CoefCarbon * xc + self.Data.CoefNitrogen * xn

        member private self.preExponentialFactor (xc: float) (xn: float) =
            let b = self.Data.CoefPreExpFactor * self.compositionModifier xc xn
            exp (-b / Constants.gasConstant)

        member private self.activationEnergy (Ea: float) (xc: float) (xn: float) =
            Ea - self.Data.ActivationEnergyBase * self.compositionModifier xc xn

        member self.carbonDiffusivity (xc: float) (xn: float) (t: float) =
            let a = Model.geometricExclusionFactor xc xn * self.preExponentialFactor xc xn
            let e = self.activationEnergy self.Data.CarbonActivationEnergy xc xn
            self.Data.CarbonInfDiffusivity * Thermophysics.arrheniusFactor a e t

        member self.nitrogenDiffusivity (xc: float) (xn: float) (t: float) =
            let a = Model.geometricExclusionFactor xn xc * self.preExponentialFactor xc xn
            let e = self.activationEnergy self.Data.NitrogenActivationEnergy xc xn
            self.Data.NitrogenInfDiffusivity * Thermophysics.arrheniusFactor a e t

    let getModel () = { Data = Data.getSlyckeData () }
