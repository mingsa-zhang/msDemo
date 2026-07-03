using System.Diagnostics;
using DbManager.Core.Interfaces;
using DbManager.Core.Models;
using Microsoft.Data.SqlClient;

namespace DbManager.Core.Services;

public class SqlServerMetadataService : IDbMetadataService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqlServerMetadataService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private static string Sch(string? schema) => string.IsNullOrWhiteSpace(schema) ? "dbo" : schema;

    public async Task<List<string>> GetDatabasesAsync(string connectionString)
    {
        var result = new List<string>();
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand("SELECT name FROM sys.databases WHERE state=0", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }
        }
        catch
        {
        }
        return result;
    }

    public async Task<List<string>> GetSchemasAsync(string connectionString, string database)
    {
        var result = new List<string>();
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            var sql = $"SELECT name FROM [{database}].sys.schemas WHERE name NOT IN ('guest','INFORMATION_SCHEMA','sys') AND name NOT LIKE 'db[_]%' ORDER BY name";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SqlServer获取Schema列表失败: {ex.Message}");
        }
        return result;
    }

    public async Task<List<string>> GetTablesAsync(string connectionString, string database, string? schema = null)
    {
        var result = new List<string>();
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            var sql = $"SELECT TABLE_NAME FROM [{database}].INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' AND TABLE_SCHEMA=@schema";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@schema", Sch(schema));
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }
        }
        catch
        {
        }
        return result;
    }

    public async Task<List<string>> GetViewsAsync(string connectionString, string database, string? schema = null)
    {
        var result = new List<string>();
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            var sql = $"SELECT TABLE_NAME FROM [{database}].INFORMATION_SCHEMA.VIEWS WHERE TABLE_SCHEMA=@schema";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@schema", Sch(schema));
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }
        }
        catch
        {
        }
        return result;
    }

    public async Task<List<TableColumnModel>> GetColumnsAsync(string connectionString, string database, string tableName, string? schema = null)
    {
        var result = new List<TableColumnModel>();
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            var sql = $@"
                SELECT
                    c.COLUMN_NAME,
                    c.DATA_TYPE,
                    c.CHARACTER_MAXIMUM_LENGTH,
                    c.IS_NULLABLE,
                    c.COLUMN_DEFAULT,
                    c.ORDINAL_POSITION,
                    CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IS_PRIMARY_KEY
                FROM [{database}].INFORMATION_SCHEMA.COLUMNS c
                LEFT JOIN (
                    SELECT ku.COLUMN_NAME, ku.TABLE_NAME, ku.TABLE_SCHEMA
                    FROM [{database}].INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                    INNER JOIN [{database}].INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                    WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                ) pk ON c.COLUMN_NAME = pk.COLUMN_NAME AND c.TABLE_NAME = pk.TABLE_NAME AND c.TABLE_SCHEMA = pk.TABLE_SCHEMA
                WHERE c.TABLE_NAME = @tableName AND c.TABLE_SCHEMA = @schema
                ORDER BY c.ORDINAL_POSITION";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@tableName", tableName);
            cmd.Parameters.AddWithValue("@schema", Sch(schema));
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new TableColumnModel
                {
                    ColumnName = reader.GetString(0),
                    DataType = reader.GetString(1),
                    MaxLength = reader.IsDBNull(2) ? 0 : (reader.GetValue(2) as int? ?? 0),
                    IsNullable = reader.GetString(3) == "YES",
                    DefaultValue = reader.IsDBNull(4) ? null : reader.GetString(4),
                    OrdinalPosition = reader.GetInt32(5),
                    IsPrimaryKey = reader.GetInt32(6) == 1
                });
            }
        }
        catch
        {
        }
        return result;
    }

    public async Task<List<string>> GetStoredProceduresAsync(string connectionString, string database, string? schema = null)
    {
        var result = new List<string>();
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            var sql = $"SELECT ROUTINE_NAME FROM [{database}].INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE='PROCEDURE' AND ROUTINE_SCHEMA=@schema";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@schema", Sch(schema));
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }
        }
        catch
        {
        }
        return result;
    }

    public async Task<List<string>> GetFunctionsAsync(string connectionString, string database, string? schema = null)
    {
        var result = new List<string>();
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            var sql = $"SELECT ROUTINE_NAME FROM [{database}].INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE='FUNCTION' AND ROUTINE_SCHEMA=@schema";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@schema", Sch(schema));
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }
        }
        catch
        {
        }
        return result;
    }

    public async Task<List<string>> GetForeignKeysAsync(string connectionString, string database, string tableName, string? schema = null)
    {
        var result = new List<string>();
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            var sql = $@"SELECT cpa.name AS col, rt.name AS ref_table, rpa.name AS ref_col
                        FROM [{database}].sys.foreign_keys fk
                        JOIN [{database}].sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
                        JOIN [{database}].sys.tables t ON fk.parent_object_id = t.object_id
                        JOIN [{database}].sys.schemas s ON t.schema_id = s.schema_id
                        JOIN [{database}].sys.columns cpa ON fkc.parent_object_id = cpa.object_id AND fkc.parent_column_id = cpa.column_id
                        JOIN [{database}].sys.tables rt ON fk.referenced_object_id = rt.object_id
                        JOIN [{database}].sys.columns rpa ON fkc.referenced_object_id = rpa.object_id AND fkc.referenced_column_id = rpa.column_id
                        WHERE t.name=@tableName AND s.name=@schema";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@tableName", tableName);
            cmd.Parameters.AddWithValue("@schema", Sch(schema));
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add($"{reader.GetString(0)} → {reader.GetString(1)}({reader.GetString(2)})");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SqlServer获取外键失败: {ex.Message}");
        }
        return result;
    }

    public async Task<List<string>> GetIndexesAsync(string connectionString, string database, string tableName, string? schema = null)
    {
        var result = new List<string>();
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            var sql = $@"SELECT DISTINCT i.name FROM [{database}].sys.indexes i
                        INNER JOIN [{database}].sys.tables t ON i.object_id=t.object_id
                        INNER JOIN [{database}].sys.schemas s ON t.schema_id=s.schema_id
                        WHERE t.name=@tableName AND s.name=@schema";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@tableName", tableName);
            cmd.Parameters.AddWithValue("@schema", Sch(schema));
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(0))
                    result.Add(reader.GetString(0));
            }
        }
        catch
        {
        }
        return result;
    }

    public async Task<string> GetCreateTableSqlAsync(string connectionString, string database, string tableName)
    {
        try
        {
            var columns = await GetColumnsAsync(connectionString, database, tableName);
            var columnDefs = columns.Select(c =>
            {
                var typeStr = c.MaxLength > 0 ? $"{c.DataType}({c.MaxLength})" : c.DataType;
                return $"[{c.ColumnName}] {typeStr}";
            });
            return $"CREATE TABLE [{tableName}] ({string.Join(", ", columnDefs)})";
        }
        catch
        {
            return string.Empty;
        }
    }

    public async Task<long> GetTableRowCountAsync(string connectionString, string database, string tableName)
    {
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            var sql = $"SELECT SUM(p.rows) FROM [{database}].sys.partitions p INNER JOIN [{database}].sys.tables t ON p.object_id=t.object_id WHERE t.name=@tableName AND p.index_id IN (0,1)";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@tableName", tableName);
            var result = await cmd.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? Convert.ToInt64(result) : 0;
        }
        catch
        {
            return 0;
        }
    }
}
