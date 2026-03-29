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

public record OllamaGenerateRequest(string Prompt);

public record OllamaPullRequest();
