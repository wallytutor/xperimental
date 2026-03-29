namespace xl_webapi.Endpoints;
using xl_webapi.Contracts;
using xl_database;

public static class AnalysisResultEndpoints
{
    public static IEndpointRouteBuilder MapAnalysisResultEndpoints(this IEndpointRouteBuilder app)
    {
        var analysisResults = app.MapGroup("/analysis-results");

        analysisResults.MapGet("/", (XlDatabase database) =>
        {
            return Results.Ok(database.GetAllAnalysisResults());
        }).WithGetAllAnalysisResultsDocs();

        analysisResults.MapPost("/", (XlDatabase database, AnalysisResultRequest request) =>
        {
            var item = new AnalysisResult(
                request.Type,
                request.MachineId,
                request.SampleId,
                request.Data,
                request.Id,
                request.CreatedOn,
                request.UpdatedOn);

            var inserted = database.InsertAnalysisResult(item);
            if (!inserted)
            {
                return Results.Conflict("AnalysisResult already exists or machine does not exist.");
            }

            return Results.Created($"/analysis-results/{item.Id}", item);
        }).WithCreateAnalysisResultDocs();

        analysisResults.MapPut("/{id}", (XlDatabase database, string id, AnalysisResultRequest request) =>
        {
            var current = database.AnalysisResultsCollection.FindById(id);
            if (current is null)
            {
                return Results.NotFound();
            }

            var updated = new AnalysisResult(
                request.Type,
                request.MachineId,
                request.SampleId,
                request.Data,
                id,
                request.CreatedOn ?? current.CreatedOn,
                DateTime.UtcNow);

            database.UpdateAnalysisResult(updated);
            return Results.Ok(updated);
        }).WithUpdateAnalysisResultDocs();

        analysisResults.MapDelete("/{id}", (XlDatabase database, string id) =>
        {
            var current = database.AnalysisResultsCollection.FindById(id);
            if (current is null)
            {
                return Results.NotFound();
            }

            database.DeleteAnalysisResult(current);
            return Results.NoContent();
        }).WithDeleteAnalysisResultDocs();

        return app;
    }
}

public static class AnalysisResultEndpointsMetadata
{
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
}
