namespace xl_database;

using LiteDB;

public class XlDatabase
{
    private readonly LiteDatabase db;

    public ILiteCollection<Equipment> Equipment
    {
        get => db.GetCollection<Equipment>("equipment");
    }

    public ILiteCollection<AnalysisResult> AnalysisResults
    {
        get => db.GetCollection<AnalysisResult>("analysis_results");
    }

    public XlDatabase(string databasePath = "sandbox.db")
    {
        db = new LiteDatabase(databasePath);
    }
}
