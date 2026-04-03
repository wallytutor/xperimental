namespace Xl.Elmer.Sif;

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
