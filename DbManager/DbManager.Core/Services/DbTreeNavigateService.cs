using DbManager.Core.Adapters;
using DbManager.Core.Enums;
using DbManager.Core.Interfaces;
using DbManager.Core.Models;

namespace DbManager.Core.Services;

public class DbTreeNavigateService : IDbTreeNavigateService
{
    private readonly DbConnectionManageService _connectionService;
    private readonly DbMetadataServiceFactory _metadataFactory;

    public DbTreeNavigateService(DbConnectionManageService connectionService, DbMetadataServiceFactory metadataFactory)
    {
        _connectionService = connectionService;
        _metadataFactory = metadataFactory;
    }

    public async Task<List<DbTreeNodeModel>> GetConnectionNodesAsync()
    {
        var connections = await _connectionService.GetAllConnectionsAsync();
        return connections.Select(c => new DbTreeNodeModel
        {
            DisplayName = c.Name,
            NodeType = TreeNodeType.Connection,
            ConnectionId = c.Id,
            IconKind = GetDbIcon(c.DbType),
            IconColor = GetDbIconColor(c.DbType)
        }).ToList();
    }

    public async Task<List<DbTreeNodeModel>> GetDatabaseNodesAsync(int connectionId)
    {
        var conn = await _connectionService.GetConnectionByIdAsync(connectionId);
        if (conn == null) return new();

        var service = _metadataFactory.Create(conn.DbType);
        var connectionString = BuildDecryptedConnectionString(conn);
        var databases = await service.GetDatabasesAsync(connectionString);

        return databases.Select(db => new DbTreeNodeModel
        {
            DisplayName = db,
            NodeType = TreeNodeType.Database,
            ConnectionId = connectionId,
            DatabaseName = db,
            IconKind = "Database"
        }).ToList();
    }

    public async Task<List<DbTreeNodeModel>> GetTableNodesAsync(int connectionId, string database)
    {
        var conn = await _connectionService.GetConnectionByIdAsync(connectionId);
        if (conn == null) return new();

        var service = _metadataFactory.Create(conn.DbType);
        var connectionString = BuildDecryptedConnectionString(conn);
        var tables = await service.GetTablesAsync(connectionString, database);

        return tables.Select(t => new DbTreeNodeModel
        {
            DisplayName = t,
            NodeType = TreeNodeType.Table,
            ConnectionId = connectionId,
            DatabaseName = database,
            ObjectName = t,
            IconKind = "Table"
        }).ToList();
    }

    public async Task<List<DbTreeNodeModel>> GetViewNodesAsync(int connectionId, string database)
    {
        var conn = await _connectionService.GetConnectionByIdAsync(connectionId);
        if (conn == null) return new();

        var service = _metadataFactory.Create(conn.DbType);
        var connectionString = BuildDecryptedConnectionString(conn);
        var views = await service.GetViewsAsync(connectionString, database);

        return views.Select(v => new DbTreeNodeModel
        {
            DisplayName = v,
            NodeType = TreeNodeType.View,
            ConnectionId = connectionId,
            DatabaseName = database,
            ObjectName = v,
            IconKind = "Eye"
        }).ToList();
    }

    public async Task<List<DbTreeNodeModel>> GetColumnNodesAsync(int connectionId, string database, string tableName)
    {
        var conn = await _connectionService.GetConnectionByIdAsync(connectionId);
        if (conn == null) return new();

        var service = _metadataFactory.Create(conn.DbType);
        var connectionString = BuildDecryptedConnectionString(conn);
        var columns = await service.GetColumnsAsync(connectionString, database, tableName);

        return columns.Select(c => new DbTreeNodeModel
        {
            DisplayName = $"{c.ColumnName} ({c.DataType})",
            NodeType = TreeNodeType.Column,
            ConnectionId = connectionId,
            DatabaseName = database,
            ObjectName = c.ColumnName,
            IconKind = c.IsPrimaryKey ? "Key" : "FormatText"
        }).ToList();
    }

    public async Task<List<DbTreeNodeModel>> GetStoredProcedureNodesAsync(int connectionId, string database)
    {
        var conn = await _connectionService.GetConnectionByIdAsync(connectionId);
        if (conn == null) return new();

        var service = _metadataFactory.Create(conn.DbType);
        var connectionString = BuildDecryptedConnectionString(conn);
        var procedures = await service.GetStoredProceduresAsync(connectionString, database);

        return procedures.Select(p => new DbTreeNodeModel
        {
            DisplayName = p,
            NodeType = TreeNodeType.Procedure,
            ConnectionId = connectionId,
            DatabaseName = database,
            ObjectName = p,
            IconKind = "Cog"
        }).ToList();
    }

    public async Task<List<DbTreeNodeModel>> GetFunctionNodesAsync(int connectionId, string database)
    {
        var conn = await _connectionService.GetConnectionByIdAsync(connectionId);
        if (conn == null) return new();

        var service = _metadataFactory.Create(conn.DbType);
        var connectionString = BuildDecryptedConnectionString(conn);
        var functions = await service.GetFunctionsAsync(connectionString, database);

        return functions.Select(f => new DbTreeNodeModel
        {
            DisplayName = f,
            NodeType = TreeNodeType.Function,
            ConnectionId = connectionId,
            DatabaseName = database,
            ObjectName = f,
            IconKind = "Function"
        }).ToList();
    }

    private static string BuildDecryptedConnectionString(DbConnectionModel conn)
    {
        return DbConnStringBuilder.BuildDecryptedConnectionString(conn);
    }

    private static string GetDbIcon(DbTypeEnum dbType) => dbType switch
    {
        DbTypeEnum.MySql => "Database",
        DbTypeEnum.MariaDB => "Database",
        DbTypeEnum.SqlServer => "DatabaseOutline",
        DbTypeEnum.PostgreSQL => "DatabaseSearch",
        DbTypeEnum.Oracle => "DatabaseEye",
        DbTypeEnum.SQLite => "FileDatabase",
        DbTypeEnum.MongoDB => "DatabaseRefresh",
        DbTypeEnum.Redis => "LightningBolt",
        DbTypeEnum.DB2 => "DatabaseClock",
        _ => "Database"
    };

    private static string GetDbIconColor(DbTypeEnum dbType) => dbType switch
    {
        DbTypeEnum.MySql or DbTypeEnum.MariaDB => "#00758F",
        DbTypeEnum.SqlServer => "#CC2927",
        DbTypeEnum.PostgreSQL => "#336791",
        DbTypeEnum.Oracle => "#F80000",
        DbTypeEnum.SQLite => "#003B57",
        DbTypeEnum.MongoDB => "#47A248",
        DbTypeEnum.Redis => "#DC382D",
        DbTypeEnum.DB2 => "#054ADA",
        _ => "#673AB7"
    };

    public async Task<List<DbTreeNodeModel>> GetIndexNodesAsync(int connectionId, string database, string tableName)
    {
        var conn = await _connectionService.GetConnectionByIdAsync(connectionId);
        if (conn == null) return new List<DbTreeNodeModel>();

        var metadataService = _metadataFactory.Create(conn.DbType);
        var connectionString = DbConnStringBuilder.BuildDecryptedConnectionString(conn);
        var indexes = await metadataService.GetIndexesAsync(connectionString, database, tableName);

        return indexes.Select(idx => new DbTreeNodeModel
        {
            DisplayName = idx,
            NodeType = TreeNodeType.Index,
            IconKind = "KeyVariant",
            IconColor = "#FF9800",
            ConnectionId = connectionId,
            DatabaseName = database,
            TableName = tableName
        }).ToList();
    }
}