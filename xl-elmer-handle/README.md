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
- Consistent PascalCase key serialization
- Programmatic section creation with automatic numeric identifiers
- Save-to-disk support

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

var material = sif.AddMaterial();
material.Name = "Steel";
material.Density = 7800.0;
material.HeatConductivity = 43.0;
material.HeatCapacity = 470.0;

var equation = sif.AddEquation();
equation.Name = "HeatEquation";

equation.ActiveSolvers = new[] { 1 };

var solver = sif.AddSolver();
solver.Equation = "Heat Equation";
solver.Procedure = "HeatSolve";
solver.Variable = "Temperature";
solver.ExecSolver = 1;

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
```
