using System.Data.Common;
using System.Diagnostics;
using DbManager.Core.Interfaces;
using DbManager.Core.Models;

namespace DbManager.Core.Services;

/// <summary>
/// 手动事务会话：持有一条打开的连接与事务，跨多次执行保持（关闭自动提交时使用）。
/// provider 无关——基于 DbConnection/DbTransaction，复用各库连接工厂。
/// 提交/回滚后自动开启新事务，会话保持可用；释放时回滚未提交内容并关闭连接。
/// </summary>
public sealed class DbTransactionSession : IAsyncDisposable
{
    private readonly DbConnectionModel _connection;
    private readonly IDbConnectionFactory _factory;
    private DbConnection? _conn;
    private DbTransaction? _tx;

    public DbTransactionSession(DbConnectionModel connection, IDbConnectionFactory factory)
    {
        _connection = connection;
        _factory = factory;
    }

    /// <summary>
    /// 事务是否处于活动状态
    /// </summary>
    public bool IsActive => _tx != null;

    /// <summary>
    /// 打开连接并开启事务。
    /// </summary>
    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        _conn = _factory.CreateConnection(_connection);
        await _conn.OpenAsync(cancellationToken);
        _tx = await _conn.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// 在当前事务内执行 SQL，返回与无状态执行服务一致的结果结构。
    /// </summary>
    public async Task<SqlExecuteResult> ExecuteAsync(string sql, int? timeout = null, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            if (_conn == null || _tx == null)
            {
                throw new InvalidOperationException("事务会话尚未开启");
            }

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Transaction = _tx;
            if (timeout.HasValue)
            {
                cmd.CommandTimeout = timeout.Value;
            }

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            var result = new SqlExecuteResult { IsSuccess = true };

            do
            {
                if (reader.FieldCount > 0)
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
                }
            } while (await reader.NextResultAsync(cancellationToken));

            // 非查询语句的影响行数（SELECT 为 -1，归零处理）
            result.AffectedRows = reader.RecordsAffected < 0 ? 0 : reader.RecordsAffected;

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

    /// <summary>
    /// 提交当前事务并开启新事务（会话保持可用）。
    /// </summary>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_tx == null || _conn == null)
        {
            return;
        }

        await _tx.CommitAsync(cancellationToken);
        await _tx.DisposeAsync();
        _tx = await _conn.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// 回滚当前事务并开启新事务（会话保持可用）。
    /// </summary>
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_tx == null || _conn == null)
        {
            return;
        }

        await _tx.RollbackAsync(cancellationToken);
        await _tx.DisposeAsync();
        _tx = await _conn.BeginTransactionAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_tx != null)
        {
            try
            {
                await _tx.RollbackAsync();
            }
            catch
            {
                // 释放阶段的回滚失败忽略（连接可能已断）
            }
            await _tx.DisposeAsync();
            _tx = null;
        }
        if (_conn != null)
        {
            await _conn.DisposeAsync();
            _conn = null;
        }
    }
}
