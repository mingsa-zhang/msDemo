using DbManager.Core.Abstractions;
using DbManager.Core.Dialects;
using Xunit;

namespace DbManager.Tests;

/// <summary>
/// 方言层分页、限定名、DDL（ADD/ALTER 列）的单元测试。
/// </summary>
public class DialectSqlTests
{
    [Fact]
    public void MySql_Paginate_UsesLimitOffset()
    {
        var d = new MySqlDialect();
        var sql = d.BuildPagedSelect("`t`", string.Empty, 2, 10);
        Assert.Contains("LIMIT 10 OFFSET 10", sql);
    }

    [Fact]
    public void SqlServer_Paginate_AddsOrderByAndFetch()
    {
        var d = new SqlServerDialect();
        var sql = d.BuildPagedSelect("[t]", string.Empty, 1, 20);
        Assert.Contains("OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY", sql);
        Assert.Contains("ORDER BY", sql); // 无排序时补默认排序
    }

    [Fact]
    public void Oracle_Paginate_UsesRownum()
    {
        var d = new OracleDialect();
        var sql = d.BuildPagedSelect("\"t\"", string.Empty, 2, 5);
        Assert.Contains("ROWNUM", sql);
        Assert.Contains("rn >= 6", sql);
    }

    [Fact]
    public void SqlServer_QualifyTable_DefaultsToDbo()
    {
        var d = new SqlServerDialect();
        Assert.Equal("[db].[dbo].[t]", d.QualifyTable("db", null, "t"));
        Assert.Equal("[db].[sales].[t]", d.QualifyTable("db", "sales", "t"));
    }

    [Fact]
    public void PostgreSql_QualifyTable_DefaultsToPublic()
    {
        var d = new PostgreSqlDialect();
        Assert.Equal("\"public\".\"t\"", d.QualifyTable("db", null, "t"));
    }

    [Fact]
    public void MySql_AddColumn_UsesAddColumnKeyword()
    {
        var d = new MySqlDialect();
        var sql = d.BuildAddColumn("`t`", "`c`", "VARCHAR(20) NOT NULL");
        Assert.Equal("ALTER TABLE `t` ADD COLUMN `c` VARCHAR(20) NOT NULL", sql);
    }

    [Fact]
    public void SqlServer_AddColumn_OmitsColumnKeyword()
    {
        var d = new SqlServerDialect();
        var sql = d.BuildAddColumn("[t]", "[c]", "INT NULL");
        Assert.Equal("ALTER TABLE [t] ADD [c] INT NULL", sql);
    }

    [Fact]
    public void MySql_AlterColumn_UsesModify()
    {
        var d = new MySqlDialect();
        var stmts = d.BuildAlterColumn("`t`", new ColumnAlterSpec
        {
            QuotedColumn = "`c`",
            FullDefinition = "VARCHAR(30) NOT NULL",
            NewTypeString = "VARCHAR(30)",
            TypeChanged = true
        });
        Assert.Single(stmts);
        Assert.Contains("MODIFY COLUMN `c` VARCHAR(30) NOT NULL", stmts[0]);
    }

    [Fact]
    public void Sqlite_AlterColumn_ReturnsCommentOnly()
    {
        var d = new SqliteDialect();
        var stmts = d.BuildAlterColumn("\"t\"", new ColumnAlterSpec
        {
            QuotedColumn = "\"c\"",
            NewTypeString = "TEXT",
            TypeChanged = true
        });
        Assert.Single(stmts);
        Assert.StartsWith("--", stmts[0].TrimStart());
    }

    [Fact]
    public void AlterColumn_NoChange_ReturnsEmpty()
    {
        var d = new PostgreSqlDialect();
        var stmts = d.BuildAlterColumn("\"t\"", new ColumnAlterSpec { QuotedColumn = "\"c\"" });
        Assert.Empty(stmts);
    }
}
