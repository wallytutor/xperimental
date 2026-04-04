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

// ------------------------------------------------------------------------------------------------
// Dual
// ------------------------------------------------------------------------------------------------

type Dual = { Value : float; Deriv : float }

type Dual with // Basic arithmetic operations
    static member ( + ) (a: Dual, b: Dual) =
        { Value = a.Value + b.Value; Deriv = a.Deriv + b.Deriv }
    static member ( - ) (a: Dual, b: Dual) =
        { Value = a.Value - b.Value; Deriv = a.Deriv - b.Deriv }
    static member ( * ) (a: Dual, b: Dual) =
        { Value = a.Value * b.Value;
          Deriv = a.Deriv * b.Value + a.Value * b.Deriv }
    static member ( / ) (a: Dual, b: Dual) =
        let num = a.Deriv * b.Value - a.Value * b.Deriv
        let den = b.Value * b.Value
        { Value = a.Value / b.Value; Deriv = num / den }
    static member Pow (a: Dual, b: Dual) =
        let v = a.Value ** b.Value
        let d = v * (b.Deriv * log a.Value + b.Value * a.Deriv / a.Value)
        { Value = v; Deriv = d }
    static member ( ** ) (a: Dual, b: Dual) =
        Dual.Pow (a, b)
    static member ( ~- ) (a: Dual) =
        { Value = -a.Value; Deriv = -a.Deriv }

type Dual with // Common math functions
    static member Sin x =
        { Value = sin x.Value; Deriv = cos x.Value * x.Deriv }
    static member Cos x =
        { Value = cos x.Value; Deriv = -sin x.Value * x.Deriv }
    static member Tan x =
        let v = tan x.Value
        { Value = v; Deriv = x.Deriv / (cos x.Value * cos x.Value) }
    static member Exp x =
        let v = exp x.Value
        { Value = v; Deriv = v * x.Deriv }
    static member Log x =
        { Value = log x.Value; Deriv = x.Deriv / x.Value }

// ------------------------------------------------------------------------------------------------
// Expr
// ------------------------------------------------------------------------------------------------

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

type Expr with
    static member ( + ) (a: Expr, b: Expr) = Add(a, b)
    static member ( - ) (a: Expr, b: Expr) = Sub(a, b)
    static member ( * ) (a: Expr, b: Expr) = Mul(a, b)
    static member ( / ) (a: Expr, b: Expr) = Div(a, b)
    static member ( ** ) (a: Expr, b: Expr) = Exp(Mul(b, Log a))
    static member ( ~- ) (a: Expr) = Mul(Const -1.0, a)

let rec diff (v: IAutomaticDifferentiation<Expr, string>) =
    function
    | Const _          -> Const 0.0
    | Var x when x = v -> Const 1.0
    | Add(a, b)        -> Add(diff v a, diff v b)
    | Sub(a, b)        -> Sub(diff v a, diff v b)
    | Mul(a, b)        -> Add(Mul(diff v a, b), Mul(a, diff v b))
    | Div(a, b)        -> Div(Sub(Mul(diff v a, b), Mul(a, diff v b)), Mul(b,b))
    | Pow(a, b)        -> Mul(Pow(a, b), Add(Mul(diff v b, Log a), Mul(b, Div(diff v a, a))))
    | Neg a            -> Neg (diff v a)
    | Sin a            -> Mul(Cos a, diff v a)
    | Cos a            -> Mul(Const -1.0 * Sin a, diff v a)
    | Tan a            -> Div(diff v a, Mul(Cos a, Cos a))
    | Exp a            -> Mul(Exp a, diff v a)
    | Log a            -> Div(diff v a, a)
    | Var _            -> Const 0.0

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
    { new IAutomaticDifferentiation<Expr, string> with
        member _.Const x      = Const x
        member _.Var x        = Var x
        member _.Add ((x, y)) = Add(x, y)
        member _.Sub ((x, y)) = Sub(x, y)
        member _.Mul ((x, y)) = Mul(x, y)
        member _.Div ((x, y)) = Div(x, y)
        member _.Pow ((x, y)) = Pow(x, y)
        member _.Neg x        = Neg x
        member _.Sin x        = Sin x
        member _.Cos x        = Cos x
        member _.Tan x        = Tan x
        member _.Exp x        = Exp x
        member _.Log x        = Log x }

let inline var name = Var name
let inline num numb = Const numb

let inline sin x = Sin x
let inline cos x = Cos x
let inline tan x = Tan x
let inline exp x = Exp x
let inline log x = Log x



    // let rec eval (env: Map<string,float>) =
    //     function
    //     | Const c  -> c
    //     | Var x    -> env.[x]
    //     | Add(a,b) -> eval env a + eval env b
    //     | Sub(a,b) -> eval env a - eval env b
    //     | Mul(a,b) -> eval env a * eval env b
    //     | Div(a,b) -> eval env a / eval env b
    //     | Sin a    -> System.Math.Sin (eval env a)
    //     | Cos a    -> System.Math.Cos (eval env a)
    //     | Tan a    -> System.Math.Tan (eval env a)
    //     | Exp a    -> System.Math.Exp (eval env a)
    //     | Log a    -> System.Math.Log (eval env a)

// ------------------------------------------------------------------------------------------------
// Main
// ------------------------------------------------------------------------------------------------