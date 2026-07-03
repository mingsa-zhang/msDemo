using System.Diagnostics;
using DbManager.Core.Interfaces;
using DbManager.Core.Models;
using Oracle.ManagedDataAccess.Client;

namespace DbManager.Core.Services;

public class OracleMetadataService : IDbMetadataService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OracleMetadataService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private static readonly HashSet<string> SystemSchemas = new()
    {
        "SYS", "SYSTEM", "DBSNMP", "SYSMAN", "OUTLN", "DIP", "ORACLE_OCM",
        "APPQOSSYS", "DBFS_OWNER", "XS$NULL", "REMOTE_SCHEDULER_AGENT",
        "GSMADMIN_INTERNAL", "GSMUSER", "GSMCATUSER", "SYSBACKUP",
        "SYSDG", "SYSKM", "SYSRAC", "AUDSYS", "OJVMSYS", "WMSYS",
        "XDB", "CTXSYS", "ORDSYS", "ORDPLUGINS", "MDSYS", "LBACSYS",
        "DVSYS", "DV_OWNER", "DV_PUBLIC", "DV_ADMIN", "DV_ACCTMGR",
        "DV_SECMGR", "OLAPSYS", "EXFSYS", "FLOWS_FILES", "APEX_PUBLIC_USER",
        "APEX_030200", "APEX_040000", "APEX_040100", "APEX_040200", "APEX_050000",
        "SCOTT", "HR", "OE", "SH", "PM", "IX"
    };

    // Oracle 的"数据库"节点本身即 Schema（OWNER=USERNAME），故不再单独提供 Schema 子层。
    private static string Owner(string database, string? schema) => string.IsNullOrWhiteSpace(schema) ? database : schema;

    public async Task<List<string>> GetDatabasesAsync(string connectionString)
    {
        try
        {
            var result = new List<string>();
            using var conn = new OracleConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT USERNAME FROM ALL_USERS WHERE ORACLE_MAINTAINED='N' ORDER BY USERNAME";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(0);
                if (!SystemSchemas.Contains(name.ToUpperInvariant()))
                    result.Add(name);
            }
            return result;
        }
        catch (Exception)
        {
            // ORACLE_MAINTAINED 列可能不存在于旧版本，回退到过滤方式
            try
            {
                var result = new List<string>();
                using var conn = new OracleConnection(connectionString);
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT USERNAME FROM ALL_USERS ORDER BY USERNAME";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var name = reader.GetString(0);
                    if (!SystemSchemas.Contains(name.ToUpperInvariant()))
                        result.Add(name);
                }
                return result;
            }
            catch (Exception ex2)
            {
                Debug.WriteLine($"Oracle获取Schema列表失败: {ex2.Message}");
                return new List<string>();
            }
        }
    }

    // Oracle 数据库层已等价于 Schema，返回空表示不再展开二级 Schema。
    public Task<List<string>> GetSchemasAsync(string connectionString, string database)
        => Task.FromResult(new List<string>());

    public async Task<List<string>> GetTablesAsync(string connectionString, string database, string? schema = null)
    {
        try
        {
            var result = new List<string>();
            using var conn = new OracleConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT TABLE_NAME FROM ALL_TABLES WHERE OWNER=:owner ORDER BY TABLE_NAME";
            cmd.Parameters.Add(new OracleParameter(":owner", Owner(database, schema)));
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Oracle获取表列表失败: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<List<string>> GetViewsAsync(string connectionString, string database, string? schema = null)
    {
        try
        {
            var result = new List<string>();
            using var conn = new OracleConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT VIEW_NAME FROM ALL_VIEWS WHERE OWNER=:owner";
            cmd.Parameters.Add(new OracleParameter(":owner", Owner(database, schema)));
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Oracle获取视图列表失败: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<List<TableColumnModel>> GetColumnsAsync(string connectionString, string database, string tableName, string? schema = null)
    {
        try
        {
            var result = new List<TableColumnModel>();
            using var conn = new OracleConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COLUMN_NAME, DATA_TYPE, DATA_LENGTH, NULLABLE, DATA_DEFAULT, COLUMN_ID FROM ALL_TAB_COLUMNS WHERE OWNER=:owner AND TABLE_NAME=:tableName ORDER BY COLUMN_ID";
            cmd.Parameters.Add(new OracleParameter(":owner", Owner(database, schema)));
            cmd.Parameters.Add(new OracleParameter(":tableName", tableName));
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new TableColumnModel
                {
                    ColumnName = reader.GetString(0),
                    DataType = reader.GetString(1),
                    MaxLength = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                    IsNullable = reader.IsDBNull(3) || reader.GetString(3) == "Y",
                    DefaultValue = reader.IsDBNull(4) ? null : reader.GetString(4),
                    OrdinalPosition = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                });
            }
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Oracle获取列信息失败: {ex.Message}");
            return new List<TableColumnModel>();
        }
    }

    public async Task<List<string>> GetStoredProceduresAsync(string connectionString, string database, string? schema = null)
    {
        try
        {
            var result = new List<string>();
            using var conn = new OracleConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT OBJECT_NAME FROM ALL_PROCEDURES WHERE OWNER=:owner AND OBJECT_TYPE='PROCEDURE'";
            cmd.Parameters.Add(new OracleParameter(":owner", Owner(database, schema)));
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Oracle获取存储过程列表失败: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<List<string>> GetFunctionsAsync(string connectionString, string database, string? schema = null)
    {
        try
        {
            var result = new List<string>();
            using var conn = new OracleConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT OBJECT_NAME FROM ALL_PROCEDURES WHERE OWNER=:owner AND OBJECT_TYPE='FUNCTION'";
            cmd.Parameters.Add(new OracleParameter(":owner", Owner(database, schema)));
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Oracle获取函数列表失败: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<List<string>> GetForeignKeysAsync(string connectionString, string database, string tableName, string? schema = null)
    {
        var result = new List<string>();
        try
        {
            using var conn = new OracleConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                @"SELECT acc.COLUMN_NAME, rk.TABLE_NAME AS REF_TABLE, rcc.COLUMN_NAME AS REF_COL
                  FROM ALL_CONSTRAINTS ac
                  JOIN ALL_CONS_COLUMNS acc ON ac.CONSTRAINT_NAME = acc.CONSTRAINT_NAME AND ac.OWNER = acc.OWNER
                  JOIN ALL_CONSTRAINTS rk ON ac.R_CONSTRAINT_NAME = rk.CONSTRAINT_NAME AND ac.R_OWNER = rk.OWNER
                  JOIN ALL_CONS_COLUMNS rcc ON rk.CONSTRAINT_NAME = rcc.CONSTRAINT_NAME AND rk.OWNER = rcc.OWNER AND acc.POSITION = rcc.POSITION
                  WHERE ac.CONSTRAINT_TYPE = 'R' AND ac.OWNER = :owner AND ac.TABLE_NAME = :tableName";
            cmd.Parameters.Add(new OracleParameter(":owner", Owner(database, schema)));
            cmd.Parameters.Add(new OracleParameter(":tableName", tableName));
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add($"{reader.GetString(0)} → {reader.GetString(1)}({reader.GetString(2)})");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Oracle获取外键失败: {ex.Message}");
        }
        return result;
    }

    public async Task<List<string>> GetIndexesAsync(string connectionString, string database, string tableName, string? schema = null)
    {
        try
        {
            var result = new List<string>();
            using var conn = new OracleConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT INDEX_NAME FROM ALL_INDEXES WHERE OWNER=:owner AND TABLE_NAME=:tableName";
            cmd.Parameters.Add(new OracleParameter(":owner", Owner(database, schema)));
            cmd.Parameters.Add(new OracleParameter(":tableName", tableName));
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Oracle获取索引列表失败: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<string> GetCreateTableSqlAsync(string connectionString, string database, string tableName, string? schema = null)
    {
        try
        {
            var columns = await GetColumnsAsync(connectionString, database, tableName, schema);
            var columnDefs = columns.Select(c =>
            {
                var parts = new List<string> { $"\"{c.ColumnName}\"", c.DataType };
                if (c.MaxLength > 0 && IsLengthRequiredType(c.DataType))
                    parts[^1] = $"{c.DataType}({c.MaxLength})";
                if (!c.IsNullable)
                    parts.Add("NOT NULL");
                if (c.DefaultValue != null)
                    parts.Add($"DEFAULT {c.DefaultValue}");
                return string.Join(" ", parts);
            });
            return $"CREATE TABLE \"{database}\".\"{tableName}\" (\n  {string.Join(",\n  ", columnDefs)}\n);";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Oracle获取建表SQL失败: {ex.Message}");
            return string.Empty;
        }
    }

    public async Task<long> GetTableRowCountAsync(string connectionString, string database, string tableName)
    {
        try
        {
            using var conn = new OracleConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM \"{database}\".\"{tableName}\"";
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(result);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Oracle获取行数失败: {ex.Message}");
            return 0;
        }
    }

    private static bool IsLengthRequiredType(string dataType)
    {
        var upper = dataType.ToUpperInvariant();
        return upper is "VARCHAR2" or "NVARCHAR2" or "CHAR" or "NCHAR" or "RAW";
    }
}
