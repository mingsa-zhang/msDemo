using DbManager.Core.Adapters;
using DbManager.Core.Enums;
using DbManager.Core.Models;
using Xunit;

namespace DbManager.Tests;

/// <summary>
/// 各库连接字符串生成的单元测试，覆盖"逐库专属项"。
/// 直接用 BuildConnectionString（UseSsh=false，明文密码）验证纯拼接逻辑。
/// </summary>
public class DbConnStringBuilderTests
{
    private static DbConnectionModel Conn(DbTypeEnum type) => new()
    {
        DbType = type,
        Host = "db.example.com",
        Port = 0,
        UserName = "u",
        Password = "p",
        DbName = "app"
    };

    [Fact]
    public void CustomConnectionString_ShortCircuits()
    {
        var c = Conn(DbTypeEnum.MySql);
        c.ConnectionString = "Server=x;custom=1;";
        Assert.Equal("Server=x;custom=1;", DbConnStringBuilder.BuildConnectionString(c));
    }

    [Fact]
    public void MySql_Charset_Emitted()
    {
        var c = Conn(DbTypeEnum.MySql);
        c.Charset = "utf8mb4";
        Assert.Contains("CharSet=utf8mb4;", DbConnStringBuilder.BuildConnectionString(c));
    }

    [Fact]
    public void SqlServer_IntegratedSecurity_OmitsUserPassword()
    {
        var c = Conn(DbTypeEnum.SqlServer);
        c.UseIntegratedSecurity = true;
        var s = DbConnStringBuilder.BuildConnectionString(c);
        Assert.Contains("Integrated Security=True;", s);
        Assert.DoesNotContain("User Id=", s);
    }

    [Fact]
    public void SqlServer_NamedInstance_UsesBackslash()
    {
        var c = Conn(DbTypeEnum.SqlServer);
        c.InstanceName = "SQLEXPRESS";
        Assert.Contains(@"Server=db.example.com\SQLEXPRESS;", DbConnStringBuilder.BuildConnectionString(c));
    }

    [Fact]
    public void PostgreSql_SchemaAndSslMode_Emitted()
    {
        var c = Conn(DbTypeEnum.PostgreSQL);
        c.PgSchema = "myschema";
        c.PgSslMode = "Require";
        var s = DbConnStringBuilder.BuildConnectionString(c);
        Assert.Contains("Search Path=myschema;", s);
        Assert.Contains("SSL Mode=Require;", s);
    }

    [Fact]
    public void Oracle_Sid_VsServiceName()
    {
        var sidConn = Conn(DbTypeEnum.Oracle);
        sidConn.OracleServiceName = "ORCL";
        sidConn.OracleUseSid = true;
        Assert.Contains("(SID=ORCL)", DbConnStringBuilder.BuildConnectionString(sidConn));

        var svcConn = Conn(DbTypeEnum.Oracle);
        svcConn.OracleServiceName = "ORCL";
        svcConn.OracleUseSid = false;
        Assert.Contains("(SERVICE_NAME=ORCL)", DbConnStringBuilder.BuildConnectionString(svcConn));
    }

    [Fact]
    public void Redis_DatabaseAndUser_Emitted()
    {
        var c = Conn(DbTypeEnum.Redis);
        c.RedisDatabase = 3;
        var s = DbConnStringBuilder.BuildConnectionString(c);
        Assert.Contains(",defaultDatabase=3", s);
        Assert.Contains(",user=u", s);
    }

    [Fact]
    public void Mongo_ReplicaSetAndDirectConnection_Emitted()
    {
        var c = Conn(DbTypeEnum.MongoDB);
        c.MongoReplicaSet = "rs0";
        c.MongoDirectConnection = true;
        var s = DbConnStringBuilder.BuildConnectionString(c);
        Assert.Contains("replicaSet=rs0", s);
        Assert.Contains("directConnection=true", s);
    }

    [Fact]
    public void Sqlite_ReadOnly_Emitted()
    {
        var c = Conn(DbTypeEnum.SQLite);
        c.SqliteFilePath = "data.db";
        c.SqliteReadOnly = true;
        Assert.Contains("Mode=ReadOnly;", DbConnStringBuilder.BuildConnectionString(c));
    }
}
