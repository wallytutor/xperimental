namespace Xl.Elmer.Sif;

public sealed class HeaderSection : SifSection
{
    protected override string SectionHeader => "Header";

    public string? CheckKeywords
    {
        set => SetParameter(nameof(CheckKeywords), value);
    }

    public string? MeshDbDirectory
    {
        set => SetParameter(nameof(MeshDbDirectory), value);
    }

    public string? MeshDbName
    {
        set => SetParameter(nameof(MeshDbName), value);
    }

    public string? IncludePath
    {
        set => SetParameter(nameof(IncludePath), value);
    }
}

public sealed class SimulationSection : SifSection
{
    protected override string SectionHeader => "Simulation";

    public string? CoordinateSystem
    {
        set => SetParameter(nameof(CoordinateSystem), value);
    }

    public string? SimulationType
    {
        set => SetParameter(nameof(SimulationType), value);
    }

    public int? MaxOutputLevel
    {
        set => SetParameter(nameof(MaxOutputLevel), value);
    }

    public string? TimesteppingMethod
    {
        set => SetParameter(nameof(TimesteppingMethod), value);
    }

    public IEnumerable<double>? TimestepSizes
    {
        set => SetParameter(nameof(TimestepSizes), value ?? Array.Empty<double>());
    }

    public int? TimestepIntervals
    {
        set => SetParameter(nameof(TimestepIntervals), value);
    }

    public string? OutputIntervals
    {
        set => SetParameter(nameof(OutputIntervals), value);
    }
}

public sealed class ConstantsSection : SifSection
{
    protected override string SectionHeader => "Constants";

    public double? Gravity
    {
        set => SetParameter(nameof(Gravity), value);
    }

    public double? StefanBoltzmann
    {
        set => SetParameter(nameof(StefanBoltzmann), value);
    }

    public double? PermittivityOfVacuum
    {
        set => SetParameter(nameof(PermittivityOfVacuum), value);
    }
}

public sealed class MaterialSection : IndexedSifSection
{
    public MaterialSection(int id) : base(id)
    {
    }

    protected override string SectionHeader => $"Material {Id}";

    public string? Name
    {
        set => SetParameter(nameof(Name), value);
    }

    public double? Density
    {
        set => SetParameter(nameof(Density), value);
    }

    public double? HeatConductivity
    {
        set => SetParameter(nameof(HeatConductivity), value);
    }

    public double? HeatCapacity
    {
        set => SetParameter(nameof(HeatCapacity), value);
    }
}

public sealed class BodySection : IndexedSifSection
{
    public BodySection(int id) : base(id)
    {
    }

    protected override string SectionHeader => $"Body {Id}";

    public string? Name
    {
        set => SetParameter(nameof(Name), value);
    }

    public IEnumerable<int>? TargetBodies
    {
        set => SetParameter(nameof(TargetBodies), value ?? Array.Empty<int>());
    }

    public int? Equation
    {
        set => SetParameter(nameof(Equation), value);
    }

    public int? Material
    {
        set => SetParameter(nameof(Material), value);
    }

    public int? BodyForce
    {
        set => SetParameter(nameof(BodyForce), value);
    }

    public int? InitialCondition
    {
        set => SetParameter(nameof(InitialCondition), value);
    }
}

public sealed class SolverSection : IndexedSifSection
{
    public SolverSection(int id) : base(id)
    {
    }

    protected override string SectionHeader => $"Solver {Id}";

    public string? Equation
    {
        set => SetParameter(nameof(Equation), value);
    }

    public string? Procedure
    {
        set => SetParameter(nameof(Procedure), value);
    }

    public string? Variable
    {
        set => SetParameter(nameof(Variable), value);
    }

    public int? ExecSolver
    {
        set => SetParameter(nameof(ExecSolver), value);
    }
}

public sealed class EquationSection : IndexedSifSection
{
    public EquationSection(int id) : base(id)
    {
    }

    protected override string SectionHeader => $"Equation {Id}";

    public string? Name
    {
        set => SetParameter(nameof(Name), value);
    }

    public IEnumerable<int>? ActiveSolvers
    {
        set => SetParameter(nameof(ActiveSolvers), value ?? Array.Empty<int>());
    }
}

public sealed class InitialConditionSection : IndexedSifSection
{
    public InitialConditionSection(int id) : base(id)
    {
    }

    protected override string SectionHeader => $"Initial Condition {Id}";

    public double? Temperature
    {
        set => SetParameter(nameof(Temperature), value);
    }

    public double? Pressure
    {
        set => SetParameter(nameof(Pressure), value);
    }
}

public sealed class BoundaryConditionSection : IndexedSifSection
{
    public BoundaryConditionSection(int id) : base(id)
    {
    }

    protected override string SectionHeader => $"Boundary Condition {Id}";

    public string? Name
    {
        set => SetParameter(nameof(Name), value);
    }

    public IEnumerable<int>? TargetBoundaries
    {
        set => SetParameter(nameof(TargetBoundaries), value ?? Array.Empty<int>());
    }

    public double? Temperature
    {
        set => SetParameter(nameof(Temperature), value);
    }

    public double? HeatFlux
    {
        set => SetParameter(nameof(HeatFlux), value);
    }
}
