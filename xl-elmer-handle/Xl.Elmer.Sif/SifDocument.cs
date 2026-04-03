using System.Text;

namespace Xl.Elmer.Sif;

public sealed class SifDocument
{
    public HeaderSection Header { get; } = new();

    public SimulationSection Simulation { get; } = new();

    public ConstantsSection Constants { get; } = new();

    public IList<MaterialSection> Materials { get; } = new List<MaterialSection>();

    public IList<BodySection> Bodies { get; } = new List<BodySection>();

    public IList<SolverSection> Solvers { get; } = new List<SolverSection>();

    public IList<EquationSection> Equations { get; } = new List<EquationSection>();

    public IList<InitialConditionSection> InitialConditions { get; } = new List<InitialConditionSection>();

    public IList<BoundaryConditionSection> BoundaryConditions { get; } = new List<BoundaryConditionSection>();

    public IList<string> MatcScripts { get; } = new List<string>();

    public void AddMatcScript(string script)
    {
        MatcScripts.Add(script ?? throw new ArgumentNullException(nameof(script)));
    }

    public MaterialSection AddMaterial() => AddIndexedSection<MaterialSection, MaterialSection>(Materials, id => new MaterialSection(id));

    public BodySection AddBody() => AddIndexedSection<BodySection, BodySection>(Bodies, id => new BodySection(id));

    public SolverSection AddSolver() => AddIndexedSection<SolverSection, SolverSection>(Solvers, id => new SolverSection(id));

    public HeatSolverSection AddHeatSolver() => AddIndexedSection<HeatSolverSection, SolverSection>(Solvers, id => new HeatSolverSection(id));

    public SaveLineSolverSection AddSaveLineSolver() => AddIndexedSection<SaveLineSolverSection, SolverSection>(Solvers, id => new SaveLineSolverSection(id));

    public SaveScalarsSolverSection AddSaveScalarsSolver() => AddIndexedSection<SaveScalarsSolverSection, SolverSection>(Solvers, id => new SaveScalarsSolverSection(id));

    public SaveMaterialsSolverSection AddSaveMaterialsSolver() => AddIndexedSection<SaveMaterialsSolverSection, SolverSection>(Solvers, id => new SaveMaterialsSolverSection(id));

    public EquationSection AddEquation() => AddIndexedSection<EquationSection, EquationSection>(Equations, id => new EquationSection(id));

    public InitialConditionSection AddInitialCondition() => AddIndexedSection<InitialConditionSection, InitialConditionSection>(InitialConditions, id => new InitialConditionSection(id));

    public BoundaryConditionSection AddBoundaryCondition() => AddIndexedSection<BoundaryConditionSection, BoundaryConditionSection>(BoundaryConditions, id => new BoundaryConditionSection(id));

    public string Serialize()
    {
        Validate();

        var builder = new StringBuilder();

        Header.WriteTo(builder);

        if (MatcScripts.Count > 0)
        {
            foreach (var script in MatcScripts)
            {
                builder.AppendLine($"$ {script}");
            }

            builder.AppendLine();
        }

        Simulation.WriteTo(builder);
        Constants.WriteTo(builder);

        WriteSectionCollection(builder, Materials);
        WriteSectionCollection(builder, Bodies);
        WriteSectionCollection(builder, Solvers);
        WriteSectionCollection(builder, Equations);
        WriteSectionCollection(builder, InitialConditions);
        WriteSectionCollection(builder, BoundaryConditions);

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    public void Save(string filePath, Encoding? encoding = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        var content = Serialize();
        var outputEncoding = encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        File.WriteAllText(filePath, content, outputEncoding);
    }

    public void Validate()
    {
        Header.Validate(this);
        Simulation.Validate(this);
        Constants.Validate(this);

        ValidateSections(Materials);
        ValidateSections(Bodies);
        ValidateSections(Solvers);
        ValidateSections(Equations);
        ValidateSections(InitialConditions);
        ValidateSections(BoundaryConditions);
    }

    private static TCreated AddIndexedSection<TCreated, TStored>(IList<TStored> list, Func<int, TCreated> factory)
        where TCreated : TStored
        where TStored : IndexedSifSection
    {
        var nextId = list.Count == 0 ? 1 : list.Max(section => section.Id) + 1;
        var section = factory(nextId);
        list.Add(section);
        return section;
    }

    private static void WriteSectionCollection<TSection>(StringBuilder builder, IEnumerable<TSection> sections)
        where TSection : SifSection
    {
        foreach (var section in sections)
        {
            section.WriteTo(builder);
        }
    }

    private void ValidateSections<TSection>(IEnumerable<TSection> sections)
        where TSection : SifSection
    {
        foreach (var section in sections)
        {
            section.Validate(this);
        }
    }
}
