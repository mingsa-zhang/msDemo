using DbManager.Core.Enums;
using DbManager.Core.Models;
using DbManager.Core.Services;
using DbManager.Common;

namespace DbManager.Core.Adapters;

public static class DbConnStringBuilder
{
    /// <summary>
    /// 构建连接字符串。要求传入的密码已经是明文（调用方负责解密）。
    /// 若启用 SSH，会先拉起隧道并把主机/端口改写为本地转发端点。
    /// </summary>
    public static string BuildConnectionString(DbConnectionModel conn)
    {
        if (!string.IsNullOrEmpty(conn.ConnectionString))
            return conn.ConnectionString;

        var effective = MaybeApplySshTunnel(conn);

        return effective.DbType switch
        {
            DbTypeEnum.MySql => BuildMySql(effective),
            DbTypeEnum.MariaDB => BuildMySql(effective),
            DbTypeEnum.SqlServer => BuildSqlServer(effective),
            DbTypeEnum.PostgreSQL => BuildPostgreSql(effective),
            DbTypeEnum.Oracle => BuildOracle(effective),
            DbTypeEnum.SQLite => BuildSQLite(effective),
            DbTypeEnum.MongoDB => BuildMongoDb(effective),
            DbTypeEnum.Redis => BuildRedis(effective),
            DbTypeEnum.DB2 => BuildDb2(effective),
            _ => throw new NotSupportedException($"不支持的数据库类型: {effective.DbType}")
        };
    }

    /// <summary>
    /// 启用 SSH 时拉起隧道并返回主机/端口改写为本地转发端点的副本；否则原样返回。
    /// </summary>
    private static DbConnectionModel MaybeApplySshTunnel(DbConnectionModel conn)
    {
        // 文件型库无网络端点，不适用 SSH
        if (!conn.UseSsh || conn.DbType == DbTypeEnum.SQLite)
        {
            return conn;
        }

        var targetPort = ResolveTargetPort(conn);
        var (localHost, localPort) = SshTunnelManager.EnsureTunnel(conn, conn.Host, targetPort);

        var tunneled = conn.Clone();
        tunneled.Host = localHost;
        tunneled.Port = localPort;
        return tunneled;
    }

    /// <summary>
    /// 解析目标库的实际端口（未填时取该库默认端口），供隧道转发到远端。
    /// </summary>
    private static int ResolveTargetPort(DbConnectionModel conn)
    {
        if (conn.Port > 0)
        {
            return conn.Port;
        }

        return conn.DbType switch
        {
            DbTypeEnum.MySql or DbTypeEnum.MariaDB => AppConst.DefaultMySqlPort,
            DbTypeEnum.SqlServer => AppConst.DefaultSqlServerPort,
            DbTypeEnum.PostgreSQL => AppConst.DefaultPostgreSqlPort,
            DbTypeEnum.Oracle => AppConst.DefaultOraclePort,
            DbTypeEnum.MongoDB => AppConst.DefaultMongoDbPort,
            DbTypeEnum.Redis => AppConst.DefaultRedisPort,
            DbTypeEnum.DB2 => AppConst.DefaultDb2Port,
            _ => conn.Port
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
        // SSH 凭据同样落库加密，构建前解密供隧道使用
        if (!string.IsNullOrEmpty(cloned.SshPassword))
            cloned.SshPassword = DecryptPassword(cloned.SshPassword);
        if (!string.IsNullOrEmpty(cloned.SshPassphrase))
            cloned.SshPassphrase = DecryptPassword(cloned.SshPassphrase);
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

    /// <summary>
    /// 转义连接串中的值：含 ; = 引号 或首尾空格时按 ADO.NET 规范加引号，
    /// 避免密码等含特殊字符时破坏连接串。
    /// </summary>
    private static string EscapeValue(string? value)
    {
        value ??= string.Empty;
        var needsQuoting = value.IndexOfAny(new[] { ';', '=', '"', '\'', ' ' }) >= 0 || value != value.Trim();
        if (!needsQuoting)
        {
            return value;
        }
        if (!value.Contains('"'))
        {
            return $"\"{value}\"";
        }
        if (!value.Contains('\''))
        {
            return $"'{value}'";
        }
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static string BuildMySql(DbConnectionModel conn)
    {
        var port = conn.Port > 0 ? conn.Port : AppConst.DefaultMySqlPort;
        var sb = $"Server={conn.Host};Port={port};Database={conn.DbName};Uid={conn.UserName};Pwd={EscapeValue(conn.Password)};";
        if (conn.ConnectTimeout > 0) sb += $"ConnectionTimeout={conn.ConnectTimeout};";
        if (conn.EnableSSL) sb += "SslMode=Required;";
        return sb;
    }

    private static string BuildSqlServer(DbConnectionModel conn)
    {
        var port = conn.Port > 0 ? conn.Port : AppConst.DefaultSqlServerPort;
        var sb = $"Server={conn.Host},{port};Database={conn.DbName};User Id={conn.UserName};Password={EscapeValue(conn.Password)};";
        if (conn.ConnectTimeout > 0) sb += $"Connection Timeout={conn.ConnectTimeout};";
        if (conn.EnableSSL) sb += "Encrypt=True;TrustServerCertificate=True;";
        return sb;
    }

    private static string BuildPostgreSql(DbConnectionModel conn)
    {
        var port = conn.Port > 0 ? conn.Port : AppConst.DefaultPostgreSqlPort;
        var sb = $"Host={conn.Host};Port={port};Database={conn.DbName};Username={conn.UserName};Password={EscapeValue(conn.Password)};";
        if (conn.ConnectTimeout > 0) sb += $"Timeout={conn.ConnectTimeout};";
        if (conn.EnableSSL) sb += "SSL Mode=Require;Trust Server Certificate=true;";
        return sb;
    }

    private static string BuildOracle(DbConnectionModel conn)
    {
        var port = conn.Port > 0 ? conn.Port : AppConst.DefaultOraclePort;
        var serviceName = !string.IsNullOrEmpty(conn.OracleServiceName) ? conn.OracleServiceName : (!string.IsNullOrEmpty(conn.DbName) ? conn.DbName : "ORCL");
        var sb = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={conn.Host})(PORT={port}))(CONNECT_DATA=(SERVICE_NAME={serviceName})));User Id={conn.UserName};Password={EscapeValue(conn.Password)};";
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
