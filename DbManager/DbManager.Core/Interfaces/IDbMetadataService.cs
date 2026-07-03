using DbManager.Core.Models;

namespace DbManager.Core.Interfaces;

public interface IDbMetadataService
{
    Task<List<string>> GetDatabasesAsync(string connectionString);

    /// <summary>
    /// 获取 Schema 列表。无 Schema 概念的库（MySQL/SQLite）返回空列表。
    /// </summary>
    Task<List<string>> GetSchemasAsync(string connectionString, string database);

    Task<List<string>> GetTablesAsync(string connectionString, string database, string? schema = null);
    Task<List<string>> GetViewsAsync(string connectionString, string database, string? schema = null);
    Task<List<TableColumnModel>> GetColumnsAsync(string connectionString, string database, string tableName, string? schema = null);
    Task<List<string>> GetStoredProceduresAsync(string connectionString, string database, string? schema = null);
    Task<List<string>> GetFunctionsAsync(string connectionString, string database, string? schema = null);
    Task<List<string>> GetIndexesAsync(string connectionString, string database, string tableName, string? schema = null);

    /// <summary>
    /// 获取表的外键，返回形如 "列 → 引用表(引用列)" 的描述列表。无外键概念的库返回空。
    /// </summary>
    Task<List<string>> GetForeignKeysAsync(string connectionString, string database, string tableName, string? schema = null);

    Task<string> GetCreateTableSqlAsync(string connectionString, string database, string tableName);
    Task<long> GetTableRowCountAsync(string connectionString, string database, string tableName);
}
