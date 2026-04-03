namespace Xl.Elmer.Sif;

public enum SolverExecution
{
    Never,
    Always,
    BeforeTimestep,
    AfterTimestep,
    BeforeSaving,
    AfterSaving
}

public static class SolverExecutionExtensions
{
    public static string ToSifKeyword(this SolverExecution execution)
    {
        return execution switch
        {
            SolverExecution.Never => "Never",
            SolverExecution.Always => "Always",
            SolverExecution.BeforeTimestep => "Before Timestep",
            SolverExecution.AfterTimestep => "After Timestep",
            SolverExecution.BeforeSaving => "Before Saving",
            SolverExecution.AfterSaving => "After Saving",
            _ => throw new ArgumentOutOfRangeException(nameof(execution), execution, null)
        };
    }
}

public sealed class HeatSolverSection : SolverSection
{
    public HeatSolverSection(int id) : base(id)
    {
        Equation = "Heat Equation";
        Variable = "Temperature";
        ConfigureProcedure("HeatSolve", "HeatSolver");
    }

    public bool? Stabilize
    {
        set => SetParameter(nameof(Stabilize), value);
    }

    public bool? Bubbles
    {
        set => SetParameter(nameof(Bubbles), value);
    }

    public bool? OptimizeBandwidth
    {
        set => SetParameter("Optimize Bandwidth", value);
    }

    public bool? CalculateLoads
    {
        set => SetParameter("Calculate Loads", value);
    }

    protected internal override void Validate(SifDocument document)
    {
        base.Validate(document);

        if (LinearSystem is null)
        {
            throw new InvalidOperationException($"Solver {Id} ({nameof(HeatSolverSection)}) requires linear system control settings.");
        }

        var linkedEquationIds = document.Equations
            .Where(equation => equation.ActiveSolvers?.Contains(Id) == true)
            .Select(equation => equation.Id)
            .ToHashSet();

        var linkedMaterialIds = document.Bodies
            .Where(body => body.Equation is not null && linkedEquationIds.Contains(body.Equation.Value))
            .Select(body => body.Material)
            .OfType<int>()
            .Distinct()
            .ToArray();

        foreach (var materialId in linkedMaterialIds)
        {
            var material = document.Materials.FirstOrDefault(candidate => candidate.Id == materialId);
            if (material is null)
            {
                throw new InvalidOperationException($"Solver {Id} references Material {materialId}, but that material does not exist.");
            }

            if (!material.HasThermalProperties())
            {
                throw new InvalidOperationException($"Material {materialId} must define Density, HeatConductivity, and HeatCapacity for HeatSolve.");
            }
        }
    }
}

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

public sealed class SaveMaterialsSolverSection : SolverSection
{
    public SaveMaterialsSolverSection(int id) : base(id)
    {
        Equation = "SaveMaterials";
        ConfigureProcedure("SaveData", "SaveMaterials");
    }

    public void SetParameter(int index, string parameterName)
    {
        if (index < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Parameter index must be at least 1.");
        }

        SetRawParameter($"Parameter {index}", SifValue.From(parameterName));
    }
}