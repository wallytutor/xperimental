#load "../src/Diffusion.Numerics/Autodiff.fs"
open Diffusion.Numerics
open Diffusion.Numerics.Autodiff

// ------------------------------------------------------------------------------------------------
// Numeric context
//
// All floating-point constants pre-lifted to the target type ^T.
// For float: identity.  For Dual: pre-computed constants with zero derivative.
// Extensible: add magnetic_permeability, boltzmann_constant, etc. here.
// ------------------------------------------------------------------------------------------------

type NumericContext<^T> =
    {
        two: ^T
    }

// ------------------------------------------------------------------------------------------------
// Thermodynamic functions
//
// All parameters share the same numeric type ^T.  Use float for direct
// evaluation and Dual for forward-mode autodiff — no code duplication.
// ------------------------------------------------------------------------------------------------

let T_ref = 298.15

let inline cp (a0, a1, a2) T =
    a0 + a1 * T + a2 / (T * T)

let inline enthalpyDef ctx (a0, a1, a2) T =
    a0 * T + (a1 / ctx.two) * T * T - a2 / T

let inline entropyDef ctx (a0, a1, a2) T =
    a0 * log T + a1 * T - (a2 / ctx.two) / (T * T)

let inline enthalpy ctx tRef deltaHf (a0, a1, a2) T =
    deltaHf + enthalpyDef ctx (a0, a1, a2) T - enthalpyDef ctx (a0, a1, a2) tRef

let inline entropy ctx tRef s0 (a0, a1, a2) T =
    s0 + entropyDef ctx (a0, a1, a2) T - entropyDef ctx (a0, a1, a2) tRef

let inline gibbs ctx tRef deltaHf s0 (a0, a1, a2) T =
    enthalpy ctx tRef deltaHf (a0, a1, a2) T - T * entropy ctx tRef s0 (a0, a1, a2) T

// ------------------------------------------------------------------------------------------------
// Specialized data format
// ------------------------------------------------------------------------------------------------

type Substance =
    {
        molarMass: float
        molarVolume: float
        deltaGf: float
        deltaHf: float
        s0: float
        cp: float
        a0: float
        a1: float
        a2: float
    }

    member self.UnpackCoefs : float * float * float =
        self.a0, 1.0e-03 * self.a1, 1.0e+05 * self.a2

    member self.UnpackDualCoefs : Dual * Dual * Dual =
        constant self.a0, 1.0e-03 * constant self.a1, 1.0e+05 * constant self.a2

// ------------------------------------------------------------------------------------------------
// Database
//
// [Calcite](https://thermoddem.brgm.fr/species/calcite)
// [Lime](https://thermoddem.brgm.fr/species/lime)
// [CO2(g)](https://thermoddem.brgm.fr/species/co2g)
// ------------------------------------------------------------------------------------------------

let calcite = {
    molarMass = 100.087
    molarVolume = 36.934
    deltaGf = -1129109.0
    deltaHf = -1207605
    s0 = 91.780
    cp = 83.47
    a0 = 99.72
    a1 = 26.92
    a2 = -21.58
}

let lime = {
    molarMass = 56.077
    molarVolume = 16.760
    deltaGf = -603296.0
    deltaHf = -634920.0
    s0 = 38.100
    cp = 42.05
    a0 = 51.86
    a1 = 2.44
    a2 = -9.37
}

let co2 = {
    molarMass = 44.010
    molarVolume = 25.300
    deltaGf = -394373
    deltaHf = -393510
    s0 = 213.785
    cp = 37.14
    a0 = 33.98
    a1 = 23.88
    a2 = -3.52
}

// ------------------------------------------------------------------------------------------------
// Main
// ------------------------------------------------------------------------------------------------

// ------------------------------------------------------------------------------------------------
// Main
// ------------------------------------------------------------------------------------------------

let ctx_float = { two = 2.0 }
let ctx_dual = { two = constant 2.0 }

let species = [co2]
species |> List.iter (fun s ->
    printfn "Cp ...........: %f" (cp s.UnpackCoefs 298.15)
    printfn "Enthalpy .....: %f" (enthalpy ctx_float T_ref s.deltaHf s.UnpackCoefs 300.0)
    printfn "Entropy ......: %f" (entropy ctx_float T_ref s.s0 s.UnpackCoefs 300.0)
    printfn "Gibbs ........: %f" (gibbs ctx_float T_ref s.deltaHf s.s0 s.UnpackCoefs 300.0)
)

let g (T: Dual) : Dual =
    gibbs ctx_dual (constant T_ref) (constant calcite.deltaHf) (constant calcite.s0) calcite.UnpackDualCoefs T

let dG = Autodiff.diff g 300.0
printfn "\nAutodiff Verification (Calcite):"
printfn "dG/dT = %f" dG
printfn "-S(T) = %f" -(entropy ctx_float T_ref calcite.s0 calcite.UnpackCoefs 300.0)
