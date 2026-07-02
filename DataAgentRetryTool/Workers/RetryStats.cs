namespace DataAgentRetryTool.Workers;

/// <summary>
/// 重推统计
/// </summary>
public class RetryStats
{
    public int SuccessCount { get; set; }
    public int SkipCount { get; set; }
    public int FailCount { get; set; }
    public int CancelledCount { get; set; }
    public int TotalCount { get; set; }
    public int InitialTotalCount { get; set; }
    public bool IsRunning { get; set; }
    public string Status { get; set; } = "未启用";
}