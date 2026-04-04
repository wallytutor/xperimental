// ------------------------------------------------------------------------------------------------
// API
// ------------------------------------------------------------------------------------------------

type Op1<'T> = 'T
type Op2<'T> = 'T * 'T

// The contract is expressed via static member constraints (SRTP), not an interface type.
// Any type ^T that provides the members below satisfies the typeclass and works with diff:
//
//   Const : float -> ^T              – lift a float constant
//   Seed  : ^U -> ^T                 – create the active input (Deriv=1 or Var node)
//   Diff  : ^U * ^T -> ^D            – extract/compute the derivative from the result
//   ( + ) ( - ) ( * ) ( / ) ( ** )  – arithmetic operators
//   Sin Cos Tan Exp Log : ^T -> ^T   – math functions (F# built-ins dispatch here via SRTP)
//
// 'inline' is mandatory: SRTP constraints are resolved at each call site at compile time.

/// Lift a float constant into any compatible type.
let inline konst (c: float) : ^T =
    (^T : (static member Const : float -> ^T) c)

/// Differentiate f at varId. Works for any type providing Seed and Diff static members.
let inline diff (varId: ^U) (f: ^T -> ^T) : ^D =
    (^T : (static member Seed : ^U -> ^T) varId)
    |> f
    |> fun result -> (^T : (static member Diff : ^U * ^T -> ^D) (varId, result))

// ------------------------------------------------------------------------------------------------
// Dual
// ------------------------------------------------------------------------------------------------

type Dual = { Value : float; Deriv : float }

type Dual with // Typeclass members: satisfies SRTP constraints used by konst and diff
    static member Const (c: float) = { Value = c; Deriv = 0.0 }
    static member Seed  (x: float) = { Value = x; Deriv = 1.0 }  // active input: Deriv = 1
    static member Diff  (_ : float, r: Dual) : float = r.Deriv    // extract numeric derivative

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

// DU cases use the E-prefix to avoid naming conflicts with the static members below
// (e.g. union case 'Sin' vs 'static member Sin' would be ambiguous).
type Expr =
    | EConst of float
    | EVar   of string
    | EAdd   of Op2<Expr>
    | ESub   of Op2<Expr>
    | EMul   of Op2<Expr>
    | EDiv   of Op2<Expr>
    | EPow   of Op2<Expr>
    | ENeg   of Op1<Expr>
    | ESin   of Op1<Expr>
    | ECos   of Op1<Expr>
    | ETan   of Op1<Expr>
    | EExp   of Op1<Expr>
    | ELog   of Op1<Expr>

module Symbolic =
    // Symbolic differentiation rules over the Expr tree.
    let rec diff (v: string) =
        function
        | EConst _           -> EConst 0.0
        | EVar x when x = v  -> EConst 1.0
        | EVar _             -> EConst 0.0
        | EAdd(a, b)         -> EAdd(diff v a, diff v b)
        | ESub(a, b)         -> ESub(diff v a, diff v b)
        | EMul(a, b)         -> EAdd(EMul(diff v a, b), EMul(a, diff v b))
        | EDiv(a, b)         -> EDiv(ESub(EMul(diff v a, b), EMul(a, diff v b)), EMul(b, b))
        | EPow(a, b)         -> EMul(EPow(a, b), EAdd(EMul(diff v b, ELog a), EMul(b, EDiv(diff v a, a))))
        | ENeg a             -> ENeg(diff v a)
        | ESin a             -> EMul(ECos a, diff v a)
        | ECos a             -> EMul(ENeg(ESin a), diff v a)
        | ETan a             -> EDiv(diff v a, EMul(ECos a, ECos a))
        | EExp a             -> EMul(EExp a, diff v a)
        | ELog a             -> EDiv(diff v a, a)

type Expr with // Typeclass members: satisfies SRTP constraints used by konst and diff
    static member Const (c: float)              = EConst c
    static member Seed  (name: string)          = EVar name
    static member Diff  (name: string, r: Expr) = Symbolic.diff name r

type Expr with // Basic arithmetic operators
    static member ( + )  (a: Expr, b: Expr) = EAdd(a, b)
    static member ( - )  (a: Expr, b: Expr) = ESub(a, b)
    static member ( * )  (a: Expr, b: Expr) = EMul(a, b)
    static member ( / )  (a: Expr, b: Expr) = EDiv(a, b)
    static member ( ** ) (a: Expr, b: Expr) = EPow(a, b)
    static member ( ~- ) (a: Expr)          = ENeg a

type Expr with // Math functions – named Sin/Cos/... so F# built-ins dispatch here via SRTP
    static member Sin (x: Expr) = ESin x
    static member Cos (x: Expr) = ECos x
    static member Tan (x: Expr) = ETan x
    static member Exp (x: Expr) = EExp x
    static member Log (x: Expr) = ELog x

// ------------------------------------------------------------------------------------------------
// Main
// ------------------------------------------------------------------------------------------------

let PI = System.Math.PI

// f is generic: works for any type satisfying the SRTP constraints.
// 'inline' is required so the compiler can resolve sin, *, konst at each specific call site.
// 'konst' lifts the float (2π) into whichever type ^T is being used.
let inline f x = sin (konst (2.0 * PI) * x)

// Forward mode: ^T=Dual, ^U=float, ^D=float
// Seed sets Deriv=1; the computation propagates it; Diff reads .Deriv off the result.
let dNum : float = diff 1.0 (fun (x: Dual) -> f x)
printfn $"> f(x) = sin(2πx)"
printfn $"> f'(1.0) [Dual]  = {dNum}"

// Symbolic mode: ^T=Expr, ^U=string, ^D=Expr
// Seed creates EVar "x"; the computation builds a tree; Diff applies symbolic rules.
let dSym : Expr = diff "x" (fun (x: Expr) -> f x)
printfn $"> f'(x)  [Expr]  = {dSym}"