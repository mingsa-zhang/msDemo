using DbManager.Core.Enums;

namespace DbManager.Core.Abstractions;

/// <summary>
/// 类型映射层：数据库原生类型 ↔ 统一逻辑类型。
/// 供呈现层按逻辑类型选择单元格编辑器，供表设计列出可选类型。
/// </summary>
public interface IDbTypeMapper
{
    /// <summary>
    /// 将数据库原生类型名（如 varchar、int、timestamp）映射为逻辑类型。
    /// </summary>
    LogicalTypeEnum ToLogicalType(string nativeType);

    /// <summary>
    /// 列出该库在表设计时可供选择的原生类型清单。
    /// </summary>
    IReadOnlyList<string> GetNativeTypes();
}
