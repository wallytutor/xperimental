// namespace Diffusion.Numerics (or should I get serious and wrap in namespace?)
[<AutoOpen>]
module Diffusion.Numerics.TridiagonalSolver

let private tdmaNonDestroyingWithMemory
  (a: float array)
  (b: float array)
  (c: float array)
  (d: float array)
  (cPrime: float array)
  (dPrime: float array)
  (x: float array)
  (n: int) : float array =
    cPrime.[0] <- c.[0] / b.[0]
    dPrime.[0] <- d.[0] / b.[0]

    for i in 1 .. n - 1 do
        let m = b.[i] - a.[i] * cPrime.[i - 1]
        cPrime.[i] <- c.[i] / m
        dPrime.[i] <- (d.[i] - a.[i] * dPrime.[i - 1]) / m

    x.[n - 1] <- dPrime.[n - 1]

    for i in n - 2 .. -1 .. 0 do
        x.[i] <- dPrime.[i] - cPrime.[i] * x.[i + 1]

    x

let private tdmaDestroyingWithMemory
  (a: float array)
  (b: float array)
  (c: float array)
  (d: float array)
  (x: float array)
  (n: int) : float array =
    c.[0] <- c.[0] / b.[0]
    d.[0] <- d.[0] / b.[0]

    for i in 1 .. n - 1 do
        let m = b.[i] - a.[i] * c.[i - 1]
        c.[i] <- c.[i] / m
        d.[i] <- (d.[i] - a.[i] * d.[i - 1]) / m

    x.[n - 1] <- d.[n - 1]

    for i in n - 2 .. -1 .. 0 do
        x.[i] <- d.[i] - c.[i] * x.[i + 1]

    x

type TridiagonalMatrix =
    { a: float array
      b: float array
      c: float array }

    static member Create (n: int) =
        { a = Array.zeroCreate n
          b = Array.zeroCreate n
          c = Array.zeroCreate n }

type TridiagonalProblem =
    { Matrix: TridiagonalMatrix
      d: float array
      n: int }

    static member Create (n: int) =
        { Matrix = TridiagonalMatrix.Create n
          d = Array.zeroCreate n
          n = n }

type TridiagonalMemory =
    { cPrime: float array
      dPrime: float array
      x: float array }

    static member Create (n: int) =
        { cPrime = Array.zeroCreate n
          dPrime = Array.zeroCreate n
          x = Array.zeroCreate n }

type ITridiagonalSolver =
    abstract member Problem : TridiagonalProblem
    abstract member Memory : TridiagonalMemory
    abstract member Solve : unit -> float array

type TridiagonalSolverDestroying =
    { Problem: TridiagonalProblem
      Memory: TridiagonalMemory }

    static member Create (n: int) =
        { Problem = TridiagonalProblem.Create n
          Memory = TridiagonalMemory.Create n }

    interface ITridiagonalSolver with
        member self.Problem = self.Problem
        member self.Memory = self.Memory
        member self.Solve () : float array =
            let p = self.Problem

            tdmaDestroyingWithMemory
                p.Matrix.a
                p.Matrix.b
                p.Matrix.c
                p.d
                self.Memory.x
                p.n

type TridiagonalSolverNonDestroying =
    { Problem: TridiagonalProblem
      Memory: TridiagonalMemory }

    static member Create (n: int) =
        { Problem = TridiagonalProblem.Create n
          Memory = TridiagonalMemory.Create n }

    interface ITridiagonalSolver with
        member self.Problem = self.Problem
        member self.Memory = self.Memory
        member self.Solve () : float array =
            let p = self.Problem

            tdmaNonDestroyingWithMemory
                p.Matrix.a
                p.Matrix.b
                p.Matrix.c
                p.d
                self.Memory.cPrime
                self.Memory.dPrime
                self.Memory.x
                p.n
