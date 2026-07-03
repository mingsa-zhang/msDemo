namespace DbManager.Core.Enums;

/// <summary>
/// 跨库统一的逻辑数据类型，用于驱动呈现层单元格编辑器选型与表设计类型下拉。
/// </summary>
public enum LogicalTypeEnum
{
    /// <summary>
    /// 文本
    /// </summary>
    Text = 0,

    /// <summary>
    /// 数值（整数/小数）
    /// </summary>
    Number = 1,

    /// <summary>
    /// 布尔
    /// </summary>
    Boolean = 2,

    /// <summary>
    /// 日期时间
    /// </summary>
    DateTime = 3,

    /// <summary>
    /// 二进制/BLOB
    /// </summary>
    Binary = 4,

    /// <summary>
    /// JSON
    /// </summary>
    Json = 5,

    /// <summary>
    /// GUID/UUID
    /// </summary>
    Guid = 6,

    /// <summary>
    /// 未知/未分类
    /// </summary>
    Unknown = 99
}
