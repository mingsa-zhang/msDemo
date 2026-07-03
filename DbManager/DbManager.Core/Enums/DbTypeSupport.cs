namespace DbManager.Core.Enums;

/// <summary>
/// 数据库类型实现状态的单一事实源。
/// 元数据/执行服务工厂已实现的类型才返回 true；
/// 未实现的类型（MongoDB/Redis/DB2）在 UI 层据此禁用，避免"建了连接却一展开就抛异常"。
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
        DbTypeEnum.SQLite
    };

    /// <summary>
    /// 判断指定数据库类型是否已具备完整的元数据/执行实现。
    /// </summary>
    public static bool IsImplemented(DbTypeEnum dbType) => _implemented.Contains(dbType);
}
