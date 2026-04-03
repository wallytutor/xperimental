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

    public MaterialSection AddMaterial() => AddIndexedSection(Materials, id => new MaterialSection(id));

    public BodySection AddBody() => AddIndexedSection(Bodies, id => new BodySection(id));

    public SolverSection AddSolver() => AddIndexedSection(Solvers, id => new SolverSection(id));

    public EquationSection AddEquation() => AddIndexedSection(Equations, id => new EquationSection(id));

    public InitialConditionSection AddInitialCondition() => AddIndexedSection(InitialConditions, id => new InitialConditionSection(id));

    public BoundaryConditionSection AddBoundaryCondition() => AddIndexedSection(BoundaryConditions, id => new BoundaryConditionSection(id));

    public string Serialize()
    {
        var builder = new StringBuilder();

        Header.WriteTo(builder);
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

    private static TSection AddIndexedSection<TSection>(IList<TSection> list, Func<int, TSection> factory)
        where TSection : IndexedSifSection
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
}
