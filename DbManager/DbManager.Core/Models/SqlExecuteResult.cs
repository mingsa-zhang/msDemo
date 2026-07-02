namespace DbManager.Core.Models;

public class SqlExecuteResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public long ExecutionTimeMs { get; set; }
    public int AffectedRows { get; set; }
    public List<QueryResultSet> ResultSets { get; set; } = new();
}

public class QueryResultSet
{
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public List<string> Columns { get; set; } = new();
}
