namespace Xl.Elmer.Sif;

public sealed class SaveLineSolverSection : SolverSection
{
    public SaveLineSolverSection(int id) : base(id)
    {
        Equation = "SaveLine";
        ConfigureProcedure("SaveData", "SaveLine");
    }

    public string? Filename
    {
        set => SetParameter(nameof(Filename), value);
    }

    public bool? ParallelReduce
    {
        set => SetParameter("Parallel Reduce", value);
    }

    public string? SaveMask
    {
        set
        {
            if (value is null)
                SetParameter("Save Mask", null);
            else
                SetParameter("Save Mask", SifValue.Raw($"String {SifValue.QuoteLiteral(value)}"));
        }
    }

    public void SetTrackedVariable(int index, string variable)
    {
        if (index < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Variable index must be at least 1.");
        }

        SetRawParameter($"Variable {index}", SifValue.From(variable));
    }
}
