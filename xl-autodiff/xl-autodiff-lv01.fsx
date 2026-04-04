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

    static member ( ** ) (a: Dual, b: Dual) =
        let v = a.Value ** b.Value
        let d = v * (b.Deriv * log a.Value + b.Value * a.Deriv / a.Value)
        Dual.create v d

    static member ( ~- ) (a: Dual) =
        Dual.create (-a.Value) (-a.Deriv)

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

let inline grad (f: Dual[] -> Dual) (x: float[]) : float[] =
    let inline gradJ i j v =
        match i, j with
        | _ when i = j -> Dual.create v 1.0
        | _            -> Dual.create v 0.0

    let inline gradI f x i =
        x |> Array.mapi (fun j v -> gradJ i j v) |> fun xs -> (f xs).Deriv

    Array.init x.Length (fun i -> gradI f x i)

module Diff =
    let inline diff (f: Dual -> Dual) (x: float) : float =
        Dual.variable x |> f |> fun y -> y.Deriv

    let inline valueAndDiff (f: Dual -> Dual) (x: float) : float * float =
        Dual.variable x |> f |> fun y -> y.Value, y.Deriv

    let inline grad (f: Dual[] -> Dual) (x: float[]) = grad f x

module Main =
    let f x = Dual.sin (Dual.constant (2.0 * PI) * x)
    let v, d = Diff.valueAndDiff f 1.0

    printfn $"> Using `f(x) = sin(2πx)`"
    printfn $"> f(1) = {v}, f'(1) = {d}"

    let g (xs: Dual[]) =
        let x = xs.[0]
        let y = xs.[1]
        x * y + Dual.sin x

    let gradAt = Diff.grad g [| PI/2.0; 2.0 |]

    printfn $"> Using `g(x, y) = xy + sin(x)`"
    printfn $"> ∇g(π/2, 2) = ({gradAt.[0]:F2}, {gradAt.[1]:F2})"
    printfn $">            = (∂g/∂x, ∂g/∂y)"
    printfn $">            = (y + cos x, x)"