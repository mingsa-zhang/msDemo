using System.Data.Common;
using System.Diagnostics;
using DbManager.Common;
using DbManager.Core.Interfaces;
using DbManager.Core.Models;
using Microsoft.Data.Sqlite;

namespace DbManager.Core.Services;

public class SqliteExecuteService : IDbExecuteService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqliteExecuteService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<SqlExecuteResult> ExecuteQueryAsync(string connectionString, string sql, int? timeout = null, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var conn = new SqliteConnection(connectionString);
            await conn.OpenAsync(cancellationToken);
            using var cmd = new SqliteCommand(sql, conn);
            if (timeout.HasValue)
                cmd.CommandTimeout = timeout.Value;

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            var result = new SqlExecuteResult { IsSuccess = true };

            do
            {
                var resultSet = new QueryResultSet();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    resultSet.Columns.Add(reader.GetName(i));
                }

                while (await reader.ReadAsync(cancellationToken))
                {
                    var row = new Dictionary<string, object?>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    resultSet.Rows.Add(row);
                }

                result.ResultSets.Add(resultSet);
            } while (await reader.NextResultAsync(cancellationToken));

            sw.Stop();
            result.ExecutionTimeMs = sw.ElapsedMilliseconds;
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new SqlExecuteResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ExecutionTimeMs = sw.ElapsedMilliseconds
            };
        }
    }

    public async Task<SqlExecuteResult> ExecuteNonQueryAsync(string connectionString, string sql, int? timeout = null, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var conn = new SqliteConnection(connectionString);
            await conn.OpenAsync(cancellationToken);
            using var cmd = new SqliteCommand(sql, conn);
            if (timeout.HasValue)
                cmd.CommandTimeout = timeout.Value;

            var affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
            sw.Stop();
            return new SqlExecuteResult
            {
                IsSuccess = true,
                AffectedRows = affected,
                ExecutionTimeMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new SqlExecuteResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ExecutionTimeMs = sw.ElapsedMilliseconds
            };
        }
    }

    public async Task<PageResult<Dictionary<string, object?>>> ExecutePagedQueryAsync(
        string connectionString, string sql, int pageIndex, int pageSize, int? timeout = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var offset = (pageIndex - 1) * pageSize;
            var pagedSql = $"{sql} LIMIT {pageSize} OFFSET {offset}";
            var countSql = $"SELECT COUNT(*) FROM ({sql}) AS _t";

            using var conn = new SqliteConnection(connectionString);
            await conn.OpenAsync(cancellationToken);

            using var countCmd = new SqliteCommand(countSql, conn);
            if (timeout.HasValue)
                countCmd.CommandTimeout = timeout.Value;
            var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken));

            using var dataCmd = new SqliteCommand(pagedSql, conn);
            if (timeout.HasValue)
                dataCmd.CommandTimeout = timeout.Value;

            using var reader = await dataCmd.ExecuteReaderAsync(cancellationToken);
            var items = new List<Dictionary<string, object?>>();

            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                items.Add(row);
            }

            return new PageResult<Dictionary<string, object?>>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }
        catch
        {
            return new PageResult<Dictionary<string, object?>> { PageIndex = pageIndex, PageSize = pageSize };
        }
    }

    public async Task<bool> TestConnectionAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        try
        {
            using var conn = new SqliteConnection(connectionString);
            await conn.OpenAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
