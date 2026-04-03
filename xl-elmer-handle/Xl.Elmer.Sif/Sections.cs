namespace Xl.Elmer.Sif;

public sealed class HeaderSection : SifSection
{
    private string? _checkKeywords;
    private string? _meshDbDirectory;
    private string? _meshDbName;
    private string? _includePath;
    private string? _resultsDirectory;

    protected override string SectionHeader => "Header";

    public string? CheckKeywords
    {
        get => _checkKeywords;
        set
        {
            _checkKeywords = value;
            SetParameter("Check Keywords", value);
        }
    }

    public string? MeshDbDirectory
    {
        get => _meshDbDirectory;
        set => _meshDbDirectory = value;
    }

    public string? MeshDbName
    {
        get => _meshDbName;
        set => _meshDbName = value;
    }

    public string? IncludePath
    {
        get => _includePath;
        set => _includePath = value;
    }

    public string? ResultsDirectory
    {
        get => _resultsDirectory;
        set => _resultsDirectory = value;
    }

    protected override void WriteAdditionalEntries(System.Text.StringBuilder builder)
    {
        if (_meshDbDirectory is not null || _meshDbName is not null)
        {
            builder.Append("  Mesh DB ");
            builder.Append(SifValue.QuoteLiteral(_meshDbDirectory ?? "."));
            builder.Append(' ');
            builder.AppendLine(SifValue.QuoteLiteral(_meshDbName ?? "."));
        }

        if (_includePath is not null)
        {
            builder.Append("  Include Path ");
            builder.AppendLine(SifValue.QuoteLiteral(_includePath));
        }

        if (_resultsDirectory is not null)
        {
            builder.Append("  Results Directory ");
            builder.AppendLine(SifValue.QuoteLiteral(_resultsDirectory));
        }
    }
}

public sealed class SimulationSection : SifSection
{
    private string? _coordinateSystem;
    private string? _simulationType;
    private int? _maxOutputLevel;
    private string? _timesteppingMethod;
    private IReadOnlyList<double>? _timestepSizes;
    private IReadOnlyList<int>? _timestepIntervals;
    private IReadOnlyList<int>? _outputIntervals;
    private string? _solverInputFile;
    private IReadOnlyList<int>? _coordinateMapping;
    private bool? _convergenceMonitor;
    private string? _bdfOrder;
    private string? _outputFile;
    private bool? _binaryOutput;

    protected override string SectionHeader => "Simulation";

    public string? CoordinateSystem
    {
        get => _coordinateSystem;
        set
        {
            _coordinateSystem = value;
            SetParameter("Coordinate System", value);
        }
    }

    public string? SimulationType
    {
        get => _simulationType;
        set
        {
            _simulationType = value;
            SetParameter("Simulation Type", value);
        }
    }

    public int? MaxOutputLevel
    {
        get => _maxOutputLevel;
        set
        {
            _maxOutputLevel = value;
            SetParameter("Max Output Level", value);
        }
    }

    public string? TimesteppingMethod
    {
        get => _timesteppingMethod;
        set
        {
            _timesteppingMethod = value;
            SetParameter("Timestepping Method", value);
        }
    }

    public IEnumerable<double>? TimestepSizes
    {
        get => _timestepSizes;
        set
        {
            _timestepSizes = value?.ToArray();
            SetParameter("Timestep Sizes", _timestepSizes);
        }
    }

    public IEnumerable<int>? TimestepIntervals
    {
        get => _timestepIntervals;
        set
        {
            _timestepIntervals = value?.ToArray();
            SetParameter("Timestep Intervals", _timestepIntervals);
        }
    }

    public IEnumerable<int>? OutputIntervals
    {
        get => _outputIntervals;
        set
        {
            _outputIntervals = value?.ToArray();
            SetParameter("Output Intervals", _outputIntervals);
        }
    }

    public string? SolverInputFile
    {
        get => _solverInputFile;
        set
        {
            _solverInputFile = value;
            SetParameter("Solver Input File", value);
        }
    }

    public IEnumerable<int>? CoordinateMapping
    {
        get => _coordinateMapping;
        set
        {
            _coordinateMapping = value?.ToArray();
            SetParameter("Coordinate Mapping", _coordinateMapping);
        }
    }

    public bool? ConvergenceMonitor
    {
        get => _convergenceMonitor;
        set
        {
            _convergenceMonitor = value;
            SetParameter("Convergence Monitor", value);
        }
    }

    public string? BdfOrder
    {
        get => _bdfOrder;
        set
        {
            _bdfOrder = value;
            SetParameter("BDF Order", value is null ? null : SifValue.Raw(value));
        }
    }

    public string? OutputFile
    {
        get => _outputFile;
        set
        {
            _outputFile = value;
            SetParameter("Output File", value);
        }
    }

    public bool? BinaryOutput
    {
        get => _binaryOutput;
        set
        {
            _binaryOutput = value;
            SetParameter("Binary Output", value);
        }
    }
}

public sealed class ConstantsSection : SifSection
{
    private IReadOnlyList<double>? _gravity;
    private double? _stefanBoltzmann;
    private double? _permittivityOfVacuum;
    private double? _permeabilityOfVacuum;
    private double? _boltzmannConstant;
    private double? _unitCharge;

    protected override string SectionHeader => "Constants";

    public IEnumerable<double>? Gravity
    {
        get => _gravity;
        set
        {
            _gravity = value?.ToArray();
            SetParameter(nameof(Gravity), _gravity);
        }
    }

    public double? StefanBoltzmann
    {
        get => _stefanBoltzmann;
        set
        {
            _stefanBoltzmann = value;
            SetParameter("Stefan Boltzmann", value);
        }
    }

    public double? PermittivityOfVacuum
    {
        get => _permittivityOfVacuum;
        set
        {
            _permittivityOfVacuum = value;
            SetParameter("Permittivity of Vacuum", value);
        }
    }

    public double? PermeabilityOfVacuum
    {
        get => _permeabilityOfVacuum;
        set
        {
            _permeabilityOfVacuum = value;
            SetParameter("Permeability of Vacuum", value);
        }
    }

    public double? BoltzmannConstant
    {
        get => _boltzmannConstant;
        set
        {
            _boltzmannConstant = value;
            SetParameter("Boltzmann Constant", value);
        }
    }

    public double? UnitCharge
    {
        get => _unitCharge;
        set
        {
            _unitCharge = value;
            SetParameter("Unit Charge", value);
        }
    }
}

public sealed class MaterialSection : IndexedSifSection
{
    private string? _name;
    private MaterialPropertyValue? _density;
    private MaterialPropertyValue? _heatConductivity;
    private MaterialPropertyValue? _heatCapacity;
    private MaterialPropertyValue? _emissivity;
    private MaterialPropertyValue? _youngsModulus;
    private MaterialPropertyValue? _poissonRatio;
    private MaterialPropertyValue? _heatExpansionCoefficient;
    private MaterialPropertyValue? _referenceTemperature;
    private MaterialPropertyValue? _internalEnergy;

    public MaterialSection(int id) : base(id)
    {
    }

    protected override string SectionHeader => $"Material {Id}";

    public string? Name
    {
        get => _name;
        set
        {
            _name = value;
            SetParameter(nameof(Name), value);
        }
    }

    public MaterialPropertyValue? Density
    {
        get => _density;
        set
        {
            _density = value;
            SetParameter(nameof(Density), value?.ToSifValue());
        }
    }

    public MaterialPropertyValue? HeatConductivity
    {
        get => _heatConductivity;
        set
        {
            _heatConductivity = value;
            SetParameter("Heat Conductivity", value?.ToSifValue());
        }
    }

    public MaterialPropertyValue? HeatCapacity
    {
        get => _heatCapacity;
        set
        {
            _heatCapacity = value;
            SetParameter("Heat Capacity", value?.ToSifValue());
        }
    }

    public MaterialPropertyValue? Emissivity
    {
        get => _emissivity;
        set
        {
            _emissivity = value;
            SetParameter(nameof(Emissivity), value?.ToSifValue());
        }
    }

    public MaterialPropertyValue? YoungsModulus
    {
        get => _youngsModulus;
        set
        {
            _youngsModulus = value;
            SetParameter("Youngs Modulus", value?.ToSifValue());
        }
    }

    public MaterialPropertyValue? PoissonRatio
    {
        get => _poissonRatio;
        set
        {
            _poissonRatio = value;
            SetParameter("Poisson Ratio", value?.ToSifValue());
        }
    }

    public MaterialPropertyValue? HeatExpansionCoefficient
    {
        get => _heatExpansionCoefficient;
        set
        {
            _heatExpansionCoefficient = value;
            SetParameter("Heat Expansion Coefficient", value?.ToSifValue());
        }
    }

    public MaterialPropertyValue? ReferenceTemperature
    {
        get => _referenceTemperature;
        set
        {
            _referenceTemperature = value;
            SetParameter("Reference Temperature", value?.ToSifValue());
        }
    }

    public MaterialPropertyValue? InternalEnergy
    {
        get => _internalEnergy;
        set
        {
            _internalEnergy = value;
            SetParameter("Internal Energy", value?.ToSifValue());
        }
    }

    public bool HasThermalProperties()
    {
        return Density is not null && HeatConductivity is not null && HeatCapacity is not null;
    }
}

public sealed class BodySection : IndexedSifSection
{
    private string? _name;
    private IReadOnlyList<int>? _targetBodies;
    private int? _equation;
    private int? _material;
    private int? _bodyForce;
    private int? _initialCondition;

    public BodySection(int id) : base(id)
    {
    }

    protected override string SectionHeader => $"Body {Id}";

    public string? Name
    {
        get => _name;
        set
        {
            _name = value;
            SetParameter(nameof(Name), value);
        }
    }

    public IEnumerable<int>? TargetBodies
    {
        get => _targetBodies;
        set
        {
            _targetBodies = value?.ToArray();
            SetParameter(nameof(TargetBodies), _targetBodies);
        }
    }

    public int? Equation
    {
        get => _equation;
        set
        {
            _equation = value;
            SetParameter(nameof(Equation), value);
        }
    }

    public int? Material
    {
        get => _material;
        set
        {
            _material = value;
            SetParameter(nameof(Material), value);
        }
    }

    public int? BodyForce
    {
        get => _bodyForce;
        set
        {
            _bodyForce = value;
            SetParameter(nameof(BodyForce), value);
        }
    }

    public int? InitialCondition
    {
        get => _initialCondition;
        set
        {
            _initialCondition = value;
            SetParameter(nameof(InitialCondition), value);
        }
    }
}

public class SolverSection : IndexedSifSection
{
    private string? _equation;
    private string? _procedure;
    private string? _variable;
    private int? _execSolver;

    public SolverSection(int id) : base(id)
    {
    }

    protected override string SectionHeader => $"Solver {Id}";

    public LinearSystemControl? LinearSystem { get; set; }

    public NonlinearSystemControl? NonlinearSystem { get; set; }

    public string? Equation
    {
        get => _equation;
        set
        {
            _equation = value;
            SetParameter(nameof(Equation), value);
        }
    }

    public string? Procedure
    {
        get => _procedure;
        set
        {
            _procedure = value;
            SetParameter(nameof(Procedure), value);
        }
    }

    public string? Variable
    {
        get => _variable;
        set
        {
            _variable = value;
            SetParameter(nameof(Variable), value);
        }
    }

    public int? ExecSolver
    {
        get => _execSolver;
        set
        {
            _execSolver = value;
            SetParameter("Exec Solver", value);
        }
    }

    public SolverExecution? ExecuteWhen
    {
        set => SetParameter("Exec Solver", value is null ? null : SifValue.Raw(value.Value.ToSifKeyword()));
    }

    public void ConfigureProcedure(string library, string procedure)
    {
        _procedure = $"{library}:{procedure}";
        SetParameter(nameof(Procedure), SifValue.Raw($"{SifValue.QuoteLiteral(library)} {SifValue.QuoteLiteral(procedure)}"));
    }

    protected override IEnumerable<KeyValuePair<string, SifValue>> GetParameters()
    {
        var parameters = new Dictionary<string, SifValue>(Parameters, StringComparer.OrdinalIgnoreCase);

        LinearSystem?.Apply(parameters);
        NonlinearSystem?.Apply(parameters);

        return parameters.OrderBy(kv => kv.Key, StringComparer.Ordinal);
    }

    protected internal override void Validate(SifDocument document)
    {
        if (!HasParameter(nameof(Equation)))
        {
            throw new InvalidOperationException($"Solver {Id} is missing Equation.");
        }

        if (!HasParameter(nameof(Procedure)))
        {
            throw new InvalidOperationException($"Solver {Id} is missing Procedure.");
        }
    }
}

public sealed class EquationSection : IndexedSifSection
{
    private string? _name;
    private IReadOnlyList<int>? _activeSolvers;

    public EquationSection(int id) : base(id)
    {
    }

    protected override string SectionHeader => $"Equation {Id}";

    public string? Name
    {
        get => _name;
        set
        {
            _name = value;
            SetParameter(nameof(Name), value);
        }
    }

    public IEnumerable<int>? ActiveSolvers
    {
        get => _activeSolvers;
        set
        {
            _activeSolvers = value?.ToArray();
            SetParameter(nameof(ActiveSolvers), _activeSolvers);
        }
    }
}

public sealed class InitialConditionSection : IndexedSifSection
{
    private double? _temperature;
    private double? _pressure;

    public InitialConditionSection(int id) : base(id)
    {
    }

    protected override string SectionHeader => $"Initial Condition {Id}";

    public double? Temperature
    {
        get => _temperature;
        set
        {
            _temperature = value;
            SetParameter(nameof(Temperature), value);
        }
    }

    public double? Pressure
    {
        get => _pressure;
        set
        {
            _pressure = value;
            SetParameter(nameof(Pressure), value);
        }
    }
}

public sealed class BoundaryConditionSection : IndexedSifSection
{
    private string? _name;
    private IReadOnlyList<int>? _targetBoundaries;
    private double? _temperature;
    private double? _heatFlux;

    public BoundaryConditionSection(int id) : base(id)
    {
    }

    protected override string SectionHeader => $"Boundary Condition {Id}";

    public string? Name
    {
        get => _name;
        set
        {
            _name = value;
            SetParameter(nameof(Name), value);
        }
    }

    public IEnumerable<int>? TargetBoundaries
    {
        get => _targetBoundaries;
        set
        {
            _targetBoundaries = value?.ToArray();
            SetParameter(nameof(TargetBoundaries), _targetBoundaries);
        }
    }

    public double? Temperature
    {
        get => _temperature;
        set
        {
            _temperature = value;
            SetParameter(nameof(Temperature), value);
        }
    }

    public double? HeatFlux
    {
        get => _heatFlux;
        set
        {
            _heatFlux = value;
            SetParameter(nameof(HeatFlux), value);
        }
    }
}
