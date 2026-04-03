namespace Xl.Elmer.Sif;

public sealed class HeaderSection : SifSection
{
    private string? _checkKeywords;
    private string? _meshDbDirectory;
    private string? _meshDbName;
    private string? _includePath;
    private string? _resultsDirectory;

    protected override string SectionHeader => "Header";

    public string? CheckKeywords
    {
        get => _checkKeywords;
        set
        {
            _checkKeywords = value;
            SetParameter("Check Keywords", value);
        }
    }

    public string? MeshDbDirectory
    {
        get => _meshDbDirectory;
        set => _meshDbDirectory = value;
    }

    public string? MeshDbName
    {
        get => _meshDbName;
        set => _meshDbName = value;
    }

    public string? IncludePath
    {
        get => _includePath;
        set => _includePath = value;
    }

    public string? ResultsDirectory
    {
        get => _resultsDirectory;
        set => _resultsDirectory = value;
    }

    protected override void WriteAdditionalEntries(System.Text.StringBuilder builder)
    {
        if (_meshDbDirectory is not null || _meshDbName is not null)
        {
            builder.Append("  Mesh DB ");
            builder.Append(SifValue.QuoteLiteral(_meshDbDirectory ?? "."));
            builder.Append(' ');
            builder.AppendLine(SifValue.QuoteLiteral(_meshDbName ?? "."));
        }

        if (_includePath is not null)
        {
            builder.Append("  Include Path ");
            builder.AppendLine(SifValue.QuoteLiteral(_includePath));
        }

        if (_resultsDirectory is not null)
        {
            builder.Append("  Results Directory ");
            builder.AppendLine(SifValue.QuoteLiteral(_resultsDirectory));
        }
    }
}
