using DbManager.Core.Abstractions;
using DbManager.Core.Dialects;
using Xunit;

namespace DbManager.Tests;

/// <summary>
/// 方言层 CREATE TABLE 生成的单元测试（本会话新增能力）。
/// </summary>
public class DialectCreateTableTests
{
    private static CreateTableColumnSpec Col(string quoted, string type, bool nullable = true, bool pk = false, bool auto = false, string? def = null)
        => new()
        {
            QuotedColumn = quoted,
            TypeString = type,
            IsNullable = nullable,
            IsPrimaryKey = pk,
            IsAutoIncrement = auto,
            DefaultLiteral = def
        };

    [Fact]
    public void PostgreSql_StandardCreate_HasPrimaryKeyClause()
    {
        var dialect = new PostgreSqlDialect();
        var sql = dialect.BuildCreateTable("\"public\".\"t\"", new[]
        {
            Col("\"id\"", "INTEGER", nullable: false, pk: true),
            Col("\"name\"", "VARCHAR(50)")
        });

        Assert.Contains("CREATE TABLE \"public\".\"t\"", sql);
        Assert.Contains("\"id\" INTEGER NOT NULL", sql);
        Assert.Contains("PRIMARY KEY (\"id\")", sql);
    }

    [Fact]
    public void MySql_AutoIncrement_UsesKeyword()
    {
        var dialect = new MySqlDialect();
        var sql = dialect.BuildCreateTable("`db`.`t`", new[]
        {
            Col("`id`", "INT", nullable: false, pk: true, auto: true)
        });

        Assert.Contains("AUTO_INCREMENT", sql);
        Assert.Contains("PRIMARY KEY (`id`)", sql);
    }

    [Fact]
    public void SqlServer_AutoIncrement_UsesIdentity()
    {
        var dialect = new SqlServerDialect();
        var sql = dialect.BuildCreateTable("[db].[dbo].[t]", new[]
        {
            Col("[id]", "INT", nullable: false, pk: true, auto: true)
        });

        Assert.Contains("IDENTITY(1,1)", sql);
    }

    [Fact]
    public void Sqlite_AutoIncrement_InlinesPrimaryKey_NoSeparateClause()
    {
        var dialect = new SqliteDialect();
        var sql = dialect.BuildCreateTable("\"t\"", new[]
        {
            Col("\"id\"", "INTEGER", nullable: false, pk: true, auto: true),
            Col("\"name\"", "TEXT")
        });

        Assert.Contains("\"id\" INTEGER PRIMARY KEY AUTOINCREMENT", sql);
        // 自增内联后不应再有表级 PRIMARY KEY 约束
        Assert.DoesNotContain("PRIMARY KEY (", sql);
    }

    [Fact]
    public void Sqlite_NoAutoIncrement_UsesStandardPrimaryKeyClause()
    {
        var dialect = new SqliteDialect();
        var sql = dialect.BuildCreateTable("\"t\"", new[]
        {
            Col("\"id\"", "INTEGER", nullable: false, pk: true),
            Col("\"name\"", "TEXT")
        });

        Assert.Contains("PRIMARY KEY (\"id\")", sql);
        Assert.DoesNotContain("AUTOINCREMENT", sql);
    }

    [Fact]
    public void DefaultLiteral_IsEmitted()
    {
        var dialect = new PostgreSqlDialect();
        var sql = dialect.BuildCreateTable("\"public\".\"t\"", new[]
        {
            Col("\"status\"", "VARCHAR(10)", def: "'active'")
        });

        Assert.Contains("DEFAULT 'active'", sql);
    }
}
