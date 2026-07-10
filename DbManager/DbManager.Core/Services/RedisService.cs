using System.Text;
using StackExchange.Redis;

namespace DbManager.Core.Services;

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
    /// 读取键值：返回 Redis 类型名与按类型格式化的文本。
    /// </summary>
    public async Task<(string Type, string Value)> GetValueAsync(string connectionString, int database, string key)
    {
        using var mux = await ConnectionMultiplexer.ConnectAsync(connectionString);
        var db = mux.GetDatabase(database);
        var type = await db.KeyTypeAsync(key);

        var value = type switch
        {
            RedisType.String => (string?)await db.StringGetAsync(key) ?? string.Empty,
            RedisType.Hash => FormatHash(await db.HashGetAllAsync(key)),
            RedisType.List => FormatList(await db.ListRangeAsync(key)),
            RedisType.Set => FormatList(await db.SetMembersAsync(key)),
            RedisType.SortedSet => FormatSortedSet(await db.SortedSetRangeByRankWithScoresAsync(key)),
            RedisType.None => "(键不存在)",
            _ => "(不支持展示的类型)"
        };

        return (type.ToString(), value);
    }

    /// <summary>
    /// 连接测试。
    /// </summary>
    public async Task<bool> TestAsync(string connectionString)
    {
        using var mux = await ConnectionMultiplexer.ConnectAsync(connectionString);
        return mux.IsConnected;
    }

    private static string FormatHash(HashEntry[] entries)
    {
        var sb = new StringBuilder();
        foreach (var e in entries)
        {
            sb.AppendLine($"{e.Name}: {e.Value}");
        }
        return sb.ToString();
    }

    private static string FormatList(RedisValue[] values)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < values.Length; i++)
        {
            sb.AppendLine($"[{i}] {values[i]}");
        }
        return sb.ToString();
    }

    private static string FormatSortedSet(SortedSetEntry[] entries)
    {
        var sb = new StringBuilder();
        foreach (var e in entries)
        {
            sb.AppendLine($"{e.Score}: {e.Element}");
        }
        return sb.ToString();
    }
}
