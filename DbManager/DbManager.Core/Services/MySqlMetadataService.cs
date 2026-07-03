using System.Data.Common;
using System.Diagnostics;
using DbManager.Core.Interfaces;
using DbManager.Core.Models;
using MySqlConnector;

namespace DbManager.Core.Services;

public class MySqlMetadataService : IDbMetadataService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlMetadataService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<string>> GetDatabasesAsync(string connectionString)
    {
        try
        {
            var result = new List<string>();
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new MySqlCommand("SHOW DATABASES", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MySQL获取数据库列表失败: {ex.Message}");
            return new List<string>();
        }
    }

    // MySQL/MariaDB 无独立 Schema 概念（database 即 schema），返回空表示不展开二级 Schema。
    public Task<List<string>> GetSchemasAsync(string connectionString, string database)
        => Task.FromResult(new List<string>());

    public async Task<List<string>> GetTablesAsync(string connectionString, string database, string? schema = null)
    {
        try
        {
            var result = new List<string>();
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new MySqlCommand($"SHOW TABLES FROM `{database}`", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MySQL获取表列表失败: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<List<string>> GetViewsAsync(string connectionString, string database, string? schema = null)
    {
        try
        {
            var result = new List<string>();
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new MySqlCommand(
                "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.VIEWS WHERE TABLE_SCHEMA=@database", conn);
            cmd.Parameters.AddWithValue("@database", database);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MySQL获取视图列表失败: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<List<TableColumnModel>> GetColumnsAsync(string connectionString, string database, string tableName, string? schema = null)
    {
        try
        {
            var result = new List<TableColumnModel>();
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new MySqlCommand(
                "SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, COLUMN_KEY, EXTRA, IS_NULLABLE, COLUMN_DEFAULT, COLUMN_COMMENT, ORDINAL_POSITION " +
                "FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=@database AND TABLE_NAME=@tableName " +
                "ORDER BY ORDINAL_POSITION", conn);
            cmd.Parameters.AddWithValue("@database", database);
            cmd.Parameters.AddWithValue("@tableName", tableName);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var columnKey = reader.IsDBNull(3) ? "" : reader.GetString(3);
                var extra = reader.IsDBNull(4) ? "" : reader.GetString(4);
                result.Add(new TableColumnModel
                {
                    ColumnName = reader.GetString(0),
                    DataType = reader.GetString(1),
                    MaxLength = reader.IsDBNull(2) ? 0 : (reader.GetValue(2) as int? ?? 0),
                    IsPrimaryKey = columnKey == "PRI",
                    IsAutoIncrement = extra.Contains("auto_increment", StringComparison.OrdinalIgnoreCase),
                    IsNullable = reader.IsDBNull(5) || reader.GetString(5) == "YES",
                    DefaultValue = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Comment = reader.IsDBNull(7) ? null : reader.GetString(7),
                    OrdinalPosition = reader.IsDBNull(8) ? 0 : reader.GetInt32(8)
                });
            }
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MySQL获取列信息失败: {ex.Message}");
            return new List<TableColumnModel>();
        }
    }

    public async Task<List<string>> GetStoredProceduresAsync(string connectionString, string database, string? schema = null)
    {
        try
        {
            var result = new List<string>();
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new MySqlCommand(
                "SELECT ROUTINE_NAME FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_SCHEMA=@database AND ROUTINE_TYPE='PROCEDURE'", conn);
            cmd.Parameters.AddWithValue("@database", database);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MySQL获取存储过程列表失败: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<List<string>> GetFunctionsAsync(string connectionString, string database, string? schema = null)
    {
        try
        {
            var result = new List<string>();
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new MySqlCommand(
                "SELECT ROUTINE_NAME FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_SCHEMA=@database AND ROUTINE_TYPE='FUNCTION'", conn);
            cmd.Parameters.AddWithValue("@database", database);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MySQL获取函数列表失败: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<List<string>> GetForeignKeysAsync(string connectionString, string database, string tableName, string? schema = null)
    {
        var result = new List<string>();
        try
        {
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new MySqlCommand(
                "SELECT COLUMN_NAME, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE " +
                "WHERE TABLE_SCHEMA=@database AND TABLE_NAME=@tableName AND REFERENCED_TABLE_NAME IS NOT NULL", conn);
            cmd.Parameters.AddWithValue("@database", database);
            cmd.Parameters.AddWithValue("@tableName", tableName);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add($"{reader.GetString(0)} → {reader.GetString(1)}({reader.GetString(2)})");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MySQL获取外键失败: {ex.Message}");
        }
        return result;
    }

    public async Task<List<string>> GetIndexesAsync(string connectionString, string database, string tableName, string? schema = null)
    {
        try
        {
            var result = new List<string>();
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new MySqlCommand(
                "SELECT DISTINCT INDEX_NAME FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA=@database AND TABLE_NAME=@tableName", conn);
            cmd.Parameters.AddWithValue("@database", database);
            cmd.Parameters.AddWithValue("@tableName", tableName);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MySQL获取索引列表失败: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<string> GetCreateTableSqlAsync(string connectionString, string database, string tableName)
    {
        try
        {
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new MySqlCommand($"SHOW CREATE TABLE `{database}`.`{tableName}`", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return reader.GetString(1);
            }
            return string.Empty;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MySQL获取建表SQL失败: {ex.Message}");
            return string.Empty;
        }
    }

    public async Task<long> GetTableRowCountAsync(string connectionString, string database, string tableName)
    {
        try
        {
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new MySqlCommand($"SELECT COUNT(*) FROM `{database}`.`{tableName}`", conn);
            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt64(result) : 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MySQL获取行数失败: {ex.Message}");
            return 0;
        }
    }
}
