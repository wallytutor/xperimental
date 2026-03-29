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
