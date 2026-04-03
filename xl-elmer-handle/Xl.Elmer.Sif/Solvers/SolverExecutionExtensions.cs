namespace Xl.Elmer.Sif;

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
