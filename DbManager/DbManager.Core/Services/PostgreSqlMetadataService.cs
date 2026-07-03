using System.Diagnostics;
using DbManager.Core.Interfaces;
using DbManager.Core.Models;
using Npgsql;

namespace DbManager.Core.Services;

public class PostgreSqlMetadataService : IDbMetadataService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PostgreSqlMetadataService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private static string Sch(string? schema) => string.IsNullOrWhiteSpace(schema) ? "public" : schema;

    public async Task<List<string>> GetDatabasesAsync(string connectionString)
    {
        var result = new List<string>();
        try
        {
            using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT datname FROM pg_database WHERE datistemplate=false", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PostgreSQL获取数据库列表失败: {ex.Message}");
        }
        return result;
    }

    public async Task<List<string>> GetSchemasAsync(string connectionString, string database)
    {
        var result = new List<string>();
        try
        {
            using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT schema_name FROM information_schema.schemata WHERE schema_name NOT LIKE 'pg_%' AND schema_name <> 'information_schema' ORDER BY schema_name", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PostgreSQL获取Schema列表失败: {ex.Message}");
        }
        return result;
    }

    public async Task<List<string>> GetTablesAsync(string connectionString, string database, string? schema = null)
    {
        var result = new List<string>();
        try
        {
            using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT tablename FROM pg_tables WHERE schemaname=@schema ORDER BY tablename", conn);
            cmd.Parameters.AddWithValue("@schema", Sch(schema));
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PostgreSQL获取表列表失败: {ex.Message}");
        }
        return result;
    }

    public async Task<List<string>> GetViewsAsync(string connectionString, string database, string? schema = null)
    {
        var result = new List<string>();
        try
        {
            using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT viewname FROM pg_views WHERE schemaname=@schema ORDER BY viewname", conn);
            cmd.Parameters.AddWithValue("@schema", Sch(schema));
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PostgreSQL获取视图列表失败: {ex.Message}");
        }
        return result;
    }

    public async Task<List<TableColumnModel>> GetColumnsAsync(string connectionString, string database, string tableName, string? schema = null)
    {
        var result = new List<TableColumnModel>();
        try
        {
            using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT column_name, data_type, character_maximum_length, is_nullable, column_default, ordinal_position FROM information_schema.columns WHERE table_schema=@schema AND table_name=@tableName ORDER BY ordinal_position", conn);
            cmd.Parameters.AddWithValue("@schema", Sch(schema));
            cmd.Parameters.AddWithValue("@tableName", tableName);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new TableColumnModel
                {
                    ColumnName = reader.GetString(0),
                    DataType = reader.GetString(1),
                    MaxLength = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                    IsNullable = reader.GetString(3) == "YES",
                    DefaultValue = reader.IsDBNull(4) ? null : reader.GetString(4),
                    OrdinalPosition = reader.GetInt32(5),
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PostgreSQL获取字段列表失败: {ex.Message}");
        }
        return result;
    }

    public async Task<List<string>> GetStoredProceduresAsync(string connectionString, string database, string? schema = null)
    {
        var result = new List<string>();
        try
        {
            using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            try
            {
                using var cmd = new NpgsqlCommand(
                    "SELECT proname FROM pg_proc WHERE prokind='p' AND pronamespace=(SELECT oid FROM pg_namespace WHERE nspname=@schema)", conn);
                cmd.Parameters.AddWithValue("@schema", Sch(schema));
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    result.Add(reader.GetString(0));
            }
            catch
            {
                using var cmd = new NpgsqlCommand(
                    "SELECT routine_name FROM information_schema.routines WHERE routine_schema=@schema AND routine_type='PROCEDURE'", conn);
                cmd.Parameters.AddWithValue("@schema", Sch(schema));
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    result.Add(reader.GetString(0));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PostgreSQL获取存储过程列表失败: {ex.Message}");
        }
        return result;
    }

    public async Task<List<string>> GetFunctionsAsync(string connectionString, string database, string? schema = null)
    {
        var result = new List<string>();
        try
        {
            using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            try
            {
                using var cmd = new NpgsqlCommand(
                    "SELECT proname FROM pg_proc WHERE prokind='f' AND pronamespace=(SELECT oid FROM pg_namespace WHERE nspname=@schema)", conn);
                cmd.Parameters.AddWithValue("@schema", Sch(schema));
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    result.Add(reader.GetString(0));
            }
            catch
            {
                using var cmd = new NpgsqlCommand(
                    "SELECT routine_name FROM information_schema.routines WHERE routine_schema=@schema AND routine_type='FUNCTION'", conn);
                cmd.Parameters.AddWithValue("@schema", Sch(schema));
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    result.Add(reader.GetString(0));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PostgreSQL获取函数列表失败: {ex.Message}");
        }
        return result;
    }

    public async Task<List<string>> GetIndexesAsync(string connectionString, string database, string tableName, string? schema = null)
    {
        var result = new List<string>();
        try
        {
            using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT indexname FROM pg_indexes WHERE schemaname=@schema AND tablename=@tableName", conn);
            cmd.Parameters.AddWithValue("@schema", Sch(schema));
            cmd.Parameters.AddWithValue("@tableName", tableName);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PostgreSQL获取索引列表失败: {ex.Message}");
        }
        return result;
    }

    public async Task<string> GetCreateTableSqlAsync(string connectionString, string database, string tableName)
    {
        try
        {
            var columns = await GetColumnsAsync(connectionString, database, tableName);
            var lines = new List<string>();
            foreach (var col in columns)
            {
                var line = $"  {col.ColumnName} {col.DataType}";
                if (col.MaxLength > 0)
                    line += $"({col.MaxLength})";
                line += col.IsNullable ? " NULL" : " NOT NULL";
                if (!string.IsNullOrEmpty(col.DefaultValue))
                    line += $" DEFAULT {col.DefaultValue}";
                lines.Add(line);
            }
            return $"CREATE TABLE public.{tableName} (\n{string.Join(",\n", lines)}\n);";
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
            using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM public.\"{tableName}\"", conn);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(result);
        }
        catch
        {
            return 0;
        }
    }
}
