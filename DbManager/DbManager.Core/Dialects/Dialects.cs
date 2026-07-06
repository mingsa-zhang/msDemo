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

    // ===== DDL：默认走标准/PostgreSQL 风格，子类按需覆写 =====

    public virtual string BuildAddColumn(string qualifiedTable, string quotedColumn, string columnDefinition)
        => $"ALTER TABLE {qualifiedTable} ADD COLUMN {quotedColumn} {columnDefinition}";

    public virtual string BuildDropColumn(string qualifiedTable, string quotedColumn)
        => $"ALTER TABLE {qualifiedTable} DROP COLUMN {quotedColumn}";

    /// <summary>
    /// PostgreSQL 风格：类型/可空/默认各用独立 ALTER COLUMN 子句，合并到一条 ALTER TABLE。
    /// </summary>
    public virtual IReadOnlyList<string> BuildAlterColumn(string qualifiedTable, ColumnAlterSpec spec)
    {
        var clauses = new List<string>();
        if (spec.TypeChanged)
        {
            clauses.Add($"ALTER COLUMN {spec.QuotedColumn} TYPE {spec.NewTypeString}");
        }
        if (spec.NullabilityChanged)
        {
            clauses.Add($"ALTER COLUMN {spec.QuotedColumn} {(spec.IsNullable ? "DROP NOT NULL" : "SET NOT NULL")}");
        }
        if (spec.DefaultChanged)
        {
            clauses.Add($"ALTER COLUMN {spec.QuotedColumn} {(spec.DefaultLiteral == null ? "DROP DEFAULT" : $"SET DEFAULT {spec.DefaultLiteral}")}");
        }
        return clauses.Count == 0
            ? Array.Empty<string>()
            : new[] { $"ALTER TABLE {qualifiedTable} {string.Join(", ", clauses)}" };
    }

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

    // MySQL 用 MODIFY COLUMN 重述完整列定义
    public override IReadOnlyList<string> BuildAlterColumn(string qualifiedTable, ColumnAlterSpec spec)
        => spec.HasAnyChange
            ? new[] { $"ALTER TABLE {qualifiedTable} MODIFY COLUMN {spec.QuotedColumn} {spec.FullDefinition}" }
            : Array.Empty<string>();
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

    // SqlServer: ADD 不带 COLUMN 关键字
    public override string BuildAddColumn(string qualifiedTable, string quotedColumn, string columnDefinition)
        => $"ALTER TABLE {qualifiedTable} ADD {quotedColumn} {columnDefinition}";

    // SqlServer: 类型+可空一条 ALTER COLUMN；默认值是独立约束，单独 ADD DEFAULT
    public override IReadOnlyList<string> BuildAlterColumn(string qualifiedTable, ColumnAlterSpec spec)
    {
        var stmts = new List<string>();
        if (spec.TypeChanged || spec.NullabilityChanged)
        {
            stmts.Add($"ALTER TABLE {qualifiedTable} ALTER COLUMN {spec.QuotedColumn} {spec.NewTypeString} {(spec.IsNullable ? "NULL" : "NOT NULL")}");
        }
        if (spec.DefaultChanged && spec.DefaultLiteral != null)
        {
            stmts.Add($"ALTER TABLE {qualifiedTable} ADD DEFAULT {spec.DefaultLiteral} FOR {spec.QuotedColumn}");
        }
        return stmts;
    }
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

    // Oracle: ADD/MODIFY 用括号包裹列定义
    public override string BuildAddColumn(string qualifiedTable, string quotedColumn, string columnDefinition)
        => $"ALTER TABLE {qualifiedTable} ADD ({quotedColumn} {columnDefinition})";

    public override IReadOnlyList<string> BuildAlterColumn(string qualifiedTable, ColumnAlterSpec spec)
        => spec.HasAnyChange
            ? new[] { $"ALTER TABLE {qualifiedTable} MODIFY ({spec.QuotedColumn} {spec.FullDefinition})" }
            : Array.Empty<string>();
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

    // SQLite 不支持直接修改列（需重建表），以注释提示；执行时会跳过注释行
    public override IReadOnlyList<string> BuildAlterColumn(string qualifiedTable, ColumnAlterSpec spec)
        => spec.HasAnyChange
            ? new[] { $"-- SQLite 不支持直接修改列 {spec.QuotedColumn}，需重建表" }
            : Array.Empty<string>();
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

    // DB2: 改类型用 SET DATA TYPE
    public override IReadOnlyList<string> BuildAlterColumn(string qualifiedTable, ColumnAlterSpec spec)
    {
        var clauses = new List<string>();
        if (spec.TypeChanged)
        {
            clauses.Add($"ALTER COLUMN {spec.QuotedColumn} SET DATA TYPE {spec.NewTypeString}");
        }
        if (spec.NullabilityChanged)
        {
            clauses.Add($"ALTER COLUMN {spec.QuotedColumn} {(spec.IsNullable ? "DROP NOT NULL" : "SET NOT NULL")}");
        }
        if (spec.DefaultChanged)
        {
            clauses.Add($"ALTER COLUMN {spec.QuotedColumn} {(spec.DefaultLiteral == null ? "DROP DEFAULT" : $"SET DEFAULT {spec.DefaultLiteral}")}");
        }
        return clauses.Count == 0
            ? Array.Empty<string>()
            : new[] { $"ALTER TABLE {qualifiedTable} {string.Join(", ", clauses)}" };
    }
}
