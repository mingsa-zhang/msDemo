using DataAgentRetryTool.Models;
using DataAgentRetryTool.Services;
using Newtonsoft.Json;

namespace DataAgentRetryTool.Workers;

/// <summary>
/// 正式环境重推工作器（不分类型）
/// </summary>
public class ProductionRetryWorker
{
    private readonly DataTransmissionService _service;
    private readonly DatabaseService _databaseService;

    public RetryStats Stats { get; } = new RetryStats();

    public event Action<string>? LogAppended;
    public event Action<RetryStats>? StatsUpdated;

    public ProductionRetryWorker(DataTransmissionService service, DatabaseService databaseService)
    {
        _service = service;
        _databaseService = databaseService;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Stats.IsRunning = true;
        Stats.SuccessCount = 0;
        Stats.SkipCount = 0;
        Stats.FailCount = 0;
        Stats.TotalCount = 0;
        Stats.InitialTotalCount = 0;
        Stats.Status = "运行中";
        UpdateStats();

        var processedIds = new HashSet<string>();
        bool isFirstQuery = true;

        try
        {
            _databaseService.CleanupOldRecords();
            AppendLog("[信息] 已清理30天前的重推记录");

            while (!cancellationToken.IsCancellationRequested)
            {
                AppendLog("[查询] 正在查询数据总数...");
                var countResult = await _service.QueryProductionRecordsAsync(1, 10);

                if (countResult == null || !countResult.IsSuccess)
                {
                    AppendLog($"[错误] 查询失败: {countResult?.Msg ?? "无响应"}");
                    break;
                }

                if (isFirstQuery && countResult.Count > 0)
                {
                    Stats.InitialTotalCount = countResult.Count;
                    isFirstQuery = false;
                    AppendLog($"[信息] 初次统计总数: {countResult.Count}");
                }

                if (countResult.Count == 0)
                {
                    AppendLog("[信息] 没有待重推数据，等待1分钟后重新扫描...");
                    Stats.TotalCount = 0;
                    UpdateStats();
                    await Task.Delay(60000, cancellationToken);
                    continue;  // 继续循环，不结束
                }

                Stats.TotalCount = countResult.Count;
                int totalPages = (int)Math.Ceiling((double)countResult.Count / 10);

                AppendLog($"[信息] 总数: {countResult.Count}, 总页数: {totalPages}");

                int currentPage = totalPages;
                bool hadActualRetry = false;

                while (currentPage >= 1 && !cancellationToken.IsCancellationRequested)
                {
                    AppendLog($"[查询] 正在查询第 {currentPage} 页...");

                    var result = await _service.QueryProductionRecordsAsync(currentPage, 10);

                    if (result == null || !result.IsSuccess)
                    {
                        AppendLog($"[错误] 查询第 {currentPage} 页失败: {result?.Msg ?? "无响应"}");
                        break;
                    }

                    if (result.Data.Count == 0)
                    {
                        AppendLog($"[信息] 第 {currentPage} 页没有数据");
                        currentPage--;
                        continue;
                    }

                    var records = result.Data.ToList();
                    int pageRetryCount = 0;
                    int pageSkipCount = 0;

                    AppendLog($"[信息] 第 {currentPage} 页共 {records.Count} 条记录");

                    for (int i = records.Count - 1; i >= 0; i--)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        var record = records[i];
                        var customerName = ExtractCustomerName(record.BusinessDataJson);

                        if (processedIds.Contains(record.Id))
                        {
                            Stats.SkipCount++;
                            pageSkipCount++;
                            AppendLog($"[跳过] ID: {record.Id}, 创建时间: {record.CreateTime}, 客户: {customerName} - 本轮已处理");
                            UpdateStats();
                            continue;
                        }

                        var (canRetry, reason) = _databaseService.CanRetry(record.Id);

                        if (!canRetry)
                        {
                            var (_, retryHistCount, _) = _databaseService.GetStatistics(record.Id);

                            if (retryHistCount > 2)
                            {
                                await TryAbandonIfMatchAsync(record, cancellationToken);
                                await Task.Delay(500, cancellationToken);
                            }
                            else
                            {
                                Stats.SkipCount++;
                            }

                            pageSkipCount++;
                            processedIds.Add(record.Id);
                            AppendLog($"[跳过] ID: {record.Id}, 创建时间: {record.CreateTime}, 客户: {customerName} - {reason}");
                            UpdateStats();
                            continue;
                        }

                        AppendLog($"[重推] ID: {record.Id}, 创建时间: {record.CreateTime}, 客户: {customerName}");

                        try
                        {
                            var retryResult = await _service.RetryAsync(record);

                            if (retryResult != null && retryResult.IsSuccess)
                            {
                                Stats.SuccessCount++;
                                pageRetryCount++;
                                hadActualRetry = true;
                                _databaseService.AddRetryRecord(record.Id, true, retryResult.Msg);
                                AppendLog($"[成功] ID: {record.Id}, 创建时间: {record.CreateTime} - {retryResult.Msg}");
                            }
                            else
                            {
                                Stats.FailCount++;
                                pageRetryCount++;
                                hadActualRetry = true;
                                var errorMsg = retryResult?.Msg ?? "未知错误";
                                _databaseService.AddRetryRecord(record.Id, false, errorMsg);
                                AppendLog($"[失败] ID: {record.Id}, 创建时间: {record.CreateTime} - {errorMsg}");

                                var (_, retryHistCount, _) = _databaseService.GetStatistics(record.Id);
                                if (retryHistCount > 2)
                                {
                                    await TryAbandonIfMatchAsync(record, cancellationToken);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Stats.FailCount++;
                            hadActualRetry = true;
                            _databaseService.AddRetryRecord(record.Id, false, ex.Message);
                            AppendLog($"[异常] ID: {record.Id}, 创建时间: {record.CreateTime} - {ex.Message}");

                            var (_, retryHistCount, _) = _databaseService.GetStatistics(record.Id);
                            if (retryHistCount > 2)
                            {
                                await TryAbandonIfMatchAsync(record, cancellationToken);
                            }
                        }

                        processedIds.Add(record.Id);
                        UpdateStats();

                        if (i > 0 && !cancellationToken.IsCancellationRequested)
                        {
                            await Task.Delay(1000, cancellationToken);
                        }
                    }

                    AppendLog($"[完成] 第 {currentPage} 页处理完成 - 重推: {pageRetryCount}, 跳过: {pageSkipCount}");

                    if (hadActualRetry && !cancellationToken.IsCancellationRequested)
                    {
                        AppendLog("[信息] 有实际重推，重新从最后一页查询实时数据...");
                        processedIds.Clear();
                        break;
                    }

                    currentPage--;

                    if (currentPage >= 1 && !cancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(500, cancellationToken);
                    }
                }

                if (!hadActualRetry && currentPage < 1 && !cancellationToken.IsCancellationRequested)
                {
                    AppendLog("[信息] 所有页都已处理，等待1分钟后重新扫描...");
                    await Task.Delay(60000, cancellationToken);
                }
            }

            Stats.Status = "已停止";
        }
        catch (OperationCanceledException)
        {
            AppendLog("[信息] 用户取消了重推操作");
            Stats.Status = "已停止";
        }
        catch (Exception ex)
        {
            AppendLog($"[异常] {ex.Message}");
            Stats.Status = "异常";
        }
        finally
        {
            Stats.IsRunning = false;
            AppendLog($"[完成] 结束 - 成功: {Stats.SuccessCount}, 跳过: {Stats.SkipCount}, 失败: {Stats.FailCount}, 取消: {Stats.CancelledCount}");
            UpdateStats();
        }
    }

    private void AppendLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        LogAppended?.Invoke($"[{timestamp}] {message}");
    }

    private void UpdateStats()
    {
        StatsUpdated?.Invoke(Stats);
    }

    private string ExtractCustomerName(string businessDataJson)
    {
        try
        {
            dynamic? data = JsonConvert.DeserializeObject(businessDataJson);
            return data?.KeyInfo?.CustomerName?.ToString() ?? "未知";
        }
        catch
        {
            return "解析失败";
        }
    }

    private async Task TryAbandonIfMatchAsync(TransmissionRecord record, CancellationToken ct)
    {
        try
        {
            AppendLog($"[检查] ID: {record.Id} - 重推历史>2次，正在查询详情...");

            var detailResult = await _service.GetDetailAsync(record.Id);
            if (detailResult == null || !detailResult.IsSuccess || detailResult.Data == null)
            {
                AppendLog($"[失败] ID: {record.Id} - 详情查询失败");
                return;
            }

            var detail = detailResult.Data;

            var failedOutApps = detail.OutApps?.Where(a =>
                a.TransferState == 4 && a.TransferStateStr == "传输失败").ToList();

            if (failedOutApps == null || failedOutApps.Count == 0)
            {
                AppendLog($"[失败] ID: {record.Id} - 未找到失败的OutApp");
                return;
            }

            Stats.CancelledCount++;
            foreach (var failedOutApp in failedOutApps)
            {
                var errorMessage = ExtractErrorMessage(failedOutApp.TransferStateDescription);

                if (string.IsNullOrEmpty(errorMessage))
                {
                    AppendLog($"[跳过取消] ID: {record.Id} - {failedOutApp.AccessAppCode} 无法提取错误Message");
                    continue;
                }

                var matchedRule = MatchFilterRules(errorMessage);

                if (!string.IsNullOrEmpty(matchedRule))
                {
                    AppendLog($"[匹配] ID: {record.Id} - {failedOutApp.AccessAppCode} 匹配规则: {matchedRule}");

                    var abandonRequest = new AbandonRequest
                    {
                        Id = detail.Id,
                        BusinessTypeCode = detail.BusinessTypeCode,
                        AppCode = detail.InApp.AccessAppCode,
                        OutAppCode = failedOutApp.AccessAppCode,
                        BusinessDataJson = null
                    };

                    var abandonResult = await _service.AbandonAsync(abandonRequest);

                    if (abandonResult != null && abandonResult.IsSuccess)
                    {
                        _databaseService.AddRetryRecord(record.Id, true, $"已取消推送({failedOutApp.AccessAppCode}): {matchedRule}");
                        AppendLog($"[取消] ID: {record.Id}, 客户: {ExtractCustomerName(record.BusinessDataJson)}, OutApp: {failedOutApp.AccessAppCode} - 原因: {matchedRule}");
                    }
                    else
                    {
                        var abandonMsg = abandonResult?.Msg ?? "取消失败";
                        AppendLog($"[取消失败] ID: {record.Id}, OutApp: {failedOutApp.AccessAppCode} - {abandonMsg}");
                    }
                }
                else
                {
                    AppendLog($"[待观察] ID: {record.Id}, 客户: {ExtractCustomerName(record.BusinessDataJson)}, OutApp: {failedOutApp.AccessAppCode} - 未匹配过滤规则");
                    AppendLog($"  Message: {errorMessage}");
                }
            }
        }
        catch (Exception ex)
        {
            AppendLog($"[检查异常] ID: {record.Id} - {ex.Message}");
        }
    }

    private string ExtractErrorMessage(string transferStateDescription)
    {
        if (string.IsNullOrEmpty(transferStateDescription))
            return string.Empty;

        try
        {
            var innerJson = JsonConvert.DeserializeObject<string>(transferStateDescription);
            if (!string.IsNullOrEmpty(innerJson))
            {
                var errorDetail = JsonConvert.DeserializeObject<ErrorDetail>(innerJson);
                if (!string.IsNullOrEmpty(errorDetail?.Message))
                {
                    return errorDetail.Message.Replace("\\n", " ").Replace("\n", " ").Trim();
                }
            }
        }
        catch
        {
            return transferStateDescription.Trim();
        }

        return string.Empty;
    }

    private string? MatchFilterRules(string errorMessage)
    {
        var filterRules = new[]
        {
            "SQL序号【89】、数据类型名称【KeyInfo】执行失败：更新触发，存在名称重复的供应商资料不能进行创建",
            "SQL序号【98】、数据类型名称【KeyInfo】执行失败：23505: duplicate key value violates unique constraint",
            "SQL序号【89】、数据类型名称【KeyInfo】执行失败：更新触发，存在名称重复的供应商资料不能进行创建，请检查!"
        };

        foreach (var rule in filterRules)
        {
            if (errorMessage.Contains(rule, StringComparison.OrdinalIgnoreCase))
            {
                return rule;
            }
        }

        // 唯一索引重复键（通配匹配）
        if (errorMessage.Contains("不能在具有唯一索引", StringComparison.OrdinalIgnoreCase)
            && errorMessage.Contains("中插入重复键的行", StringComparison.OrdinalIgnoreCase))
        {
            return "唯一索引重复键";
        }

        return null;
    }
}