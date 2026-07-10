using DbManager.Core.Dialects;
using Xunit;

namespace DbManager.Tests;

/// <summary>
/// 标识符引用与限定名拼接的单元测试。
/// </summary>
public class IdentifierQuoterTests
{
    [Fact]
    public void Backtick_QuotesAndEscapes()
    {
        var q = new BacktickQuoter();
        Assert.Equal("`col`", q.Quote("col"));
        Assert.Equal("`a``b`", q.Quote("a`b"));
    }

    [Fact]
    public void Bracket_EscapesClosingBracket()
    {
        var q = new BracketQuoter();
        Assert.Equal("[a]]b]", q.Quote("a]b"));
    }

    [Fact]
    public void DoubleQuote_QuotesAndEscapes()
    {
        var q = new DoubleQuoteQuoter();
        Assert.Equal("\"col\"", q.Quote("col"));
        Assert.Equal("\"a\"\"b\"", q.Quote("a\"b"));
    }

    [Fact]
    public void QuoteQualified_SkipsEmptyParts()
    {
        var q = new DoubleQuoteQuoter();
        Assert.Equal("\"db\".\"t\"", q.QuoteQualified("db", null, "t"));
        Assert.Equal("\"t\"", q.QuoteQualified(null, "", "t"));
    }
}
