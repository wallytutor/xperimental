// #r "library-xl/bin/Debug/net10.0/library-xl.dll"
#load "library-xl/Common/Constants.fs"
#load "library-xl/Common/Elements.fs"
#load "library-xl/Common/Mixtures.fs"
#load "library-xl/Common/Numerical.fs"
#load "library-xl/Common/Thermophysics.fs"
#load "library-xl/Slycke/Data.fs"
#load "library-xl/Slycke/Models.fs"
open LibraryXl.Common
open LibraryXl.Slycke

module Main =
    let yc = 0.0023
    let yn = 0.0000
    let temperature = 1173.0

    let model = Models.getModel ()

    let mass2Mole = Models.getMassFractionToMolarFractionConverter ()
    let mole2Mass = Models.getMolarFractionToMassFractionConverter ()

    let x = mass2Mole [| yc; yn |]
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

    let num_points = 100
    let domain_depth = 0.002

    let z = Numerical.linearSpace 0.0 domain_depth num_points
    let dz = Array.map2 (fun zi zj -> zj - zi) z[.. num_points - 2] z[1 ..]

    let xcArray = Array.create num_points xc
    let xnArray = Array.create num_points xn

    // let a = Array.create n -1.0
    // let b = Array.create n  2.0
    // let c = Array.create n -1.0
    // let d = Array.create n (h * h)   // f * h²  with  f = 1

    open System
    open System.Diagnostics

    let gp =
        new ProcessStartInfo (
            FileName              = "gnuplot",
            UseShellExecute       = false,
            CreateNoWindow        = true,
            RedirectStandardInput = true
        ) |> Process.Start

    // Draw graph of two simple functions
    gp.StandardInput.WriteLine "plot sin(x) + sin(3*x), -x"