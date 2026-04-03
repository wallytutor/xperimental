namespace Xl.Elmer.Sif;

public sealed class BoundaryConditionSection : IndexedSifSection
{
    private string? _name;
    private int[]? _targetBoundaries;
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

    public int[]? TargetBoundaries
    {
        get => _targetBoundaries;
        set
        {
            _targetBoundaries = value;
            SetParameter(nameof(TargetBoundaries), value);
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
