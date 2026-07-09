using System.Data.Common;
using DbManager.Core.Adapters;
using DbManager.Core.Enums;
using DbManager.Core.Interfaces;
using DbManager.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;

namespace DbManager.Core.Services;

public class DbConnectionFactory : IDbConnectionFactory
{
    public DbConnection CreateConnection(DbConnectionModel conn)
    {
        var connectionString = BuildDecryptedConnectionString(conn);

        return conn.DbType switch
        {
            DbTypeEnum.MySql or DbTypeEnum.MariaDB => new MySqlConnection(connectionString),
            DbTypeEnum.SqlServer => new SqlConnection(connectionString),
            DbTypeEnum.PostgreSQL => new NpgsqlConnection(connectionString),
            DbTypeEnum.Oracle => new OracleConnection(connectionString),
            DbTypeEnum.SQLite => new SqliteConnection(connectionString),
            DbTypeEnum.MongoDB => throw new NotSupportedException("MongoDB 暂不支持，请使用关系型数据库"),
            DbTypeEnum.Redis => throw new NotSupportedException("Redis 暂不支持，请使用关系型数据库"),
            DbTypeEnum.DB2 => throw new NotSupportedException("DB2 暂不支持，需要安装 IBM.Data.DB2.Core 包"),
            _ => throw new NotSupportedException($"不支持的数据库类型: {conn.DbType}")
        };
    }

    private static string BuildDecryptedConnectionString(DbConnectionModel conn)
        // 统一走一站式解密+构建（含 SSH 凭据解密与隧道拉起）
        => DbConnStringBuilder.BuildDecryptedConnectionString(conn);
}