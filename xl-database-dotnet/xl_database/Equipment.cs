namespace xl_database;

public class Equipment
{
    public string Id { get; }
    public string Name { get; }
    public string Model { get; }
    public string Manufacturer { get; }
    public string SerialNumber { get; }

    public Equipment(
        string name,
        string model,
        string manufacturer,
        string serialNumber
        )
    {
        Id = Guid.NewGuid().ToString();
        Name = name;
        Model = model;
        Manufacturer = manufacturer;
        SerialNumber = serialNumber;
    }

    public override string ToString()
    {
        return $"""
            Equipment(
                Id           = '{Id}',
                Name         = '{Name}',
                Model        = '{Model}',
                Manufacturer = '{Manufacturer}',
                SerialNumber = '{SerialNumber}'
            )
            """;
    }
}