namespace Xl.Elmer.Sif;

public sealed class EquationSection : IndexedSifSection
{
    private string? _name;
    private int[]? _activeSolvers;

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

    public int[]? ActiveSolvers
    {
        get => _activeSolvers;
        set
        {
            _activeSolvers = value;
            SetParameter(nameof(ActiveSolvers), value);
        }
    }
}
