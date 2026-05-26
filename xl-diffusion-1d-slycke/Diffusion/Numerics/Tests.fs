module Tests

open System
open Xunit
open Diffusion.Numerics

let testAnalytical
  (x: float)
  (f: Dual -> Dual) 
  (df_analytical: float -> float) =
    let expected = df_analytical x
    let actual   = diff f x
    abs (expected - actual) < 1e-9

let x0 = 2.0

// ------------------------------------------------------------------------------------------------
// Unary
// ------------------------------------------------------------------------------------------------

[<Fact>]
let ``Unary Minus (~-)`` () =
    let status = testAnalytical x0
                    (fun x -> -x) 
                    (fun _ -> -1.0)
    Assert.True(status)

// ------------------------------------------------------------------------------------------------
// Addition
// ------------------------------------------------------------------------------------------------

[<Fact>]
let ``Add Dual-Dual`` () =
    let status = testAnalytical x0
                    (fun x -> x + x)
                    (fun _ -> 2.0)
    Assert.True(status)

[<Fact>]
let ``Add Dual-Float`` () =
    let status = testAnalytical x0
                    (fun x -> x + 3.0)
                    (fun _ -> 1.0)
    Assert.True(status)

[<Fact>]
let ``Add Float-Dual`` () =
    let status = testAnalytical x0
                    (fun x -> 3.0 + x)
                    (fun _ -> 1.0)
    Assert.True(status)

// ------------------------------------------------------------------------------------------------
// Subtraction
// ------------------------------------------------------------------------------------------------

[<Fact>]
let ``Sub Dual-Dual`` () =
    let status = testAnalytical x0
                    (fun x -> x - x)
                    (fun _ -> 0.0)
    Assert.True(status)

[<Fact>]
let ``Sub Dual-Float`` () =
    let status = testAnalytical x0
                    (fun x -> x - 3.0)
                    (fun _ -> 1.0)
    Assert.True(status)

[<Fact>]
let ``Sub Float-Dual`` () =
    let status = testAnalytical x0
                    (fun x -> 3.0 - x)
                    (fun _ -> -1.0)
    Assert.True(status)

// ------------------------------------------------------------------------------------------------
// Multiplication
// ------------------------------------------------------------------------------------------------

[<Fact>]
let ``Mul Dual-Dual`` () =
    let status = testAnalytical x0
                    (fun x -> x * x)
                    (fun x -> 2.0 * x)
    Assert.True(status)

[<Fact>]
let ``Mul Dual-Float`` () =
    let status = testAnalytical x0
                    (fun x -> x * 3.0)
                    (fun _ -> 3.0)
    Assert.True(status)

[<Fact>]
let ``Mul Float-Dual`` () =
    let status = testAnalytical x0
                    (fun x -> 3.0 * x)
                    (fun _ -> 3.0)
    Assert.True(status)

// ------------------------------------------------------------------------------------------------
// Division
// ------------------------------------------------------------------------------------------------

[<Fact>]
let ``Div Dual-Dual`` () =
    let status = testAnalytical x0
                    (fun x -> x / x)
                    (fun _ -> 0.0)
    Assert.True(status)

[<Fact>]
let ``Div Dual-Float`` () =
    let status = testAnalytical x0
                    (fun x -> x / 3.0)
                    (fun _ -> 1.0 / 3.0)
    Assert.True(status)

[<Fact>]
let ``Div Float-Dual`` () =
    let status = testAnalytical x0
                    (fun x -> 3.0 / x)
                    (fun x -> -3.0 / (x * x))
    Assert.True(status)

// ------------------------------------------------------------------------------------------------
// Power
// ------------------------------------------------------------------------------------------------

[<Fact>]
let ``Pow Dual-Dual`` () =
    let status = testAnalytical x0
                    (fun x -> Dual.Pow(x, x))
                    (fun x -> (x ** x) * (log x + 1.0))
    Assert.True(status)

[<Fact>]
let ``Pow Dual-Float`` () =
    let status = testAnalytical x0
                    (fun x -> Dual.Pow(x, 3.0))
                    (fun x -> 3.0 * (x ** 2.0))
    Assert.True(status)

[<Fact>]
let ``Pow Float-Dual`` () =
    let status = testAnalytical x0
                    (fun x -> Dual.Pow(3.0, x))
                    (fun x -> (3.0 ** x) * log 3.0)
    Assert.True(status)

// ------------------------------------------------------------------------------------------------
// Math functions
// ------------------------------------------------------------------------------------------------

[<Fact>]
let ``Sin`` () =
    let status = testAnalytical x0
                    (fun x -> sin x)
                    (fun x -> cos x)
    Assert.True(status)

[<Fact>]
let ``Cos`` () =
    let status = testAnalytical x0
                    (fun x -> cos x)
                    (fun x -> -sin x)
    Assert.True(status)

[<Fact>]
let ``Tan`` () =
    let status = testAnalytical x0
                    (fun x -> tan x)
                    (fun x -> 1.0 / (cos x ** 2.0))
    Assert.True(status)

[<Fact>]
let ``Exp`` () =
    let status = testAnalytical x0
                    (fun x -> exp x)
                    (fun x -> exp x)
    Assert.True(status)

[<Fact>]
let ``Log`` () =
    let status = testAnalytical x0
                    (fun x -> log x)
                    (fun x -> 1.0 / x)
    Assert.True(status)

[<Fact>]
let ``Sqrt`` () =
    let status = testAnalytical x0
                    (fun x -> Dual.Sqrt(x))
                    (fun x -> 0.5 / sqrt x)
    Assert.True(status)

[<Fact>]
let ``Sinh`` () =
    let status = testAnalytical x0
                    (fun x -> sinh x)
                    (fun x -> cosh x)
    Assert.True(status)

[<Fact>]
let ``Cosh`` () =
    let status = testAnalytical x0
                    (fun x -> cosh x)
                    (fun x -> sinh x)
    Assert.True(status)

[<Fact>]
let ``Tanh`` () =
    let status = testAnalytical x0
                    (fun x -> tanh x)
                    (fun x -> 1.0 - (tanh x ** 2.0))
    Assert.True(status)
