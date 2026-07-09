namespace DbManager.Core.Abstractions;

/// <summary>
/// "新建表"单列定义，供方言层拼装各库专属的 CREATE TABLE 语句。
/// </summary>
public sealed class CreateTableColumnSpec
{
    /// <summary>
    /// 已加引号的列名
    /// </summary>
    public string QuotedColumn { get; init; } = string.Empty;

    /// <summary>
    /// 类型串（含长度），如 VARCHAR(50)
    /// </summary>
    public string TypeString { get; init; } = string.Empty;

    /// <summary>
    /// 是否可空
    /// </summary>
    public bool IsNullable { get; init; }

    /// <summary>
    /// 是否主键
    /// </summary>
    public bool IsPrimaryKey { get; init; }

    /// <summary>
    /// 是否自增
    /// </summary>
    public bool IsAutoIncrement { get; init; }

    /// <summary>
    /// 已格式化的默认值字面量；null 表示无默认值
    /// </summary>
    public string? DefaultLiteral { get; init; }
}
