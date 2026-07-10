using System.Text;
using StackExchange.Redis;

namespace DbManager.Core.Services;

/// <summary>
/// Redis 键值读取结果：类型、格式化文本值、TTL。
/// </summary>
public sealed class RedisValueInfo
{
    /// <summary>
    /// Redis 类型名（string/hash/list/set/zset）
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// 按类型格式化的文本值
    /// </summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// 剩余存活时间（"永久" 或人类可读时长）
    /// </summary>
    public string Ttl { get; init; } = string.Empty;
}

/// <summary>
/// Redis 只读访问服务：按模式扫描键、按类型读取并格式化值。
/// 直接使用 StackExchange.Redis，不经关系型的元数据/执行工厂。
/// </summary>
public sealed class RedisService
{
    /// <summary>
    /// 扫描键（SCAN，按 pattern 匹配，最多返回 limit 个）。
    /// </summary>
    public async Task<List<string>> ListKeysAsync(string connectionString, int database, string? pattern, int limit)
    {
        using var mux = await ConnectionMultiplexer.ConnectAsync(connectionString);
        var endpoint = mux.GetEndPoints().FirstOrDefault();
        if (endpoint == null)
        {
            return new List<string>();
        }

        var server = mux.GetServer(endpoint);
        var match = string.IsNullOrWhiteSpace(pattern) ? "*" : pattern;

        var keys = new List<string>();
        foreach (var key in server.Keys(database, match, pageSize: Math.Min(limit, 1000)))
        {
            keys.Add(key.ToString());
            if (keys.Count >= limit)
            {
                break;
            }
        }
        return keys;
    }

    /// <summary>
    /// 读取键值：返回 Redis 类型名、按类型格式化的文本、剩余存活时间。
    /// </summary>
    public async Task<RedisValueInfo> GetValueAsync(string connectionString, int database, string key)
    {
        using var mux = await ConnectionMultiplexer.ConnectAsync(connectionString);
        var db = mux.GetDatabase(database);
        var type = await db.KeyTypeAsync(key);

        // 大集合仅取前 MaxItems 项，避免巨型键拉取导致卡死/内存膨胀
        var value = type switch
        {
            RedisType.String => (string?)await db.StringGetAsync(key) ?? string.Empty,
            RedisType.Hash => FormatHash(db.HashScan(key, pageSize: 250).Take(MaxItems + 1).ToArray()),
            RedisType.List => FormatList(await db.ListRangeAsync(key, 0, MaxItems)),
            RedisType.Set => FormatList(db.SetScan(key, pageSize: 250).Take(MaxItems + 1).ToArray()),
            RedisType.SortedSet => FormatSortedSet(await db.SortedSetRangeByRankWithScoresAsync(key, 0, MaxItems)),
            RedisType.None => "(键不存在)",
            _ => "(不支持展示的类型)"
        };

        var ttl = await db.KeyTimeToLiveAsync(key);
        return new RedisValueInfo
        {
            Type = type.ToString(),
            Value = value,
            Ttl = FormatTtl(ttl)
        };
    }

    /// <summary>
    /// 格式化 TTL：null 表示永久（未设置过期）。
    /// </summary>
    private static string FormatTtl(TimeSpan? ttl)
    {
        if (ttl == null)
        {
            return "永久";
        }

        var total = ttl.Value;
        if (total.TotalSeconds < 60)
        {
            return $"{(int)total.TotalSeconds}s";
        }
        if (total.TotalMinutes < 60)
        {
            return $"{(int)total.TotalMinutes}m{total.Seconds}s";
        }
        if (total.TotalHours < 24)
        {
            return $"{(int)total.TotalHours}h{total.Minutes}m";
        }
        return $"{(int)total.TotalDays}d{total.Hours}h";
    }

    /// <summary>
    /// 连接测试。
    /// </summary>
    public async Task<bool> TestAsync(string connectionString)
    {
        using var mux = await ConnectionMultiplexer.ConnectAsync(connectionString);
        return mux.IsConnected;
    }

    /// <summary>
    /// 单键值展示的最大条目数（超出仅显示前 N 项）。
    /// </summary>
    private const int MaxItems = 500;

    private static string FormatHash(HashEntry[] entries)
    {
        var sb = new StringBuilder();
        foreach (var e in entries.Take(MaxItems))
        {
            sb.AppendLine($"{e.Name}: {e.Value}");
        }
        AppendOverflow(sb, entries.Length);
        return sb.ToString();
    }

    private static string FormatList(RedisValue[] values)
    {
        var sb = new StringBuilder();
        var shown = Math.Min(values.Length, MaxItems);
        for (int i = 0; i < shown; i++)
        {
            sb.AppendLine($"[{i}] {values[i]}");
        }
        AppendOverflow(sb, values.Length);
        return sb.ToString();
    }

    private static string FormatSortedSet(SortedSetEntry[] entries)
    {
        var sb = new StringBuilder();
        foreach (var e in entries.Take(MaxItems))
        {
            sb.AppendLine($"{e.Score}: {e.Element}");
        }
        AppendOverflow(sb, entries.Length);
        return sb.ToString();
    }

    /// <summary>
    /// 取到超过上限时追加提示（拉取时多取 1 项用于探测是否溢出）。
    /// </summary>
    private static void AppendOverflow(StringBuilder sb, int fetchedCount)
    {
        if (fetchedCount > MaxItems)
        {
            sb.AppendLine($"… （仅显示前 {MaxItems} 项）");
        }
    }
}
