using DbManager.Core.Abstractions;
using DbManager.Core.Dialects;
using DbManager.Core.Enums;

namespace DbManager.Core.Services;

/// <summary>
/// 方言/类型映射的统一提供者：上层按 <see cref="DbTypeEnum"/> 一次取到对应抽象。
/// 实例无状态，按库缓存复用。
/// </summary>
public static class DialectProvider
{
    private static readonly Dictionary<DbTypeEnum, IDialect> _dialects = new()
    {
        [DbTypeEnum.MySql] = new MySqlDialect(),
        [DbTypeEnum.MariaDB] = new MySqlDialect(),
        [DbTypeEnum.SqlServer] = new SqlServerDialect(),
        [DbTypeEnum.PostgreSQL] = new PostgreSqlDialect(),
        [DbTypeEnum.Oracle] = new OracleDialect(),
        [DbTypeEnum.SQLite] = new SqliteDialect(),
        [DbTypeEnum.DB2] = new Db2Dialect()
    };

    /// <summary>
    /// 获取指定数据库类型的方言（含标识符引用层）。
    /// </summary>
    public static IDialect GetDialect(DbTypeEnum dbType)
    {
        if (_dialects.TryGetValue(dbType, out var dialect))
        {
            return dialect;
        }

        throw new NotSupportedException($"数据库类型 {dbType} 暂无方言实现");
    }

    /// <summary>
    /// 获取指定数据库类型的标识符引用层。
    /// </summary>
    public static IIdentifierQuoter GetQuoter(DbTypeEnum dbType) => GetDialect(dbType).Quoter;

    /// <summary>
    /// 获取指定数据库类型的类型映射器。
    /// </summary>
    public static IDbTypeMapper GetTypeMapper(DbTypeEnum dbType) => DefaultDbTypeMapper.For(dbType);
}
