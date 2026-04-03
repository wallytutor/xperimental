# %% [markdown]
# # Sample 1

# %% [markdown]
# Generate the equivalent of `model.sif` programmatically using Xl.Elmer.Sif C# library via pythonnet.
#
# Notes on equivalence:
#
# - Parameters are sorted alphabetically within each section (Elmer ignores order).
#
# - Multi-word Elmer keywords are stored as PascalCase (e.g. HeatConductivity).
#
# - Elmer's SIF parser is case-insensitive and normalises spaces, so HeatConductivity and Heat Conductivity are treated identically.
#
# - Array size hints (n) in the original are cosmetic and are omitted.
#
# - MATC script variables and inline $(...) references are reproduced verbatim.
#
# - Comment lines are not generated.

# %%
import subprocess
from pathlib import Path

sol_dir = Path(__file__).parent.parent
subprocess.run(["dotnet", "build", "Xl.Elmer.Sif"], cwd=sol_dir)

# %%
import sys
import os

prj_dir = sol_dir / "Xl.Elmer.Sif"
dll_dir = prj_dir / "bin" / "Debug" / "net9.0"

if not dll_dir.exists():
    raise FileNotFoundError(f"Library not found at {dll_dir}\n")

import pythonnet
runtime_config = sol_dir / "runtime.json"
pythonnet.load("coreclr", runtime_config=runtime_config)

import clr
sys.path.insert(0, str(dll_dir))
clr.AddReference("Xl.Elmer.Sif")

from Xl.Elmer.Sif import (
    SifDocument,
    LinearSystemControl,
    NonlinearSystemControl,
    SolverExecution,
    SifValue,
)

# %%
sif = SifDocument()

# %% [markdown]
# - Header: Mesh DB, Include Path, and Results Directory use special SIF directives (no `=`).

# %%
sif.Header.CheckKeywords    = "Warn"
sif.Header.MeshDbDirectory  = "elmer"
sif.Header.MeshDbName       = "."
sif.Header.IncludePath      = "."
sif.Header.ResultsDirectory = "results"

# %% [markdown]
# - MATC script variables - written as `$ name = expr;` before being used.

# %%
sif.AddMatcScript("OVERHEAT = 20.0;")
sif.AddMatcScript("T_MELT = 1900.00 + OVERHEAT + 273.15;")
sif.AddMatcScript("T_INGOT = 20.0 + 273.15;")
sif.AddMatcScript("T_RAD = 20.0 + 273.15;")
sif.AddMatcScript("T_CONV = 20.0 + 273.15;")
sif.AddMatcScript("HTC = 8.0;")
sif.AddMatcScript("BDF_ORDER = 1;")
sif.AddMatcScript("MAX_ITER = 500;")
sif.AddMatcScript("NONLINEAR_TOL = 1.0e-06;")
sif.AddMatcScript("RELAXATION = 0.3;")
sif.AddMatcScript("K_EQUIV = 0.02;")

# %% [markdown]
# - Simulation

# %%
sif.Simulation.MaxOutputLevel = 5
sif.Simulation.SolverInputFile = "case.sif"
sif.Simulation.CoordinateSystem = "Cartesian 2D"
sif.Simulation.CoordinateMapping = [1, 2, 3]
sif.Simulation.ConvergenceMonitor = True
sif.Simulation.SimulationType = "Transient"
sif.Simulation.TimesteppingMethod = "BDF"

# MATC reference — written raw (no quotes)
sif.Simulation.BdfOrder = "$(BDF_ORDER)"
sif.Simulation.TimestepSizes = [1e-6, 1e-5, 1e-4]
sif.Simulation.TimestepIntervals = [10, 9, 9]
sif.Simulation.OutputIntervals = [10, 9, 9]
sif.Simulation.OutputFile = "init.result"

# indexed parameter
sif.Simulation.SetParameter("Output Variable 1", "Temperature")
sif.Simulation.BinaryOutput = True

# %% [markdown]
# - Constants: uses default values if none are supplied

# %%
sif.Constants.Gravity = [0.0, -1.0, 0.0, 9.82]
sif.Constants.StefanBoltzmann = 5.670374419e-08
sif.Constants.PermittivityOfVacuum = 8.85418781e-12
sif.Constants.PermeabilityOfVacuum = 1.25663706e-6
sif.Constants.BoltzmannConstant = 1.380649e-23
sif.Constants.UnitCharge = 1.6021766e-19

# %% [markdown]
# - Materials

# %%
vars_temperature = ["Temperature"]

molten = sif.AddMaterial("Molten")
molten.SetEmissivityConstant(0.9)
molten.SetPoissonRatioConstant(0.25)
molten.SetDensityFile("data/rho.dat", vars_temperature)
molten.SetHeatConductivityFile("data/k.dat", vars_temperature)
molten.SetHeatCapacityFile("data/cp.dat", vars_temperature)
molten.SetHeatExpansionCoefficientFile("data/alpha.dat", vars_temperature)
molten.SetInternalEnergyFile("data/h.dat", vars_temperature)

youngs_temperature = [298.15, 1873.15, 1900.0, 3000.0]
youngs_value = [417.0e9, 417.0e9, 100.0e9, 100.0e9]
youngs_data = [list(row) for row in zip(youngs_temperature, youngs_value)]

# "" → emits just "Real" (no interpolation qualifier)
molten.SetYoungsModulusTabular(youngs_data, vars_temperature, interpolation="")
molten.SetReferenceTemperatureRaw("$(T_MELT)")

# %%
print(sif.Serialize())
