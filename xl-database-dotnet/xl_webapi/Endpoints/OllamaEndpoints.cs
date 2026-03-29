public static class OllamaEndpoints
{
    public static IEndpointRouteBuilder MapOllamaEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/ollama/generate", async (
            OllamaClient ollama,
            OllamaGenerateRequest request) =>
        {
            var response = await ollama.GenerateAsync(request.Prompt);
            return Results.Ok(new { request.Prompt, Response = response });
        }).WithGenerateDocs();

        app.MapPost("/ollama/pull", async (
            OllamaClient ollama,
            OllamaPullRequest request) =>
        {
            _ = request;
            var status = await ollama.OllamaPull();
            return Results.Ok(new { Status = status });
        }).WithPullDocs();

        return app;
    }
}

public static class OllamaEndpointsMetadata
{
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
}