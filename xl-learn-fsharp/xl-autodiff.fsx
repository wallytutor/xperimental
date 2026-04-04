// Automatic differentiation in F#
// ===============================
//
// 2026-04-04
// ----------
// Progressive complexity, starting with forward mode based on dual numbers.
// This does not scale well for functions with many inputs, but is a good
// starting point for understanding the concepts.

let PI = System.Math.PI

type Dual = { Value : float; Deriv : float }

module Dual =
    let inline create v d = { Value = v; Deriv = d }
    let inline constant v = create v 0.0
    let inline variable v = create v 1.0

type Dual with // Basic arithmetic operations
    static member ( + ) (a: Dual, b: Dual) =
        Dual.create (a.Value + b.Value) (a.Deriv + b.Deriv)

    static member ( - )(a: Dual, b: Dual) =
        Dual.create (a.Value - b.Value) (a.Deriv - b.Deriv)

    static member ( * ) (a: Dual, b: Dual) =
        Dual.create (a.Value * b.Value)
                    (a.Deriv * b.Value + a.Value * b.Deriv)

    static member ( / ) (a: Dual, b: Dual) =
        let v = a.Value / b.Value
        let d = (a.Deriv * b.Value - a.Value * b.Deriv) / (b.Value * b.Value)
        Dual.create v d

    static member ( ~- ) (a: Dual) =
        Dual.create (-a.Value) (-a.Deriv)

    static member ( ** ) (a: Dual, b: Dual) =
        let v = a.Value ** b.Value
        let d = v * (b.Deriv * log a.Value + b.Value * a.Deriv / a.Value)
        Dual.create v d

type Dual with // Common math functions
    static member sin x =
        Dual.create (sin x.Value) (cos x.Value * x.Deriv)

    static member cos x =
        Dual.create (cos x.Value) (-sin x.Value * x.Deriv)

    static member tan x =
        let v = tan x.Value
        Dual.create v (x.Deriv / (cos x.Value * cos x.Value))

    static member exp x =
        let v = exp x.Value
        Dual.create v (v * x.Deriv)

    static member log x =
        Dual.create (log x.Value) (x.Deriv / x.Value)

module Diff =
    let inline diff (f: Dual -> Dual) (x: float) : float =
        let xDual = Dual.variable x
        let yDual = f xDual
        yDual.Deriv

    let inline valueAndDiff (f: Dual -> Dual) (x: float) : float * float =
        let xDual = Dual.variable x
        let yDual = f xDual
        yDual.Value, yDual.Deriv

let f x = Dual.sin x

let v, d = Diff.valueAndDiff f PI
