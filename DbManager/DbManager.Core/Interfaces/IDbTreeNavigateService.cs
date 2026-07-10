using DbManager.Core.Enums;
using DbManager.Core.Models;

namespace DbManager.Core.Interfaces;

public interface IDbTreeNavigateService
{
    Task<List<DbTreeNodeModel>> GetConnectionNodesAsync();
    Task<List<DbTreeNodeModel>> GetDatabaseNodesAsync(int connectionId);
    Task<List<DbTreeNodeModel>> GetSchemaNodesAsync(int connectionId, string database);
    Task<List<DbTreeNodeModel>> GetTableNodesAsync(int connectionId, string database, string? schema = null);
    Task<List<DbTreeNodeModel>> GetViewNodesAsync(int connectionId, string database, string? schema = null);
    Task<List<DbTreeNodeModel>> GetColumnNodesAsync(int connectionId, string database, string tableName, string? schema = null);
    Task<List<DbTreeNodeModel>> GetStoredProcedureNodesAsync(int connectionId, string database, string? schema = null);
    Task<List<DbTreeNodeModel>> GetFunctionNodesAsync(int connectionId, string database, string? schema = null);
    Task<List<DbTreeNodeModel>> GetIndexNodesAsync(int connectionId, string database, string tableName, string? schema = null);
    Task<List<DbTreeNodeModel>> GetForeignKeyNodesAsync(int connectionId, string database, string tableName, string? schema = null);

    /// <summary>
    /// 列出 MongoDB 库下的集合节点。
    /// </summary>
    Task<List<DbTreeNodeModel>> GetCollectionNodesAsync(int connectionId, string database);

    /// <summary>
    /// 列出 MongoDB 集合的索引节点。
    /// </summary>
    Task<List<DbTreeNodeModel>> GetCollectionIndexNodesAsync(int connectionId, string database, string collection);

    /// <summary>
    /// 失效指定连接下的元数据缓存（单节点刷新时调用，确保重新查库）。
    /// </summary>
    void InvalidateConnection(int connectionId);
}

public enum TreeNodeType
{
    Group,
    Connection,
    Database,
    Schema,
    TableGroup,
    ViewGroup,
    ProcedureGroup,
    FunctionGroup,
    ColumnGroup,
    IndexGroup,
    ForeignKeyGroup,
    Table,
    View,
    Column,
    Procedure,
    Function,
    Index,
    ForeignKey,
    Collection
}

public class DbTreeNodeModel
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public TreeNodeType NodeType { get; set; }
    public DbTypeEnum DbType { get; set; }
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