using CommunityToolkit.Mvvm.ComponentModel;
using DbManager.Core.Enums;

namespace DbManager.Core.Models;

public partial class DbConnectionModel : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private DbTypeEnum _dbType;

    [ObservableProperty]
    private string _host = string.Empty;

    [ObservableProperty]
    private int _port;

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _dbName = string.Empty;

    [ObservableProperty]
    private int _groupId;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _color = string.Empty;

    [ObservableProperty]
    private bool _isFavorite;

    [ObservableProperty]
    private string _connectionString = string.Empty;

    [ObservableProperty]
    private string _sqliteFilePath = string.Empty;

    [ObservableProperty]
    private int _connectTimeout = 30;

    [ObservableProperty]
    private bool _enableSSL;

    [ObservableProperty]
    private string _sslCertPath = string.Empty;

    [ObservableProperty]
    private string _mongoAuthDb = string.Empty;

    [ObservableProperty]
    private string _redisPassword = string.Empty;

    [ObservableProperty]
    private string _oracleServiceName = string.Empty;

    // ===== 各库专属连接项 =====
    /// <summary>
    /// MySQL/MariaDB 字符集
    /// </summary>
    [ObservableProperty]
    private string _charset = string.Empty;

    /// <summary>
    /// SqlServer 使用 Windows 集成认证
    /// </summary>
    [ObservableProperty]
    private bool _useIntegratedSecurity;

    /// <summary>
    /// SqlServer 命名实例（host\instance）
    /// </summary>
    [ObservableProperty]
    private string _instanceName = string.Empty;

    /// <summary>
    /// PostgreSQL 默认 schema（search_path）
    /// </summary>
    [ObservableProperty]
    private string _pgSchema = string.Empty;

    /// <summary>
    /// PostgreSQL SSL 模式（Disable/Prefer/Require）
    /// </summary>
    [ObservableProperty]
    private string _pgSslMode = string.Empty;

    /// <summary>
    /// Oracle 用 SID（否则用 Service Name）
    /// </summary>
    [ObservableProperty]
    private bool _oracleUseSid;

    /// <summary>
    /// Redis 库序号（0-15）
    /// </summary>
    [ObservableProperty]
    private int _redisDatabase;

    /// <summary>
    /// MongoDB 副本集名
    /// </summary>
    [ObservableProperty]
    private string _mongoReplicaSet = string.Empty;

    /// <summary>
    /// MongoDB 直连
    /// </summary>
    [ObservableProperty]
    private bool _mongoDirectConnection;

    /// <summary>
    /// SQLite 只读模式
    /// </summary>
    [ObservableProperty]
    private bool _sqliteReadOnly;

    // ===== SSH 隧道 =====
    [ObservableProperty]
    private bool _useSsh;

    [ObservableProperty]
    private string _sshHost = string.Empty;

    [ObservableProperty]
    private int _sshPort = 22;

    [ObservableProperty]
    private string _sshUser = string.Empty;

    [ObservableProperty]
    private string _sshPassword = string.Empty;

    /// <summary>
    /// SSH 认证方式：false=密码，true=私钥文件
    /// </summary>
    [ObservableProperty]
    private bool _sshUseKeyFile;

    [ObservableProperty]
    private string _sshKeyPath = string.Empty;

    [ObservableProperty]
    private string _sshPassphrase = string.Empty;

    [ObservableProperty]
    private DateTime _createdTime;

    [ObservableProperty]
    private DateTime _updatedTime;

    public DbConnectionModel Clone()
    {
        return new DbConnectionModel
        {
            Id = Id,
            Name = Name,
            DbType = DbType,
            Host = Host,
            Port = Port,
            UserName = UserName,
            Password = Password,
            DbName = DbName,
            GroupId = GroupId,
            Description = Description,
            Color = Color,
            IsFavorite = IsFavorite,
            ConnectionString = ConnectionString,
            SqliteFilePath = SqliteFilePath,
            ConnectTimeout = ConnectTimeout,
            EnableSSL = EnableSSL,
            SslCertPath = SslCertPath,
            MongoAuthDb = MongoAuthDb,
            RedisPassword = RedisPassword,
            OracleServiceName = OracleServiceName,
            Charset = Charset,
            UseIntegratedSecurity = UseIntegratedSecurity,
            InstanceName = InstanceName,
            PgSchema = PgSchema,
            PgSslMode = PgSslMode,
            OracleUseSid = OracleUseSid,
            RedisDatabase = RedisDatabase,
            MongoReplicaSet = MongoReplicaSet,
            MongoDirectConnection = MongoDirectConnection,
            SqliteReadOnly = SqliteReadOnly,
            UseSsh = UseSsh,
            SshHost = SshHost,
            SshPort = SshPort,
            SshUser = SshUser,
            SshPassword = SshPassword,
            SshUseKeyFile = SshUseKeyFile,
            SshKeyPath = SshKeyPath,
            SshPassphrase = SshPassphrase,
            CreatedTime = CreatedTime,
            UpdatedTime = UpdatedTime
        };
    }
}
