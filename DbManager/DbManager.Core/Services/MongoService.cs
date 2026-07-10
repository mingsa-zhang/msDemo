using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;

namespace DbManager.Core.Services;

/// <summary>
/// MongoDB 只读访问服务：列库/集合、按过滤分页查文档（返回美化 JSON）。
/// 直接使用 MongoDB.Driver，不经关系型的元数据/执行工厂。
/// </summary>
public sealed class MongoService
{
    private static readonly JsonWriterSettings JsonSettings = new() { Indent = true, OutputMode = JsonOutputMode.RelaxedExtendedJson };

    private static IMongoClient CreateClient(string connectionString) => new MongoClient(connectionString);

    /// <summary>
    /// 列出所有数据库名。
    /// </summary>
    public async Task<List<string>> ListDatabasesAsync(string connectionString)
    {
        var client = CreateClient(connectionString);
        using var cursor = await client.ListDatabaseNamesAsync();
        return await cursor.ToListAsync();
    }

    /// <summary>
    /// 列出指定库的所有集合名。
    /// </summary>
    public async Task<List<string>> ListCollectionsAsync(string connectionString, string database)
    {
        var db = CreateClient(connectionString).GetDatabase(database);
        using var cursor = await db.ListCollectionNamesAsync();
        return await cursor.ToListAsync();
    }

    /// <summary>
    /// 列出集合的索引名。
    /// </summary>
    public async Task<List<string>> ListIndexNamesAsync(string connectionString, string database, string collection)
    {
        var db = CreateClient(connectionString).GetDatabase(database);
        var col = db.GetCollection<BsonDocument>(collection);
        using var cursor = await col.Indexes.ListAsync();
        var indexes = await cursor.ToListAsync();
        return indexes
            .Select(ix => ix.TryGetValue("name", out var n) ? n.AsString : "(未命名索引)")
            .ToList();
    }

    /// <summary>
    /// 分页查询集合文档。filterJson 为空则不过滤；返回美化后的 JSON 文档列表与总数。
    /// </summary>
    public async Task<(List<string> Docs, long Total)> QueryAsync(
        string connectionString, string database, string collection, string? filterJson, int skip, int limit)
    {
        var db = CreateClient(connectionString).GetDatabase(database);
        var col = db.GetCollection<BsonDocument>(collection);

        var noFilter = string.IsNullOrWhiteSpace(filterJson);
        var filter = noFilter ? new BsonDocument() : BsonDocument.Parse(filterJson);

        // 无过滤时用估算计数（走集合元数据，大集合更快）；有过滤才精确计数
        var total = noFilter
            ? await col.EstimatedDocumentCountAsync()
            : await col.CountDocumentsAsync(filter);
        var docs = await col.Find(filter).Skip(skip).Limit(limit).ToListAsync();
        var json = docs.Select(d => d.ToJson(JsonSettings)).ToList();
        return (json, total);
    }

    /// <summary>
    /// 按文档原始 JSON 中的 _id 删除该文档。
    /// </summary>
    public async Task DeleteDocumentAsync(string connectionString, string database, string collection, string documentJson)
    {
        var col = GetCollection(connectionString, database, collection);
        var doc = BsonDocument.Parse(documentJson);
        if (!doc.TryGetValue("_id", out var id))
        {
            throw new InvalidOperationException("文档缺少 _id，无法定位删除");
        }
        await col.DeleteOneAsync(new BsonDocument("_id", id));
    }

    /// <summary>
    /// 用新 JSON 替换文档（按原文档 _id 定位）。
    /// </summary>
    public async Task ReplaceDocumentAsync(string connectionString, string database, string collection, string originalJson, string newJson)
    {
        var col = GetCollection(connectionString, database, collection);
        var original = BsonDocument.Parse(originalJson);
        if (!original.TryGetValue("_id", out var id))
        {
            throw new InvalidOperationException("原文档缺少 _id，无法定位替换");
        }
        var newDoc = BsonDocument.Parse(newJson);
        await col.ReplaceOneAsync(new BsonDocument("_id", id), newDoc);
    }

    /// <summary>
    /// 插入一篇文档。
    /// </summary>
    public async Task InsertDocumentAsync(string connectionString, string database, string collection, string documentJson)
    {
        var col = GetCollection(connectionString, database, collection);
        await col.InsertOneAsync(BsonDocument.Parse(documentJson));
    }

    private static IMongoCollection<BsonDocument> GetCollection(string connectionString, string database, string collection)
        => CreateClient(connectionString).GetDatabase(database).GetCollection<BsonDocument>(collection);

    /// <summary>
    /// 连接测试：能列出数据库即视为成功。
    /// </summary>
    public async Task<bool> TestAsync(string connectionString)
    {
        var client = CreateClient(connectionString);
        using var cursor = await client.ListDatabaseNamesAsync();
        return true;
    }
}
