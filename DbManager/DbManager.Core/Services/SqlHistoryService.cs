using DbManager.Core.Models;
using DbManager.Common;
using Newtonsoft.Json;

namespace DbManager.Core.Services;

public class SqlHistoryService
{
    private readonly string _historyDir;

    public SqlHistoryService()
    {
        _historyDir = AppConst.SqlHistoryDir;
        Directory.CreateDirectory(_historyDir);
    }

    private const int MaxHistoryFiles = 500;

    public async Task SaveHistoryAsync(SqlHistoryModel history)
    {
        var fileName = $"{history.ExecuteTime:yyyyMMdd_HHmmss}_{history.Id}.json";
        var filePath = Path.Combine(_historyDir, fileName);
        var json = JsonConvert.SerializeObject(history, Formatting.Indented);
        await File.WriteAllTextAsync(filePath, json);

        TrimOldHistories();
    }

    /// <summary>
    /// 限制历史文件数量，超出上限时删除最旧的，避免无限累积。
    /// </summary>
    private void TrimOldHistories()
    {
        try
        {
            var files = Directory.GetFiles(_historyDir, "*.json");
            if (files.Length <= MaxHistoryFiles)
            {
                return;
            }
            foreach (var old in files.OrderBy(f => f).Take(files.Length - MaxHistoryFiles))
            {
                File.Delete(old);
            }
        }
        catch
        {
            // 清理失败不影响主流程
        }
    }

    public async Task<List<SqlHistoryModel>> LoadHistoriesAsync(int limit = 100)
    {
        var result = new List<SqlHistoryModel>();
        var files = Directory.GetFiles(_historyDir, "*.json")
            .OrderByDescending(f => f)
            .Take(limit);

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var history = JsonConvert.DeserializeObject<SqlHistoryModel>(json);
                if (history != null)
                {
                    history.FileName = Path.GetFileName(file);
                    result.Add(history);
                }
            }
            catch
            {
                // 跳过无法解析的文件
            }
        }
        return result;
    }

    public void DeleteHistory(string fileName)
    {
        var filePath = Path.Combine(_historyDir, fileName);
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    public void ClearAllHistories()
    {
        var files = Directory.GetFiles(_historyDir, "*.json");
        foreach (var file in files)
            File.Delete(file);
    }
}