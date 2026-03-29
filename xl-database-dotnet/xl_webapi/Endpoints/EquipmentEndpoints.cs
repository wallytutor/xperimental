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

public static class EquipmentEndpointsMetadata
{
    public static RouteHandlerBuilder WithGetAllEquipmentDocs(this RouteHandlerBuilder builder)
    {
        return builder
            .WithName("GetAllEquipment")
            .WithSummary("List all equipment")
            .WithDescription("Returns all equipment items currently stored in the database.")
            .Produces<IEnumerable<Equipment>>(StatusCodes.Status200OK);
    }

    public static RouteHandlerBuilder WithCreateEquipmentDocs(this RouteHandlerBuilder builder)
    {
        return builder
            .WithName("CreateEquipment")
            .WithSummary("Create equipment")
            .WithDescription("Adds a new equipment item. Example body: { \"name\": \"SEM\", \"model\": \"Model XYZ\", \"manufacturer\": \"SEM Corp\", \"serialNumber\": \"12345\" }. Returns conflict if an equivalent item already exists.")
            .Accepts<EquipmentRequest>("application/json")
            .Produces<Equipment>(StatusCodes.Status201Created)
            .Produces<string>(StatusCodes.Status409Conflict);
    }

    public static RouteHandlerBuilder WithUpdateEquipmentDocs(this RouteHandlerBuilder builder)
    {
        return builder
            .WithName("UpdateEquipment")
            .WithSummary("Update equipment")
            .WithDescription("Updates an existing equipment item by id. Example body: { \"name\": \"SEM\", \"model\": \"Model XYZ\", \"manufacturer\": \"SEM Corp\", \"serialNumber\": \"12345\" }.")
            .Accepts<EquipmentRequest>("application/json")
            .Produces<Equipment>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    public static RouteHandlerBuilder WithDeleteEquipmentDocs(this RouteHandlerBuilder builder)
    {
        return builder
            .WithName("DeleteEquipment")
            .WithSummary("Delete equipment")
            .WithDescription("Deletes an equipment item by id.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }
}