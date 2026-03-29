namespace xl_webapi.Endpoints;
using xl_webapi.Contracts;

public static class ModelEndpoints
{
    private static async Task<IResult> GenerateAsync(
        ILanguageClient languageClient,
        ModelGenerateRequest request)
    {
        var response = await languageClient.GenerateAsync(request.Prompt);
        return Results.Ok(new { request.Prompt, Response = response });
    }

    private static async Task<IResult> PullAsync(
        ILanguageClient languageClient,
        ModelPullRequest request)
    {
        _ = request;
        var status = await languageClient.PullAsync();
        return Results.Ok(new { Status = status });
    }

    public static IEndpointRouteBuilder MapModelEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/model/generate", GenerateAsync)
            .WithGenerateDocs();

        app.MapPost("/model/pull", PullAsync)
            .WithPullDocs();

        return app;
    }
}

public static class ModelEndpointsMetadata
{
    public static RouteHandlerBuilder WithGenerateDocs(this RouteHandlerBuilder builder)
    {
        return builder
            .WithName("Generate")
            .WithSummary("Generate text from a prompt using the model")
            .WithDescription("Sends a prompt to the configured model and returns the generated response. Example body: { \"prompt\": \"What is SEM?\" }")
            .Accepts<ModelGenerateRequest>("application/json")
            .Produces(StatusCodes.Status200OK);
    }

    public static RouteHandlerBuilder WithPullDocs(this RouteHandlerBuilder builder)
    {
        return builder
            .WithName("PullModel")
            .WithSummary("Pull model")
            .WithDescription("Pulls the configured model to the local runtime.")
            .Accepts<ModelPullRequest>("application/json")
            .Produces(StatusCodes.Status200OK);
    }
}