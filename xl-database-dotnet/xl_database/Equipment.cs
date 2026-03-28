namespace xl_database;

public class Equipment
{
    public readonly string Id = Guid.NewGuid().ToString();
    public readonly string Name;
    public readonly string Model;
    public readonly string Manufacturer;
    public readonly string SerialNumber;

    public Equipment(
        string name,
        string model,
        string manufacturer,
        string serialNumber
        )
    {
        Name = name;
        Model = model;
        Manufacturer = manufacturer;
        SerialNumber = serialNumber;
    }
}