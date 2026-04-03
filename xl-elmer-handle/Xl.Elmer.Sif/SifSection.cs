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

        if (value is null)
        {
            _parameters.Remove(normalizedKey);
            return;
        }

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

        foreach (var (key, value) in GetParameters())
        {
            WriteParameter(builder, key, value);
        }

        WriteAdditionalEntries(builder);

        builder.AppendLine("End");
        builder.AppendLine();
    }

    protected virtual void WriteAdditionalEntries(StringBuilder builder) { }

    protected bool HasParameter(string key)
    {
        return _parameters.ContainsKey(ToPascalCase(key));
    }

    protected void SetRawParameter(string key, SifValue value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Parameter key cannot be null or empty.", nameof(key));
        }

        _parameters[key] = value;
    }

    protected void RemoveRawParameter(string key)
    {
        _parameters.Remove(key);
    }

    protected virtual IEnumerable<KeyValuePair<string, SifValue>> GetParameters()
    {
        return _parameters.OrderBy(kv => kv.Key, StringComparer.Ordinal);
    }

    protected internal virtual void Validate(SifDocument document)
    {
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

    protected static void WriteParameter(StringBuilder builder, string key, SifValue value)
    {
        var lines = value.ToString().Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        builder.Append("  ");
        builder.Append(key);
        builder.Append(" = ");
        builder.AppendLine(lines[0]);

        for (var index = 1; index < lines.Length; index++)
        {
            builder.Append("    ");
            builder.AppendLine(lines[index]);
        }
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
