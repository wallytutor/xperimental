namespace Xl.Elmer.Sif;

public sealed class LinearSystemControl
{
    public string? Solver { get; set; }

    public string? DirectMethod { get; set; }

    public string? IterativeMethod { get; set; }

    public string? Preconditioning { get; set; }

    public int? MaxIterations { get; set; }

    public double? ConvergenceTolerance { get; set; }

    public bool? AbortNotConverged { get; set; }

    public double? IlutTolerance { get; set; }

    public int? ResidualOutput { get; set; }

    public int? PreconditionRecompute { get; set; }

    public int? BiCGstablPolynomialDegree { get; set; }

    internal void Apply(IDictionary<string, SifValue> parameters)
    {
        Set(parameters, "LinearSystemSolver", Solver);
        Set(parameters, "LinearSystemDirectMethod", DirectMethod);
        Set(parameters, "LinearSystemIterativeMethod", IterativeMethod);
        Set(parameters, "LinearSystemPreconditioning", Preconditioning);
        Set(parameters, "LinearSystemMaxIterations", MaxIterations);
        Set(parameters, "LinearSystemConvergenceTolerance", ConvergenceTolerance);
        Set(parameters, "LinearSystemAbortNotConverged", AbortNotConverged);
        Set(parameters, "LinearSystemIlutTolerance", IlutTolerance);
        Set(parameters, "LinearSystemResidualOutput", ResidualOutput);
        Set(parameters, "LinearSystemPreconditionRecompute", PreconditionRecompute);
        Set(parameters, "BiCGstablPolynomialDegree", BiCGstablPolynomialDegree);
    }

    private static void Set(IDictionary<string, SifValue> parameters, string key, object? value)
    {
        if (value is null)
        {
            return;
        }

        parameters[key] = SifValue.From(value);
    }
}

public sealed class NonlinearSystemControl
{
    public int? MaxIterations { get; set; }

    public double? ConvergenceTolerance { get; set; }

    public double? NewtonAfterTolerance { get; set; }

    public int? NewtonAfterIterations { get; set; }

    public double? RelaxationFactor { get; set; }

    internal void Apply(IDictionary<string, SifValue> parameters)
    {
        Set(parameters, "NonlinearSystemMaxIterations", MaxIterations);
        Set(parameters, "NonlinearSystemConvergenceTolerance", ConvergenceTolerance);
        Set(parameters, "NonlinearSystemNewtonAfterTolerance", NewtonAfterTolerance);
        Set(parameters, "NonlinearSystemNewtonAfterIterations", NewtonAfterIterations);
        Set(parameters, "NonlinearSystemRelaxationFactor", RelaxationFactor);
    }

    private static void Set(IDictionary<string, SifValue> parameters, string key, object? value)
    {
        if (value is null)
        {
            return;
        }

        parameters[key] = SifValue.From(value);
    }
}