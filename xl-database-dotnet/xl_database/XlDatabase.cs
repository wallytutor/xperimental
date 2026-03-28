namespace xl_database;

using LiteDB;

public class XlDatabase
{
    private readonly LiteDatabase _database;

    public XlDatabase(string databasePath = "sandbox.db")
    {
        _database = new LiteDatabase(databasePath);
    }
}
