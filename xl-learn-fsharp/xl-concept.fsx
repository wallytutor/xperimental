// #r "library-xl/bin/Debug/net10.0/library-xl.dll"
#load "library-xl/Common/Core.fs"
#load "library-xl/Common/Elements.fs"
#load "library-xl/Common/Mixtures.fs"
#load "library-xl/Common/Numerical.fs"
#load "library-xl/Common/Plotting.fs"
#load "library-xl/Common/Thermophysics.fs"
#load "library-xl/Slycke/Data.fs"
#load "library-xl/Slycke/Models.fs"
open LibraryXl.Common
open LibraryXl.Slycke
open System.IO
open System.Threading.Tasks

module Parameters =
    // Mass transfer coefficients:
    let hc_inf = 1.0e-05
    let hn_inf = 1.0e-05

    // External concentration for BCs:
    let yc_inf = 0.011
    let yn_inf = 0.005

    // Initial conditions:
    let yc_ini = 0.0023
    let yn_ini = 0.0000

    // Temperature in K (operation conditions):
    let temperature = 1173.0

    // Space discretization:
    let num_points = 200
    let domain_depth = 0.002

    // Time step for transient simulations:
    let tau = 1.0

    // Relaxation factor for iterative solvers:
    let relaxation_factor = 0.5

    // maximum number of iterations:
    let max_nonlin_iter = 20

    // Convergence criteria for fixed-point iterations:
    let rtol = 1.0e-06
    let atol = 1.0e-10


let model = Models.getModel ()

let mass2Mole = Models.getMassFractionToMolarFractionConverter ()
let mole2Mass = Models.getMolarFractionToMassFractionConverter ()

let x = mass2Mole [| Parameters.yc_ini; Parameters.yn_ini |]
let y = mole2Mass [| x.[0]; x.[1] |]

let xc = x.[0]
let xn = x.[1]

// Convert external BCs from mass fractions to mole fractions for transport equations.
let xInf = mass2Mole [| Parameters.yc_inf; Parameters.yn_inf |]
let xc_inf = xInf.[0]
let xn_inf = xInf.[1]

let carbonDiff = model.carbonDiffusivity xc xn Parameters.temperature
let nitrogenDiff = model.nitrogenDiffusivity xc xn Parameters.temperature

printfn $"Mass fractions ........ C = {y.[0]:F4}, N = {y.[1]:F4}"
printfn $"Mole fractions ........ C = {x.[0]:F4}, N = {x.[1]:F4}"
printfn $"Carbon diffusivity .... {carbonDiff:E} m²/s"
printfn $"Nitrogen diffusivity .. {nitrogenDiff:E} m²/s"

let z = Numerical.linearSpace 0.0 Parameters.domain_depth Parameters.num_points
let t = Numerical.arangeInclusive 0.0 7200.0 Parameters.tau
let nTimeSteps = Array.length t - 1
let dt = Array.map2 (fun ti tj -> tj - ti) t[.. nTimeSteps - 1] t[1 ..]
