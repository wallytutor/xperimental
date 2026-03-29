using xl_database;
using xl_webapi.Contracts;
using xl_webapi.Endpoints;
using xl_webapi.Models;

var builder = WebApplication.CreateBuilder(args);
var config  = builder.Configuration;

var ollamaCnf = config.GetSection("Ollama");
var dataFile  = config.GetValue<string>("Database", "sandbox.db");
var database  = new XlDatabase(dataFile);

builder.Services.AddOpenApi();
builder.Services.AddSingleton(database);
builder.Services.Configure<OllamaOptions>(ollamaCnf);
builder.Services.AddHttpClient<ILanguageClient, OllamaClient>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapEquipmentEndpoints();
app.MapAnalysisResultEndpoints();
app.MapModelEndpoints();
app.Run();
