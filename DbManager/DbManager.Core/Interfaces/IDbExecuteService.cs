using DbManager.Common;
using DbManager.Core.Models;

namespace DbManager.Core.Interfaces;

public interface IDbExecuteService
{
    Task<SqlExecuteResult> ExecuteQueryAsync(string connectionString, string sql, int? timeout = null, CancellationToken cancellationToken = default);
    Task<SqlExecuteResult> ExecuteNonQueryAsync(string connectionString, string sql, int? timeout = null, CancellationToken cancellationToken = default);
    Task<PageResult<Dictionary<string, object?>>> ExecutePagedQueryAsync(string connectionString, string sql, int pageIndex, int pageSize, int? timeout = null, CancellationToken cancellationToken = default);
    Task<bool> TestConnectionAsync(string connectionString, CancellationToken cancellationToken = default);
}