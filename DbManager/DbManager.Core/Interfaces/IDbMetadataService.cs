using DbManager.Core.Models;

namespace DbManager.Core.Interfaces;

public interface IDbMetadataService
{
    Task<List<string>> GetDatabasesAsync(string connectionString);
    Task<List<string>> GetTablesAsync(string connectionString, string database);
    Task<List<string>> GetViewsAsync(string connectionString, string database);
    Task<List<TableColumnModel>> GetColumnsAsync(string connectionString, string database, string tableName);
    Task<List<string>> GetStoredProceduresAsync(string connectionString, string database);
    Task<List<string>> GetFunctionsAsync(string connectionString, string database);
    Task<List<string>> GetIndexesAsync(string connectionString, string database, string tableName);
    Task<string> GetCreateTableSqlAsync(string connectionString, string database, string tableName);
    Task<long> GetTableRowCountAsync(string connectionString, string database, string tableName);
}
