namespace LibraryXl.Slycke

module Data =
    type Data =
        { CarbonInfDiffusivity: float
          NitrogenInfDiffusivity: float
          CarbonActivationEnergy: float
          NitrogenActivationEnergy: float
          CoefCarbon: float
          CoefNitrogen: float
          ActivationEnergyBase: float
          CoefPreExpFactor: float }

    let getSlyckeData () =
        { CarbonInfDiffusivity = 4.85e-05
          NitrogenInfDiffusivity = 9.10e-05
          CarbonActivationEnergy = 155_000.0
          NitrogenActivationEnergy = 168_600.0
          CoefCarbon = 1.0
          CoefNitrogen = 0.72
          ActivationEnergyBase = 570_000.0
          CoefPreExpFactor = 320.0 }