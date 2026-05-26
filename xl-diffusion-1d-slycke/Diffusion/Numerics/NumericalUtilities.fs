[<AutoOpen>]
module Diffusion.Numerics.NumericalUtilities
open System

let pairwiseHarmonic (x: float) (y: float) : float =
    2.0 * x * y / (x + y)

let linearSpace (start: float) (stop: float) (num: int) : float array =
    let step = (stop - start) / float (num - 1)
    Array.init num (fun i -> start + float i * step)

let geometricSpace (start: float) (stop: float) (num: int) (d0: float) (d1: float) : float array =
    if num < 2 then
        invalidArg "num" "num must be at least 2"

    if d0 <= 0.0 then
        invalidArg "d0" "d0 must be positive"

    if d1 <= 0.0 then
        invalidArg "d1" "d1 must be positive"

    if num = 2 then
        [| start; stop |]
    else
        let nSeg   = num - 1
        let ratio  = (d1 / d0) ** (1.0 / float (nSeg - 1))
        let length = stop - start

        if abs (1.0 - ratio) < Double.Epsilon * 100.0 then
            linearSpace start stop num
        else
            // Build cumulative geometric distances and normalize to exact [start, stop].
            let sum = d0 * (1.0 - ratio ** float nSeg) / (1.0 - ratio)
            let points = Array.zeroCreate num
            points.[0] <- start

            let mutable cumulative = 0.0
            let mutable segment = d0

            for i in 1 .. nSeg do
                cumulative <- cumulative + segment
                points.[i] <- start + length * (cumulative / sum)
                segment <- segment * ratio

            points.[nSeg] <- stop
            points

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