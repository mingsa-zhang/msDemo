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

    public async Task SaveHistoryAsync(SqlHistoryModel history)
    {
        var fileName = $"{history.ExecuteTime:yyyyMMdd_HHmmss}_{history.Id}.json";
        var filePath = Path.Combine(_historyDir, fileName);
        var json = JsonConvert.SerializeObject(history, Formatting.Indented);
        await File.WriteAllTextAsync(filePath, json);
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