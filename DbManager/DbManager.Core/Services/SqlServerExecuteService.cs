using System.Data.Common;
using System.Diagnostics;
using DbManager.Common;
using DbManager.Core.Interfaces;
using DbManager.Core.Models;
using Microsoft.Data.SqlClient;

namespace DbManager.Core.Services;

public class SqlServerExecuteService : IDbExecuteService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqlServerExecuteService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<SqlExecuteResult> ExecuteQueryAsync(string connectionString, string sql, int? timeout = null, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var result = new SqlExecuteResult();
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(cancellationToken);
            using var cmd = new SqlCommand(sql, conn);
            if (timeout.HasValue)
                cmd.CommandTimeout = timeout.Value;

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
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
                    var value = reader.GetValue(i);
                    row[reader.GetName(i)] = value == DBNull.Value ? null : value;
                }
                resultSet.Rows.Add(row);
            }
            result.ResultSets.Add(resultSet);

            while (await reader.NextResultAsync(cancellationToken))
            {
                var nextSet = new QueryResultSet();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    nextSet.Columns.Add(reader.GetName(i));
                }
                while (await reader.ReadAsync(cancellationToken))
                {
                    var row = new Dictionary<string, object?>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var value = reader.GetValue(i);
                        row[reader.GetName(i)] = value == DBNull.Value ? null : value;
                    }
                    nextSet.Rows.Add(row);
                }
                result.ResultSets.Add(nextSet);
            }

            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
        }
        sw.Stop();
        result.ExecutionTimeMs = sw.ElapsedMilliseconds;
        return result;
    }

    public async Task<SqlExecuteResult> ExecuteNonQueryAsync(string connectionString, string sql, int? timeout = null, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var result = new SqlExecuteResult();
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(cancellationToken);
            using var cmd = new SqlCommand(sql, conn);
            if (timeout.HasValue)
                cmd.CommandTimeout = timeout.Value;

            result.AffectedRows = await cmd.ExecuteNonQueryAsync(cancellationToken);
            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
        }
        sw.Stop();
        result.ExecutionTimeMs = sw.ElapsedMilliseconds;
        return result;
    }

    public async Task<PageResult<Dictionary<string, object?>>> ExecutePagedQueryAsync(string connectionString, string sql, int pageIndex, int pageSize, int? timeout = null, CancellationToken cancellationToken = default)
    {
        var pageResult = new PageResult<Dictionary<string, object?>> { PageIndex = pageIndex, PageSize = pageSize };
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(cancellationToken);

            var countSql = $"SELECT COUNT(*) FROM ({sql}) AS [_count_tmp]";
            using var countCmd = new SqlCommand(countSql, conn);
            if (timeout.HasValue)
                countCmd.CommandTimeout = timeout.Value;
            var countResult = await countCmd.ExecuteScalarAsync(cancellationToken);
            pageResult.TotalCount = Convert.ToInt32(countResult);

            var offset = (pageIndex - 1) * pageSize;
            string pagedSql;
            if (sql.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase))
            {
                pagedSql = $"{sql} OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
            }
            else
            {
                pagedSql = $"{sql} ORDER BY (SELECT NULL) OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
            }

            using var cmd = new SqlCommand(pagedSql, conn);
            if (timeout.HasValue)
                cmd.CommandTimeout = timeout.Value;

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.GetValue(i);
                    row[reader.GetName(i)] = value == DBNull.Value ? null : value;
                }
                pageResult.Items.Add(row);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SQL Server分页查询失败: {ex.Message}");
        }
        return pageResult;
    }

    public async Task<bool> TestConnectionAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
