using DbManager.Core.Enums;

namespace DbManager.Core.Adapters;

public static class DbSqlSyntaxAdapter
{
    public static string GetPagedSql(DbTypeEnum dbType, string sql, int pageIndex, int pageSize)
    {
        var offset = (pageIndex - 1) * pageSize;
        return dbType switch
        {
            DbTypeEnum.MySql or DbTypeEnum.MariaDB or DbTypeEnum.PostgreSQL or DbTypeEnum.SQLite
                => $"{sql} LIMIT {pageSize} OFFSET {offset}",
            DbTypeEnum.SqlServer or DbTypeEnum.DB2
                => sql.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase)
                    ? $"{sql} OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY"
                    : $"{sql} ORDER BY (SELECT NULL) OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY",
            DbTypeEnum.Oracle
                => $"SELECT * FROM (SELECT a.*, ROWNUM rn FROM ({sql}) a WHERE ROWNUM <= {offset + pageSize}) WHERE rn >= {offset + 1}",
            DbTypeEnum.MongoDB or DbTypeEnum.Redis => throw new NotSupportedException("非关系型数据库不支持SQL分页"),
            _ => sql
        };
    }

    public static string GetTopSql(DbTypeEnum dbType, string sql, int topCount)
    {
        return dbType switch
        {
            DbTypeEnum.SqlServer => ReplaceFirstSelect(sql, topCount),
            DbTypeEnum.Oracle => $"SELECT * FROM ({sql}) WHERE ROWNUM <= {topCount}",
            DbTypeEnum.DB2 => $"{sql} FETCH FIRST {topCount} ROWS ONLY",
            DbTypeEnum.MongoDB or DbTypeEnum.Redis => throw new NotSupportedException("非关系型数据库不支持SQL"),
            _ => $"{sql} LIMIT {topCount}"
        };
    }

    private static string ReplaceFirstSelect(string sql, int topCount)
    {
        var index = sql.IndexOf("SELECT ", StringComparison.OrdinalIgnoreCase);
        if (index < 0) return sql;
        return string.Concat(sql.AsSpan(0, index), $"SELECT TOP {topCount} ", sql.AsSpan(index + 7));
    }

    public static string GetCurrentTimeSql(DbTypeEnum dbType)
    {
        return dbType switch
        {
            DbTypeEnum.MySql or DbTypeEnum.MariaDB => "SELECT NOW()",
            DbTypeEnum.SqlServer => "SELECT GETDATE()",
            DbTypeEnum.PostgreSQL => "SELECT NOW()",
            DbTypeEnum.Oracle => "SELECT SYSDATE FROM DUAL",
            DbTypeEnum.SQLite => "SELECT datetime('now')",
            DbTypeEnum.DB2 => "SELECT CURRENT TIMESTAMP FROM SYSIBM.SYSDUMMY1",
            DbTypeEnum.MongoDB or DbTypeEnum.Redis => throw new NotSupportedException("非关系型数据库不支持SQL"),
            _ => "SELECT NOW()"
        };
    }

    public static string GetAutoIncrementSyntax(DbTypeEnum dbType)
    {
        return dbType switch
        {
            DbTypeEnum.MySql or DbTypeEnum.MariaDB => "AUTO_INCREMENT",
            DbTypeEnum.SqlServer => "IDENTITY(1,1)",
            DbTypeEnum.PostgreSQL => "GENERATED ALWAYS AS IDENTITY",
            DbTypeEnum.SQLite => "AUTOINCREMENT",
            DbTypeEnum.Oracle => "GENERATED ALWAYS AS IDENTITY",
            DbTypeEnum.DB2 => "GENERATED ALWAYS AS IDENTITY",
            DbTypeEnum.MongoDB or DbTypeEnum.Redis => throw new NotSupportedException("非关系型数据库不支持自增"),
            _ => "AUTO_INCREMENT"
        };
    }

    public static string GetConcatOperator(DbTypeEnum dbType)
    {
        return dbType switch
        {
            DbTypeEnum.MySql or DbTypeEnum.MariaDB => "CONCAT",
            DbTypeEnum.SqlServer => "+",
            DbTypeEnum.PostgreSQL or DbTypeEnum.Oracle or DbTypeEnum.SQLite or DbTypeEnum.DB2 => "||",
            DbTypeEnum.MongoDB or DbTypeEnum.Redis => throw new NotSupportedException("非关系型数据库不支持SQL"),
            _ => "CONCAT"
        };
    }
}