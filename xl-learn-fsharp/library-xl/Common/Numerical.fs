namespace LibraryXl.Common

module Numerical =
    let tdma (a: float array) (b: float array) (c: float array) (d: float array) : float array =
        let n = Array.length d
        let cPrime = Array.zeroCreate n
        let dPrime = Array.zeroCreate n

        cPrime.[0] <- c.[0] / b.[0]
        dPrime.[0] <- d.[0] / b.[0]

        for i in 1 .. n - 1 do
            let m = b.[i] - a.[i] * cPrime.[i - 1]
            cPrime.[i] <- c.[i] / m
            dPrime.[i] <- (d.[i] - a.[i] * dPrime.[i - 1]) / m

        let x = Array.zeroCreate n
        x.[n - 1] <- dPrime.[n - 1]

        for i in n - 2 .. -1 .. 0 do
            x.[i] <- dPrime.[i] - cPrime.[i] * x.[i + 1]

        x

    /// Verify TDMA by solving the 1D discrete Poisson equation
    /// -u'' = 1, u(0) = u(1) = 0  on n interior nodes.
    /// Exact solution: u(x) = x(1 - x) / 2  (quadratic).
    let testTdmaQuadratic (n: int) (tol: float) : bool =
        let h   = 1.0 / float (n + 1)
        let nodes = Array.init n (fun i -> float (i + 1) * h)

        // Second-difference stencil: -u[i-1] + 2*u[i] - u[i+1] = h²
        let a = Array.create n -1.0
        let b = Array.create n  2.0
        let c = Array.create n -1.0
        let d = Array.create n (h * h)   // f * h²  with  f = 1

        let u      = tdma a b c d
        let uExact = Array.map (fun x -> x * (1.0 - x) / 2.0) nodes

        let maxErr =
            Array.map2 (fun ui ei -> abs (ui - ei)) u uExact
            |> Array.max

        maxErr < tol

    let runTests () =
        let tdmaPassed = testTdmaQuadratic 100 1.0e-12
        let tdmaStatus = if tdmaPassed then "PASSED" else "FAILED"
        printfn $"TDMA test (quadratic) .. {tdmaStatus}"

    let pairwiseHarmonic (x: float) (y: float) : float =
        2.0 * x * y / (x + y)

    let pairwiseGeometric (x: float) (y: float) : float =
        sqrt (x * y)

    let linearSpace (start: float) (stop: float) (num: int) : float array =
        let step = (stop - start) / float (num - 1)
        Array.init num (fun i -> start + float i * step)

    /// Similar to NumPy arange, but always includes both start and stop values.
    /// If step does not exactly land on stop, stop is appended as the last element.
    let arangeInclusive (start: float) (stop: float) (step: float) : float array =
        if step = 0.0 then
            invalidArg "step" "step must be non-zero"

        if start = stop then
            [| start |]
        else
            let span = stop - start
            if span * step < 0.0 then
                invalidArg "step" "step sign does not move from start toward stop"

            let values = System.Collections.Generic.List<float>()
            let eps = abs step * 1.0e-12 + 1.0e-15
            let mutable current = start
            values.Add current

            let keepGoing (x: float) =
                if step > 0.0 then x <= stop + eps else x >= stop - eps

            let mutable next = current + step
            while keepGoing next do
                values.Add next
                next <- next + step

            if abs (values.[values.Count - 1] - stop) > eps then
                values.Add stop

            values.ToArray()

    // TODO: use something more in the sense of what is in majordome.
    let geometricSpace (start: float) (stop: float) (num: int) : float array =
        let xs = 1.0 + start
        let xe = 1.0 + stop
        let ratio = (xe / xs) ** (1.0 / float (num - 1))
        Array.init num (fun i -> -1.0 + xs * ratio ** float i)
