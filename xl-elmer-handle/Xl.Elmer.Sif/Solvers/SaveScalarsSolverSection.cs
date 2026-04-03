namespace Xl.Elmer.Sif;

public sealed class SaveScalarsSolverSection : SolverSection
{
    public SaveScalarsSolverSection(int id) : base(id)
    {
        Equation = "SaveScalars";
        ConfigureProcedure("SaveData", "SaveScalars");
    }

    public string? Filename
    {
        set => SetParameter(nameof(Filename), value);
    }

    public bool? FileAppend
    {
        set => SetParameter("File Append", value);
    }

    public bool? ParallelReduce
    {
        set => SetParameter("Parallel Reduce", value);
    }

    public bool? PartitionNumbering
    {
        set => SetParameter("Partition Numbering", value);
    }

    public void SetTrackedVariable(
        int index,
        string variable,
        string? operatorName = null,
        string? maskName = null)
    {
        if (index < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Variable index must be at least 1.");
        }

        SetRawParameter($"Variable {index}", SifValue.From(variable));

        if (operatorName is not null)
        {
            SetRawParameter($"Operator {index}", SifValue.From(operatorName));
        }

        if (maskName is not null)
        {
            SetRawParameter($"Mask Name {index}", SifValue.From(maskName));
        }
    }
}
