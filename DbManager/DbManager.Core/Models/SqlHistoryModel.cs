namespace DbManager.Core.Models;

public class SqlHistoryModel
{
    public int Id { get; set; }
    public int ConnectionId { get; set; }
    public string ConnectionName { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string SqlText { get; set; } = string.Empty;
    public DateTime ExecuteTime { get; set; }
    public long ExecutionTimeMs { get; set; }
    public bool IsSuccess { get; set; }
    public int AffectedRows { get; set; }

    /// <summary>
    /// 对应的历史文件名（加载时填充，用于单条删除；不参与序列化）。
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    public string? FileName { get; set; }
}