using xl_database;

public static class EquipmentEndpoints
{
    public static IEndpointRouteBuilder MapEquipmentEndpoints(this IEndpointRouteBuilder app)
    {
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

        return app;
    }
}
