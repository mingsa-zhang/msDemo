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
            CreatedTime = CreatedTime,
            UpdatedTime = UpdatedTime
        };
    }
}
