using DbManager.Core.Abstractions;

namespace DbManager.Core.Dialects;

/// <summary>
/// 通用引用符实现：由左右引用符 + 转义规则构成。
/// </summary>
public abstract class IdentifierQuoterBase : IIdentifierQuoter
{
    /// <summary>
    /// 左引用符
    /// </summary>
    protected abstract char Open { get; }

    /// <summary>
    /// 右引用符
    /// </summary>
    protected abstract char Close { get; }

    /// <summary>
    /// 对单个标识符加引号，并把内部出现的右引用符双写转义。
    /// </summary>
    public string Quote(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return identifier;
        }

        var escaped = identifier.Replace(Close.ToString(), $"{Close}{Close}");
        return $"{Open}{escaped}{Close}";
    }

    /// <summary>
    /// 对多段标识符分别加引号后用点号连接，空段忽略。
    /// </summary>
    public string QuoteQualified(params string?[] parts)
    {
        var quoted = parts
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => Quote(p!));
        return string.Join(".", quoted);
    }
}

/// <summary>
/// 反引号风格：MySQL / MariaDB
/// </summary>
public sealed class BacktickQuoter : IdentifierQuoterBase
{
    protected override char Open => '`';
    protected override char Close => '`';
}

/// <summary>
/// 方括号风格：SQL Server
/// </summary>
public sealed class BracketQuoter : IdentifierQuoterBase
{
    protected override char Open => '[';
    protected override char Close => ']';
}

/// <summary>
/// 双引号风格：PostgreSQL / Oracle / SQLite / DB2
/// </summary>
public sealed class DoubleQuoteQuoter : IdentifierQuoterBase
{
    protected override char Open => '"';
    protected override char Close => '"';
}
