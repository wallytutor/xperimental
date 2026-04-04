let PI = System.Math.PI

type Op1<'T> = 'T
type Op2<'T> = 'T * 'T

type Expr =
    | Const of float
    | Var   of string
    | Add   of Op2<Expr>
    | Sub   of Op2<Expr>
    | Mul   of Op2<Expr>
    | Div   of Op2<Expr>
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
    static member ( ~- ) (a: Expr) = Mul(Const -1.0, a)
    static member ( ** ) (a: Expr, b: Expr) = Exp(Mul(b, Log a))

type Expr with
    static member sin (x: Expr) = Sin x
    static member cos (x: Expr) = Cos x
    static member tan (x: Expr) = Tan x
    static member exp (x: Expr) = Exp x
    static member log (x: Expr) = Log x

module Sym =
    let inline var name = Var name
    let inline num numb = Const numb
    let inline sin x = Sin x
    let inline cos x = Cos x
    let inline tan x = Tan x
    let inline exp x = Exp x
    let inline log x = Log x
    let rec diff v =
        function
        | Const _ -> Const 0.0
        | Var x when x = v -> Const 1.0
        | Var _ -> Const 0.0
        | Add(a, b) -> Add(diff v a, diff v b)
        | Sub(a, b) -> Sub(diff v a, diff v b)
        | Mul(a, b) -> Add(Mul(diff v a, b), Mul(a, diff v b))
        | Div(a, b) -> Div(Sub(Mul(diff v a, b), Mul(a, diff v b)), Mul(b,b))
        | Sin a     -> Mul(Cos a, diff v a)
        | Cos a     -> Mul(Const -1.0 * Sin a, diff v a)
        | Tan a     -> Div(diff v a, Mul(Cos a, Cos a))
        | Exp a     -> Mul(Exp a, diff v a)
        | Log a     -> Div(diff v a, a)
    let rec eval (env: Map<string,float>) =
        function
        | Const c  -> c
        | Var x    -> env.[x]
        | Add(a,b) -> eval env a + eval env b
        | Sub(a,b) -> eval env a - eval env b
        | Mul(a,b) -> eval env a * eval env b
        | Div(a,b) -> eval env a / eval env b
        | Sin a    -> System.Math.Sin (eval env a)
        | Cos a    -> System.Math.Cos (eval env a)
        | Tan a    -> System.Math.Tan (eval env a)
        | Exp a    -> System.Math.Exp (eval env a)
        | Log a    -> System.Math.Log (eval env a)

module Main =
    open Sym

    let pi = num PI
    let x = var "x"
    let f = x * x + sin pi * x
    let df = diff "x" f

    printfn $"> f(x) = x^2 + sin(pi * x)"
    printfn $"> df/dx = 2x + cos(pi * x) * pi"
    printfn $"> symbolic derivative : {df}"

    let v  = eval (Map ["x", 1.0]) f
    let dv = eval (Map ["x", 1.0]) df

    printfn $"> f(1.0) = {v}"
    printfn $"> df/dx at x=1.0 = {dv}"
