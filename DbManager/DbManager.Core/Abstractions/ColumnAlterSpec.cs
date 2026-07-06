namespace DbManager.Core.Abstractions;

/// <summary>
/// "修改列"的目标状态与变更标记，供方言层生成各库专属的 ALTER 语句。
/// </summary>
public sealed class ColumnAlterSpec
{
    /// <summary>
    /// 已加引号的列名
    /// </summary>
    public string QuotedColumn { get; init; } = string.Empty;

    /// <summary>
    /// 新类型串（含长度），如 VARCHAR(50)
    /// </summary>
    public string NewTypeString { get; init; } = string.Empty;

    /// <summary>
    /// 目标是否可空
    /// </summary>
    public bool IsNullable { get; init; }

    /// <summary>
    /// 已格式化的默认值字面量；null 表示无默认值/删除默认
    /// </summary>
    public string? DefaultLiteral { get; init; }

    /// <summary>
    /// 完整的新列定义（类型 + NOT NULL + DEFAULT），供 MySQL/Oracle 的 MODIFY 语法使用
    /// </summary>
    public string FullDefinition { get; init; } = string.Empty;

    /// <summary>
    /// 类型是否变更
    /// </summary>
    public bool TypeChanged { get; init; }

    /// <summary>
    /// 可空性是否变更
    /// </summary>
    public bool NullabilityChanged { get; init; }

    /// <summary>
    /// 默认值是否变更
    /// </summary>
    public bool DefaultChanged { get; init; }

    /// <summary>
    /// 是否存在任何变更
    /// </summary>
    public bool HasAnyChange => TypeChanged || NullabilityChanged || DefaultChanged;
}
