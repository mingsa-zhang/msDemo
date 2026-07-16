using DbManager.Core.Services;
using Xunit;

namespace DbManager.Tests;

/// <summary>
/// Mongo 聚合管道 JSON 解析的单元测试。
/// </summary>
public class MongoServiceParseTests
{
    [Fact]
    public void ParsePipelineStages_EmptyInput_ReturnsEmptyList()
    {
        Assert.Empty(MongoService.ParsePipelineStages(null));
        Assert.Empty(MongoService.ParsePipelineStages(string.Empty));
        Assert.Empty(MongoService.ParsePipelineStages("   "));
    }

    [Fact]
    public void ParsePipelineStages_ParsesMultipleStagesInOrder()
    {
        var stages = MongoService.ParsePipelineStages(
            "[{\"$match\":{\"age\":{\"$gt\":18}}},{\"$group\":{\"_id\":\"$city\",\"count\":{\"$sum\":1}}}]");

        Assert.Equal(2, stages.Count);
        Assert.True(stages[0].Contains("$match"));
        Assert.True(stages[1].Contains("$group"));
        Assert.Equal("$city", stages[1]["$group"]["_id"].AsString);
    }

    [Fact]
    public void ParsePipelineStages_SingleStage()
    {
        var stages = MongoService.ParsePipelineStages("[{\"$limit\":10}]");

        Assert.Single(stages);
        Assert.Equal(10, stages[0]["$limit"].AsInt32);
    }

    [Fact]
    public void ParsePipelineStages_InvalidJson_Throws()
    {
        Assert.ThrowsAny<Exception>(() => MongoService.ParsePipelineStages("not-json"));
    }

    [Fact]
    public void ParsePipelineStages_NotAnArray_Throws()
    {
        Assert.ThrowsAny<Exception>(() => MongoService.ParsePipelineStages("{\"$match\":{}}"));
    }
}
