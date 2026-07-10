namespace DbManager.Core.Enums;

/// <summary>
/// 数据库类型实现状态的单一事实源。
/// 关系型走完整元数据/执行实现；MongoDB/Redis 走各自的只读专用服务与浏览界面。
/// 仍未实现的类型（DB2）在 UI 层据此禁用，避免"建了连接却一展开就抛异常"。
/// </summary>
public static class DbTypeSupport
{
    private static readonly HashSet<DbTypeEnum> _implemented = new()
    {
        DbTypeEnum.MySql,
        DbTypeEnum.MariaDB,
        DbTypeEnum.SqlServer,
        DbTypeEnum.PostgreSQL,
        DbTypeEnum.Oracle,
        DbTypeEnum.SQLite,
        DbTypeEnum.MongoDB,
        DbTypeEnum.Redis
    };

    /// <summary>
    /// 关系型数据库（走 SQL 元数据/执行工厂与关系型树/数据浏览）。
    /// MongoDB/Redis 虽已支持，但走独立的只读服务与界面，需单独判断。
    /// </summary>
    public static bool IsRelational(DbTypeEnum dbType) => dbType is DbTypeEnum.MySql or DbTypeEnum.MariaDB
        or DbTypeEnum.SqlServer or DbTypeEnum.PostgreSQL or DbTypeEnum.Oracle or DbTypeEnum.SQLite or DbTypeEnum.DB2;

    /// <summary>
    /// 判断指定数据库类型是否已可连接使用。
    /// </summary>
    public static bool IsImplemented(DbTypeEnum dbType) => _implemented.Contains(dbType);
}
