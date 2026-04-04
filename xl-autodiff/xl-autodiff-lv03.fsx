// ------------------------------------------------------------------------------------------------
// API
// ------------------------------------------------------------------------------------------------

type Op1<'T> = 'T
type Op2<'T> = 'T * 'T

type IAutomaticDifferentiation<'T, 'U> =
    abstract Const : Op1<float> -> 'T
    abstract Var   : Op1<'U> -> 'T
    abstract Add   : Op2<'T> -> 'T
    abstract Sub   : Op2<'T> -> 'T
    abstract Mul   : Op2<'T> -> 'T
    abstract Div   : Op2<'T> -> 'T
    abstract Pow   : Op2<'T> -> 'T
    abstract Neg   : Op1<'T> -> 'T
    abstract Sin   : Op1<'T> -> 'T
    abstract Cos   : Op1<'T> -> 'T
    abstract Tan   : Op1<'T> -> 'T
    abstract Exp   : Op1<'T> -> 'T
    abstract Log   : Op1<'T> -> 'T

let inline sin (x: ^T) : ^T =
    (^T : (static member Sin : Op1<^T> -> ^T) x)
// let inline cos (x: ^T) = (^T : (member Cos : Op1<^T> -> ^T) (x))
// let inline tan (x: ^T) = (^T : (member Tan : Op1<^T> -> ^T) (x))
// let inline exp (x: ^T) = (^T : (member Exp : Op1<^T> -> ^T) (x))
// let inline log (x: ^T) = (^T : (member Log : Op1<^T> -> ^T) (x))

// ------------------------------------------------------------------------------------------------
// Dual
// ------------------------------------------------------------------------------------------------

type Dual = { Value : float; Deriv : float }

type Dual with // Constructors
    static member create v d = { Value = v; Deriv = d }
    static member constant v = Dual.create v 0.0
    static member variable v = Dual.create v 1.0

type Dual with // Evaluation
    static member diff (f: Dual -> Dual) (x: float) : float =
        Dual.variable x |> f |> fun y -> y.Deriv
    static member valueAndDiff (f: Dual -> Dual) (x: float) : float * float =
        Dual.variable x |> f |> fun y -> y.Value, y.Deriv

type Dual with // Operator overloads
    static member ( + ) (a: Dual, b: Dual) =
        Dual.create (a.Value + b.Value) (a.Deriv + b.Deriv)
    static member ( - ) (a: Dual, b: Dual) =
        Dual.create (a.Value - b.Value) (a.Deriv - b.Deriv)
    static member ( * ) (a: Dual, b: Dual) =
        Dual.create (a.Value * b.Value) (a.Deriv * b.Value + a.Value * b.Deriv)
    static member ( / ) (a: Dual, b: Dual) =
        let num = a.Deriv * b.Value - a.Value * b.Deriv
        let den = b.Value * b.Value
        Dual.create (a.Value / b.Value) (num / den)
    static member Pow (a: Dual, b: Dual) =
        let v = a.Value ** b.Value
        let d = v * (b.Deriv * log a.Value + b.Value * a.Deriv / a.Value)
        Dual.create v d
    static member ( ** ) (a: Dual, b: Dual) =
        Dual.Pow (a, b)
    static member ( ~- ) (a: Dual) =
        Dual.create (-a.Value) (-a.Deriv)

type Dual with // Common math functions
    static member Sin x =
        Dual.create (sin x.Value) (cos x.Value * x.Deriv)
    static member Cos x =
        Dual.create (cos x.Value) (-sin x.Value * x.Deriv)
    static member Tan x =
        let v = tan x.Value
        Dual.create (v) (x.Deriv / (cos x.Value * cos x.Value))
    static member Exp x =
        let v = exp x.Value
        Dual.create (v) (v * x.Deriv)
    static member Log x =
        Dual.create (log x.Value) (x.Deriv / x.Value)

// ------------------------------------------------------------------------------------------------
// Expr
// ------------------------------------------------------------------------------------------------

module Symbolic =
    type Expr =
        | Const of float
        | Var   of string
        | Add   of Op2<Expr>
        | Sub   of Op2<Expr>
        | Mul   of Op2<Expr>
        | Div   of Op2<Expr>
        | Pow   of Op2<Expr>
        | Neg   of Op1<Expr>
        | Sin   of Op1<Expr>
        | Cos   of Op1<Expr>
        | Tan   of Op1<Expr>
        | Exp   of Op1<Expr>
        | Log   of Op1<Expr>

    type Expr with // Constructors
        static member create name = Var name
        static member constant value = Const value
        static member variable name = Var name

    type Expr with // Operator overloads
        static member ( + ) (a: Expr, b: Expr) = Add(a, b)
        static member ( - ) (a: Expr, b: Expr) = Sub(a, b)
        static member ( * ) (a: Expr, b: Expr) = Mul(a, b)
        static member ( / ) (a: Expr, b: Expr) = Div(a, b)
        static member ( ** ) (a: Expr, b: Expr) = Exp(Mul(b, Log a))
        static member ( ~- ) (a: Expr) = Mul(Const -1.0, a)

    type Expr with
        static member recursive diff v =
            function
            | Var x when x = v -> Const 1.0
            | Const _   -> Const 0.0
            | Var _     -> Const 0.0
            | Add(a, b) -> Add(diff v a, diff v b)
            | Sub(a, b) -> Sub(diff v a, diff v b)
            | Mul(a, b) -> Add(Mul(diff v a, b), Mul(a, diff v b))
            | Div(a, b) -> Div(Sub(Mul(diff v a, b), Mul(a, diff v b)), Mul(b,b))
            | Pow(a, b) -> Mul(Pow(a, b), Add(Mul(diff v b, Log a), Mul(b, Div(diff v a, a))))
            | Neg a     -> Neg (diff v a)
            | Sin a     -> Mul(Cos a, diff v a)
            | Cos a     -> Mul(Const -1.0 * Sin a, diff v a)
            | Tan a     -> Div(diff v a, Mul(Cos a, Cos a))
            | Exp a     -> Mul(Exp a, diff v a)
            | Log a     -> Div(diff v a, a)

        static member recursive eval (env: Map<string,float>) =
            function
            | Const c  -> c
            | Var x    -> env.[x]
            | Add(a,b) -> eval env a + eval env b
            | Sub(a,b) -> eval env a - eval env b
            | Mul(a,b) -> eval env a * eval env b
            | Div(a,b) -> eval env a / eval env b
            | Pow(a,b) -> eval env a ** eval env b
            | Neg a    -> - (eval env a)
            | Sin a    -> System.Math.Sin (eval env a)
            | Cos a    -> System.Math.Cos (eval env a)
            | Tan a    -> System.Math.Tan (eval env a)
            | Exp a    -> System.Math.Exp (eval env a)
            | Log a    -> System.Math.Log (eval env a)

// ------------------------------------------------------------------------------------------------
// API implementations
// ------------------------------------------------------------------------------------------------

// type Function<'T> =
//     { Eval : 'T -> float
//       Diff : 'T -> float }

// ------------------------------------------------------------------------------------------------
// API implementations
// ------------------------------------------------------------------------------------------------

let dual =
    { new IAutomaticDifferentiation<Dual, float> with
        member _.Const x      = { Value = x; Deriv = 0.0 }
        member _.Var x        = { Value = x; Deriv = 1.0 }
        member _.Add ((x, y)) = x + y
        member _.Sub ((x, y)) = x - y
        member _.Mul ((x, y)) = x * y
        member _.Div ((x, y)) = x / y
        member _.Pow ((x, y)) = x ** y
        member _.Neg x        = -x
        member _.Sin x        = Dual.Sin x
        member _.Cos x        = Dual.Cos x
        member _.Tan x        = Dual.Tan x
        member _.Exp x        = Dual.Exp x
        member _.Log x        = Dual.Log x }

let expr =
    { new IAutomaticDifferentiation<Symbolic.Expr, string> with
        member _.Const x      = Symbolic.Const x
        member _.Var x        = Symbolic.Var x
        member _.Add ((x, y)) = Symbolic.Add(x, y)
        member _.Sub ((x, y)) = Symbolic.Sub(x, y)
        member _.Mul ((x, y)) = Symbolic.Mul(x, y)
        member _.Div ((x, y)) = Symbolic.Div(x, y)
        member _.Pow ((x, y)) = Symbolic.Pow(x, y)
        member _.Neg x        = Symbolic.Neg x
        member _.Sin x        = Symbolic.Sin x
        member _.Cos x        = Symbolic.Cos x
        member _.Tan x        = Symbolic.Tan x
        member _.Exp x        = Symbolic.Exp x
        member _.Log x        = Symbolic.Log x }

// ------------------------------------------------------------------------------------------------
// Main
// ------------------------------------------------------------------------------------------------

let PI = System.Math.PI

let f (x: Dual) = sin (x)
// let f_1 x = Dual.Sin (Dual.constant (2.0 * PI) * x)
// let f_2 x = Expr.Sin (Expr.constant (2.0 * PI) * x)

// let v, d = Dual.valueAndDiff f_1 1.0

// printfn $"> Using `f(x) = sin(2πx)`"
// printfn $"> f(1) = {v}, f'(1) = {d}"