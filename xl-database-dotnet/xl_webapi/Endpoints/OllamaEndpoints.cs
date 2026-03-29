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
