using xl_database;

var builder = WebApplication.CreateBuilder(args);
var config  = builder.Configuration;

var dataFile  = config.GetValue<string>("Database") ?? "sandbox.db";
var database  = new XlDatabase(dataFile);
var ollamaCnf = config.GetSection("Ollama");

builder.Services.AddOpenApi();
builder.Services.AddSingleton(database);
builder.Services.Configure<OllamaOptions>(ollamaCnf);
builder.Services.AddHttpClient<OllamaClient>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapEquipmentEndpoints();
app.MapAnalysisResultEndpoints();
app.MapOllamaEndpoints();
app.Run();
