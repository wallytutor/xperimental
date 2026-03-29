namespace xl_database;
using LiteDB;

public class Equipment : IDocument<Equipment>
{
    #region IDocument properties
    public string Id { get; }
    public DateTime CreatedOn { get; }
    public DateTime UpdatedOn { get; }
    #endregion IDocument properties

    public string Name { get; }
    public string Model { get; }
    public string Manufacturer { get; }
    public string SerialNumber { get; }

    public Equipment(
        string name,
        string model,
        string manufacturer,
        string serialNumber,
        string? id = null,
        DateTime? createdOn = null,
        DateTime? updatedOn = null
        )
    {
        Id = id ?? Guid.NewGuid().ToString();

        var now = DateTime.UtcNow;
        CreatedOn = createdOn ?? now;
        UpdatedOn = updatedOn ?? now;

        Name = name;
        Model = model;
        Manufacturer = manufacturer;
        SerialNumber = serialNumber;
    }

    [BsonCtor]
    public Equipment(
        string id,
        DateTime createdOn,
        DateTime updatedOn,
        string name,
        string model,
        string manufacturer,
        string serialNumber
        )
    {
        Id = id;
        CreatedOn = createdOn;
        UpdatedOn = updatedOn;
        Name = name;
        Model = model;
        Manufacturer = manufacturer;
        SerialNumber = serialNumber;
    }

    public bool IsSameAs(Equipment item)
    {
        var sameId    = item.Id    == Id;
        var sameName  = item.Name  == Name;
        var sameModel = item.Model == Model;
        return sameId || (sameName && sameModel);
    }

    public override string ToString()
    {
        return $"""
            Equipment(
                Id           = '{Id}',
                Name         = '{Name}',
                Model        = '{Model}',
                Manufacturer = '{Manufacturer}',
                SerialNumber = '{SerialNumber}'
            )
            """;
    }
}

public class AnalysisResult : IDocument<AnalysisResult>
{
    #region IDocument properties
    public string Id { get; set; }
    public DateTime CreatedOn { get; }
    public DateTime UpdatedOn { get; }
    #endregion IDocument properties

    /// <summary>
    /// The type of analysis performed. See <see cref="AnalysisType"/>.
    /// </summary>
    public AnalysisType Type { get; set; }

    /// <summary>
    /// The machine or equipment used to perform the analysis.
    /// </summary>
    public string MachineId { get; set; }

    /// <summary>
    /// The identifier for the sample that was analyzed. This could be
    /// a unique sample ID, a batch number, or any other identifier that
    /// links the analysis result to a specific sample
    /// </summary>
    public string SampleId { get; set; }

    /// <summary>
    /// The actual data from the analysis. TBD based on actual types.
    /// </summary>
    public string Data { get; set; }

    public AnalysisResult(
        AnalysisType type,
        string machineId,
        string sampleId,
        string data,
        string? id = null,
        DateTime? createdOn = null,
        DateTime? updatedOn = null
        )
    {
        Id = id ?? Guid.NewGuid().ToString();

        var now = DateTime.UtcNow;
        CreatedOn = createdOn ?? now;
        UpdatedOn = updatedOn ?? now;

        Type      = type;
        MachineId = machineId;
        SampleId  = sampleId;
        Data      = data;
    }

    [BsonCtor]
    public AnalysisResult(
        string id,
        DateTime createdOn,
        DateTime updatedOn,
        AnalysisType type,
        string machineId,
        string sampleId,
        string data
        )
    {
        Id = id;
        CreatedOn = createdOn;
        UpdatedOn = updatedOn;
        Type = type;
        MachineId = machineId;
        SampleId = sampleId;
        Data = data;
    }

    public bool IsSameAs(AnalysisResult item)
    {
        var sameId       = item.Id        == Id;
        var sameType     = item.Type      == Type;
        var sameMachine  = item.MachineId == MachineId;
        var sameSample   = item.SampleId  == SampleId;
        return sameId || (sameType && sameMachine && sameSample);
    }

    public override string ToString()
    {
        return $"""
            AnalysisResult(
                Id        = '{Id}',
                Type      = '{Type}',
                MachineId = '{MachineId}',
                SampleId  = '{SampleId}',
            )
            """;
    }
}