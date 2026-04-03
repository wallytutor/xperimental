namespace Xl.Elmer.Sif;

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
