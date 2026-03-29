using xl_database;

#region builder
var database = new XlDatabase("equipment.db");
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton(database);
#endregion builder

#region app
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
#endregion app

#region equipment endpoints
var equipment = app.MapGroup("/equipment");

equipment.MapGet("/", (XlDatabase database) =>
{
    return Results.Ok(database.GetAllEquipment());
}).WithGetAllEquipmentDocs();

equipment.MapPost("/", (XlDatabase database, EquipmentRequest request) =>
{
    var item = new Equipment(
        request.Name,
        request.Model,
        request.Manufacturer,
        request.SerialNumber,
        request.Id,
        request.CreatedOn,
        request.UpdatedOn);

    var inserted = database.InsertEquipment(item);
    if (!inserted)
    {
        return Results.Conflict("Equipment already exists.");
    }

    return Results.Created($"/equipment/{item.Id}", item);
}).WithCreateEquipmentDocs();

equipment.MapPut("/{id}", (XlDatabase database, string id, EquipmentRequest request) =>
{
    var current = database.EquipmentCollection.FindById(id);
    if (current is null)
    {
        return Results.NotFound();
    }

    var updated = new Equipment(
        request.Name,
        request.Model,
        request.Manufacturer,
        request.SerialNumber,
        id,
        request.CreatedOn ?? current.CreatedOn,
        DateTime.UtcNow);

    database.UpdateEquipment(updated);
    return Results.Ok(updated);
}).WithUpdateEquipmentDocs();

equipment.MapDelete("/{id}", (XlDatabase database, string id) =>
{
    var current = database.EquipmentCollection.FindById(id);
    if (current is null)
    {
        return Results.NotFound();
    }

    database.DeleteEquipment(current);
    return Results.NoContent();
}).WithDeleteEquipmentDocs();
#endregion equipment endpoints

#region analysis result endpoints
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
#endregion analysis result endpoints

app.Run();

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
