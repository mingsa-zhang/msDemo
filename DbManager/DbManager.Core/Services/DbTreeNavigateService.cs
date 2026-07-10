using DbManager.Core.Adapters;
using DbManager.Core.Enums;
using DbManager.Core.Interfaces;
using DbManager.Core.Models;

namespace DbManager.Core.Services;

public class DbTreeNavigateService : IDbTreeNavigateService
{
    private readonly DbConnectionManageService _connectionService;
    private readonly DbMetadataServiceFactory _metadataFactory;
    private readonly MetadataCache _cache = new();

    public DbTreeNavigateService(DbConnectionManageService connectionService, DbMetadataServiceFactory metadataFactory)
    {
        _connectionService = connectionService;
        _metadataFactory = metadataFactory;
    }

    /// <summary>
    /// 组合缓存 key（连接号打头，便于按连接前缀失效）。
    /// </summary>
    private static string Key(int connectionId, string kind, string? database = null, string? schema = null, string? table = null)
        => $"{connectionId}|{kind}|{database}|{schema}|{table}";

    /// <summary>
    /// 失效某连接下的全部元数据缓存（单节点刷新时调用）。
    /// </summary>
    public void InvalidateConnection(int connectionId) => _cache.InvalidateByPrefix($"{connectionId}|");

    public async Task<List<DbTreeNodeModel>> GetConnectionNodesAsync()
    {
        var connections = await _connectionService.GetAllConnectionsAsync();
        return connections.Select(c => new DbTreeNodeModel
        {
            DisplayName = c.Name,
            NodeType = TreeNodeType.Connection,
            DbType = c.DbType,
            ConnectionId = c.Id,
            IconKind = GetDbIcon(c.DbType),
            IconColor = GetDbIconColor(c.DbType)
        }).ToList();
    }

    public async Task<List<DbTreeNodeModel>> GetDatabaseNodesAsync(int connectionId)
    {
        var conn = await _connectionService.GetConnectionByIdAsync(connectionId);
        if (conn == null) return new();

        var connectionString = BuildDecryptedConnectionString(conn);

        // MongoDB 走专用服务列库
        if (conn.DbType == DbTypeEnum.MongoDB)
        {
            var mongoDbs = await _cache.GetOrAddAsync(Key(connectionId, "db"),
                () => new MongoService().ListDatabasesAsync(connectionString));
            return mongoDbs.Select(db => new DbTreeNodeModel
            {
                DisplayName = db,
                NodeType = TreeNodeType.Database,
                DbType = DbTypeEnum.MongoDB,
                ConnectionId = connectionId,
                DatabaseName = db,
                IconKind = "Database"
            }).ToList();
        }

        var service = _metadataFactory.Create(conn.DbType);
        var databases = await _cache.GetOrAddAsync(Key(connectionId, "db"),
            () => service.GetDatabasesAsync(connectionString));

        return databases.Select(db => new DbTreeNodeModel
        {
            DisplayName = db,
            NodeType = TreeNodeType.Database,
            DbType = conn.DbType,
            ConnectionId = connectionId,
            DatabaseName = db,
            IconKind = "Database"
        }).ToList();
    }

    public async Task<List<DbTreeNodeModel>> GetCollectionNodesAsync(int connectionId, string database)
    {
        var conn = await _connectionService.GetConnectionByIdAsync(connectionId);
        if (conn == null) return new();

        var connectionString = BuildDecryptedConnectionString(conn);
        var collections = await _cache.GetOrAddAsync(Key(connectionId, "collections", database),
            () => new MongoService().ListCollectionsAsync(connectionString, database));

        return collections.Select(c => new DbTreeNodeModel
        {
            DisplayName = c,
            NodeType = TreeNodeType.Collection,
            DbType = DbTypeEnum.MongoDB,
            ConnectionId = connectionId,
            DatabaseName = database,
            ObjectName = c,
            IconKind = "FileDocumentOutline",
            IconColor = "#47A248"
        }).ToList();
    }

    public async Task<List<DbTreeNodeModel>> GetSchemaNodesAsync(int connectionId, string database)
    {
        var conn = await _connectionService.GetConnectionByIdAsync(connectionId);
        if (conn == null) return new();

        var service = _metadataFactory.Create(conn.DbType);
        var connectionString = BuildDecryptedConnectionString(conn);
        var schemas = await _cache.GetOrAddAsync(Key(connectionId, "schema", database),
            () => service.GetSchemasAsync(connectionString, database));

        return schemas.Select(s => new DbTreeNodeModel
        {
            DisplayName = s,
            NodeType = TreeNodeType.Schema,
            ConnectionId = connectionId,
            DatabaseName = database,
            SchemaName = s,
            IconKind = "FolderTableOutline",
            IconColor = "#795548"
        }).ToList();
    }

    public async Task<List<DbTreeNodeModel>> GetTableNodesAsync(int connectionId, string database, string? schema = null)
    {
        var conn = await _connectionService.GetConnectionByIdAsync(connectionId);
        if (conn == null) return new();

        var service = _metadataFactory.Create(conn.DbType);
        var connectionString = BuildDecryptedConnectionString(conn);
        var tables = await _cache.GetOrAddAsync(Key(connectionId, "tables", database, schema),
            () => service.GetTablesAsync(connectionString, database, schema));

        return tables.Select(t => new DbTreeNodeModel
        {
            DisplayName = t,
            NodeType = TreeNodeType.Table,
            ConnectionId = connectionId,
            DatabaseName = database,
            SchemaName = schema,
            ObjectName = t,
            IconKind = "Table"
        }).ToList();
    }

    public async Task<List<DbTreeNodeModel>> GetViewNodesAsync(int connectionId, string database, string? schema = null)
    {
        var conn = await _connectionService.GetConnectionByIdAsync(connectionId);
        if (conn == null) return new();

        var service = _metadataFactory.Create(conn.DbType);
        var connectionString = BuildDecryptedConnectionString(conn);
        var views = await _cache.GetOrAddAsync(Key(connectionId, "views", database, schema),
            () => service.GetViewsAsync(connectionString, database, schema));

        return views.Select(v => new DbTreeNodeModel
        {
            DisplayName = v,
            NodeType = TreeNodeType.View,
            ConnectionId = connectionId,
            DatabaseName = database,
            SchemaName = schema,
            ObjectName = v,
            IconKind = "Eye"
        }).ToList();
    }

    public async Task<List<DbTreeNodeModel>> GetColumnNodesAsync(int connectionId, string database, string tableName, string? schema = null)
    {
        var conn = await _connectionService.GetConnectionByIdAsync(connectionId);
        if (conn == null) return new();

        var service = _metadataFactory.Create(conn.DbType);
        var connectionString = BuildDecryptedConnectionString(conn);
        var columns = await _cache.GetOrAddAsync(Key(connectionId, "columns", database, schema, tableName),
            () => service.GetColumnsAsync(connectionString, database, tableName, schema));

        return columns.Select(c => new DbTreeNodeModel
        {
            DisplayName = $"{c.ColumnName} ({c.DataType})",
            NodeType = TreeNodeType.Column,
            ConnectionId = connectionId,
            DatabaseName = database,
            SchemaName = schema,
            ObjectName = c.ColumnName,
            IconKind = c.IsPrimaryKey ? "Key" : "FormatText"
        }).ToList();
    }

    public async Task<List<DbTreeNodeModel>> GetStoredProcedureNodesAsync(int connectionId, string database, string? schema = null)
    {
        var conn = await _connectionService.GetConnectionByIdAsync(connectionId);
        if (conn == null) return new();

        var service = _metadataFactory.Create(conn.DbType);
        var connectionString = BuildDecryptedConnectionString(conn);
        var procedures = await _cache.GetOrAddAsync(Key(connectionId, "procs", database, schema),
            () => service.GetStoredProceduresAsync(connectionString, database, schema));

        return procedures.Select(p => new DbTreeNodeModel
        {
            DisplayName = p,
            NodeType = TreeNodeType.Procedure,
            ConnectionId = connectionId,
            DatabaseName = database,
            SchemaName = schema,
            ObjectName = p,
            IconKind = "Cog"
        }).ToList();
    }

    public async Task<List<DbTreeNodeModel>> GetFunctionNodesAsync(int connectionId, string database, string? schema = null)
    {
        var conn = await _connectionService.GetConnectionByIdAsync(connectionId);
        if (conn == null) return new();

        var service = _metadataFactory.Create(conn.DbType);
        var connectionString = BuildDecryptedConnectionString(conn);
        var functions = await _cache.GetOrAddAsync(Key(connectionId, "funcs", database, schema),
            () => service.GetFunctionsAsync(connectionString, database, schema));

        return functions.Select(f => new DbTreeNodeModel
        {
            DisplayName = f,
            NodeType = TreeNodeType.Function,
            ConnectionId = connectionId,
            DatabaseName = database,
            SchemaName = schema,
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

    public async Task<List<DbTreeNodeModel>> GetIndexNodesAsync(int connectionId, string database, string tableName, string? schema = null)
    {
        var conn = await _connectionService.GetConnectionByIdAsync(connectionId);
        if (conn == null) return new List<DbTreeNodeModel>();

        var metadataService = _metadataFactory.Create(conn.DbType);
        var connectionString = DbConnStringBuilder.BuildDecryptedConnectionString(conn);
        var indexes = await _cache.GetOrAddAsync(Key(connectionId, "indexes", database, schema, tableName),
            () => metadataService.GetIndexesAsync(connectionString, database, tableName, schema));

        return indexes.Select(idx => new DbTreeNodeModel
        {
            DisplayName = idx,
            NodeType = TreeNodeType.Index,
            IconKind = "KeyVariant",
            IconColor = "#FF9800",
            ConnectionId = connectionId,
            DatabaseName = database,
            SchemaName = schema,
            TableName = tableName
        }).ToList();
    }

    public async Task<List<DbTreeNodeModel>> GetForeignKeyNodesAsync(int connectionId, string database, string tableName, string? schema = null)
    {
        var conn = await _connectionService.GetConnectionByIdAsync(connectionId);
        if (conn == null) return new List<DbTreeNodeModel>();

        var metadataService = _metadataFactory.Create(conn.DbType);
        var connectionString = DbConnStringBuilder.BuildDecryptedConnectionString(conn);
        var fks = await _cache.GetOrAddAsync(Key(connectionId, "fks", database, schema, tableName),
            () => metadataService.GetForeignKeysAsync(connectionString, database, tableName, schema));

        return fks.Select(fk => new DbTreeNodeModel
        {
            DisplayName = fk,
            NodeType = TreeNodeType.ForeignKey,
            IconKind = "KeyLink",
            IconColor = "#9C27B0",
            ConnectionId = connectionId,
            DatabaseName = database,
            SchemaName = schema,
            TableName = tableName
        }).ToList();
    }
}