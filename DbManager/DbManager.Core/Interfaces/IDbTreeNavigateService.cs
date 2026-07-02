using DbManager.Core.Models;

namespace DbManager.Core.Interfaces;

public interface IDbTreeNavigateService
{
    Task<List<DbTreeNodeModel>> GetConnectionNodesAsync();
    Task<List<DbTreeNodeModel>> GetDatabaseNodesAsync(int connectionId);
    Task<List<DbTreeNodeModel>> GetTableNodesAsync(int connectionId, string database);
    Task<List<DbTreeNodeModel>> GetViewNodesAsync(int connectionId, string database);
    Task<List<DbTreeNodeModel>> GetColumnNodesAsync(int connectionId, string database, string tableName);
    Task<List<DbTreeNodeModel>> GetStoredProcedureNodesAsync(int connectionId, string database);
    Task<List<DbTreeNodeModel>> GetFunctionNodesAsync(int connectionId, string database);
    Task<List<DbTreeNodeModel>> GetIndexNodesAsync(int connectionId, string database, string tableName);
}

public enum TreeNodeType
{
    Group,
    Connection,
    Database,
    TableGroup,
    ViewGroup,
    ProcedureGroup,
    FunctionGroup,
    IndexGroup,
    Table,
    View,
    Column,
    Procedure,
    Function,
    Index
}

public class DbTreeNodeModel
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public TreeNodeType NodeType { get; set; }
    public int ConnectionId { get; set; }
    public string? DatabaseName { get; set; }
    public string? SchemaName { get; set; }
    public string? ObjectName { get; set; }
    public string? TableName { get; set; }
    public string? IconKind { get; set; }
    public string IconColor { get; set; } = "#673AB7";
    public bool IsExpanded { get; set; }
    public bool IsLoaded { get; set; }
    public List<DbTreeNodeModel> Children { get; set; } = new();
}