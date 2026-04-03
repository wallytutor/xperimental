using System.Globalization;

namespace Xl.Elmer.Sif;

public readonly record struct SifValue(string Text)
{
    public static SifValue Raw(string raw) => new(raw);

    public static SifValue From(object? value)
    {
        if (value is null)
        {
            return Raw("Null");
        }

        if (value is SifValue sifValue)
        {
            return sifValue;
        }

        if (value is string text)
        {
            return new SifValue(Quote(text));
        }

        if (value is bool boolValue)
        {
            return Raw(boolValue ? "True" : "False");
        }

        if (value is Enum enumValue)
        {
            return Raw(enumValue.ToString());
        }

        if (value is IEnumerable<double> doubles)
        {
            return Raw(string.Join(' ', doubles.Select(v => v.ToString(CultureInfo.InvariantCulture))));
        }

        if (value is IEnumerable<int> ints)
        {
            return Raw(string.Join(' ', ints));
        }

        if (value is IFormattable formattable)
        {
            return Raw(formattable.ToString(null, CultureInfo.InvariantCulture));
        }

        return Raw(value.ToString() ?? string.Empty);
    }

    public override string ToString() => Text;

    private static string Quote(string input)
    {
        if (input.StartsWith('"') && input.EndsWith('"'))
        {
            return input;
        }

        var escaped = input.Replace("\"", "\\\"");
        return $"\"{escaped}\"";
    }
}
