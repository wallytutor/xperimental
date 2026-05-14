// ------------------------------------------------------------------------------------------------
// AutoDiff - Forward Mode (Robust & Efficient)
// ------------------------------------------------------------------------------------------------

type Dual = 
    { Value: float; Deriv: float }
    
    // Unary operations
    static member ( ~- ) (a: Dual) = { Value = -a.Value; Deriv = -a.Deriv }

    // Dual-Dual arithmetic
    static member ( + ) (a: Dual, b: Dual) = { Value = a.Value + b.Value; Deriv = a.Deriv + b.Deriv }
    static member ( - ) (a: Dual, b: Dual) = { Value = a.Value - b.Value; Deriv = a.Deriv - b.Deriv }
    static member ( * ) (a: Dual, b: Dual) = { Value = a.Value * b.Value; Deriv = a.Deriv * b.Value + a.Value * b.Deriv }
    static member ( / ) (a: Dual, b: Dual) = { Value = a.Value / b.Value; Deriv = (a.Deriv * b.Value - a.Value * b.Deriv) / (b.Value * b.Value) }
    
    // Dual-Float arithmetic (enables user-friendly formulas without explicit lifting)
    static member ( + ) (a: Dual, b: float) = { Value = a.Value + b; Deriv = a.Deriv }
    static member ( + ) (a: float, b: Dual) = { Value = a + b.Value; Deriv = b.Deriv }
    static member ( - ) (a: Dual, b: float) = { Value = a.Value - b; Deriv = a.Deriv }
    static member ( - ) (a: float, b: Dual) = { Value = a - b.Value; Deriv = -b.Deriv }
    static member ( * ) (a: Dual, b: float) = { Value = a.Value * b; Deriv = a.Deriv * b }
    static member ( * ) (a: float, b: Dual) = { Value = a * b.Value; Deriv = a * b.Deriv }
    static member ( / ) (a: Dual, b: float) = { Value = a.Value / b; Deriv = a.Deriv / b }
    static member ( / ) (a: float, b: Dual) = { Value = a / b.Value; Deriv = (-a * b.Deriv) / (b.Value * b.Value) }

    // Power operations
    static member Pow (a: Dual, b: Dual) =
        let v = a.Value ** b.Value
        let d = v * (b.Deriv * log a.Value + b.Value * a.Deriv / a.Value)
        { Value = v; Deriv = d }
    static member Pow (a: Dual, b: float) =
        { Value = a.Value ** b; Deriv = b * (a.Value ** (b - 1.0)) * a.Deriv }
    static member Pow (a: float, b: Dual) =
        let v = a ** b.Value
        { Value = v; Deriv = v * log a * b.Deriv }

    // Standard math functions (F# inline math functions dispatch here via SRTP automatically)
    static member Sin  (x: Dual) = { Value = sin x.Value; Deriv = cos x.Value * x.Deriv }
    static member Cos  (x: Dual) = { Value = cos x.Value; Deriv = -sin x.Value * x.Deriv }
    static member Tan  (x: Dual) = { Value = tan x.Value; Deriv = x.Deriv / (cos x.Value * cos x.Value) }
    static member Exp  (x: Dual) = let v = exp x.Value in { Value = v; Deriv = v * x.Deriv }
    static member Log  (x: Dual) = { Value = log x.Value; Deriv = x.Deriv / x.Value }
    static member Sqrt (x: Dual) = let v = sqrt x.Value in { Value = v; Deriv = x.Deriv / (2.0 * v) }
    static member Sinh (x: Dual) = { Value = sinh x.Value; Deriv = cosh x.Value * x.Deriv }
    static member Cosh (x: Dual) = { Value = cosh x.Value; Deriv = sinh x.Value * x.Deriv }
    static member Tanh (x: Dual) = 
        let t = tanh x.Value
        { Value = t; Deriv = (1.0 - t * t) * x.Deriv }

// ------------------------------------------------------------------------------------------------
// API
// ------------------------------------------------------------------------------------------------

module AutoDiff =
    /// Lift a scalar into a Dual number (constant, no derivative)
    let constant x = { Value = x; Deriv = 0.0 }
    
    /// Create an active Dual variable (derivative is 1)
    let variable x = { Value = x; Deriv = 1.0 }

    /// Differentiate a function of 1 variable using Forward Mode AutoDiff
    let diff f x =
        let res = f (variable x)
        res.Deriv

// ------------------------------------------------------------------------------------------------
// Main / Tests
// ------------------------------------------------------------------------------------------------

let test name (f: Dual -> Dual) (df_analytical: float -> float) x =
    let expected = df_analytical x
    let actual = AutoDiff.diff f x
    let diff = abs (expected - actual)
    let status = if diff < 1e-9 then "PASS" else "FAIL"
    printfn "[%s] %s" status name
    if status = "FAIL" then
        printfn "       x=%g | expected=%g, actual=%g, diff=%g" x expected actual diff

printfn "=== Running Tests ===\n"

let x0 = 2.0

// Unary
test "Unary Minus (~-)" (fun x -> -x) (fun _ -> -1.0) x0

// Addition
test "Add Dual-Dual"  (fun x -> x + x) (fun _ -> 2.0) x0
test "Add Dual-Float" (fun x -> x + 3.0) (fun _ -> 1.0) x0
test "Add Float-Dual" (fun x -> 3.0 + x) (fun _ -> 1.0) x0

// Subtraction
test "Sub Dual-Dual"  (fun x -> x - x) (fun _ -> 0.0) x0
test "Sub Dual-Float" (fun x -> x - 3.0) (fun _ -> 1.0) x0
test "Sub Float-Dual" (fun x -> 3.0 - x) (fun _ -> -1.0) x0

// Multiplication
test "Mul Dual-Dual"  (fun x -> x * x) (fun x -> 2.0 * x) x0
test "Mul Dual-Float" (fun x -> x * 3.0) (fun _ -> 3.0) x0
test "Mul Float-Dual" (fun x -> 3.0 * x) (fun _ -> 3.0) x0

// Division
test "Div Dual-Dual"  (fun x -> x / x) (fun _ -> 0.0) x0
test "Div Dual-Float" (fun x -> x / 3.0) (fun _ -> 1.0 / 3.0) x0
test "Div Float-Dual" (fun x -> 3.0 / x) (fun x -> -3.0 / (x * x)) x0

// Power
test "Pow Dual-Dual"  (fun x -> x ** x) (fun x -> (x ** x) * (log x + 1.0)) x0
test "Pow Dual-Float" (fun x -> x ** 3.0) (fun x -> 3.0 * (x ** 2.0)) x0
test "Pow Float-Dual" (fun x -> Dual.Pow(3.0, x)) (fun x -> (3.0 ** x) * log 3.0) x0

// Math functions
test "Sin"  (fun x -> sin x) (fun x -> cos x) x0
test "Cos"  (fun x -> cos x) (fun x -> -sin x) x0
test "Tan"  (fun x -> tan x) (fun x -> 1.0 / (cos x ** 2.0)) x0
test "Exp"  (fun x -> exp x) (fun x -> exp x) x0
test "Log"  (fun x -> log x) (fun x -> 1.0 / x) x0
test "Sqrt" (fun x -> sqrt x) (fun x -> 0.5 / sqrt x) x0
test "Sinh" (fun x -> sinh x) (fun x -> cosh x) x0
test "Cosh" (fun x -> cosh x) (fun x -> sinh x) x0
test "Tanh" (fun x -> tanh x) (fun x -> 1.0 - (tanh x ** 2.0)) x0

printfn "\n=== CALPHAD Example ===\n"

// Example relevant to CALPHAD: mixed float and dual, logs and powers
let g (T: Dual) = 10.0 + 2.5 * T - 3.0 * T * log T + 0.5 * T ** 2.0
let dG = AutoDiff.diff g 300.0
let dG_analytical T = 2.5 - 3.0 * (log T + 1.0) + T
printfn $"> G(T)  = 10.0 + 2.5*T - 3.0*T*ln(T) + 0.5*T^2"
printfn $"> G'(300.0) [AutoDiff]   = {dG}"
printfn $"> G'(300.0) [Analytical] = {dG_analytical 300.0}"