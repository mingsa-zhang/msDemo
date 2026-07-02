using DbManager.Core.Enums;
using DbManager.Core.Models;
using DbManager.Common;

namespace DbManager.Core.Adapters;

public static class DbConnStringBuilder
{
    /// <summary>
    /// 构建连接字符串。要求传入的密码已经是明文（调用方负责解密）。
    /// </summary>
    public static string BuildConnectionString(DbConnectionModel conn)
    {
        if (!string.IsNullOrEmpty(conn.ConnectionString))
            return conn.ConnectionString;

        return conn.DbType switch
        {
            DbTypeEnum.MySql => BuildMySql(conn),
            DbTypeEnum.MariaDB => BuildMySql(conn),
            DbTypeEnum.SqlServer => BuildSqlServer(conn),
            DbTypeEnum.PostgreSQL => BuildPostgreSql(conn),
            DbTypeEnum.Oracle => BuildOracle(conn),
            DbTypeEnum.SQLite => BuildSQLite(conn),
            DbTypeEnum.MongoDB => BuildMongoDb(conn),
            DbTypeEnum.Redis => BuildRedis(conn),
            DbTypeEnum.DB2 => BuildDb2(conn),
            _ => throw new NotSupportedException($"不支持的数据库类型: {conn.DbType}")
        };
    }

    /// <summary>
    /// 解密密码并构建连接字符串（一站式方法，避免双重解密）。
    /// </summary>
    public static string BuildDecryptedConnectionString(DbConnectionModel conn)
    {
        var cloned = conn.Clone();
        if (!string.IsNullOrEmpty(cloned.Password))
            cloned.Password = DecryptPassword(cloned.Password);
        if (!string.IsNullOrEmpty(cloned.RedisPassword))
            cloned.RedisPassword = DecryptPassword(cloned.RedisPassword);
        return BuildConnectionString(cloned);
    }

    public static string EncryptPassword(string plainPassword)
    {
        return PasswordEncryptHelper.Encrypt(plainPassword);
    }

    public static string DecryptPassword(string encryptedPassword)
    {
        return PasswordEncryptHelper.Decrypt(encryptedPassword);
    }

    private static string BuildMySql(DbConnectionModel conn)
    {
        var port = conn.Port > 0 ? conn.Port : AppConst.DefaultMySqlPort;
        var sb = $"Server={conn.Host};Port={port};Database={conn.DbName};Uid={conn.UserName};Pwd={conn.Password};";
        if (conn.ConnectTimeout > 0) sb += $"ConnectionTimeout={conn.ConnectTimeout};";
        if (conn.EnableSSL) sb += "SslMode=Required;";
        return sb;
    }

    private static string BuildSqlServer(DbConnectionModel conn)
    {
        var port = conn.Port > 0 ? conn.Port : AppConst.DefaultSqlServerPort;
        var sb = $"Server={conn.Host},{port};Database={conn.DbName};User Id={conn.UserName};Password={conn.Password};";
        if (conn.ConnectTimeout > 0) sb += $"Connection Timeout={conn.ConnectTimeout};";
        if (conn.EnableSSL) sb += "Encrypt=True;TrustServerCertificate=True;";
        return sb;
    }

    private static string BuildPostgreSql(DbConnectionModel conn)
    {
        var port = conn.Port > 0 ? conn.Port : AppConst.DefaultPostgreSqlPort;
        var sb = $"Host={conn.Host};Port={port};Database={conn.DbName};Username={conn.UserName};Password={conn.Password};";
        if (conn.ConnectTimeout > 0) sb += $"Timeout={conn.ConnectTimeout};";
        if (conn.EnableSSL) sb += "SSL Mode=Require;Trust Server Certificate=true;";
        return sb;
    }

    private static string BuildOracle(DbConnectionModel conn)
    {
        var port = conn.Port > 0 ? conn.Port : AppConst.DefaultOraclePort;
        var serviceName = !string.IsNullOrEmpty(conn.OracleServiceName) ? conn.OracleServiceName : (!string.IsNullOrEmpty(conn.DbName) ? conn.DbName : "ORCL");
        var sb = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={conn.Host})(PORT={port}))(CONNECT_DATA=(SERVICE_NAME={serviceName})));User Id={conn.UserName};Password={conn.Password};";
        if (conn.ConnectTimeout > 0) sb += $"Connection Timeout={conn.ConnectTimeout};";
        return sb;
    }

    private static string BuildSQLite(DbConnectionModel conn)
    {
        var filePath = !string.IsNullOrEmpty(conn.SqliteFilePath) ? conn.SqliteFilePath : (!string.IsNullOrEmpty(conn.DbName) ? conn.DbName : "data.db");
        var timeout = conn.ConnectTimeout > 0 ? conn.ConnectTimeout * 1000 : 30000;
        return $"Data Source={filePath};BusyTimeout={timeout};";
    }

    private static string BuildMongoDb(DbConnectionModel conn)
    {
        var port = conn.Port > 0 ? conn.Port : AppConst.DefaultMongoDbPort;
        var authDb = !string.IsNullOrEmpty(conn.MongoAuthDb) ? conn.MongoAuthDb : "admin";
        var encodedUser = Uri.EscapeDataString(conn.UserName ?? "");
        var encodedPwd = Uri.EscapeDataString(conn.Password ?? "");
        var sb = $"mongodb://{encodedUser}:{encodedPwd}@{conn.Host}:{port}/{conn.DbName}?authSource={authDb}";
        if (conn.EnableSSL) sb += "&ssl=true";
        return sb;
    }

    private static string BuildRedis(DbConnectionModel conn)
    {
        var port = conn.Port > 0 ? conn.Port : AppConst.DefaultRedisPort;
        var password = !string.IsNullOrEmpty(conn.RedisPassword) ? conn.RedisPassword : conn.Password;
        var sb = $"{conn.Host}:{port}";
        if (!string.IsNullOrEmpty(password)) sb += $",password={password}";
        if (conn.ConnectTimeout > 0) sb += $",connectTimeout={conn.ConnectTimeout * 1000}";
        return sb;
    }

    private static string BuildDb2(DbConnectionModel conn)
    {
        var port = conn.Port > 0 ? conn.Port : AppConst.DefaultDb2Port;
        return $"Server={conn.Host}:{port};Database={conn.DbName};UID={conn.UserName};PWD={conn.Password};";
    }
}
