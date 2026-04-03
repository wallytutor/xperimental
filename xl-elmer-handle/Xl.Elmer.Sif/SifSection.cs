using System.Text;

namespace Xl.Elmer.Sif;

public abstract class SifSection
{
    private readonly Dictionary<string, SifValue> _parameters = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, SifValue> Parameters => _parameters;

    protected abstract string SectionHeader { get; }

    public void SetParameter(string key, object? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Parameter key cannot be null or empty.", nameof(key));
        }

        var normalizedKey = ToPascalCase(key);
        _parameters[normalizedKey] = SifValue.From(value);
    }

    public bool RemoveParameter(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        return _parameters.Remove(ToPascalCase(key));
    }

    public bool TryGetParameter(string key, out SifValue value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            value = default;
            return false;
        }

        return _parameters.TryGetValue(ToPascalCase(key), out value);
    }

    internal virtual void WriteTo(StringBuilder builder)
    {
        builder.AppendLine(SectionHeader);

        foreach (var (key, value) in _parameters.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            builder.Append("  ");
            builder.Append(key);
            builder.Append(" = ");
            builder.AppendLine(value.ToString());
        }

        builder.AppendLine("End");
        builder.AppendLine();
    }

    protected static string ToPascalCase(string text)
    {
        var pieces = text
            .Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Trim())
            .Where(part => part.Length > 0)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant());

        return string.Concat(pieces);
    }
}

public abstract class IndexedSifSection : SifSection
{
    protected IndexedSifSection(int id)
    {
        if (id < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Section identifier must be greater than zero.");
        }

        Id = id;
    }

    public int Id { get; }
}
