namespace Xl.Elmer.Sif;

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

    public MaterialPropertyValue? Density => _density;

    public MaterialPropertyValue? HeatConductivity => _heatConductivity;

    public MaterialPropertyValue? HeatCapacity => _heatCapacity;

    public MaterialPropertyValue? Emissivity => _emissivity;

    public MaterialPropertyValue? YoungsModulus => _youngsModulus;

    public MaterialPropertyValue? PoissonRatio => _poissonRatio;

    public MaterialPropertyValue? HeatExpansionCoefficient => _heatExpansionCoefficient;

    public MaterialPropertyValue? ReferenceTemperature => _referenceTemperature;

    public MaterialPropertyValue? InternalEnergy => _internalEnergy;

    public void SetDensityConstant(double value) => SetDensity(MaterialPropertyValue.Constant(value));

    public void SetDensityMatc(string variable, string expression, string valueType = "Real")
        => SetDensity(MaterialPropertyValue.Matc(variable, expression, valueType));

    public void SetDensityLua(string variable, string expression, string valueType = "Real")
        => SetDensity(MaterialPropertyValue.Lua(variable, expression, valueType));

    public void SetDensityUserFunction(string variable, string library, string function, string valueType = "Real")
        => SetDensity(MaterialPropertyValue.UserFunction(variable, library, function, valueType));

    public void SetDensityTabular(string variable, IEnumerable<TabulatedMaterialPoint> points, string interpolation = "Linear", string valueType = "Real")
        => SetDensity(MaterialPropertyValue.Tabular(variable, points, interpolation, valueType));

    public void SetDensityTabular(double[][] data, string[] variables, string interpolation = "Linear", string valueType = "Real")
        => SetDensity(MaterialPropertyValue.Tabular(data, variables, interpolation, valueType));

    public void SetDensityFile(string filePath, string[] variables, string valueType = "Real")
        => SetDensity(MaterialPropertyValue.IncludeFile(filePath, variables, valueType));

    public void SetDensityRaw(string text) => SetDensity(MaterialPropertyValue.Raw(text));

    public void ClearDensity() => SetDensity(null);

    public void SetHeatConductivityConstant(double value) => SetHeatConductivity(MaterialPropertyValue.Constant(value));

    public void SetHeatConductivityMatc(string variable, string expression, string valueType = "Real")
        => SetHeatConductivity(MaterialPropertyValue.Matc(variable, expression, valueType));

    public void SetHeatConductivityLua(string variable, string expression, string valueType = "Real")
        => SetHeatConductivity(MaterialPropertyValue.Lua(variable, expression, valueType));

    public void SetHeatConductivityUserFunction(string variable, string library, string function, string valueType = "Real")
        => SetHeatConductivity(MaterialPropertyValue.UserFunction(variable, library, function, valueType));

    public void SetHeatConductivityTabular(string variable, IEnumerable<TabulatedMaterialPoint> points, string interpolation = "Linear", string valueType = "Real")
        => SetHeatConductivity(MaterialPropertyValue.Tabular(variable, points, interpolation, valueType));

    public void SetHeatConductivityTabular(double[][] data, string[] variables, string interpolation = "Linear", string valueType = "Real")
        => SetHeatConductivity(MaterialPropertyValue.Tabular(data, variables, interpolation, valueType));

    public void SetHeatConductivityFile(string filePath, string[] variables, string valueType = "Real")
        => SetHeatConductivity(MaterialPropertyValue.IncludeFile(filePath, variables, valueType));

    public void SetHeatConductivityRaw(string text) => SetHeatConductivity(MaterialPropertyValue.Raw(text));

    public void ClearHeatConductivity() => SetHeatConductivity(null);

    public void SetHeatCapacityConstant(double value) => SetHeatCapacity(MaterialPropertyValue.Constant(value));

    public void SetHeatCapacityMatc(string variable, string expression, string valueType = "Real")
        => SetHeatCapacity(MaterialPropertyValue.Matc(variable, expression, valueType));

    public void SetHeatCapacityLua(string variable, string expression, string valueType = "Real")
        => SetHeatCapacity(MaterialPropertyValue.Lua(variable, expression, valueType));

    public void SetHeatCapacityUserFunction(string variable, string library, string function, string valueType = "Real")
        => SetHeatCapacity(MaterialPropertyValue.UserFunction(variable, library, function, valueType));

    public void SetHeatCapacityTabular(string variable, IEnumerable<TabulatedMaterialPoint> points, string interpolation = "Linear", string valueType = "Real")
        => SetHeatCapacity(MaterialPropertyValue.Tabular(variable, points, interpolation, valueType));

    public void SetHeatCapacityTabular(double[][] data, string[] variables, string interpolation = "Linear", string valueType = "Real")
        => SetHeatCapacity(MaterialPropertyValue.Tabular(data, variables, interpolation, valueType));

    public void SetHeatCapacityFile(string filePath, string[] variables, string valueType = "Real")
        => SetHeatCapacity(MaterialPropertyValue.IncludeFile(filePath, variables, valueType));

    public void SetHeatCapacityRaw(string text) => SetHeatCapacity(MaterialPropertyValue.Raw(text));

    public void ClearHeatCapacity() => SetHeatCapacity(null);

    public void SetEmissivityConstant(double value) => SetEmissivity(MaterialPropertyValue.Constant(value));

    public void SetEmissivityMatc(string variable, string expression, string valueType = "Real")
        => SetEmissivity(MaterialPropertyValue.Matc(variable, expression, valueType));

    public void SetEmissivityLua(string variable, string expression, string valueType = "Real")
        => SetEmissivity(MaterialPropertyValue.Lua(variable, expression, valueType));

    public void SetEmissivityUserFunction(string variable, string library, string function, string valueType = "Real")
        => SetEmissivity(MaterialPropertyValue.UserFunction(variable, library, function, valueType));

    public void SetEmissivityTabular(string variable, IEnumerable<TabulatedMaterialPoint> points, string interpolation = "Linear", string valueType = "Real")
        => SetEmissivity(MaterialPropertyValue.Tabular(variable, points, interpolation, valueType));

    public void SetEmissivityTabular(double[][] data, string[] variables, string interpolation = "Linear", string valueType = "Real")
        => SetEmissivity(MaterialPropertyValue.Tabular(data, variables, interpolation, valueType));

    public void SetEmissivityFile(string filePath, string[] variables, string valueType = "Real")
        => SetEmissivity(MaterialPropertyValue.IncludeFile(filePath, variables, valueType));

    public void SetEmissivityRaw(string text) => SetEmissivity(MaterialPropertyValue.Raw(text));

    public void ClearEmissivity() => SetEmissivity(null);

    public void SetYoungsModulusConstant(double value) => SetYoungsModulus(MaterialPropertyValue.Constant(value));

    public void SetYoungsModulusMatc(string variable, string expression, string valueType = "Real")
        => SetYoungsModulus(MaterialPropertyValue.Matc(variable, expression, valueType));

    public void SetYoungsModulusLua(string variable, string expression, string valueType = "Real")
        => SetYoungsModulus(MaterialPropertyValue.Lua(variable, expression, valueType));

    public void SetYoungsModulusUserFunction(string variable, string library, string function, string valueType = "Real")
        => SetYoungsModulus(MaterialPropertyValue.UserFunction(variable, library, function, valueType));

    public void SetYoungsModulusTabular(string variable, IEnumerable<TabulatedMaterialPoint> points, string interpolation = "Linear", string valueType = "Real")
        => SetYoungsModulus(MaterialPropertyValue.Tabular(variable, points, interpolation, valueType));

    public void SetYoungsModulusTabular(double[][] data, string[] variables, string interpolation = "Linear", string valueType = "Real")
        => SetYoungsModulus(MaterialPropertyValue.Tabular(data, variables, interpolation, valueType));

    public void SetYoungsModulusFile(string filePath, string[] variables, string valueType = "Real")
        => SetYoungsModulus(MaterialPropertyValue.IncludeFile(filePath, variables, valueType));

    public void SetYoungsModulusRaw(string text) => SetYoungsModulus(MaterialPropertyValue.Raw(text));

    public void ClearYoungsModulus() => SetYoungsModulus(null);

    public void SetPoissonRatioConstant(double value) => SetPoissonRatio(MaterialPropertyValue.Constant(value));

    public void SetPoissonRatioMatc(string variable, string expression, string valueType = "Real")
        => SetPoissonRatio(MaterialPropertyValue.Matc(variable, expression, valueType));

    public void SetPoissonRatioLua(string variable, string expression, string valueType = "Real")
        => SetPoissonRatio(MaterialPropertyValue.Lua(variable, expression, valueType));

    public void SetPoissonRatioUserFunction(string variable, string library, string function, string valueType = "Real")
        => SetPoissonRatio(MaterialPropertyValue.UserFunction(variable, library, function, valueType));

    public void SetPoissonRatioTabular(string variable, IEnumerable<TabulatedMaterialPoint> points, string interpolation = "Linear", string valueType = "Real")
        => SetPoissonRatio(MaterialPropertyValue.Tabular(variable, points, interpolation, valueType));

    public void SetPoissonRatioTabular(double[][] data, string[] variables, string interpolation = "Linear", string valueType = "Real")
        => SetPoissonRatio(MaterialPropertyValue.Tabular(data, variables, interpolation, valueType));

    public void SetPoissonRatioFile(string filePath, string[] variables, string valueType = "Real")
        => SetPoissonRatio(MaterialPropertyValue.IncludeFile(filePath, variables, valueType));

    public void SetPoissonRatioRaw(string text) => SetPoissonRatio(MaterialPropertyValue.Raw(text));

    public void ClearPoissonRatio() => SetPoissonRatio(null);

    public void SetHeatExpansionCoefficientConstant(double value) => SetHeatExpansionCoefficient(MaterialPropertyValue.Constant(value));

    public void SetHeatExpansionCoefficientMatc(string variable, string expression, string valueType = "Real")
        => SetHeatExpansionCoefficient(MaterialPropertyValue.Matc(variable, expression, valueType));

    public void SetHeatExpansionCoefficientLua(string variable, string expression, string valueType = "Real")
        => SetHeatExpansionCoefficient(MaterialPropertyValue.Lua(variable, expression, valueType));

    public void SetHeatExpansionCoefficientUserFunction(string variable, string library, string function, string valueType = "Real")
        => SetHeatExpansionCoefficient(MaterialPropertyValue.UserFunction(variable, library, function, valueType));

    public void SetHeatExpansionCoefficientTabular(string variable, IEnumerable<TabulatedMaterialPoint> points, string interpolation = "Linear", string valueType = "Real")
        => SetHeatExpansionCoefficient(MaterialPropertyValue.Tabular(variable, points, interpolation, valueType));

    public void SetHeatExpansionCoefficientTabular(double[][] data, string[] variables, string interpolation = "Linear", string valueType = "Real")
        => SetHeatExpansionCoefficient(MaterialPropertyValue.Tabular(data, variables, interpolation, valueType));

    public void SetHeatExpansionCoefficientFile(string filePath, string[] variables, string valueType = "Real")
        => SetHeatExpansionCoefficient(MaterialPropertyValue.IncludeFile(filePath, variables, valueType));

    public void SetHeatExpansionCoefficientRaw(string text) => SetHeatExpansionCoefficient(MaterialPropertyValue.Raw(text));

    public void ClearHeatExpansionCoefficient() => SetHeatExpansionCoefficient(null);

    public void SetReferenceTemperatureConstant(double value) => SetReferenceTemperature(MaterialPropertyValue.Constant(value));

    public void SetReferenceTemperatureMatc(string variable, string expression, string valueType = "Real")
        => SetReferenceTemperature(MaterialPropertyValue.Matc(variable, expression, valueType));

    public void SetReferenceTemperatureLua(string variable, string expression, string valueType = "Real")
        => SetReferenceTemperature(MaterialPropertyValue.Lua(variable, expression, valueType));

    public void SetReferenceTemperatureUserFunction(string variable, string library, string function, string valueType = "Real")
        => SetReferenceTemperature(MaterialPropertyValue.UserFunction(variable, library, function, valueType));

    public void SetReferenceTemperatureTabular(string variable, IEnumerable<TabulatedMaterialPoint> points, string interpolation = "Linear", string valueType = "Real")
        => SetReferenceTemperature(MaterialPropertyValue.Tabular(variable, points, interpolation, valueType));

    public void SetReferenceTemperatureTabular(double[][] data, string[] variables, string interpolation = "Linear", string valueType = "Real")
        => SetReferenceTemperature(MaterialPropertyValue.Tabular(data, variables, interpolation, valueType));

    public void SetReferenceTemperatureFile(string filePath, string[] variables, string valueType = "Real")
        => SetReferenceTemperature(MaterialPropertyValue.IncludeFile(filePath, variables, valueType));

    public void SetReferenceTemperatureRaw(string text) => SetReferenceTemperature(MaterialPropertyValue.Raw(text));

    public void ClearReferenceTemperature() => SetReferenceTemperature(null);

    public void SetInternalEnergyConstant(double value) => SetInternalEnergy(MaterialPropertyValue.Constant(value));

    public void SetInternalEnergyMatc(string variable, string expression, string valueType = "Real")
        => SetInternalEnergy(MaterialPropertyValue.Matc(variable, expression, valueType));

    public void SetInternalEnergyLua(string variable, string expression, string valueType = "Real")
        => SetInternalEnergy(MaterialPropertyValue.Lua(variable, expression, valueType));

    public void SetInternalEnergyUserFunction(string variable, string library, string function, string valueType = "Real")
        => SetInternalEnergy(MaterialPropertyValue.UserFunction(variable, library, function, valueType));

    public void SetInternalEnergyTabular(string variable, IEnumerable<TabulatedMaterialPoint> points, string interpolation = "Linear", string valueType = "Real")
        => SetInternalEnergy(MaterialPropertyValue.Tabular(variable, points, interpolation, valueType));

    public void SetInternalEnergyTabular(double[][] data, string[] variables, string interpolation = "Linear", string valueType = "Real")
        => SetInternalEnergy(MaterialPropertyValue.Tabular(data, variables, interpolation, valueType));

    public void SetInternalEnergyFile(string filePath, string[] variables, string valueType = "Real")
        => SetInternalEnergy(MaterialPropertyValue.IncludeFile(filePath, variables, valueType));

    public void SetInternalEnergyRaw(string text) => SetInternalEnergy(MaterialPropertyValue.Raw(text));

    public void ClearInternalEnergy() => SetInternalEnergy(null);

    public bool HasThermalProperties()
    {
        return _density is not null && _heatConductivity is not null && _heatCapacity is not null;
    }

    private void SetDensity(MaterialPropertyValue? value)
    {
        _density = value;
        SetParameter(nameof(Density), value?.ToSifValue());
    }

    private void SetHeatConductivity(MaterialPropertyValue? value)
    {
        _heatConductivity = value;
        SetParameter("Heat Conductivity", value?.ToSifValue());
    }

    private void SetHeatCapacity(MaterialPropertyValue? value)
    {
        _heatCapacity = value;
        SetParameter("Heat Capacity", value?.ToSifValue());
    }

    private void SetEmissivity(MaterialPropertyValue? value)
    {
        _emissivity = value;
        SetParameter(nameof(Emissivity), value?.ToSifValue());
    }

    private void SetYoungsModulus(MaterialPropertyValue? value)
    {
        _youngsModulus = value;
        SetParameter("Youngs Modulus", value?.ToSifValue());
    }

    private void SetPoissonRatio(MaterialPropertyValue? value)
    {
        _poissonRatio = value;
        SetParameter("Poisson Ratio", value?.ToSifValue());
    }

    private void SetHeatExpansionCoefficient(MaterialPropertyValue? value)
    {
        _heatExpansionCoefficient = value;
        SetParameter("Heat Expansion Coefficient", value?.ToSifValue());
    }

    private void SetReferenceTemperature(MaterialPropertyValue? value)
    {
        _referenceTemperature = value;
        SetParameter("Reference Temperature", value?.ToSifValue());
    }

    private void SetInternalEnergy(MaterialPropertyValue? value)
    {
        _internalEnergy = value;
        SetParameter("Internal Energy", value?.ToSifValue());
    }
}
