module Diffusion.Slycke.DiffusionPlainFeCN

open Diffusion.Core.ElementData
open Diffusion.Core.Carbonitriding1D

[<Literal>]
let private gasConstant: float = 8.314462618

let private ironMolarMass: float = Array.head (getMolarMassArray ["Fe"]) / 1000.0

let private ironDensity: float = 7870.0

type SlyckeModel =
    { CarbonInfDiffusivity: float
      NitrogenInfDiffusivity: float
      CarbonActivationEnergy: float
      NitrogenActivationEnergy: float
      CoefCarbon: float
      CoefNitrogen: float
      ActivationEnergyBase: float
      CoefPreExpFactor: float }

    static member Default : SlyckeModel =
        { CarbonInfDiffusivity = 4.85e-05
          NitrogenInfDiffusivity = 9.10e-05
          CarbonActivationEnergy = 155_000.0
          NitrogenActivationEnergy = 168_600.0
          CoefCarbon = 1.0
          CoefNitrogen = 0.72
          ActivationEnergyBase = 570_000.0
          CoefPreExpFactor = 320.0 }

    interface ICarbonitridingModel with
        member _.MoleFractionToConcentration
          (xC: float) (xN: float) : float * float =
            let c0 = ironDensity / ((1.0 - xC - xN) * ironMolarMass)
            xC * c0, xN * c0

        member _.ConcentrationToMoleFraction
          (C: float) (N: float) : float * float =
            let den = (C + N) * ironMolarMass + ironDensity
            C * ironMolarMass / den, N * ironMolarMass / den

        member self.CarbonDiffusivity
          (xc: float) (xn: float) (t: float) : float =
            // printf $"xC[0] = {xc:E6}, xN[0] = {xn:E6}, T = {t:E6} K"
            // failwith "Debug stop"
            let f = (1.0 - xn) / (1.0 - 5.0 * (xc + xn))
            let D = self.CarbonInfDiffusivity
            let E = self.CarbonActivationEnergy
            f * self.Diffusivity D E xc xn t

        member self.NitrogenDiffusivity
          (xc: float) (xn: float) (t: float) : float =
            let f = (1.0 - xc) / (1.0 - 5.0 * (xc + xn))
            let D = self.NitrogenInfDiffusivity
            let E = self.NitrogenActivationEnergy
            f * self.Diffusivity D E xc xn t

    member private self.CompositionModifier
      (xc: float) (xn: float) : float =
        self.CoefCarbon * xc + self.CoefNitrogen * xn

    member private self.Diffusivity
      (D: float) (E: float) (xc: float) (xn: float) (t: float) : float =
        let b = self.CoefPreExpFactor * self.CompositionModifier xc xn
        let e = E - self.ActivationEnergyBase * self.CompositionModifier xc xn
        D * exp (-b / gasConstant) * exp(-e / (gasConstant * t))
