using System.Data.Common;
using DbManager.Core.Enums;
using DbManager.Core.Interfaces;
using DbManager.Core.Models;

namespace DbManager.Core.Services;

public class DbExecuteServiceFactory
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DbExecuteServiceFactory(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public IDbExecuteService Create(DbTypeEnum dbType)
    {
        return dbType switch
        {
            DbTypeEnum.MySql or DbTypeEnum.MariaDB => new MySqlExecuteService(_connectionFactory),
            DbTypeEnum.SqlServer => new SqlServerExecuteService(_connectionFactory),
            DbTypeEnum.PostgreSQL => new PostgreSqlExecuteService(_connectionFactory),
            DbTypeEnum.Oracle => new OracleExecuteService(_connectionFactory),
            DbTypeEnum.SQLite => new SqliteExecuteService(_connectionFactory),
            DbTypeEnum.MongoDB => throw new NotSupportedException("MongoDB 暂不支持"),
            DbTypeEnum.Redis => throw new NotSupportedException("Redis 暂不支持"),
            DbTypeEnum.DB2 => throw new NotSupportedException("DB2 暂不支持"),
            _ => throw new NotSupportedException($"不支持的数据库类型: {dbType}")
        };
    }
}