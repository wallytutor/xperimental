# ── Material 2: Dummy (gap) ───────────────────────────────────────────────────
dummy = sif.AddMaterial("Dummy")
dummy.Name = "Dummy"
dummy.Density = MaterialPropertyValue.IncludeFile("Temperature", "data/rho.dat")
dummy.Emissivity = 0.90
dummy.HeatConductivity = MaterialPropertyValue.Raw("$(K_EQUIV)")  # MATC scalar
dummy.HeatCapacity = MaterialPropertyValue.IncludeFile("Temperature", "data/cp.dat")
dummy.YoungsModulus = MaterialPropertyValue.Tabular(
    "Temperature",
    [
        TabulatedMaterialPoint(298.15, 417.0e9),
        TabulatedMaterialPoint(1873.15, 417.0e9),
        TabulatedMaterialPoint(1900.0, 100.0e9),
        TabulatedMaterialPoint(3000.0, 100.0e9),
    ],
    interpolation="",
)
dummy.PoissonRatio = 0.25
dummy.HeatExpansionCoefficient = MaterialPropertyValue.IncludeFile(
    "Temperature", "data/alpha.dat"
)
dummy.ReferenceTemperature = MaterialPropertyValue.Raw("$(T_MELT)")
dummy.InternalEnergy = MaterialPropertyValue.IncludeFile(
    "Temperature", "data/h.dat"
)

# ── Material 3: Metal (mould) ─────────────────────────────────────────────────
metal = sif.AddMaterial("Metal")
metal.Name = "Metal"
metal.Density = 7800  # implicit double → MaterialPropertyValue.Constant
metal.Emissivity = 0.6
metal.HeatConductivity = 17.0
metal.HeatCapacity = 500
metal.YoungsModulus = 207.0e9
metal.PoissonRatio = 0.30
metal.HeatExpansionCoefficient = 0.0
metal.ReferenceTemperature = MaterialPropertyValue.Raw("$(T_INGOT)")
# InternalEnergy: tabular with inline MATC expressions — must use Raw.
metal.InternalEnergy = MaterialPropertyValue.Raw(
    "Variable Temperature\nReal\n"
    "   200.0  $(7800 * 500.0 * ( 200.0 - 298.15))\n"
    "  3000.0  $(7800 * 500.0 * (3000.0 - 298.15))\n"
    "End"
)

# ────────────────────────────────────────────────────────────────────────────
# Initial Conditions
# ────────────────────────────────────────────────────────────────────────────
print("Creating Initial Conditions...")
ic1 = sif.AddInitialCondition()
ic1.SetParameter("Name", "Temperature")
ic1.SetParameter("Temperature", SifValue.Raw("$(T_MELT)"))

ic2 = sif.AddInitialCondition()
ic2.SetParameter("Name", "Temperature")
ic2.SetParameter("Temperature", SifValue.Raw("$(T_INGOT)"))

# ────────────────────────────────────────────────────────────────────────────
# Bodies
# ────────────────────────────────────────────────────────────────────────────
print("Creating Bodies...")

body1 = sif.AddBody()
body1.Name = "molten"
body1.Material = molten.Id
body1.TargetBodies = [1]
body1.InitialCondition = ic1.Id
body1.Equation = 1
body1.SetParameter("BodyMolten", SifValue.Raw("Logical True"))

body2 = sif.AddBody()
body2.Name = "gap"
body2.Material = dummy.Id
body2.TargetBodies = [2]
body2.InitialCondition = ic2.Id
body2.Equation = 1
body2.SetParameter("BodyGap", SifValue.Raw("Logical True"))

body3 = sif.AddBody()
body3.Name = "mould"
body3.Material = metal.Id
body3.TargetBodies = [3]
body3.InitialCondition = ic2.Id
body3.Equation = 1
body3.SetParameter("BodyMould", SifValue.Raw("Logical True"))

# ────────────────────────────────────────────────────────────────────────────
# Solvers
# ────────────────────────────────────────────────────────────────────────────
print("Configuring Solvers...")

# ── Solver 1: HeatSolver ──────────────────────────────────────────────────────
heat = sif.AddHeatSolver()
heat.ExecuteWhen = SolverExecution.Always
heat.Stabilize = True
heat.OptimizeBandwidth = True
heat.LinearSystem = LinearSystemControl()
heat.LinearSystem.Solver = "Iterative"
heat.LinearSystem.IterativeMethod = "BiCGStab"
heat.LinearSystem.MaxIterations = 500
heat.LinearSystem.ConvergenceTolerance = 1.0e-10
heat.LinearSystem.Preconditioning = "ILU0"
heat.LinearSystem.IlutTolerance = 1.0e-3
heat.LinearSystem.AbortNotConverged = True
heat.LinearSystem.ResidualOutput = 10
heat.LinearSystem.PreconditionRecompute = 1
heat.LinearSystem.BiCGstablPolynomialDegree = 2

# Nonlinear settings that reference MATC variables are set directly as raw values.
heat.NonlinearSystem = NonlinearSystemControl()
heat.NonlinearSystem.NewtonAfterIterations = 3
heat.SetParameter(
    "Nonlinear System Convergence Tolerance", SifValue.Raw("$(NONLINEAR_TOL)")
)
heat.SetParameter("Nonlinear System Max Iterations", SifValue.Raw("$(MAX_ITER)"))
heat.SetParameter(
    "Nonlinear System Newton After Tolerance",
    SifValue.Raw("$(100 * NONLINEAR_TOL)"),
)
heat.SetParameter(
    "Nonlinear System Relaxation Factor", SifValue.Raw("$(RELAXATION)")
)
heat.CalculateLoads = True

# ── Solver 2: SaveMaterials ───────────────────────────────────────────────────
save_materials = sif.AddSaveMaterialsSolver()
save_materials.ExecuteWhen = SolverExecution.AfterTimestep
save_materials.SetParameter(1, "Density")
save_materials.SetParameter(2, "Heat Capacity")
save_materials.SetParameter(3, "Heat Conductivity")
save_materials.SetParameter(4, "Heat Expansion Coefficient")
save_materials.SetParameter(5, "Internal Energy")

# ── Solver 3: ResultOutput ────────────────────────────────────────────────────
# No dedicated specialisation — generic SolverSection is sufficient.
result_output = sif.AddSolver()
result_output.Equation = "ResultOutput"
result_output.ConfigureProcedure("ResultOutputSolve", "ResultOutputSolver")
result_output.ExecuteWhen = SolverExecution.BeforeSaving
result_output.SetParameter("Output File Name", "case")
result_output.SetParameter("Output Format", "vtu")
result_output.SetParameter("Binary Output", True)
result_output.SetParameter("Single Precision", False)
result_output.SetParameter("Save Geometry Ids", True)
result_output.SetParameter("Vtu Time Collection", True)

# ── Solvers 4–7: SaveLine (one per boundary mask) ──────────────────────────
save_lines = [
    ("SaveLine 1", "line-side", "SideMask"),
    ("SaveLine 2", "line-top", "TopMask"),
    ("SaveLine 3", "line-bottom", "BottomMask"),
    ("SaveLine 4", "line-middle", "MiddleMask"),
]
for eq_name, filename, mask in save_lines:
    sl = sif.AddSaveLineSolver()
    sl.Equation = eq_name  # override default "SaveLine"
    sl.ExecuteWhen = SolverExecution.AfterTimestep
    sl.ParallelReduce = True
    sl.SetTrackedVariable(1, "Temperature")
    sl.Filename = filename
    sl.SaveMask = mask

# ── Solver 8: SaveScalars ────────────────────────────────────────────────────
scalars = sif.AddSaveScalarsSolver()
scalars.ExecuteWhen = SolverExecution.AfterTimestep
scalars.Filename = "scalars.dat"
scalars.FileAppend = False
scalars.PartitionNumbering = True
scalars.ParallelReduce = True

scalars.SetTrackedVariable(1, "Time")
scalars.SetTrackedVariable(2, "temperature", operatorName="nonlin converged")
scalars.SetTrackedVariable(
    3, "temperature loads", maskName="ExternalMask", operatorName="boundary sum"
)
scalars.SetTrackedVariable(4, "temperature", maskName="BodyMolten", operatorName="body mean")
scalars.SetTrackedVariable(5, "temperature", maskName="BodyMould", operatorName="body mean")
scalars.SetTrackedVariable(
    6, "internal energy", maskName="BodyMolten", operatorName="body int"
)
scalars.SetTrackedVariable(
    7, "internal energy", maskName="BodyMould", operatorName="body int"
)
scalars.SetTrackedVariable(
    8, "temperature", maskName="BodyMolten", operatorName="body volume"
)
scalars.SetTrackedVariable(9, "density", maskName="BodyMolten", operatorName="body int")
scalars.SetTrackedVariable(10, "density", maskName="BodyMolten", operatorName="body mean")

# ────────────────────────────────────────────────────────────────────────────
# Equation
# ────────────────────────────────────────────────────────────────────────────
print("Creating Equation...")
equation = sif.AddEquation()
equation.Name = "System"
equation.ActiveSolvers = list(range(1, len(sif.Solvers) + 1))

# ────────────────────────────────────────────────────────────────────────────
# Boundary Conditions
# ────────────────────────────────────────────────────────────────────────────
print("Creating Boundary Conditions...")

# ── Internal interfaces ───────────────────────────────────────────────────────
bc1 = sif.AddBoundaryCondition()
bc1.Name = "i_molten_gap"
bc1.TargetBoundaries = [1]
bc1.SetParameter("Save Scalars", SifValue.Raw("True"))
bc1.SetParameter("InnerMask", SifValue.Raw("Logical True"))

bc2 = sif.AddBoundaryCondition()
bc2.Name = "i_gap_mould"
bc2.TargetBoundaries = [2]
bc2.SetParameter("Save Scalars", SifValue.Raw("True"))
bc2.SetParameter("InnerMask", SifValue.Raw("Logical True"))

# ── MiddleMask boundaries (symmetry plane) ────────────────────────────────────
middles = [
    ("l_molten", 3, True, False),
    ("l_gap", 4, False, False),
    ("l_mould", 5, True, True),
]
for name, target, fix_x, fix_y in middles:
    bc = sif.AddBoundaryCondition()
    bc.Name = name
    bc.TargetBoundaries = [target]
    bc.HeatFlux = 0
    if fix_x:
        bc.SetParameter("Displacement 1", 0.0)
    if fix_y:
        bc.SetParameter("Displacement 2", 0.0)
    bc.SetParameter("Save Scalars", SifValue.Raw("True"))
    bc.SetParameter("Save Line", SifValue.Raw("True"))
    bc.SetParameter("MiddleMask", SifValue.Raw("Logical True"))

# ── TopMask boundaries ────────────────────────────────────────────────────────
for name, target in [("t_molten", 6), ("t_gap", 7), ("t_mould", 8)]:
    bc = sif.AddBoundaryCondition()
    bc.Name = name
    bc.TargetBoundaries = [target]
    bc.HeatFlux = 0
    bc.SetParameter("Displacement 1", 0.0)
    bc.SetParameter("Displacement 2", 0.0)
    bc.SetParameter("Save Scalars", SifValue.Raw("True"))
    bc.SetParameter("Save Line", SifValue.Raw("True"))
    bc.SetParameter("TopMask", SifValue.Raw("Logical True"))

# ── External mould face — convection + idealised radiation ───────────────────
ext = sif.AddBoundaryCondition()
ext.Name = "e_mould"
ext.TargetBoundaries = [9]
ext.SetParameter("Radiation External Temperature", SifValue.Raw("$(T_RAD)"))
ext.SetParameter("External Temperature", SifValue.Raw("$(T_CONV)"))
ext.SetParameter("Heat Transfer Coefficient", SifValue.Raw("$(HTC)"))
ext.SetParameter("Radiation", "Idealized")
ext.SetParameter("Displacement 1", 0.0)
ext.SetParameter("Displacement 2", 0.0)
ext.SetParameter("Save Scalars", SifValue.Raw("True"))
ext.SetParameter("Save Line", SifValue.Raw("True"))
ext.SetParameter("ExternalMask", SifValue.Raw("Logical True"))

# ────────────────────────────────────────────────────────────────────────────
# Serialize and save
# ────────────────────────────────────────────────────────────────────────────
print("\nGenerating SIF file...")
sif_text = sif.Serialize()
print(sif_text)

output_path = Path(__file__).parent / "model-generated.sif"
print(f"\nSaving to: {output_path}")
sif.Save(str(output_path))
print("✓ Done!")
