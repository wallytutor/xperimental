using xl_database;

public static class EndpointMetadata
{
    #region Equipment endpoints
    public static RouteHandlerBuilder WithGetAllEquipmentDocs(this RouteHandlerBuilder builder)
    {
        return builder
            .WithName("GetAllEquipment")
            .WithSummary("List all equipment")
            .WithDescription("Returns all equipment items currently stored in the database.")
            .Produces<IEnumerable<Equipment>>(StatusCodes.Status200OK);
    }

    public static RouteHandlerBuilder WithCreateEquipmentDocs(this RouteHandlerBuilder builder)
    {
        return builder
            .WithName("CreateEquipment")
            .WithSummary("Create equipment")
            .WithDescription("Adds a new equipment item. Example body: { \"name\": \"SEM\", \"model\": \"Model XYZ\", \"manufacturer\": \"SEM Corp\", \"serialNumber\": \"12345\" }. Returns conflict if an equivalent item already exists.")
            .Accepts<EquipmentRequest>("application/json")
            .Produces<Equipment>(StatusCodes.Status201Created)
            .Produces<string>(StatusCodes.Status409Conflict);
    }

    public static RouteHandlerBuilder WithUpdateEquipmentDocs(this RouteHandlerBuilder builder)
    {
        return builder
            .WithName("UpdateEquipment")
            .WithSummary("Update equipment")
            .WithDescription("Updates an existing equipment item by id. Example body: { \"name\": \"SEM\", \"model\": \"Model XYZ\", \"manufacturer\": \"SEM Corp\", \"serialNumber\": \"12345\" }.")
            .Accepts<EquipmentRequest>("application/json")
            .Produces<Equipment>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    public static RouteHandlerBuilder WithDeleteEquipmentDocs(this RouteHandlerBuilder builder)
    {
        return builder
            .WithName("DeleteEquipment")
            .WithSummary("Delete equipment")
            .WithDescription("Deletes an equipment item by id.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }
    #endregion Equipment endpoints

    #region AnalysisResult endpoints
    public static RouteHandlerBuilder WithGetAllAnalysisResultsDocs(this RouteHandlerBuilder builder)
    {
        return builder
            .WithName("GetAllAnalysisResults")
            .WithSummary("List all analysis results")
            .WithDescription("Returns all analysis result items currently stored in the database.")
            .Produces<IEnumerable<AnalysisResult>>(StatusCodes.Status200OK);
    }

    public static RouteHandlerBuilder WithCreateAnalysisResultDocs(this RouteHandlerBuilder builder)
    {
        return builder
            .WithName("CreateAnalysisResult")
            .WithSummary("Create analysis result")
            .WithDescription("Adds a new analysis result. Example body: { \"type\": 2, \"machineId\": \"your-equipment-id\", \"sampleId\": \"Sample123\", \"data\": \"Analysis data goes here...\" }. Returns conflict when duplicate data exists or machine id is missing.")
            .Accepts<AnalysisResultRequest>("application/json")
            .Produces<AnalysisResult>(StatusCodes.Status201Created)
            .Produces<string>(StatusCodes.Status409Conflict);
    }

    public static RouteHandlerBuilder WithUpdateAnalysisResultDocs(this RouteHandlerBuilder builder)
    {
        return builder
            .WithName("UpdateAnalysisResult")
            .WithSummary("Update analysis result")
            .WithDescription("Updates an existing analysis result by id. Example body: { \"type\": 2, \"machineId\": \"your-equipment-id\", \"sampleId\": \"Sample123\", \"data\": \"Analysis data goes here...\" }.")
            .Accepts<AnalysisResultRequest>("application/json")
            .Produces<AnalysisResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    public static RouteHandlerBuilder WithDeleteAnalysisResultDocs(this RouteHandlerBuilder builder)
    {
        return builder
            .WithName("DeleteAnalysisResult")
            .WithSummary("Delete analysis result")
            .WithDescription("Deletes an analysis result by id.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }
    #endregion AnalysisResult endpoints

    #region Ollama endpoints
    public static RouteHandlerBuilder WithGenerateDocs(this RouteHandlerBuilder builder)
    {
        return builder
            .WithName("Generate")
            .WithSummary("Generate text from a prompt using Ollama")
            .WithDescription("Sends a prompt to the configured Ollama model and returns the generated response. Example body: { \"prompt\": \"What is SEM?\" }")
            .Accepts<OllamaGenerateRequest>("application/json")
            .Produces(StatusCodes.Status200OK);
    }

    public static RouteHandlerBuilder WithPullDocs(this RouteHandlerBuilder builder)
    {
        return builder
            .WithName("PullModel")
            .WithSummary("Pull Ollama model")
            .WithDescription("Pulls the configured Ollama model to the local runtime.")
            .Accepts<OllamaPullRequest>("application/json")
            .Produces(StatusCodes.Status200OK);
    }
    #endregion Ollama endpoints
}
