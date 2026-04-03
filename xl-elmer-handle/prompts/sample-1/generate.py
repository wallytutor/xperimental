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
import sys
import os
from pathlib import Path

sol_dir = Path(__file__).parent.parent
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
    MaterialPropertyValue,
    TabulatedMaterialPoint,
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

# %%
print(sif.Serialize())

# %%
