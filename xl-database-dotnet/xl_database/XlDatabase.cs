namespace xl_database;

using LiteDB;

public class XlDatabase
{
    private readonly LiteDatabase db;

    public XlDatabase(string databasePath = "sandbox.db")
    {
        db = new LiteDatabase(databasePath);
    }

    #region Equipment
    private static bool EquipmentExists(Equipment entry, Equipment item)
    {
        var sameId    = entry.Id    == item.Id;
        var sameName  = entry.Name  == item.Name;
        var sameModel = entry.Model == item.Model;
        return sameId || (sameName && sameModel);
    }

    public ILiteCollection<Equipment> Equipment
    {
        get {
            var col = db.GetCollection<Equipment>("equipment");
            col.EnsureIndex(x => x.Id);
            return col;
        }
    }

    public IEnumerable<Equipment> GetAllEquipment()
    {
        return Equipment.FindAll();
    }

    public void InsertEquipment(Equipment item)
    {
        if (Equipment.FindAll().Any(x => EquipmentExists(x, item)))
        {
            Console.WriteLine($"""
                An instance of 'Equipment' with Id '{item.Id}'
                already exists. Skipping insert.

                {item}
                """);
            return;
        }

        Equipment.Insert(item);
    }

    public void UpdateEquipment(Equipment item)
    {
        Equipment.Update(item);
    }

    public void DeleteEquipment(Equipment item)
    {
        Equipment.Delete(item.Id);
    }
    #endregion Equipment

    #region AnalysisResults
    public ILiteCollection<AnalysisResult> AnalysisResults
    {
        get {
            var col = db.GetCollection<AnalysisResult>("analysis_results");
            col.EnsureIndex(x => x.Id);
            return col;
        }
    }

    public IEnumerable<AnalysisResult> GetAllAnalysisResults()
    {
        return AnalysisResults.FindAll();
    }

    public void InsertAnalysisResult(AnalysisResult item)
    {
        if (AnalysisResults.Exists(x => x.Id == item.Id))
        {
            Console.WriteLine($"""
                An instance of 'AnalysisResult' with Id '{item.Id}'
                already exists. Skipping insert.
                """);
            return;
        }
        AnalysisResults.Insert(item);
    }

    public void UpdateAnalysisResult(AnalysisResult item)
    {
        AnalysisResults.Update(item);
    }

    public void DeleteAnalysisResult(AnalysisResult item)
    {
        AnalysisResults.Delete(item.Id);
    }

    #endregion AnalysisResults
}
