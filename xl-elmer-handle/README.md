# Xl.Elmer.Sif

A lightweight C# (`net10.0`) library for building Elmer `.sif` files programmatically.

## Features

- Section classes for:
  - `Header`
  - `Simulation`
  - `Constants`
  - `Material <n>`
  - `Body <n>`
  - `Solver <n>`
  - `Equation <n>`
  - `Initial Condition <n>`
  - `Boundary Condition <n>`
- Specialized solver types for `HeatSolve`, `SaveLine`, and `SaveData`
- Expression-capable material properties with constant, `MATC`, `Lua`, user-library, and tabular forms
- Linear and nonlinear solver control objects injected into solver bodies
- Validation before serialization for specialized solver requirements
- `ElmerGrid` process helpers for conversion and partitioning
- Consistent PascalCase key serialization
- Programmatic section creation with automatic numeric identifiers
- Save-to-disk support

## Installation

```bash
dotnet build Xl.Elmer.Sif
```

## Quick Example

```csharp
using Xl.Elmer.Sif;

var sif = new SifDocument();

sif.Header.CheckKeywords = "Warn";
sif.Header.MeshDbDirectory = "./mesh";
sif.Header.MeshDbName = "case";

sif.Simulation.SimulationType = "Transient";
sif.Simulation.MaxOutputLevel = 5;
sif.Simulation.TimestepSizes = new[] { 0.1, 0.5, 1.0 };
sif.Simulation.TimestepIntervals = 10;

var material = sif.AddMaterial("Steel");
material.SetDensityConstant(7800.0);
material.SetHeatConductivityMatc("Temperature", "16.2 + 0.01*tx");
material.SetHeatCapacityTabular(
  "Temperature",
  [
    new TabulatedMaterialPoint(293.15, 470.0),
    new TabulatedMaterialPoint(473.15, 510.0)
  ]);

var equation = sif.AddEquation();
equation.Name = "HeatEquation";

equation.ActiveSolvers = new[] { 1 };

var solver = sif.AddHeatSolver();
solver.ExecuteWhen = SolverExecution.Always;
solver.LinearSystem = new LinearSystemControl
{
  Solver = "Iterative",
  IterativeMethod = "BiCGStab",
  Preconditioning = "ILU0",
  MaxIterations = 500,
  ConvergenceTolerance = 1.0e-10
};

var body = sif.AddBody();
body.Name = "Workpiece";
body.TargetBodies = new[] { 1 };
body.Material = material.Id;
body.Equation = equation.Id;

var initial = sif.AddInitialCondition();
initial.Temperature = 293.15;

body.InitialCondition = initial.Id;

var boundary = sif.AddBoundaryCondition();
boundary.Name = "ConvectionFace";
boundary.TargetBoundaries = new[] { 2 };
boundary.Temperature = 293.15;

sif.Save("case.sif");

var project = new ElmerGridProject(Environment.CurrentDirectory);
await project.ConvertAsync(ElmerGridFormat.Gmsh, ElmerGridFormat.Elmer, "geometry.msh", "mesh");
```
