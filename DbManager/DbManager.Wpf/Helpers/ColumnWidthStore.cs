using System.Collections.Generic;
using System.IO;
using DbManager.Common;
using Newtonsoft.Json;

namespace DbManager.Wpf.Helpers;

/// <summary>
/// 数据浏览列宽持久化：按「表键 → 列名 → 宽度」存到 JSON 文件，跨会话保留用户调整的列宽。
/// </summary>
public static class ColumnWidthStore
{
    private static readonly string FilePath = Path.Combine(AppConst.AppDataDir, "column_widths.json");
    private static readonly object Lock = new();
    private static Dictionary<string, Dictionary<string, double>>? _cache;

    private static Dictionary<string, Dictionary<string, double>> Cache
    {
        get
        {
            if (_cache != null)
            {
                return _cache;
            }

            lock (Lock)
            {
                if (_cache != null)
                {
                    return _cache;
                }

                try
                {
                    if (File.Exists(FilePath))
                    {
                        var json = File.ReadAllText(FilePath);
                        _cache = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, double>>>(json)
                                 ?? new Dictionary<string, Dictionary<string, double>>();
                    }
                    else
                    {
                        _cache = new Dictionary<string, Dictionary<string, double>>();
                    }
                }
                catch
                {
                    _cache = new Dictionary<string, Dictionary<string, double>>();
                }

                return _cache;
            }
        }
    }

    /// <summary>
    /// 取指定表某列的保存宽度；无记录返回 null。
    /// </summary>
    public static double? Get(string tableKey, string column)
    {
        lock (Lock)
        {
            if (Cache.TryGetValue(tableKey, out var cols) && cols.TryGetValue(column, out var w) && w > 0)
            {
                return w;
            }
            return null;
        }
    }

    /// <summary>
    /// 记录某表某列的宽度（仅入内存，需 Flush 落盘）。
    /// </summary>
    public static void Set(string tableKey, string column, double width)
    {
        if (width <= 0)
        {
            return;
        }

        lock (Lock)
        {
            if (!Cache.TryGetValue(tableKey, out var cols))
            {
                cols = new Dictionary<string, double>();
                Cache[tableKey] = cols;
            }
            cols[column] = width;
        }
    }

    /// <summary>
    /// 落盘。写失败静默忽略（列宽非关键数据）。
    /// </summary>
    public static void Flush()
    {
        lock (Lock)
        {
            if (_cache == null)
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(AppConst.AppDataDir);
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(_cache, Formatting.Indented));
            }
            catch
            {
                // 列宽持久化失败不影响主流程
            }
        }
    }
}
