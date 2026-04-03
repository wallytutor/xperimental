namespace Xl.Elmer.Sif;

public sealed class SimulationSection : SifSection
{
    private string? _coordinateSystem;
    private string? _simulationType;
    private int? _maxOutputLevel;
    private string? _timesteppingMethod;
    private double[]? _timestepSizes;
    private int[]? _timestepIntervals;
    private int[]? _outputIntervals;
    private string? _solverInputFile;
    private int[]? _coordinateMapping;
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

    public double[]? TimestepSizes
    {
        get => _timestepSizes;
        set
        {
            _timestepSizes = value;
            SetParameter("Timestep Sizes", value);
        }
    }

    public int[]? TimestepIntervals
    {
        get => _timestepIntervals;
        set
        {
            _timestepIntervals = value;
            SetParameter("Timestep Intervals", value);
        }
    }

    public int[]? OutputIntervals
    {
        get => _outputIntervals;
        set
        {
            _outputIntervals = value;
            SetParameter("Output Intervals", value);
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

    public int[]? CoordinateMapping
    {
        get => _coordinateMapping;
        set
        {
            _coordinateMapping = value;
            SetParameter("Coordinate Mapping", value);
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
