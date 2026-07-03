using System.Collections.Concurrent;

namespace DbManager.Core.Services;

/// <summary>
/// 元数据缓存：按 key 缓存树节点/元数据查询结果，避免重复查库。
/// 单节点刷新时按前缀失效对应条目。线程安全。
/// </summary>
public sealed class MetadataCache
{
    private readonly ConcurrentDictionary<string, Lazy<Task<object>>> _entries = new();

    /// <summary>
    /// 取缓存；未命中则调用 <paramref name="factory"/> 查询并缓存。
    /// </summary>
    public async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory)
    {
        var lazy = _entries.GetOrAdd(key, _ => new Lazy<Task<object>>(async () => (object)(await factory())!));
        try
        {
            return (T)await lazy.Value;
        }
        catch
        {
            // 查询失败不缓存失败结果，移除以便下次重试
            _entries.TryRemove(key, out _);
            throw;
        }
    }

    /// <summary>
    /// 按前缀失效缓存（如某连接、某库、某 schema 下的所有条目）。
    /// </summary>
    public void InvalidateByPrefix(string keyPrefix)
    {
        foreach (var key in _entries.Keys.Where(k => k.StartsWith(keyPrefix, StringComparison.Ordinal)).ToList())
        {
            _entries.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// 清空全部缓存。
    /// </summary>
    public void Clear() => _entries.Clear();
}
