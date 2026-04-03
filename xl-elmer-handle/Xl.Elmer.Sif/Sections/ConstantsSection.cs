namespace Xl.Elmer.Sif;

public sealed class ConstantsSection : SifSection
{
    private static readonly double[] DefaultGravity = new[] { 0.0, -1.0, 0.0, 9.82 };
    private const double DefaultStefanBoltzmann = 5.670374419e-08;
    private const double DefaultPermittivityOfVacuum = 8.85418781e-12;
    private const double DefaultPermeabilityOfVacuum = 1.25663706e-06;
    private const double DefaultBoltzmannConstant = 1.380649e-23;
    private const double DefaultUnitCharge = 1.6021766e-19;

    private double[]? _gravity;
    private double? _stefanBoltzmann;
    private double? _permittivityOfVacuum;
    private double? _permeabilityOfVacuum;
    private double? _boltzmannConstant;
    private double? _unitCharge;

    public ConstantsSection()
    {
        Gravity = DefaultGravity;
        StefanBoltzmann = DefaultStefanBoltzmann;
        PermittivityOfVacuum = DefaultPermittivityOfVacuum;
        PermeabilityOfVacuum = DefaultPermeabilityOfVacuum;
        BoltzmannConstant = DefaultBoltzmannConstant;
        UnitCharge = DefaultUnitCharge;
    }

    protected override string SectionHeader => "Constants";

    public double[]? Gravity
    {
        get => _gravity;
        set
        {
            _gravity = value;
            SetParameter(nameof(Gravity), value);
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
