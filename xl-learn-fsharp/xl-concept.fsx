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

module Main =

    //  Mass transfer coefficients:
    let hc_inf = 1.0e-05
    let hn_inf = 1.0e-05

    // External concentration for BCs:
    let xc_inf = 0.011
    let xn_inf = 0.005

    // Initial conditions:
    let yc_ini = 0.0023
    let yn_ini = 0.0000

    // Temperature in K (operation conditions):
    let temperature = 1173.0

    // Space discretization:
    let num_points = 100
    let domain_depth = 0.002

    // Time step for transient simulations:
    let tau = 1.0

    // Relaxation factor for iterative solvers:
    let relaxation_factor = 0.5

    // maximum number of iterations:
    let max_iters = 20

    let model = Models.getModel ()

    let mass2Mole = Models.getMassFractionToMolarFractionConverter ()
    let mole2Mass = Models.getMolarFractionToMassFractionConverter ()

    let x = mass2Mole [| yc_ini; yn_ini |]
    let y = mole2Mass [| x.[0]; x.[1] |]

    let xc = x.[0]
    let xn = x.[1]

    let carbonDiff = model.carbonDiffusivity xc xn temperature
    let nitrogenDiff = model.nitrogenDiffusivity xc xn temperature

    printfn $"Mass fractions ........ C = {y.[0]:F4}, N = {y.[1]:F4}"
    printfn $"Mole fractions ........ C = {x.[0]:F4}, N = {x.[1]:F4}"
    printfn $"Carbon diffusivity .... {carbonDiff:E} m²/s"
    printfn $"Nitrogen diffusivity .. {nitrogenDiff:E} m²/s"

    Numerical.runTests ()



    let z = Numerical.linearSpace 0.0 domain_depth num_points
    let dz = Array.map2 (fun zi zj -> zj - zi) z[.. num_points - 2] z[1 ..]

    let xcArray = Array.create num_points xc
    let xnArray = Array.create num_points xn

    // let a = Array.create n -1.0
    // let b = Array.create n  2.0
    // let c = Array.create n -1.0
    // let d = Array.create n (h * h)   // f * h²  with  f = 1

    Gnuplot.GnuplotInteractive ()
    |>> "set term png size 800,600"
    |>> "set xlabel 'X-axis'"
    |>> "set ylabel 'Y-axis'"
    |>> "plot sin(x) + sin(3*x), -x"
