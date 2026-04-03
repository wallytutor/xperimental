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
        => new TabularMaterialPropertyValue(variable, points.ToArray(), interpolation, valueType);

    public static implicit operator MaterialPropertyValue(double value) => Constant(value);

    public static MaterialPropertyValue Raw(string text) => new RawMaterialPropertyValue(text);

    public static MaterialPropertyValue IncludeFile(string variable, string filePath, string valueType = "Real")
        => new IncludeFileMaterialPropertyValue(variable, filePath, valueType);

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
    string Variable,
    IReadOnlyList<TabulatedMaterialPoint> Points,
    string Interpolation = "Linear",
    string ValueType = "Real") : MaterialPropertyValue
{
    internal override SifValue ToSifValue()
    {
        if (Points.Count == 0)
        {
            throw new InvalidOperationException("Tabular material properties require at least one point.");
        }

        var builder = new StringBuilder();
        builder.AppendLine($"Variable {Variable}");
        builder.AppendLine(string.IsNullOrEmpty(Interpolation) ? ValueType : $"{ValueType} {Interpolation}");

        foreach (var point in Points)
        {
            builder.Append("  ");
            builder.Append(point.VariableValue.ToString(CultureInfo.InvariantCulture));
            builder.Append(' ');
            builder.AppendLine(point.PropertyValue.ToString(CultureInfo.InvariantCulture));
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

public sealed record IncludeFileMaterialPropertyValue(string Variable, string FilePath, string ValueType = "Real") : MaterialPropertyValue
{
    internal override SifValue ToSifValue()
    {
        return SifValue.Raw(string.Join(Environment.NewLine,
            $"Variable {Variable}",
            ValueType,
            $"  Include {SifValue.QuoteLiteral(FilePath)}",
            "End"));
    }
}