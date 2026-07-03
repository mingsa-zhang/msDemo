namespace DbManager.Core.Abstractions;

/// <summary>
/// 标识符引用层：统一封装各数据库对库名/表名/列名的引用差异
/// （MySQL 反引号、SqlServer 方括号、PG/Oracle/SQLite/DB2 双引号）。
/// 全项目唯一加引号入口，杜绝硬编码引用符散落各处。
/// </summary>
public interface IIdentifierQuoter
{
    /// <summary>
    /// 对单个标识符加引号（并转义内部的引用符）。
    /// </summary>
    string Quote(string identifier);

    /// <summary>
    /// 对多段标识符分别加引号后用点号连接（如 库.表、schema.表）。空段自动忽略。
    /// </summary>
    string QuoteQualified(params string?[] parts);
}
