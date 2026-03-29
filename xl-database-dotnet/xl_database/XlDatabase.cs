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
    public ILiteCollection<Equipment> EquipmentCollection
    {
        get {
            var col = db.GetCollection<Equipment>("equipment");
            col.EnsureIndex(x => x.Id);
            return col;
        }
    }

    public IEnumerable<Equipment> GetAllEquipment()
    {
        return EquipmentCollection.FindAll();
    }

    public bool InsertEquipment(Equipment item)
    {
        if (EquipmentCollection.FindAll().Any(x => x.IsSameAs(item)))
        {
            Console.WriteLine($"""
                An instance of 'Equipment' with Id '{item.Id}'
                already exists. Skipping insert.

                {item}
                """);
            return false;
        }

        EquipmentCollection.Insert(item);
        return true;
    }

    public void UpdateEquipment(Equipment item)
    {
        EquipmentCollection.Update(item);
    }

    public void DeleteEquipment(Equipment item)
    {
        EquipmentCollection.Delete(item.Id);
    }
    #endregion Equipment

    #region AnalysisResults
    public ILiteCollection<AnalysisResult> AnalysisResultsCollection
    {
        get {
            var col = db.GetCollection<AnalysisResult>("analysis_results");
            col.EnsureIndex(x => x.Id);
            return col;
        }
    }

    public IEnumerable<AnalysisResult> GetAllAnalysisResults()
    {
        return AnalysisResultsCollection.FindAll();
    }

    public bool InsertAnalysisResult(AnalysisResult item)
    {
        if (EquipmentCollection.FindById(item.MachineId) == null)
        {
            Console.WriteLine($"""
                No 'Equipment' with Id '{item.MachineId}' exists.
                Cannot insert 'AnalysisResult' with Id '{item.Id}'.
                """);
            return false;
        }

        if (AnalysisResultsCollection.FindAll().Any(x => x.IsSameAs(item)))
        {
            Console.WriteLine($"""
                An instance of 'AnalysisResult' with Id '{item.Id}'
                already exists. Skipping insert.
                """);
            return false;
        }

        AnalysisResultsCollection.Insert(item);
        return true;
    }

    public void UpdateAnalysisResult(AnalysisResult item)
    {
        AnalysisResultsCollection.Update(item);
    }

    public void DeleteAnalysisResult(AnalysisResult item)
    {
        AnalysisResultsCollection.Delete(item.Id);
    }
    #endregion AnalysisResults
}
