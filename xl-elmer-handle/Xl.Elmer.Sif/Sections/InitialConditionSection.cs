namespace Xl.Elmer.Sif;

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
