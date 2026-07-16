using DbManager.Core.Services;
using Xunit;

namespace DbManager.Tests;

/// <summary>
/// Redis 结构化写回：编辑文本解析（Hash/List-Set/SortedSet）的单元测试。
/// </summary>
public class RedisServiceParseTests
{
    [Fact]
    public void ParseHashLines_ParsesNameValuePairs()
    {
        var result = RedisService.ParseHashLines("name: Tom\nage: 18\n");

        Assert.Equal(2, result.Count);
        Assert.Equal("Tom", result["name"]);
        Assert.Equal("18", result["age"]);
    }

    [Fact]
    public void ParseHashLines_SkipsBlankLines()
    {
        var result = RedisService.ParseHashLines("a: 1\n\n\nb: 2\n");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ParseHashLines_ValueMayContainColon()
    {
        var result = RedisService.ParseHashLines("url: http://a.com:8080\n");

        Assert.Equal("http://a.com:8080", result["url"]);
    }

    [Fact]
    public void ParseHashLines_InvalidLine_Throws()
    {
        Assert.Throws<FormatException>(() => RedisService.ParseHashLines("not-a-valid-line\n"));
    }

    [Fact]
    public void ParseIndexedLines_ParsesValuesInOrder()
    {
        var result = RedisService.ParseIndexedLines("[0] apple\n[1] banana\n[2] cherry\n");

        Assert.Equal(new[] { "apple", "banana", "cherry" }, result);
    }

    [Fact]
    public void ParseIndexedLines_ValueMayContainBracket()
    {
        var result = RedisService.ParseIndexedLines("[0] a] b\n");

        Assert.Equal("a] b", result[0]);
    }

    [Fact]
    public void ParseIndexedLines_InvalidLine_Throws()
    {
        Assert.Throws<FormatException>(() => RedisService.ParseIndexedLines("no-brackets-here\n"));
    }

    [Fact]
    public void ParseSortedSetLines_ParsesScoreAndMember()
    {
        var result = RedisService.ParseSortedSetLines("1.5: alice\n2: bob\n");

        Assert.Equal(2, result.Count);
        Assert.Equal((1.5, "alice"), result[0]);
        Assert.Equal((2d, "bob"), result[1]);
    }

    [Fact]
    public void ParseSortedSetLines_InvalidScore_Throws()
    {
        Assert.Throws<FormatException>(() => RedisService.ParseSortedSetLines("not-a-score: alice\n"));
    }

    [Fact]
    public void IsTruncated_DetectsOverflowMarker()
    {
        Assert.True(RedisService.IsTruncated("[0] a\n… （仅显示前 500 项）\n"));
        Assert.False(RedisService.IsTruncated("[0] a\n[1] b\n"));
    }
}
