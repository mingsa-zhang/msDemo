using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbManager.Core.Models;
using DbManager.Core.Services;
using DbManager.Wpf.Views;
using System.Collections.ObjectModel;
using System.Windows;

namespace DbManager.Wpf.ViewModels;

public partial class ConnListViewModel : ObservableObject
{
    private readonly DbConnectionManageService _connectionService;

    private List<DbConnectionModel> _allConnections = new();

    [ObservableProperty] private ObservableCollection<DbConnectionModel> _connections = new();
    [ObservableProperty] private DbConnectionModel? _selectedConnection;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private ObservableCollection<ConnGroupFilter> _groups = new();
    [ObservableProperty] private ConnGroupFilter? _selectedGroup;

    public ConnListViewModel(DbConnectionManageService connectionService)
    {
        _connectionService = connectionService;
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        _allConnections = await _connectionService.GetAllConnectionsAsync();
        var groupModels = await _connectionService.GetAllGroupsAsync();

        var filters = new List<ConnGroupFilter> { new(0, "全部连接") };
        filters.AddRange(groupModels.OrderBy(g => g.SortOrder).ThenBy(g => g.Name)
            .Select(g => new ConnGroupFilter(g.Id, g.Name)));
        filters.Add(new ConnGroupFilter(-1, "未分组"));

        var keepId = SelectedGroup?.Id ?? 0;
        Groups = new ObservableCollection<ConnGroupFilter>(filters);
        SelectedGroup = Groups.FirstOrDefault(g => g.Id == keepId) ?? Groups[0];
        ApplyGroupFilter();
    }

    partial void OnSelectedGroupChanged(ConnGroupFilter? value) => ApplyGroupFilter();

    private void ApplyGroupFilter()
    {
        IEnumerable<DbConnectionModel> filtered = SelectedGroup?.Id switch
        {
            null or 0 => _allConnections,                       // 全部
            -1 => _allConnections.Where(c => c.GroupId <= 0),   // 未分组
            _ => _allConnections.Where(c => c.GroupId == SelectedGroup!.Id)
        };
        Connections = new ObservableCollection<DbConnectionModel>(filtered);
    }

    [RelayCommand]
    private async Task AddConnection()
    {
        var window = new AddOrEditConnWindow
        {
            DataContext = new AddEditConnViewModel(_connectionService, new DbConnectionModel()),
            Owner = Application.Current.MainWindow
        };
        window.ShowDialog();
        if (window.DialogResult == true) await LoadDataAsync();
    }

    [RelayCommand]
    private async Task EditConnection()
    {
        if (SelectedConnection == null) return;
        var window = new AddOrEditConnWindow
        {
            DataContext = new AddEditConnViewModel(_connectionService, SelectedConnection),
            Owner = Application.Current.MainWindow
        };
        window.ShowDialog();
        if (window.DialogResult == true) await LoadDataAsync();
    }

    [RelayCommand]
    private async Task DeleteConnection()
    {
        if (SelectedConnection == null) return;
        if (!Helpers.MessageTipHelper.Confirm($"确定删除连接「{SelectedConnection.Name}」？")) return;
        await _connectionService.DeleteConnectionAsync(SelectedConnection.Id);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task TestConnection()
    {
        if (SelectedConnection == null) return;
        if (!DbManager.Core.Enums.DbTypeSupport.IsImplemented(SelectedConnection.DbType))
        {
            Helpers.MessageTipHelper.Warning($"{SelectedConnection.DbType} 尚在开发中，暂不支持连接。");
            return;
        }
        try
        {
            var connStr = DbManager.Core.Adapters.DbConnStringBuilder.BuildDecryptedConnectionString(SelectedConnection);
            var service = App.MetadataFactory.Create(SelectedConnection.DbType);
            var databases = await service.GetDatabasesAsync(connStr);
            Helpers.MessageTipHelper.Success($"连接成功，发现 {databases.Count} 个数据库");
        }
        catch (Exception ex)
        {
            Helpers.MessageTipHelper.Error($"连接失败: {DbManager.Common.DbErrorTranslator.Translate(ex)}");
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task DeleteGroup()
    {
        // "全部"(0) 与 "未分组"(-1) 不可删除
        if (SelectedGroup == null || SelectedGroup.Id <= 0)
        {
            Helpers.MessageTipHelper.Warning("请先选择一个可删除的分组");
            return;
        }
        if (!Helpers.MessageTipHelper.Confirm($"确定删除分组「{SelectedGroup.Name}」？组内连接将变为未分组（不会删除连接）。"))
        {
            return;
        }
        await _connectionService.DeleteGroupAsync(SelectedGroup.Id);
        await LoadDataAsync();
    }
}

/// <summary>
/// 连接分组过滤项。Id: 0=全部, -1=未分组, &gt;0=具体分组。
/// </summary>
public record ConnGroupFilter(int Id, string Name);