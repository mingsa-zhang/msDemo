using Dapper;
using Microsoft.Data.Sqlite;
using System.IO;

namespace DataAgentRetryTool.Services;

/// <summary>
/// 重推记录
/// </summary>
public class RetryRecord
{
    public string Id { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public DateTime RetryTime { get; set; }
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// 数据库服务（线程安全）
/// </summary>
public class DatabaseService : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly object _lock = new object();
    private static readonly string DbPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "retry_records.db");

    public DatabaseService()
    {
        // 确保目录存在
        var directory = Path.GetDirectoryName(DbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var connectionString = $"Data Source={DbPath}";
        _connection = new SqliteConnection(connectionString);
        _connection.Open();
        InitializeDatabase();
    }

    /// <summary>
    /// 初始化数据库表
    /// </summary>
    private void InitializeDatabase()
    {
        lock (_lock)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS RetryRecords (
                    Id TEXT PRIMARY KEY,
                    RecordId TEXT NOT NULL,
                    RetryTime TEXT NOT NULL,
                    IsSuccess INTEGER NOT NULL,
                    Message TEXT
                );
                CREATE INDEX IF NOT EXISTS IX_RetryRecords_RecordId ON RetryRecords(RecordId);
                CREATE INDEX IF NOT EXISTS IX_RetryRecords_RetryTime ON RetryRecords(RetryTime);
            ";
            _connection.Execute(sql);
        }
    }

    /// <summary>
    /// 添加重推记录
    /// </summary>
    public void AddRetryRecord(string recordId, bool isSuccess, string? message = null)
    {
        lock (_lock)
        {
            var sql = @"
                INSERT INTO RetryRecords (Id, RecordId, RetryTime, IsSuccess, Message)
                VALUES (@Id, @RecordId, @RetryTime, @IsSuccess, @Message)
            ";
            _connection.Execute(sql, new
            {
                Id = Guid.NewGuid().ToString(),
                RecordId = recordId,
                RetryTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                IsSuccess = isSuccess ? 1 : 0,
                Message = message
            });
        }
    }

    /// <summary>
    /// 检查是否可以重推
    /// 返回：(是否可以重推, 原因)
    /// 新规则：同一小时内失败3次则跳过
    /// </summary>
    public (bool CanRetry, string Reason) CanRetry(string recordId)
    {
        lock (_lock)
        {
            var now = DateTime.Now;
            var currentHourStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).ToString("yyyy-MM-dd HH:mm:ss");

            // 检查当前小时内失败次数
            var failCountInCurrentHour = _connection.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM RetryRecords WHERE RecordId = @RecordId AND RetryTime >= @Time AND IsSuccess = 0",
                new { RecordId = recordId, Time = currentHourStart });

            if (failCountInCurrentHour >= 3)
            {
                return (false, $"当前小时内已失败{failCountInCurrentHour}次");
            }

            // 检查一小时内重推次数（>=3次才跳过）
            var oneHourAgo = now.AddHours(-1).ToString("yyyy-MM-dd HH:mm:ss");
            var oneHourCount = _connection.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM RetryRecords WHERE RecordId = @RecordId AND RetryTime >= @Time",
                new { RecordId = recordId, Time = oneHourAgo });

            if (oneHourCount >= 3)
            {
                return (false, $"一小时内已重推{oneHourCount}次");
            }

            // 检查一天内重推次数
            var oneDayAgo = now.AddDays(-1).ToString("yyyy-MM-dd HH:mm:ss");
            var oneDayCount = _connection.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM RetryRecords WHERE RecordId = @RecordId AND RetryTime >= @Time",
                new { RecordId = recordId, Time = oneDayAgo });

            if (oneDayCount >= 10)
            {
                return (false, $"一天内已重推{oneDayCount}次");
            }

            return (true, string.Empty);
        }
    }

    /// <summary>
    /// 获取记录的统计信息
    /// </summary>
    public (int TotalRetries, int SuccessCount, int FailCount) GetStatistics(string recordId)
    {
        lock (_lock)
        {
            var total = _connection.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM RetryRecords WHERE RecordId = @RecordId",
                new { RecordId = recordId });

            var success = _connection.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM RetryRecords WHERE RecordId = @RecordId AND IsSuccess = 1",
                new { RecordId = recordId });

            return (total, success, total - success);
        }
    }

    /// <summary>
    /// 清理过期记录（保留30天）
    /// </summary>
    public void CleanupOldRecords()
    {
        lock (_lock)
        {
            var threshold = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd HH:mm:ss");
            _connection.Execute("DELETE FROM RetryRecords WHERE RetryTime < @Time", new { Time = threshold });
        }
    }

    /// <summary>
    /// 清空所有重推记录
    /// </summary>
    public void ClearAllRecords()
    {
        lock (_lock)
        {
            _connection.Execute("DELETE FROM RetryRecords");
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}