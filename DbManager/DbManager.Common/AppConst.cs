namespace DbManager.Common;

public static class AppConst
{
    // 默认端口
    public const int DefaultMySqlPort = 3306;
    public const int DefaultSqlServerPort = 1433;
    public const int DefaultPostgreSqlPort = 5432;
    public const int DefaultOraclePort = 1521;
    public const int DefaultMongoDbPort = 27017;
    public const int DefaultRedisPort = 6379;
    public const int DefaultDb2Port = 50000;

    // 超时
    public const int DefaultConnectTimeout = 30;
    public const int DefaultCommandTimeout = 60;

    // 路径
    public static string AppDataDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DbManager");
    public static string DbFilePath => Path.Combine(AppDataDir, "dbmanager.db");
    public static string LogDir => Path.Combine(AppDataDir, "logs");
    public static string SqlHistoryDir => Path.Combine(AppDataDir, "sql_history");
    public static string ScriptsDir => Path.Combine(AppDataDir, "scripts");

    // 分页
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 500;

    // 提示
    public const string ConnSuccess = "连接成功";
    public const string ConnFail = "连接失败";
    public const string SaveSuccess = "保存成功";
    public const string DeleteConfirm = "确定要删除吗？";
    public const string UnsavedConfirm = "有未保存的更改，确定要关闭吗？";
}
