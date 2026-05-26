[<AutoOpen>]
module Diffusion.Numerics.Autodiff

type Dual =
    {
        Value: float
        Deriv: float
    }

    static member Zero = { Value = 0.0; Deriv = 0.0 }
    static member One  = { Value = 1.0; Deriv = 0.0 }

    // Unary operations
    static member ( ~- ) (a: Dual) =
        {
            Value = -a.Value
            Deriv = -a.Deriv
        }

    // Dual-Dual arithmetic
    static member ( + ) (a: Dual, b: Dual) =
        {
            Value = a.Value + b.Value
            Deriv = a.Deriv + b.Deriv
        }
    static member ( - ) (a: Dual, b: Dual) =
        {
            Value = a.Value - b.Value
            Deriv = a.Deriv - b.Deriv
        }
    static member ( * ) (a: Dual, b: Dual) =
        {
            Value = a.Value * b.Value
            Deriv = a.Deriv * b.Value + a.Value * b.Deriv
        }
    static member ( / ) (a: Dual, b: Dual) =
        {
            Value = a.Value / b.Value
            Deriv = (a.Deriv * b.Value - a.Value * b.Deriv) / (b.Value * b.Value)
        }

    // Dual-Float arithmetic
    static member ( + ) (a: Dual, b: float) =
        {
            Value = a.Value + b
            Deriv = a.Deriv
        }
    static member ( + ) (a: float, b: Dual) =
        {
            Value = a + b.Value
            Deriv = b.Deriv
        }
    static member ( - ) (a: Dual, b: float) =
        {
            Value = a.Value - b
            Deriv = a.Deriv
        }
    static member ( - ) (a: float, b: Dual) =
        {
            Value = a - b.Value
            Deriv = -b.Deriv
        }
    static member ( * ) (a: Dual, b: float) =
        {
            Value = a.Value * b
            Deriv = a.Deriv * b
        }
    static member ( * ) (a: float, b: Dual) =
        {
            Value = a * b.Value
            Deriv = a * b.Deriv
        }
    static member ( / ) (a: Dual, b: float) =
        {
            Value = a.Value / b
            Deriv = a.Deriv / b
        }
    static member ( / ) (a: float, b: Dual) =
        {
            Value = a / b.Value
            Deriv = (-a * b.Deriv) / (b.Value * b.Value)
        }

    // Power operations
    static member Pow (a: Dual, b: Dual) =
        let v = a.Value ** b.Value
        let d = v * (b.Deriv * log a.Value + b.Value * a.Deriv / a.Value)
        {
            Value = v
            Deriv = d
        }
    static member Pow (a: Dual, b: float) =
        {
            Value = a.Value ** b
            Deriv = b * (a.Value ** (b - 1.0)) * a.Deriv
        }
    static member Pow (a: float, b: Dual) =
        let v = a ** b.Value
        {
            Value = v
            Deriv = v * log a * b.Deriv
        }

    // Standard math functions
    static member Sin  (x: Dual) =
        {
            Value = sin x.Value
            Deriv = cos x.Value * x.Deriv
        }
    static member Cos  (x: Dual) =
        {
            Value = cos x.Value
            Deriv = -sin x.Value * x.Deriv
        }
    static member Tan  (x: Dual) =
        {
            Value = tan x.Value
            Deriv = x.Deriv / (cos x.Value * cos x.Value)
        }
    static member Exp  (x: Dual) =
        let v = exp x.Value
        {
            Value = v
            Deriv = v * x.Deriv
        }
    static member Log  (x: Dual) =
        {
            Value = log x.Value
            Deriv = x.Deriv / x.Value
        }
    static member Sqrt (x: Dual) =
        let v = sqrt x.Value
        {
            Value = v
            Deriv = x.Deriv / (2.0 * v)
        }
    static member Sinh (x: Dual) =
        {
            Value = sinh x.Value
            Deriv = cosh x.Value * x.Deriv
        }
    static member Cosh (x: Dual) =
        {
            Value = cosh x.Value
            Deriv = sinh x.Value * x.Deriv
        }
    static member Tanh (x: Dual) =
        let t = tanh x.Value
        {
            Value = t
            Deriv = (1.0 - t * t) * x.Deriv
        }

/// Lift a scalar into a Dual number (constant, no derivative)
let constant x =
    {
        Value = x
        Deriv = 0.0
    }

/// Create an active Dual variable (derivative is 1)
let variable x =
    {
        Value = x
        Deriv = 1.0
    }

/// Differentiate a function of 1 variable using Forward Mode AutoDiff
let diff f x =
    let res = f (variable x)
    res.Deriv
