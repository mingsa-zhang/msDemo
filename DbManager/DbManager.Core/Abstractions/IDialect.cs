namespace DbManager.Core.Abstractions;

/// <summary>
/// 方言层：屏蔽各数据库的分页、限行、语法差异，并统一持有标识符引用层。
/// 上层（数据浏览、SQL 执行、表设计）只依赖本接口，禁止散写 switch(dbType)。
/// </summary>
public interface IDialect
{
    /// <summary>
    /// 该方言对应的标识符引用层
    /// </summary>
    IIdentifierQuoter Quoter { get; }

    /// <summary>
    /// 组合"库/Schema/表"为已加引号的限定名（各库限定规则不同）。
    /// </summary>
    string QualifyTable(string? database, string? schema, string table);

    /// <summary>
    /// 为一段 SELECT 追加分页（LIMIT/OFFSET、FETCH、ROWNUM 等）。
    /// </summary>
    string Paginate(string sql, int pageIndex, int pageSize);

    /// <summary>
    /// 构建"取一页数据"的 SELECT 语句。whereClause 已含 WHERE 关键字，可为空。
    /// </summary>
    string BuildPagedSelect(string qualifiedTable, string whereClause, int pageIndex, int pageSize);

    /// <summary>
    /// 构建"统计总数"的 SELECT COUNT 语句。whereClause 已含 WHERE 关键字，可为空。
    /// </summary>
    string BuildCount(string qualifiedTable, string whereClause);

    /// <summary>
    /// 当前时间函数的查询语句
    /// </summary>
    string CurrentTimeSql();

    /// <summary>
    /// 自增列语法片段
    /// </summary>
    string AutoIncrementKeyword();

    /// <summary>
    /// 字符串拼接运算符/函数
    /// </summary>
    string ConcatOperator();

    /// <summary>
    /// 生成"新增列"语句。
    /// </summary>
    string BuildAddColumn(string qualifiedTable, string quotedColumn, string columnDefinition);

    /// <summary>
    /// 生成"删除列"语句。
    /// </summary>
    string BuildDropColumn(string qualifiedTable, string quotedColumn);

    /// <summary>
    /// 生成"修改列"语句（各库语法差异大，可能返回 0 或多条）。
    /// </summary>
    IReadOnlyList<string> BuildAlterColumn(string qualifiedTable, ColumnAlterSpec spec);
}
