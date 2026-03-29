using xl_database;
using xl_webapi.Contracts;
using xl_webapi.Endpoints;
using xl_webapi.Models;

var app = new Application(args);
app.Run();

class Application
{
    private WebApplication _app;

    public Application(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddOpenApi();

        ConfigureDatabase(builder);
        ConfigureModelClient(builder);

        _app = builder.Build();
        _app.MapEquipmentEndpoints();
        _app.MapAnalysisResultEndpoints();
        _app.MapModelEndpoints();

        if (_app.Environment.IsDevelopment())
        {
            _app.MapOpenApi();
        }
    }

    private static void ConfigureDatabase(WebApplicationBuilder builder)
    {
        var dataFile = builder.Configuration.GetValue<string>("Database", "sandbox.db");
        var database = new XlDatabase(dataFile);
        builder.Services.AddSingleton(database);
    }

    private static void ConfigureModelClient(WebApplicationBuilder builder)
    {
        var modelClient = builder.Configuration.GetValue<string>("ModelClient", "Ollama");
        var modelConfig = builder.Configuration.GetSection("ModelConfig");

        if (modelClient.Equals("OLLAMA", StringComparison.CurrentCultureIgnoreCase))
        {
            builder.Services.Configure<OllamaOptions>(modelConfig);
            builder.Services.AddHttpClient<ILanguageClient, OllamaClient>();
        } else
        {
            throw new Exception($"Unsupported ModelClient: {modelClient}");
        }
    }

    public void Run() {
        _app.Run();
    }
}
