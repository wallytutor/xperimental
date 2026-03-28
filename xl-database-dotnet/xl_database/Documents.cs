namespace xl_database;

public class AnalysisResult
{
    /// <summary>
    /// A unique identifier for the analysis result, typically a GUID
    /// or a combination of sample ID and timestamp.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The identifier for the sample that was analyzed. This could be
    /// a unique sample ID, a batch number, or any other identifier that
    /// links the analysis result to a specific sample
    /// </summary>
    public string SampleId { get; set; }

    /// <summary>
    /// The type of analysis performed. See <see cref="AnalysisType"/>.
    /// </summary>
    public AnalysisType Type { get; set; }

    /// <summary>
    /// The machine or equipment used to perform the analysis.
    /// </summary>
    public Equipment Machine { get; set; }

    /// <summary>
    /// The date and time when the analysis was performed. This should
    /// be stored in a standardized format (e.g., ISO 8601) to ensure
    /// consistency across different systems and time zones.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// The actual data from the analysis. TBD based on actual types.
    /// </summary>
    public string Data { get; set; }

    public AnalysisResult(
        string sampleId,
        AnalysisType type,
        Equipment machine,
        string data
        )
    {
        Id   = Guid.NewGuid().ToString();
        Date = DateTime.UtcNow;

        SampleId = sampleId;
        Type     = type;
        Machine  = machine;
        Data     = data;
    }
}