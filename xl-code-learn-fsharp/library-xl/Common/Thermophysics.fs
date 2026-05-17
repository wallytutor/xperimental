namespace LibraryXl.Common

module Thermophysics =
    let arrheniusFactor (a: float) (e: float) (t: float) : float =
        a * exp(-e / (Constants.gasConstant * t))

    let idealGasDensity (p: float) (t: float) (m: float) : float =
        p * m / (Constants.gasConstant * t)

    let makeSutherlandMu (mu0: float) (Tr: float) (S: float) =
        fun (T: float) -> mu0 * (T / Tr)**(3.0/2.0) * (Tr + S) / (T + S)

    let reynoldsNumber (rho: float) (u: float) (d: float) (mu: float) : float =
        rho * u * d / mu