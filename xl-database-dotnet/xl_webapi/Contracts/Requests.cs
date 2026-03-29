namespace xl_webapi.Contracts;
using xl_database;

public record EquipmentRequest(
    string Name,
    string Model,
    string Manufacturer,
    string SerialNumber,
    string? Id = null,
    DateTime? CreatedOn = null,
    DateTime? UpdatedOn = null
);

public record AnalysisResultRequest(
    AnalysisType Type,
    string MachineId,
    string SampleId,
    string Data,
    string? Id = null,
    DateTime? CreatedOn = null,
    DateTime? UpdatedOn = null
);

public record ModelGenerateRequest(string Prompt);

public record ModelPullRequest();
