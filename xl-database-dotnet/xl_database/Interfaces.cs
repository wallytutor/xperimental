namespace xl_database;

public interface IDocument
{
    string Id { get; }
    DateTime CreatedOn { get; }
    DateTime UpdatedOn { get; }
}

public interface IDocument<TSelf> : IDocument
    where TSelf : IDocument<TSelf>
{
    bool IsSameAs(TSelf item);
}
