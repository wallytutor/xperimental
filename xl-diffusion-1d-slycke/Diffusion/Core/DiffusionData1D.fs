module Diffusion.Core.DiffusionData1D

open Diffusion.Numerics
open NumericalUtilities
open FunctionTypes
open FvmDomain1D

type DiffusionField1D =
    { Solver: ITridiagonalSolver
      Diffusivity: float array
      Concentration: float array
      // MoleFraction: float array --> TODO
      FaceDiffusivity: float array
      mutable SurfaceFlux: float }

    static member Create (numPoints: int) =
        { Solver = TridiagonalSolverNonDestroying.Create numPoints
          Diffusivity = Array.zeroCreate numPoints
          Concentration = Array.zeroCreate numPoints
          FaceDiffusivity = Array.zeroCreate (numPoints - 1)
          SurfaceFlux = 0.0 }

    static member FromConcentration (concentration: float array) =
        let numPoints = concentration.Length

        { Solver = TridiagonalSolverNonDestroying.Create numPoints
          Diffusivity = Array.zeroCreate numPoints
          Concentration = Array.copy concentration
          FaceDiffusivity = Array.zeroCreate (numPoints - 1)
          SurfaceFlux = 0.0 }

// TODO this is not general enough for all diffusion problems, but specialized
// for carbonitriding. Consider refactoring to eliminate the elements!
// TODO store the domain here instead of coordinates
type IDiffusion1DSolverInit =
    abstract member Grid: ImmersedNodeDomain1D

    // abstract member Numerics: NumericalOptions ---> TODO
    abstract member Relaxation: float
    abstract member MaxNonlinIter: int
    abstract member RelativeTolerance: float
    abstract member AbsoluteTolerance: float

    abstract member TimePoints: float array
    abstract member YcField: float array
    abstract member YnField: float array
    abstract member YInf: float -> float array
    abstract member HInf: float -> float array
    abstract member Temperature: float -> float

type NumericalOptions =
    { Relaxation: float
      MaxNonlinIter: int
      RelativeTolerance: float
      AbsoluteTolerance: float }

    static member Default =
        { Relaxation        = 0.75
          MaxNonlinIter     = 50
          RelativeTolerance = 1e-06
          AbsoluteTolerance = 1e-10 }

let simulationSetup
  (grid: ImmersedNodeDomain1D)
  (totalTime: float)
  (timeStep: float)
  (ycField: CellField)
  (ynField: CellField)
  (yInf: TimeValue<float array>)
  (hInf: TimeValue<float array>)
  (temperature: TimeValue<float>)
  (numericalOptions: NumericalOptions option) =
    let options = defaultArg numericalOptions NumericalOptions.Default

    let cellCount = grid.Interior.Length
    let ycField = CellField.expand cellCount ycField
    let ynField = CellField.expand cellCount ynField

    let yInf        = TimeValue.ToFunc yInf
    let hInf        = TimeValue.ToFunc hInf
    let temperature = TimeValue.ToFunc temperature

    if cellCount <> ycField.Length || cellCount <> ynField.Length then
        failwith "Length of cellCenters must match length of ycField and ynField."

    let timePoints = arangeInclusive 0.0 totalTime timeStep

    { new IDiffusion1DSolverInit with
        member _.Grid              = grid
        member _.Relaxation        = options.Relaxation
        member _.MaxNonlinIter     = options.MaxNonlinIter
        member _.AbsoluteTolerance = options.AbsoluteTolerance
        member _.RelativeTolerance = options.RelativeTolerance
        member _.TimePoints        = timePoints
        member _.YcField           = ycField
        member _.YnField           = ynField
        member _.YInf (t: float) : float array = yInf t
        member _.HInf (t: float) : float array = hInf t
        member _.Temperature (t: float) : float = temperature t }

let fourierNumberDeltaSq (D: float) (t: float) (l2: float) : float = D * t / l2

let fourierNumber (D: float) (t: float) (l1: float) : float = D * t / (l1 * l1)

let sherwoodNumber (h: float) (L: float) (D: float) : float = h * L / D