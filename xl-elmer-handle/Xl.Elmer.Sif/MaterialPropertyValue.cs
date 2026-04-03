using System.Globalization;
using System.Text;

namespace Xl.Elmer.Sif;

public abstract record MaterialPropertyValue
{
    public static MaterialPropertyValue Constant(double value) => new ConstantMaterialPropertyValue(value);

    public static MaterialPropertyValue Matc(string variable, string expression, string valueType = "Real")
        => new MatcMaterialPropertyValue(variable, expression, valueType);

    public static MaterialPropertyValue Lua(string variable, string expression, string valueType = "Real")
        => new LuaMaterialPropertyValue(variable, expression, valueType);

    public static MaterialPropertyValue UserFunction(string variable, string library, string function, string valueType = "Real")
        => new UserFunctionMaterialPropertyValue(variable, library, function, valueType);

    public static MaterialPropertyValue Tabular(
        string variable,
        IEnumerable<TabulatedMaterialPoint> points,
        string interpolation = "Linear",
        string valueType = "Real")
        => new TabularMaterialPropertyValue(
            new[] { variable },
            points.Select(point => new[] { point.VariableValue, point.PropertyValue }).ToArray(),
            interpolation,
            valueType);

    public static MaterialPropertyValue Tabular(
        IEnumerable<IEnumerable<double>> data,
        string[] variables,
        string interpolation = "Linear",
        string valueType = "Real")
        => new TabularMaterialPropertyValue(
            variables,
            data.Select(row => row.ToArray()).ToArray(),
            interpolation,
            valueType);

    public static implicit operator MaterialPropertyValue(double value) => Constant(value);

    public static MaterialPropertyValue Raw(string text) => new RawMaterialPropertyValue(text);

    public static MaterialPropertyValue IncludeFile(string filePath, string[] variables, string valueType = "Real")
        => new IncludeFileMaterialPropertyValue(filePath, variables, valueType);

    internal abstract SifValue ToSifValue();
}

public sealed record ConstantMaterialPropertyValue(double Value) : MaterialPropertyValue
{
    internal override SifValue ToSifValue() => SifValue.Raw(Value.ToString(CultureInfo.InvariantCulture));
}

public sealed record MatcMaterialPropertyValue(string Variable, string Expression, string ValueType = "Real") : MaterialPropertyValue
{
    internal override SifValue ToSifValue()
    {
        return SifValue.Raw(string.Join(Environment.NewLine,
            $"Variable {Variable}",
            $"{ValueType} MATC {SifValue.QuoteLiteral(Expression)}"));
    }
}

public sealed record LuaMaterialPropertyValue(string Variable, string Expression, string ValueType = "Real") : MaterialPropertyValue
{
    internal override SifValue ToSifValue()
    {
        return SifValue.Raw(string.Join(Environment.NewLine,
            $"Variable {Variable}",
            $"{ValueType} Lua {SifValue.QuoteLiteral(Expression)}"));
    }
}

public sealed record UserFunctionMaterialPropertyValue(
    string Variable,
    string Library,
    string Function,
    string ValueType = "Real") : MaterialPropertyValue
{
    internal override SifValue ToSifValue()
    {
        return SifValue.Raw(string.Join(Environment.NewLine,
            $"Variable {Variable}",
            $"{ValueType} Procedure {SifValue.QuoteLiteral(Library)} {SifValue.QuoteLiteral(Function)}"));
    }
}

public sealed record TabularMaterialPropertyValue(
    string[] Variables,
    IReadOnlyList<double[]> Rows,
    string Interpolation = "Linear",
    string ValueType = "Real") : MaterialPropertyValue
{
    internal override SifValue ToSifValue()
    {
        if (Variables.Length == 0)
        {
            throw new InvalidOperationException("Tabular material properties require at least one variable.");
        }

        if (Variables.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("Tabular material property variables cannot be blank.");
        }

        if (Rows.Count == 0)
        {
            throw new InvalidOperationException("Tabular material properties require at least one point.");
        }

        var builder = new StringBuilder();
        builder.AppendLine($"Variable {string.Join(" ", Variables)}");
        builder.AppendLine(string.IsNullOrEmpty(Interpolation) ? ValueType : $"{ValueType} {Interpolation}");

        var expectedColumns = Variables.Length + 1;
        foreach (var row in Rows)
        {
            if (row.Length != expectedColumns)
            {
                throw new InvalidOperationException($"Each tabular row must contain {expectedColumns} values: {Variables.Length} variable columns and one property-value column.");
            }

            builder.Append("  ");
            builder.AppendLine(string.Join(" ", row.Select(value => value.ToString(CultureInfo.InvariantCulture))));
        }

        builder.Append("End");
        return SifValue.Raw(builder.ToString());
    }
}

public readonly record struct TabulatedMaterialPoint(double VariableValue, double PropertyValue);

public sealed record RawMaterialPropertyValue(string Text) : MaterialPropertyValue
{
    internal override SifValue ToSifValue() => SifValue.Raw(Text);
}

public sealed record IncludeFileMaterialPropertyValue(string FilePath, string[] Variables, string ValueType = "Real") : MaterialPropertyValue
{
    internal override SifValue ToSifValue()
    {
        if (Variables.Length == 0)
        {
            throw new InvalidOperationException("Include-file material properties require at least one variable.");
        }

        if (Variables.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("Include-file material property variables cannot be blank.");
        }

        return SifValue.Raw(string.Join(Environment.NewLine,
            $"Variable {string.Join(" ", Variables)}",
            ValueType,
            $"  Include {SifValue.QuoteLiteral(FilePath)}",
            "End"));
    }
}