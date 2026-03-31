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

module Main =

    //  Mass transfer coefficients:
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
    let max_iters = 20

    // Convergence criterion for fixed-point iterations:
    let tolerance = 1.0e-10

    let model = Models.getModel ()

    let mass2Mole = Models.getMassFractionToMolarFractionConverter ()
    let mole2Mass = Models.getMolarFractionToMassFractionConverter ()

    let x = mass2Mole [| yc_ini; yn_ini |]
    let y = mole2Mass [| x.[0]; x.[1] |]

    let xc = x.[0]
    let xn = x.[1]

    // Convert external BCs from mass fractions to mole fractions for transport equations.
    let xInf = mass2Mole [| yc_inf; yn_inf |]
    let xc_inf = xInf.[0]
    let xn_inf = xInf.[1]

    let carbonDiff = model.carbonDiffusivity xc xn temperature
    let nitrogenDiff = model.nitrogenDiffusivity xc xn temperature

    printfn $"Mass fractions ........ C = {y.[0]:F4}, N = {y.[1]:F4}"
    printfn $"Mole fractions ........ C = {x.[0]:F4}, N = {x.[1]:F4}"
    printfn $"Carbon diffusivity .... {carbonDiff:E} m²/s"
    printfn $"Nitrogen diffusivity .. {nitrogenDiff:E} m²/s"

    Numerical.runTests ()

    let z = Numerical.linearSpace 0.0 domain_depth num_points

    let t = Numerical.arangeInclusive 0.0 7200.0 tau
    let nTimeSteps = Array.length t - 1
    let dt = Array.map2 (fun ti tj -> tj - ti) t[.. nTimeSteps - 1] t[1 ..]

    let buildDiffusionSystem
        (diffusivity: float array)
        (transferCoeff: float)
        (xInf: float)
        (timeStep: float)
        (previousTime: float array)
        : float array * float array * float array * float array =

        let n = Array.length previousTime
        let a = Array.zeroCreate n
        let b = Array.zeroCreate n
        let c = Array.zeroCreate n
        let d = Array.zeroCreate n

        // Surface control volume with convective exchange at z = 0.
        let dE0 = Numerical.pairwiseHarmonic diffusivity.[0] diffusivity.[1]
        let dzE0 = z.[1] - z.[0]
        let volume0 = 0.5 * dzE0
        let aE0 = dE0 / dzE0
        b.[0] <- volume0 / timeStep + transferCoeff + aE0
        c.[0] <- -aE0
        d.[0] <- volume0 / timeStep * previousTime.[0] + transferCoeff * xInf

        // Interior control volumes.
        for i in 1 .. n - 2 do
            let dzW = z.[i] - z.[i - 1]
            let dzE = z.[i + 1] - z.[i]
            let volume = 0.5 * (dzW + dzE)
            let dW = Numerical.pairwiseHarmonic diffusivity.[i - 1] diffusivity.[i]
            let dE = Numerical.pairwiseHarmonic diffusivity.[i] diffusivity.[i + 1]
            let aW = dW / dzW
            let aE = dE / dzE
            a.[i] <- -aW
            b.[i] <- volume / timeStep + aW + aE
            c.[i] <- -aE
            d.[i] <- volume / timeStep * previousTime.[i]

        // Back boundary with zero gradient at z = L.
        let nLast = n - 1
        let dzWLast = z.[nLast] - z.[nLast - 1]
        let volumeLast = 0.5 * dzWLast
        let dWLast = Numerical.pairwiseHarmonic diffusivity.[nLast - 1] diffusivity.[nLast]
        let aWLast = dWLast / dzWLast
        a.[nLast] <- -aWLast
        b.[nLast] <- volumeLast / timeStep + aWLast
        d.[nLast] <- volumeLast / timeStep * previousTime.[nLast]

        a, b, c, d

    let updateDiffusivities (xcField: float array) (xnField: float array) : float array * float array =
        let carbonField =
            Array.init num_points (fun i -> model.carbonDiffusivity xcField.[i] xnField.[i] temperature)

        let nitrogenField =
            Array.init num_points (fun i -> model.nitrogenDiffusivity xcField.[i] xnField.[i] temperature)

        carbonField, nitrogenField

    let convertMoleToMassFields (xcField: float array) (xnField: float array) : float array * float array =
        let ycField = Array.zeroCreate num_points
        let ynField = Array.zeroCreate num_points

        for i in 0 .. num_points - 1 do
            let yi = mole2Mass [| xcField.[i]; xnField.[i] |]
            ycField.[i] <- yi.[0]
            ynField.[i] <- yi.[1]

        ycField, ynField

    let relaxSolution (xNew: float array) (xOld: float array) (alpha: float) : float array =
        Array.map2 (fun newValue oldValue -> alpha * newValue + (1.0 - alpha) * oldValue) xNew xOld

    let innerLoop
        (timeStep: float)
        (previousTimeXc: float array)
        (previousTimeXn: float array)
        (tol: float)
        (relaxation: float)
        (hcInf: float)
        (hnInf: float)
        (xcInf: float)
        (xnInf: float)
        (buildSystem: float array -> float -> float -> float -> float array -> float array * float array * float array * float array)
        (updateDiff: float array -> float array -> float array * float array)
        (solveTridiagonal: float array -> float array -> float array -> float array -> float array)
        (relax: float array -> float array -> float -> float array)
        (xcIter: float array)
        (xnIter: float array)
        : float array * float array * float * bool =

        let oldXc = Array.copy xcIter
        let oldXn = Array.copy xnIter

        let carbonDiffField, nitrogenDiffField = updateDiff oldXc oldXn

        let carbonTask =
            Task.Run(fun () ->
                let aC, bC, cC, dC = buildSystem carbonDiffField hcInf xcInf timeStep previousTimeXc
                solveTridiagonal aC bC cC dC
            )

        let nitrogenTask =
            Task.Run(fun () ->
                let aN, bN, cN, dN = buildSystem nitrogenDiffField hnInf xnInf timeStep previousTimeXn
                solveTridiagonal aN bN cN dN
            )

        Task.WaitAll [| carbonTask :> Task; nitrogenTask :> Task |]
        let xcNew = carbonTask.Result
        let xnNew = nitrogenTask.Result

        let xcNext = relax xcNew oldXc relaxation
        let xnNext = relax xnNew oldXn relaxation

        let maxC =
            Array.map2 (fun newValue oldValue -> abs (newValue - oldValue)) xcNext oldXc
            |> Array.max

        let maxN =
            Array.map2 (fun newValue oldValue -> abs (newValue - oldValue)) xnNext oldXn
            |> Array.max

        let residual = max maxC maxN
        let converged = residual < tol

        xcNext, xnNext, residual, converged

    let outerLoop
        (timeStep: float)
        (previousTimeXc: float array)
        (previousTimeXn: float array)
        (maxIters: int)
        (tol: float)
        (relaxation: float)
        (hcInf: float)
        (hnInf: float)
        (xcInf: float)
        (xnInf: float)
        (buildSystem: float array -> float -> float -> float -> float array -> float array * float array * float array * float array)
        (updateDiff: float array -> float array -> float array * float array)
        (solveTridiagonal: float array -> float array -> float array -> float array -> float array)
        (relax: float array -> float array -> float -> float array)
        : float array * float array * bool * int * float =

        let mutable xcIter = Array.copy previousTimeXc
        let mutable xnIter = Array.copy previousTimeXn

        let mutable converged = false
        let mutable iteration = 0
        let mutable residual = 0.0

        while not converged && iteration < maxIters do
            let xcNext, xnNext, stepResidual, stepConverged =
                innerLoop
                    timeStep
                    previousTimeXc
                    previousTimeXn
                    tol
                    relaxation
                    hcInf
                    hnInf
                    xcInf
                    xnInf
                    buildSystem
                    updateDiff
                    solveTridiagonal
                    relax
                    xcIter
                    xnIter

            xcIter    <- xcNext
            xnIter    <- xnNext
            residual  <- stepResidual
            iteration <- iteration + 1
            converged <- stepConverged

        xcIter, xnIter, converged, iteration, residual

    let integrate
        (timePoints: float array)
        (timeSteps: float array)
        (xcInitial: float array)
        (xnInitial: float array)
        (convertMoleToMass: float array -> float array -> float array * float array)
        (solveStep: float -> float array -> float array -> float array * float array * bool * int * float)
        : float array array * float array array * bool * int * float =

        let nSteps = Array.length timeSteps

        let mutable xcSolution = Array.copy xcInitial
        let mutable xnSolution = Array.copy xnInitial

        let ycResults = Array.zeroCreate<float array> (nSteps + 1)
        let ynResults = Array.zeroCreate<float array> (nSteps + 1)

        let yc0, yn0 = convertMoleToMass xcSolution xnSolution
        ycResults.[0] <- yc0
        ynResults.[0] <- yn0

        let mutable finalConverged = false
        let mutable finalIteration = 0
        let mutable finalResidual = 0.0

        for k in 0 .. nSteps - 1 do
            let xcNext, xnNext, converged, iteration, residual = solveStep timeSteps.[k] xcSolution xnSolution

            xcSolution <- xcNext
            xnSolution <- xnNext

            let ycStep, ynStep = convertMoleToMass xcSolution xnSolution
            ycResults.[k + 1] <- ycStep
            ynResults.[k + 1] <- ynStep

            finalConverged <- converged
            finalIteration <- iteration
            finalResidual <- residual

            printfn $"Step {k + 1}/{nSteps} (t = {timePoints.[k + 1]:F1} s) .. iters = {iteration:D2}, residual = {residual:E3}, yCsurf = {ycStep.[0]:F6}, yNsurf = {ynStep.[0]:F6}"

        ycResults, ynResults, finalConverged, finalIteration, finalResidual

    let xcArray = Array.create num_points xc
    let xnArray = Array.create num_points xn

    let solveStepForCurrentModel (timeStep: float) (previousTimeXc: float array) (previousTimeXn: float array) =
        outerLoop
            timeStep
            previousTimeXc
            previousTimeXn
            max_iters
            tolerance
            relaxation_factor
            hc_inf
            hn_inf
            xc_inf
            xn_inf
            buildDiffusionSystem
            updateDiffusivities
            Numerical.tdma
            relaxSolution

    let ycResults, ynResults, finalConverged, finalIteration, finalResidual =
        integrate t dt xcArray xnArray convertMoleToMassFields solveStepForCurrentModel

    let last = num_points - 1
    printfn $"Final converged ....... {finalConverged}"
    printfn $"Final iterations ...... {finalIteration}"
    printfn $"Final residual ........ {finalResidual:E3}"
    printfn $"yC surface/depth ...... {ycResults.[nTimeSteps].[0]:F6} / {ycResults.[nTimeSteps].[last]:F6}"
    printfn $"yN surface/depth ...... {ynResults.[nTimeSteps].[0]:F6} / {ynResults.[nTimeSteps].[last]:F6}"

    // Save final profiles for post-processing and plotting.
    let outputDir = Path.Combine(__SOURCE_DIRECTORY__, "sandbox")
    Directory.CreateDirectory outputDir |> ignore

    let finalStatePath = Path.Combine(outputDir, "final-state.dat")
    let finalStateLines =
        Array.init num_points (fun i -> $"{z.[i]:G17} {ycResults.[nTimeSteps].[i]:G17} {ynResults.[nTimeSteps].[i]:G17}")

    File.WriteAllLines(finalStatePath, finalStateLines)

    let gnuplotPath = finalStatePath.Replace("\\", "/")

    Gnuplot.GnuplotInteractive ()
    |>> "set title 'Final mass-fraction state'"
    |>> "set xlabel 'Depth (m)'"
    |>> "set ylabel 'Mass fraction (-)'"
    |>> "set grid"
    |>> "set key left top"
    |>> $"plot '{gnuplotPath}' using 1:2 with lines lw 2 title 'yC', '' using 1:3 with lines lw 2 title 'yN'"
