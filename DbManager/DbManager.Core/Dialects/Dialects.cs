using DbManager.Core.Abstractions;

namespace DbManager.Core.Dialects;

/// <summary>
/// 方言基类：默认走 LIMIT/OFFSET 分页，子类按需覆写。
/// </summary>
public abstract class DialectBase : IDialect
{
    public IIdentifierQuoter Quoter { get; }

    protected DialectBase(IIdentifierQuoter quoter)
    {
        Quoter = quoter;
    }

    /// <summary>
    /// 默认限定规则：库.表（库为空则仅表）。SqlServer/PG 等覆写。
    /// </summary>
    public virtual string QualifyTable(string? database, string? schema, string table)
        => Quoter.QuoteQualified(database, table);

    /// <summary>
    /// 默认 LIMIT/OFFSET 分页
    /// </summary>
    public virtual string Paginate(string sql, int pageIndex, int pageSize)
    {
        var offset = (pageIndex - 1) * pageSize;
        return $"{sql} LIMIT {pageSize} OFFSET {offset}";
    }

    public string BuildPagedSelect(string qualifiedTable, string whereClause, int pageIndex, int pageSize)
        => Paginate($"SELECT * FROM {qualifiedTable}{NormalizeWhere(whereClause)}", pageIndex, pageSize);

    public string BuildCount(string qualifiedTable, string whereClause)
        => $"SELECT COUNT(*) FROM {qualifiedTable}{NormalizeWhere(whereClause)}";

    public abstract string CurrentTimeSql();
    public abstract string AutoIncrementKeyword();
    public abstract string ConcatOperator();

    /// <summary>
    /// 规范化 where 片段：为空则空串，否则确保前置一个空格。
    /// </summary>
    protected static string NormalizeWhere(string whereClause)
    {
        if (string.IsNullOrWhiteSpace(whereClause))
        {
            return string.Empty;
        }

        return whereClause.StartsWith(' ') ? whereClause : $" {whereClause}";
    }
}

/// <summary>
/// MySQL / MariaDB
/// </summary>
public sealed class MySqlDialect : DialectBase
{
    public MySqlDialect() : base(new BacktickQuoter()) { }
    public override string CurrentTimeSql() => "SELECT NOW()";
    public override string AutoIncrementKeyword() => "AUTO_INCREMENT";
    public override string ConcatOperator() => "CONCAT";
}

/// <summary>
/// SQL Server：库.dbo.表 限定，OFFSET/FETCH 分页（需 ORDER BY）。
/// </summary>
public sealed class SqlServerDialect : DialectBase
{
    public SqlServerDialect() : base(new BracketQuoter()) { }

    public override string QualifyTable(string? database, string? schema, string table)
        => Quoter.QuoteQualified(database, string.IsNullOrWhiteSpace(schema) ? "dbo" : schema, table);

    public override string Paginate(string sql, int pageIndex, int pageSize)
    {
        var offset = (pageIndex - 1) * pageSize;
        var ordered = sql.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase)
            ? sql
            : $"{sql} ORDER BY (SELECT NULL)";
        return $"{ordered} OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
    }

    public override string CurrentTimeSql() => "SELECT GETDATE()";
    public override string AutoIncrementKeyword() => "IDENTITY(1,1)";
    public override string ConcatOperator() => "+";
}

/// <summary>
/// PostgreSQL：schema.表（默认 public）限定。
/// </summary>
public sealed class PostgreSqlDialect : DialectBase
{
    public PostgreSqlDialect() : base(new DoubleQuoteQuoter()) { }

    public override string QualifyTable(string? database, string? schema, string table)
        => Quoter.QuoteQualified(string.IsNullOrWhiteSpace(schema) ? "public" : schema, table);

    public override string CurrentTimeSql() => "SELECT NOW()";
    public override string AutoIncrementKeyword() => "GENERATED ALWAYS AS IDENTITY";
    public override string ConcatOperator() => "||";
}

/// <summary>
/// Oracle：schema.表 限定，ROWNUM 分页。
/// </summary>
public sealed class OracleDialect : DialectBase
{
    public OracleDialect() : base(new DoubleQuoteQuoter()) { }

    public override string QualifyTable(string? database, string? schema, string table)
        => Quoter.QuoteQualified(schema, table);

    public override string Paginate(string sql, int pageIndex, int pageSize)
    {
        var offset = (pageIndex - 1) * pageSize;
        return $"SELECT * FROM (SELECT a.*, ROWNUM rn FROM ({sql}) a WHERE ROWNUM <= {offset + pageSize}) WHERE rn >= {offset + 1}";
    }

    public override string CurrentTimeSql() => "SELECT SYSDATE FROM DUAL";
    public override string AutoIncrementKeyword() => "GENERATED ALWAYS AS IDENTITY";
    public override string ConcatOperator() => "||";
}

/// <summary>
/// SQLite：仅表名限定。
/// </summary>
public sealed class SqliteDialect : DialectBase
{
    public SqliteDialect() : base(new DoubleQuoteQuoter()) { }

    public override string QualifyTable(string? database, string? schema, string table)
        => Quoter.Quote(table);

    public override string CurrentTimeSql() => "SELECT datetime('now')";
    public override string AutoIncrementKeyword() => "AUTOINCREMENT";
    public override string ConcatOperator() => "||";
}

/// <summary>
/// DB2：schema.表 限定，OFFSET/FETCH 分页。
/// </summary>
public sealed class Db2Dialect : DialectBase
{
    public Db2Dialect() : base(new DoubleQuoteQuoter()) { }

    public override string QualifyTable(string? database, string? schema, string table)
        => Quoter.QuoteQualified(schema, table);

    public override string Paginate(string sql, int pageIndex, int pageSize)
    {
        var offset = (pageIndex - 1) * pageSize;
        var ordered = sql.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase)
            ? sql
            : $"{sql} ORDER BY 1";
        return $"{ordered} OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
    }

    public override string CurrentTimeSql() => "SELECT CURRENT TIMESTAMP FROM SYSIBM.SYSDUMMY1";
    public override string AutoIncrementKeyword() => "GENERATED ALWAYS AS IDENTITY";
    public override string ConcatOperator() => "||";
}
