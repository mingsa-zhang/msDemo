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

    [ObservableProperty] private ObservableCollection<DbConnectionModel> _connections = new();
    [ObservableProperty] private DbConnectionModel? _selectedConnection;
    [ObservableProperty] private string _searchText = string.Empty;

    public ConnListViewModel(DbConnectionManageService connectionService)
    {
        _connectionService = connectionService;
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var connections = await _connectionService.GetAllConnectionsAsync();
        Connections = new ObservableCollection<DbConnectionModel>(connections);
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
        try
        {
            var connStr = DbManager.Core.Adapters.DbConnStringBuilder.BuildConnectionString(SelectedConnection);
            var service = App.MetadataFactory.Create(SelectedConnection.DbType);
            var databases = await service.GetDatabasesAsync(connStr);
            Helpers.MessageTipHelper.Success($"连接成功，发现 {databases.Count} 个数据库");
        }
        catch (Exception ex)
        {
            Helpers.MessageTipHelper.Error($"连接失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadDataAsync();
    }
}