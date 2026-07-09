using DbManager.Core.Abstractions;
using DbManager.Core.Enums;

namespace DbManager.Core.Dialects;

/// <summary>
/// 默认类型映射：基于原生类型名的关键字启发式分类，覆盖主流关系库。
/// 各库仅"可选类型清单"不同，分类规则共用。
/// </summary>
public sealed class DefaultDbTypeMapper : IDbTypeMapper
{
    private readonly IReadOnlyList<string> _nativeTypes;

    public DefaultDbTypeMapper(IReadOnlyList<string> nativeTypes)
    {
        _nativeTypes = nativeTypes;
    }

    public LogicalTypeEnum ToLogicalType(string nativeType)
    {
        if (string.IsNullOrWhiteSpace(nativeType))
        {
            return LogicalTypeEnum.Unknown;
        }

        var t = nativeType.Trim().ToLowerInvariant();

        // 去掉长度/精度括号，如 varchar(255) -> varchar
        var paren = t.IndexOf('(');
        if (paren > 0)
        {
            t = t[..paren];
        }
        t = t.Trim();

        if (t.Contains("json"))
        {
            return LogicalTypeEnum.Json;
        }
        if (t is "uniqueidentifier" or "uuid" or "guid")
        {
            return LogicalTypeEnum.Guid;
        }
        if (t.Contains("bool") || t == "bit")
        {
            return LogicalTypeEnum.Boolean;
        }
        if (t.Contains("date") || t.Contains("time") || t.Contains("timestamp") || t.Contains("year"))
        {
            return LogicalTypeEnum.DateTime;
        }
        if (t.Contains("blob") || t.Contains("binary") || t.Contains("bytea") || t.Contains("image") || t == "raw")
        {
            return LogicalTypeEnum.Binary;
        }
        if (t.Contains("int") || t.Contains("dec") || t.Contains("numeric") || t.Contains("float")
            || t.Contains("double") || t.Contains("real") || t.Contains("money") || t.Contains("number"))
        {
            return LogicalTypeEnum.Number;
        }
        // CLOB/NCLOB 为字符大对象，归为文本（不是二进制）
        if (t.Contains("char") || t.Contains("text") || t.Contains("string") || t.Contains("nvarchar") || t.Contains("clob") || t == "enum")
        {
            return LogicalTypeEnum.Text;
        }

        return LogicalTypeEnum.Unknown;
    }

    public IReadOnlyList<string> GetNativeTypes() => _nativeTypes;

    /// <summary>
    /// 按数据库类型创建带对应可选类型清单的映射器。
    /// </summary>
    public static DefaultDbTypeMapper For(DbTypeEnum dbType)
    {
        var types = dbType switch
        {
            DbTypeEnum.MySql or DbTypeEnum.MariaDB => new[]
            {
                "INT", "BIGINT", "TINYINT", "SMALLINT", "DECIMAL", "FLOAT", "DOUBLE",
                "VARCHAR", "CHAR", "TEXT", "LONGTEXT", "DATE", "DATETIME", "TIMESTAMP",
                "TIME", "YEAR", "BOOLEAN", "BLOB", "JSON", "ENUM"
            },
            DbTypeEnum.SqlServer => new[]
            {
                "INT", "BIGINT", "SMALLINT", "TINYINT", "DECIMAL", "NUMERIC", "FLOAT", "REAL", "MONEY",
                "VARCHAR", "NVARCHAR", "CHAR", "NCHAR", "TEXT", "NTEXT", "DATE", "DATETIME", "DATETIME2",
                "TIME", "BIT", "UNIQUEIDENTIFIER", "VARBINARY", "IMAGE"
            },
            DbTypeEnum.PostgreSQL => new[]
            {
                "INTEGER", "BIGINT", "SMALLINT", "NUMERIC", "REAL", "DOUBLE PRECISION",
                "VARCHAR", "CHAR", "TEXT", "DATE", "TIMESTAMP", "TIME", "BOOLEAN",
                "BYTEA", "JSON", "JSONB", "UUID"
            },
            DbTypeEnum.Oracle => new[]
            {
                "NUMBER", "FLOAT", "VARCHAR2", "CHAR", "NVARCHAR2", "CLOB", "DATE",
                "TIMESTAMP", "BLOB", "RAW"
            },
            DbTypeEnum.SQLite => new[]
            {
                "INTEGER", "REAL", "TEXT", "BLOB", "NUMERIC"
            },
            DbTypeEnum.DB2 => new[]
            {
                "INTEGER", "BIGINT", "SMALLINT", "DECIMAL", "DOUBLE", "VARCHAR", "CHAR",
                "CLOB", "DATE", "TIME", "TIMESTAMP", "BLOB"
            },
            _ => Array.Empty<string>()
        };

        return new DefaultDbTypeMapper(types);
    }
}
