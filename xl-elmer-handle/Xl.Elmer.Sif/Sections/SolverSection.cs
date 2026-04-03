namespace Xl.Elmer.Sif;

public class SolverSection : IndexedSifSection
{
    private string? _equation;
    private string? _procedure;
    private string? _variable;
    private int? _execSolver;

    public SolverSection(int id) : base(id)
    {
    }

    protected override string SectionHeader => $"Solver {Id}";

    public LinearSystemControl? LinearSystem { get; set; }

    public NonlinearSystemControl? NonlinearSystem { get; set; }

    public string? Equation
    {
        get => _equation;
        set
        {
            _equation = value;
            SetParameter(nameof(Equation), value);
        }
    }

    public string? Procedure
    {
        get => _procedure;
        set
        {
            _procedure = value;
            SetParameter(nameof(Procedure), value);
        }
    }

    public string? Variable
    {
        get => _variable;
        set
        {
            _variable = value;
            SetParameter(nameof(Variable), value);
        }
    }

    public int? ExecSolver
    {
        get => _execSolver;
        set
        {
            _execSolver = value;
            SetParameter("Exec Solver", value);
        }
    }

    public SolverExecution? ExecuteWhen
    {
        set => SetParameter("Exec Solver", value is null ? null : SifValue.Raw(value.Value.ToSifKeyword()));
    }

    public void ConfigureProcedure(string library, string procedure)
    {
        _procedure = $"{library}:{procedure}";
        SetParameter(nameof(Procedure), SifValue.Raw($"{SifValue.QuoteLiteral(library)} {SifValue.QuoteLiteral(procedure)}"));
    }

    protected override IEnumerable<KeyValuePair<string, SifValue>> GetParameters()
    {
        var parameters = new Dictionary<string, SifValue>(Parameters, StringComparer.OrdinalIgnoreCase);

        LinearSystem?.Apply(parameters);
        NonlinearSystem?.Apply(parameters);

        return parameters.OrderBy(kv => kv.Key, StringComparer.Ordinal);
    }

    protected internal override void Validate(SifDocument document)
    {
        if (!HasParameter(nameof(Equation)))
        {
            throw new InvalidOperationException($"Solver {Id} is missing Equation.");
        }

        if (!HasParameter(nameof(Procedure)))
        {
            throw new InvalidOperationException($"Solver {Id} is missing Procedure.");
        }
    }
}
