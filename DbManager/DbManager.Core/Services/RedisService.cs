using System.Globalization;
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
    /// 删除键。
    /// </summary>
    public async Task<bool> DeleteKeyAsync(string connectionString, int database, string key)
    {
        using var mux = await ConnectionMultiplexer.ConnectAsync(connectionString);
        return await mux.GetDatabase(database).KeyDeleteAsync(key);
    }

    /// <summary>
    /// 设置 String 键的值（键不存在则创建）。
    /// </summary>
    public async Task SetStringAsync(string connectionString, int database, string key, string value)
    {
        using var mux = await ConnectionMultiplexer.ConnectAsync(connectionString);
        await mux.GetDatabase(database).StringSetAsync(key, value);
    }

    /// <summary>
    /// 设置或清除键的过期时间。ttl 为 null 表示持久化（移除过期）。
    /// </summary>
    public async Task SetTtlAsync(string connectionString, int database, string key, TimeSpan? ttl)
    {
        using var mux = await ConnectionMultiplexer.ConnectAsync(connectionString);
        await mux.GetDatabase(database).KeyExpireAsync(key, ttl);
    }

    /// <summary>
    /// 整体替换 Hash 键的全部字段（先删后写，写入前后保留原 TTL）。
    /// </summary>
    public async Task ReplaceHashAsync(string connectionString, int database, string key, IReadOnlyDictionary<string, string> fields)
    {
        using var mux = await ConnectionMultiplexer.ConnectAsync(connectionString);
        var db = mux.GetDatabase(database);
        var ttl = await db.KeyTimeToLiveAsync(key);
        await db.KeyDeleteAsync(key);
        if (fields.Count > 0)
        {
            var entries = fields.Select(kv => new HashEntry(kv.Key, kv.Value)).ToArray();
            await db.HashSetAsync(key, entries);
            if (ttl != null)
            {
                await db.KeyExpireAsync(key, ttl);
            }
        }
    }

    /// <summary>
    /// 整体替换 List 键的全部元素（先删后写，写入前后保留原 TTL）。
    /// </summary>
    public async Task ReplaceListAsync(string connectionString, int database, string key, IReadOnlyList<string> items)
    {
        using var mux = await ConnectionMultiplexer.ConnectAsync(connectionString);
        var db = mux.GetDatabase(database);
        var ttl = await db.KeyTimeToLiveAsync(key);
        await db.KeyDeleteAsync(key);
        if (items.Count > 0)
        {
            var values = items.Select(v => (RedisValue)v).ToArray();
            await db.ListRightPushAsync(key, values);
            if (ttl != null)
            {
                await db.KeyExpireAsync(key, ttl);
            }
        }
    }

    /// <summary>
    /// 整体替换 Set 键的全部成员（先删后写，写入前后保留原 TTL）。
    /// </summary>
    public async Task ReplaceSetAsync(string connectionString, int database, string key, IReadOnlyList<string> members)
    {
        using var mux = await ConnectionMultiplexer.ConnectAsync(connectionString);
        var db = mux.GetDatabase(database);
        var ttl = await db.KeyTimeToLiveAsync(key);
        await db.KeyDeleteAsync(key);
        if (members.Count > 0)
        {
            var values = members.Select(v => (RedisValue)v).ToArray();
            await db.SetAddAsync(key, values);
            if (ttl != null)
            {
                await db.KeyExpireAsync(key, ttl);
            }
        }
    }

    /// <summary>
    /// 整体替换 SortedSet 键的全部成员及分数（先删后写，写入前后保留原 TTL）。
    /// </summary>
    public async Task ReplaceSortedSetAsync(string connectionString, int database, string key, IReadOnlyList<(double Score, string Member)> entries)
    {
        using var mux = await ConnectionMultiplexer.ConnectAsync(connectionString);
        var db = mux.GetDatabase(database);
        var ttl = await db.KeyTimeToLiveAsync(key);
        await db.KeyDeleteAsync(key);
        if (entries.Count > 0)
        {
            var values = entries.Select(e => new SortedSetEntry(e.Member, e.Score)).ToArray();
            await db.SortedSetAddAsync(key, values);
            if (ttl != null)
            {
                await db.KeyExpireAsync(key, ttl);
            }
        }
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

    /// <summary>
    /// 溢出提示前缀，供上层判断"当前展示文本是否已被截断"（截断的文本不应回写，避免结构化保存时丢数据）。
    /// </summary>
    public const string OverflowMarkerPrefix = "… （仅显示前 ";

    /// <summary>
    /// 判断格式化后的文本是否因超过 <see cref="MaxItems"/> 而被截断。
    /// </summary>
    public static bool IsTruncated(string formattedValue) => formattedValue.Contains(OverflowMarkerPrefix);

    /// <summary>
    /// 解析「名称: 值」形式的 Hash 编辑文本（对应 <see cref="FormatHash"/> 的输出格式）。
    /// </summary>
    public static Dictionary<string, string> ParseHashLines(string text)
    {
        var dict = new Dictionary<string, string>();
        foreach (var line in SplitNonEmptyLines(text))
        {
            var idx = line.IndexOf(": ", StringComparison.Ordinal);
            if (idx < 0)
            {
                throw new FormatException($"字段行格式应为「名称: 值」，无法解析: {line}");
            }
            dict[line[..idx]] = line[(idx + 2)..];
        }
        return dict;
    }

    /// <summary>
    /// 解析「[序号] 值」形式的 List/Set 编辑文本（对应 <see cref="FormatList"/> 的输出格式，序号仅供展示，回写按行序）。
    /// </summary>
    public static List<string> ParseIndexedLines(string text)
    {
        var list = new List<string>();
        foreach (var line in SplitNonEmptyLines(text))
        {
            var idx = line.IndexOf("] ", StringComparison.Ordinal);
            if (!line.StartsWith('[') || idx < 0)
            {
                throw new FormatException($"元素行格式应为「[序号] 值」，无法解析: {line}");
            }
            list.Add(line[(idx + 2)..]);
        }
        return list;
    }

    /// <summary>
    /// 解析「分数: 成员」形式的 SortedSet 编辑文本（对应 <see cref="FormatSortedSet"/> 的输出格式）。
    /// </summary>
    public static List<(double Score, string Member)> ParseSortedSetLines(string text)
    {
        var list = new List<(double, string)>();
        foreach (var line in SplitNonEmptyLines(text))
        {
            var idx = line.IndexOf(": ", StringComparison.Ordinal);
            if (idx < 0 || !double.TryParse(line[..idx], NumberStyles.Float, CultureInfo.InvariantCulture, out var score))
            {
                throw new FormatException($"成员行格式应为「分数: 成员」，无法解析: {line}");
            }
            list.Add((score, line[(idx + 2)..]));
        }
        return list;
    }

    private static IEnumerable<string> SplitNonEmptyLines(string text)
    {
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.TrimEnd();
            if (!string.IsNullOrWhiteSpace(line))
            {
                yield return line;
            }
        }
    }

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
            sb.AppendLine($"{e.Score.ToString(CultureInfo.InvariantCulture)}: {e.Element}");
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
            sb.AppendLine($"{OverflowMarkerPrefix}{MaxItems} 项）");
        }
    }
}
