using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbManager.Common;
using DbManager.Core.Adapters;
using DbManager.Core.Enums;
using DbManager.Core.Models;
using DbManager.Core.Services;
using DbManager.Wpf.Helpers;
using System.Collections.ObjectModel;
using System.Windows;

namespace DbManager.Wpf.ViewModels;

public partial class AddEditConnViewModel : ObservableObject
{
    private readonly DbConnectionManageService _connectionService;

    [ObservableProperty] private DbConnectionModel _connection;
    [ObservableProperty] private string _windowTitle = "新建连接";
    [ObservableProperty] private bool _isRelational = true;
    [ObservableProperty] private bool _isSQLite;
    [ObservableProperty] private bool _isOracle;
    [ObservableProperty] private bool _isMongoDB;
    [ObservableProperty] private bool _isRedis;
    [ObservableProperty] private bool _isDbTypeSupported = true;
    [ObservableProperty] private string _unsupportedHint = string.Empty;
    [ObservableProperty] private string _testResult = string.Empty;
    [ObservableProperty] private bool? _isTestSuccess;
    [ObservableProperty] private ObservableCollection<string> _groupNames = new();
    [ObservableProperty] private string _selectedGroupName = string.Empty;

    private List<DbConnectionGroupModel> _groups = new();

    public ObservableCollection<DbTypeEnum> DbTypeList { get; } = new(Enum.GetValues<DbTypeEnum>());

    public AddEditConnViewModel(DbConnectionManageService connectionService, DbConnectionModel connection)
    {
        _connectionService = connectionService;
        _connection = connection;

        if (connection.Id > 0)
        {
            WindowTitle = "编辑连接";
        }

        _ = LoadGroupsAsync();

        Connection.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Connection.DbType))
            {
                UpdateDbTypeVisibility();
                AutoFillDefaultPort();
            }
        };

        UpdateDbTypeVisibility();
    }

    private async Task LoadGroupsAsync()
    {
        _groups = await _connectionService.GetAllGroupsAsync();
        GroupNames = new ObservableCollection<string>(_groups.Select(g => g.Name));
        if (Connection.GroupId > 0)
        {
            SelectedGroupName = _groups.FirstOrDefault(g => g.Id == Connection.GroupId)?.Name ?? string.Empty;
        }
    }

    /// <summary>
    /// 解析分组名为分组 Id：为空则无分组；不存在则新建。
    /// </summary>
    private async Task<int> ResolveGroupIdAsync(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return 0;
        }
        name = name.Trim();
        var existing = _groups.FirstOrDefault(g => string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            return existing.Id;
        }
        await _connectionService.AddGroupAsync(new DbConnectionGroupModel { Name = name });
        var groups = await _connectionService.GetAllGroupsAsync();
        return groups.FirstOrDefault(g => string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase))?.Id ?? 0;
    }

    private void UpdateDbTypeVisibility()
    {
        IsRelational = Connection.DbType is DbTypeEnum.MySql or DbTypeEnum.MariaDB
            or DbTypeEnum.SqlServer or DbTypeEnum.PostgreSQL or DbTypeEnum.DB2;
        IsSQLite = Connection.DbType == DbTypeEnum.SQLite;
        IsOracle = Connection.DbType == DbTypeEnum.Oracle;
        IsMongoDB = Connection.DbType == DbTypeEnum.MongoDB;
        IsRedis = Connection.DbType == DbTypeEnum.Redis;

        IsDbTypeSupported = DbTypeSupport.IsImplemented(Connection.DbType);
        UnsupportedHint = IsDbTypeSupported
            ? string.Empty
            : $"{Connection.DbType} 尚在开发中，暂不支持连接，敬请期待。";
    }

    private void AutoFillDefaultPort()
    {
        if (Connection.Port > 0) return;
        Connection.Port = Connection.DbType switch
        {
            DbTypeEnum.MySql or DbTypeEnum.MariaDB => AppConst.DefaultMySqlPort,
            DbTypeEnum.SqlServer => AppConst.DefaultSqlServerPort,
            DbTypeEnum.PostgreSQL => AppConst.DefaultPostgreSqlPort,
            DbTypeEnum.Oracle => AppConst.DefaultOraclePort,
            DbTypeEnum.MongoDB => AppConst.DefaultMongoDbPort,
            DbTypeEnum.Redis => AppConst.DefaultRedisPort,
            DbTypeEnum.DB2 => AppConst.DefaultDb2Port,
            _ => 0
        };
    }

    [RelayCommand]
    private async Task TestConnection()
    {
        if (!DbTypeSupport.IsImplemented(Connection.DbType))
        {
            TestResult = UnsupportedHint;
            IsTestSuccess = false;
            return;
        }

        try
        {
            var connStr = DbConnStringBuilder.BuildDecryptedConnectionString(Connection);

            // MongoDB / Redis 走各自专用服务测试
            if (Connection.DbType == DbTypeEnum.MongoDB)
            {
                await new MongoService().TestAsync(connStr);
                TestResult = "MongoDB 连接成功";
                IsTestSuccess = true;
                return;
            }
            if (Connection.DbType == DbTypeEnum.Redis)
            {
                await new RedisService().TestAsync(connStr);
                TestResult = "Redis 连接成功";
                IsTestSuccess = true;
                return;
            }

            var service = App.MetadataFactory.Create(Connection.DbType);
            var databases = await service.GetDatabasesAsync(connStr);
            TestResult = $"连接成功，发现 {databases.Count} 个数据库";
            IsTestSuccess = true;
        }
        catch (Exception ex)
        {
            TestResult = $"连接失败: {DbErrorTranslator.Translate(ex)}";
            IsTestSuccess = false;
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Connection.Name))
        {
            MessageTipHelper.Warning("请输入连接名称");
            return;
        }

        if (!DbTypeSupport.IsImplemented(Connection.DbType))
        {
            MessageTipHelper.Warning(UnsupportedHint);
            return;
        }

        try
        {
            Connection.GroupId = await ResolveGroupIdAsync(SelectedGroupName);

            if (Connection.Id > 0)
            {
                await _connectionService.UpdateConnectionAsync(Connection);
            }
            else
            {
                Connection.CreatedTime = DateTime.Now;
                await _connectionService.AddConnectionAsync(Connection);
            }

            CloseWindow();
        }
        catch (Exception ex)
        {
            MessageTipHelper.Error($"保存失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow();
    }

    private void CloseWindow()
    {
        foreach (Window window in Application.Current.Windows)
        {
            if (window.DataContext == this)
            {
                window.DialogResult = true;
                window.Close();
                return;
            }
        }
    }
}