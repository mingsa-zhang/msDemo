using System.Data.Common;
using DbManager.Core.Enums;
using DbManager.Core.Interfaces;
using DbManager.Core.Models;

namespace DbManager.Core.Services;

public class DbMetadataServiceFactory
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DbMetadataServiceFactory(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public IDbMetadataService Create(DbTypeEnum dbType)
    {
        return dbType switch
        {
            DbTypeEnum.MySql or DbTypeEnum.MariaDB => new MySqlMetadataService(_connectionFactory),
            DbTypeEnum.SqlServer => new SqlServerMetadataService(_connectionFactory),
            DbTypeEnum.PostgreSQL => new PostgreSqlMetadataService(_connectionFactory),
            DbTypeEnum.Oracle => new OracleMetadataService(_connectionFactory),
            DbTypeEnum.SQLite => new SqliteMetadataService(_connectionFactory),
            DbTypeEnum.MongoDB => throw new NotSupportedException("MongoDB 暂不支持"),
            DbTypeEnum.Redis => throw new NotSupportedException("Redis 暂不支持"),
            DbTypeEnum.DB2 => throw new NotSupportedException("DB2 暂不支持"),
            _ => throw new NotSupportedException($"不支持的数据库类型: {dbType}")
        };
    }
}