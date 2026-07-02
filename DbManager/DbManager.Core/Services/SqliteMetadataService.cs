using System.Data.Common;
using System.Diagnostics;
using DbManager.Core.Interfaces;
using DbManager.Core.Models;
using Microsoft.Data.Sqlite;

namespace DbManager.Core.Services;

public class SqliteMetadataService : IDbMetadataService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqliteMetadataService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<string>> GetDatabasesAsync(string connectionString)
    {
        await Task.CompletedTask;
        return ["main"];
    }

    public async Task<List<string>> GetTablesAsync(string connectionString, string database)
    {
        try
        {
            var result = new List<string>();
            using var conn = new SqliteConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new SqliteCommand(
                "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SQLite获取表列表失败: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<List<string>> GetViewsAsync(string connectionString, string database)
    {
        try
        {
            var result = new List<string>();
            using var conn = new SqliteConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new SqliteCommand(
                "SELECT name FROM sqlite_master WHERE type='view' ORDER BY name", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SQLite获取视图列表失败: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<List<TableColumnModel>> GetColumnsAsync(string connectionString, string database, string tableName)
    {
        try
        {
            var result = new List<TableColumnModel>();
            using var conn = new SqliteConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new SqliteCommand($"PRAGMA table_info(\"{tableName}\")", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new TableColumnModel
                {
                    OrdinalPosition = reader.GetInt32(0),
                    ColumnName = reader.GetString(1),
                    DataType = reader.GetString(2),
                    IsNullable = reader.GetInt32(3) == 0,
                    DefaultValue = reader.IsDBNull(4) ? null : reader.GetValue(4)?.ToString(),
                    IsPrimaryKey = reader.GetInt32(5) > 0
                });
            }
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SQLite获取列信息失败: {ex.Message}");
            return new List<TableColumnModel>();
        }
    }

    public async Task<List<string>> GetStoredProceduresAsync(string connectionString, string database)
    {
        await Task.CompletedTask;
        return [];
    }

    public async Task<List<string>> GetFunctionsAsync(string connectionString, string database)
    {
        await Task.CompletedTask;
        return [];
    }

    public async Task<List<string>> GetIndexesAsync(string connectionString, string database, string tableName)
    {
        try
        {
            var result = new List<string>();
            using var conn = new SqliteConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new SqliteCommand($"PRAGMA index_list(\"{tableName}\")", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(1));
            }
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SQLite获取索引列表失败: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<string> GetCreateTableSqlAsync(string connectionString, string database, string tableName)
    {
        try
        {
            using var conn = new SqliteConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new SqliteCommand(
                "SELECT sql FROM sqlite_master WHERE type='table' AND name=@tableName", conn);
            cmd.Parameters.AddWithValue("@tableName", tableName);
            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SQLite获取建表SQL失败: {ex.Message}");
            return string.Empty;
        }
    }

    public async Task<long> GetTableRowCountAsync(string connectionString, string database, string tableName)
    {
        try
        {
            using var conn = new SqliteConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new SqliteCommand($"SELECT COUNT(*) FROM \"{tableName}\"", conn);
            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt64(result) : 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SQLite获取行数失败: {ex.Message}");
            return 0;
        }
    }
}
