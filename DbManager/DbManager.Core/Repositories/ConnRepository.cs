using Dapper;
using DbManager.Core.Interfaces;
using DbManager.Core.Models;
using Microsoft.Data.Sqlite;

namespace DbManager.Core.Repositories;

public class ConnRepository : IConnRepository
{
    private readonly string _dbPath;

    public ConnRepository(string dbPath)
    {
        _dbPath = dbPath;
    }

    private SqliteConnection GetConnection() => new($"Data Source={_dbPath}");

    public async Task InitializeDatabaseAsync()
    {
        using var connection = GetConnection();
        await connection.OpenAsync();

        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS db_conn_group (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Description TEXT,
                SortOrder INTEGER DEFAULT 0,
                CreatedTime TEXT NOT NULL,
                UpdatedTime TEXT
            )");

        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS db_connection (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                DbType INTEGER NOT NULL,
                Host TEXT,
                Port INTEGER,
                UserName TEXT,
                Password TEXT,
                DbName TEXT,
                GroupId INTEGER,
                Description TEXT,
                Color TEXT,
                IsFavorite INTEGER DEFAULT 0,
                ConnectionString TEXT,
                SqliteFilePath TEXT,
                ConnectTimeout INTEGER DEFAULT 30,
                EnableSSL INTEGER DEFAULT 0,
                SslCertPath TEXT,
                MongoAuthDb TEXT,
                RedisPassword TEXT,
                OracleServiceName TEXT,
                UseSsh INTEGER DEFAULT 0,
                SshHost TEXT,
                SshPort INTEGER DEFAULT 22,
                SshUser TEXT,
                SshPassword TEXT,
                SshUseKeyFile INTEGER DEFAULT 0,
                SshKeyPath TEXT,
                SshPassphrase TEXT,
                Charset TEXT,
                UseIntegratedSecurity INTEGER DEFAULT 0,
                InstanceName TEXT,
                PgSchema TEXT,
                PgSslMode TEXT,
                OracleUseSid INTEGER DEFAULT 0,
                RedisDatabase INTEGER DEFAULT 0,
                MongoReplicaSet TEXT,
                MongoDirectConnection INTEGER DEFAULT 0,
                SqliteReadOnly INTEGER DEFAULT 0,
                CreatedTime TEXT NOT NULL,
                UpdatedTime TEXT
            )");

        // 迁移步骤失败不应阻断启动（核心表已建好），仅记录日志
        try
        {
            await MigrateSchemaAsync(connection);
        }
        catch (Exception ex)
        {
            DbManager.Common.LogHelper.Error(ex, "数据库迁移失败，已跳过（不影响核心功能）");
        }
    }

    private static async Task MigrateSchemaAsync(SqliteConnection connection)
    {
        // 自动迁移：为旧表补充缺失列
        var existingCols = (await connection.QueryAsync<string>(
            "SELECT name FROM pragma_table_info('db_connection')")).ToHashSet();

        // 先处理旧列名重命名
        if (existingCols.Contains("SQLiteFilePath") && !existingCols.Contains("SqliteFilePath"))
        {
            await connection.ExecuteAsync("ALTER TABLE db_connection RENAME COLUMN SQLiteFilePath TO SqliteFilePath");
            existingCols.Remove("SQLiteFilePath");
            existingCols.Add("SqliteFilePath");
        }

        var newColumns = new Dictionary<string, string>
        {
            ["SqliteFilePath"] = "TEXT",
            ["ConnectTimeout"] = "INTEGER DEFAULT 30",
            ["EnableSSL"] = "INTEGER DEFAULT 0",
            ["SslCertPath"] = "TEXT",
            ["MongoAuthDb"] = "TEXT",
            ["RedisPassword"] = "TEXT",
            ["OracleServiceName"] = "TEXT",
            ["ConnectionString"] = "TEXT",
            ["Color"] = "TEXT",
            ["IsFavorite"] = "INTEGER DEFAULT 0",
            ["UpdatedTime"] = "TEXT",
            ["UseSsh"] = "INTEGER DEFAULT 0",
            ["SshHost"] = "TEXT",
            ["SshPort"] = "INTEGER DEFAULT 22",
            ["SshUser"] = "TEXT",
            ["SshPassword"] = "TEXT",
            ["SshUseKeyFile"] = "INTEGER DEFAULT 0",
            ["SshKeyPath"] = "TEXT",
            ["SshPassphrase"] = "TEXT",
            ["Charset"] = "TEXT",
            ["UseIntegratedSecurity"] = "INTEGER DEFAULT 0",
            ["InstanceName"] = "TEXT",
            ["PgSchema"] = "TEXT",
            ["PgSslMode"] = "TEXT",
            ["OracleUseSid"] = "INTEGER DEFAULT 0",
            ["RedisDatabase"] = "INTEGER DEFAULT 0",
            ["MongoReplicaSet"] = "TEXT",
            ["MongoDirectConnection"] = "INTEGER DEFAULT 0",
            ["SqliteReadOnly"] = "INTEGER DEFAULT 0",
        };

        foreach (var (colName, colDef) in newColumns)
        {
            if (!existingCols.Contains(colName))
                await connection.ExecuteAsync($"ALTER TABLE db_connection ADD COLUMN {colName} {colDef}");
        }

        var existingGroupCols = (await connection.QueryAsync<string>(
            "SELECT name FROM pragma_table_info('db_conn_group')")).ToHashSet();
        var groupColumns = new Dictionary<string, string>
        {
            ["Description"] = "TEXT",
            ["SortOrder"] = "INTEGER DEFAULT 0",
            ["UpdatedTime"] = "TEXT",
        };

        foreach (var (colName, colDef) in groupColumns)
        {
            if (!existingGroupCols.Contains(colName))
                await connection.ExecuteAsync($"ALTER TABLE db_conn_group ADD COLUMN {colName} {colDef}");
        }

        // 旧表可能有外键约束，需要重建去掉它
        await DropForeignKeyIfNeededAsync(connection);
    }

    private static async Task DropForeignKeyIfNeededAsync(SqliteConnection connection)
    {
        // 检查 db_connection 是否有外键，有则重建表去掉外键
        // 注意：pragma_foreign_key_list 无 count 列，用 COUNT(*) 统计行数
        var fkCount = (await connection.QueryAsync<long>(
            "SELECT COUNT(*) FROM pragma_foreign_key_list('db_connection')")).FirstOrDefault();
        if (fkCount == 0) return;

        // 使用事务保护，防止迁移失败后数据库损坏
        using var transaction = connection.BeginTransaction();

        try
        {
            // 重建表去掉外键
            await connection.ExecuteAsync("ALTER TABLE db_connection RENAME TO db_connection_old;", transaction);

            await connection.ExecuteAsync(@"
                CREATE TABLE db_connection (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    DbType INTEGER NOT NULL,
                    Host TEXT,
                    Port INTEGER,
                    UserName TEXT,
                    Password TEXT,
                    DbName TEXT,
                    GroupId INTEGER,
                    Description TEXT,
                    Color TEXT,
                    IsFavorite INTEGER DEFAULT 0,
                    ConnectionString TEXT,
                    SqliteFilePath TEXT,
                    ConnectTimeout INTEGER DEFAULT 30,
                    EnableSSL INTEGER DEFAULT 0,
                    SslCertPath TEXT,
                    MongoAuthDb TEXT,
                    RedisPassword TEXT,
                    OracleServiceName TEXT,
                    CreatedTime TEXT NOT NULL,
                    UpdatedTime TEXT
                );
            ", transaction);

            // 获取旧表的所有列
            var oldCols = (await connection.QueryAsync<string>(
                "SELECT name FROM pragma_table_info('db_connection_old')", transaction)).ToList();

            // 旧列名 SQLiteFilePath 映射到新列名 SqliteFilePath
            var colMapping = oldCols.Select(c => c == "SQLiteFilePath" ? "SqliteFilePath" : c).ToList();
            var oldColList = string.Join(", ", oldCols);
            var newColList = string.Join(", ", colMapping);

            await connection.ExecuteAsync($"INSERT INTO db_connection ({newColList}) SELECT {oldColList} FROM db_connection_old", transaction);
            await connection.ExecuteAsync("DROP TABLE db_connection_old", transaction);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<DbConnectionModel>> GetAllConnectionsAsync()
    {
        using var connection = GetConnection();
        await connection.OpenAsync();
        return (await connection.QueryAsync<DbConnectionModel>("SELECT * FROM db_connection ORDER BY GroupId, Name")).ToList();
    }

    public async Task<DbConnectionModel?> GetConnectionByIdAsync(int id)
    {
        using var connection = GetConnection();
        await connection.OpenAsync();
        return await connection.QueryFirstOrDefaultAsync<DbConnectionModel>("SELECT * FROM db_connection WHERE Id = @Id", new { Id = id });
    }

    public async Task<DbConnectionModel?> GetConnectionByNameAsync(string name)
    {
        using var connection = GetConnection();
        await connection.OpenAsync();
        return await connection.QueryFirstOrDefaultAsync<DbConnectionModel>("SELECT * FROM db_connection WHERE Name = @Name", new { Name = name });
    }

    public async Task<int> AddConnectionAsync(DbConnectionModel connection)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        return await conn.ExecuteAsync(@"
            INSERT INTO db_connection (Name, DbType, Host, Port, UserName, Password, DbName, GroupId, Description, Color, IsFavorite, ConnectionString, SqliteFilePath, ConnectTimeout, EnableSSL, SslCertPath, MongoAuthDb, RedisPassword, OracleServiceName, UseSsh, SshHost, SshPort, SshUser, SshPassword, SshUseKeyFile, SshKeyPath, SshPassphrase, Charset, UseIntegratedSecurity, InstanceName, PgSchema, PgSslMode, OracleUseSid, RedisDatabase, MongoReplicaSet, MongoDirectConnection, SqliteReadOnly, CreatedTime)
            VALUES (@Name, @DbType, @Host, @Port, @UserName, @Password, @DbName, @GroupId, @Description, @Color, @IsFavorite, @ConnectionString, @SqliteFilePath, @ConnectTimeout, @EnableSSL, @SslCertPath, @MongoAuthDb, @RedisPassword, @OracleServiceName, @UseSsh, @SshHost, @SshPort, @SshUser, @SshPassword, @SshUseKeyFile, @SshKeyPath, @SshPassphrase, @Charset, @UseIntegratedSecurity, @InstanceName, @PgSchema, @PgSslMode, @OracleUseSid, @RedisDatabase, @MongoReplicaSet, @MongoDirectConnection, @SqliteReadOnly, @CreatedTime)", connection);
    }

    public async Task<int> UpdateConnectionAsync(DbConnectionModel connection)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        return await conn.ExecuteAsync(@"
            UPDATE db_connection SET Name=@Name, DbType=@DbType, Host=@Host, Port=@Port, UserName=@UserName, Password=@Password, DbName=@DbName, GroupId=@GroupId, Description=@Description, Color=@Color, IsFavorite=@IsFavorite, ConnectionString=@ConnectionString, SqliteFilePath=@SqliteFilePath, ConnectTimeout=@ConnectTimeout, EnableSSL=@EnableSSL, SslCertPath=@SslCertPath, MongoAuthDb=@MongoAuthDb, RedisPassword=@RedisPassword, OracleServiceName=@OracleServiceName, UseSsh=@UseSsh, SshHost=@SshHost, SshPort=@SshPort, SshUser=@SshUser, SshPassword=@SshPassword, SshUseKeyFile=@SshUseKeyFile, SshKeyPath=@SshKeyPath, SshPassphrase=@SshPassphrase, Charset=@Charset, UseIntegratedSecurity=@UseIntegratedSecurity, InstanceName=@InstanceName, PgSchema=@PgSchema, PgSslMode=@PgSslMode, OracleUseSid=@OracleUseSid, RedisDatabase=@RedisDatabase, MongoReplicaSet=@MongoReplicaSet, MongoDirectConnection=@MongoDirectConnection, SqliteReadOnly=@SqliteReadOnly, UpdatedTime=@UpdatedTime
            WHERE Id=@Id", connection);
    }

    public async Task<int> DeleteConnectionAsync(int id)
    {
        using var connection = GetConnection();
        await connection.OpenAsync();
        return await connection.ExecuteAsync("DELETE FROM db_connection WHERE Id = @Id", new { Id = id });
    }

    public async Task<int> DeleteConnectionsAsync(List<int> ids)
    {
        if (ids == null || ids.Count == 0) return 0;
        using var connection = GetConnection();
        await connection.OpenAsync();
        return await connection.ExecuteAsync("DELETE FROM db_connection WHERE Id IN @Ids", new { Ids = ids });
    }

    public async Task<List<DbConnectionGroupModel>> GetAllGroupsAsync()
    {
        using var connection = GetConnection();
        await connection.OpenAsync();
        return (await connection.QueryAsync<DbConnectionGroupModel>("SELECT * FROM db_conn_group ORDER BY SortOrder, Name")).ToList();
    }

    public async Task<int> AddGroupAsync(DbConnectionGroupModel group)
    {
        using var connection = GetConnection();
        await connection.OpenAsync();
        return await connection.ExecuteAsync("INSERT INTO db_conn_group (Name, Description, SortOrder, CreatedTime) VALUES (@Name, @Description, @SortOrder, @CreatedTime)", group);
    }

    public async Task<int> UpdateGroupAsync(DbConnectionGroupModel group)
    {
        using var connection = GetConnection();
        await connection.OpenAsync();
        return await connection.ExecuteAsync("UPDATE db_conn_group SET Name=@Name, Description=@Description, SortOrder=@SortOrder, UpdatedTime=@UpdatedTime WHERE Id=@Id", group);
    }

    public async Task<int> DeleteGroupAsync(int id)
    {
        using var connection = GetConnection();
        await connection.OpenAsync();
        // 删除分组时把其下连接置为未分组，而不是一并删除连接
        await connection.ExecuteAsync("UPDATE db_connection SET GroupId = 0 WHERE GroupId = @Id", new { Id = id });
        return await connection.ExecuteAsync("DELETE FROM db_conn_group WHERE Id = @Id", new { Id = id });
    }
}