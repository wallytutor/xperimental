namespace Xl.Elmer.Sif;

public sealed class BodySection : IndexedSifSection
{
    private string? _name;
    private int[]? _targetBodies;
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

    public int[]? TargetBodies
    {
        get => _targetBodies;
        set
        {
            _targetBodies = value;
            SetParameter(nameof(TargetBodies), value);
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
