using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbManager.Core.Interfaces;
using DbManager.Core.Models;
using DbManager.Core.Services;

namespace DbManager.Wpf.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly DbConnectionManageService _connectionService;
    private readonly IDbTreeNavigateService _treeNavigateService;

    [ObservableProperty] private DbTreeViewModel _treeViewModel;
    [ObservableProperty] private ObservableCollection<TabItemViewModel> _tabs = new();
    [ObservableProperty] private TabItemViewModel? _selectedTab;
    [ObservableProperty] private string _statusMessage = "就绪";
    [ObservableProperty] private int _connectionCount;
    [ObservableProperty] private int _tabCount;

    private int _queryCounter = 1;

    public MainWindowViewModel(DbConnectionManageService connectionService, IDbTreeNavigateService treeNavigateService)
    {
        _connectionService = connectionService;
        _treeNavigateService = treeNavigateService;
        _treeViewModel = new DbTreeViewModel(treeNavigateService);
        _treeViewModel.MainViewModel = this;

        // 订阅树导航事件
        _treeViewModel.OpenSqlQueryRequested += (id, db) => _ = OpenSqlQueryAsync(id, db);
        _treeViewModel.OpenDataBrowserRequested += (id, db, table, schema) => _ = OpenDataBrowserAsync(id, db, table, schema);
        _treeViewModel.OpenTableDesignRequested += (id, db, table, schema) => _ = OpenTableDesignAsync(id, db, table, schema);
        _treeViewModel.OpenNewTableRequested += (id, db, schema) => _ = OpenNewTableAsync(id, db, schema);
        _treeViewModel.OpenMongoBrowserRequested += (id, db, coll) => _ = OpenMongoBrowserAsync(id, db, coll);
        _treeViewModel.OpenRedisBrowserRequested += id => _ = OpenRedisBrowserAsync(id);

        _ = LoadConnectionCountAsync();
        _ = _treeViewModel.RefreshAsync();
    }

    private async Task LoadConnectionCountAsync()
    {
        var conns = await _connectionService.GetAllConnectionsAsync();
        ConnectionCount = conns.Count;
    }

    #region 标签页管理

    public async Task OpenSqlQueryAsync(int connectionId, string databaseName)
    {
        DbConnectionModel? connection = null;
        if (connectionId > 0)
            connection = await _connectionService.GetConnectionByIdAsync(connectionId);

        if (connection == null)
        {
            // 无连接时打开空白查询标签
            var emptyTabVm = new SqlQueryTabViewModel(new DbConnectionModel { Name = "未连接" }, databaseName);
            var emptyTab = new TabItemViewModel
            {
                Header = $"查询 {_queryCounter}",
                IconKind = "SqlQuery",
                ContentType = TabContentType.SqlQuery,
                Content = emptyTabVm
            };
            _queryCounter++;
            Tabs.Add(emptyTab);
            SelectedTab = emptyTab;
            TabCount = Tabs.Count;
            return;
        }

        var tabVm = new SqlQueryTabViewModel(connection, databaseName);
        var tabItem = new TabItemViewModel
        {
            Header = string.IsNullOrEmpty(databaseName) ? $"查询 {_queryCounter}" : $"{connection.Name} - {databaseName}",
            IconKind = "SqlQuery",
            ContentType = TabContentType.SqlQuery,
            Content = tabVm
        };
        _queryCounter++;
        Tabs.Add(tabItem);
        SelectedTab = tabItem;
        TabCount = Tabs.Count;
    }

    public async Task OpenDataBrowserAsync(int connectionId, string databaseName, string tableName, string? schema = null)
    {
        var connection = await _connectionService.GetConnectionByIdAsync(connectionId);
        if (connection == null) return;

        var tabVm = new DataBrowserViewModel(connection, databaseName, tableName, schema);
        var tabItem = new TabItemViewModel
        {
            Header = tableName,
            IconKind = "TableSearch",
            ContentType = TabContentType.DataBrowser,
            Content = tabVm
        };
        Tabs.Add(tabItem);
        SelectedTab = tabItem;
    }

    public async Task OpenTableDesignAsync(int connectionId, string databaseName, string tableName, string? schema = null)
    {
        var connection = await _connectionService.GetConnectionByIdAsync(connectionId);
        if (connection == null) return;

        var tabVm = new TableDesignViewModel(connection, databaseName, tableName, schema);
        var tabItem = new TabItemViewModel
        {
            Header = $"{tableName} (设计)",
            IconKind = "TableEdit",
            ContentType = TabContentType.TableDesign,
            Content = tabVm
        };
        Tabs.Add(tabItem);
        SelectedTab = tabItem;
    }

    public async Task OpenNewTableAsync(int connectionId, string databaseName, string? schema = null)
    {
        var connection = await _connectionService.GetConnectionByIdAsync(connectionId);
        if (connection == null)
        {
            return;
        }

        // 建表成功后刷新导航树，展示新表
        var tabVm = new TableDesignViewModel(connection, databaseName, schema, true, () => _ = TreeViewModel.RefreshTree());
        var tabItem = new TabItemViewModel
        {
            Header = "新建表",
            IconKind = "TablePlus",
            ContentType = TabContentType.TableDesign,
            Content = tabVm
        };
        Tabs.Add(tabItem);
        SelectedTab = tabItem;
        TabCount = Tabs.Count;
    }

    public async Task OpenMongoBrowserAsync(int connectionId, string database, string collection)
    {
        var connection = await _connectionService.GetConnectionByIdAsync(connectionId);
        if (connection == null)
        {
            return;
        }

        var tabVm = new MongoBrowserViewModel(connection, database, collection);
        var tabItem = new TabItemViewModel
        {
            Header = collection,
            IconKind = "FileDocumentOutline",
            ContentType = TabContentType.MongoBrowser,
            Content = tabVm
        };
        Tabs.Add(tabItem);
        SelectedTab = tabItem;
        TabCount = Tabs.Count;
    }

    public async Task OpenRedisBrowserAsync(int connectionId)
    {
        var connection = await _connectionService.GetConnectionByIdAsync(connectionId);
        if (connection == null)
        {
            return;
        }

        var tabVm = new RedisBrowserViewModel(connection);
        var tabItem = new TabItemViewModel
        {
            Header = $"{connection.Name} (Redis)",
            IconKind = "LightningBolt",
            ContentType = TabContentType.RedisBrowser,
            Content = tabVm
        };
        Tabs.Add(tabItem);
        SelectedTab = tabItem;
        TabCount = Tabs.Count;
    }

    [RelayCommand]
    private void CloseTab(TabItemViewModel tab)
    {
        // 释放持有资源的标签（如手动事务会话）
        if (tab.Content is IAsyncDisposable disposable)
        {
            _ = disposable.DisposeAsync();
        }

        var idx = Tabs.IndexOf(tab);
        Tabs.Remove(tab);
        TabCount = Tabs.Count;
        if (Tabs.Count > 0)
            SelectedTab = Tabs[Math.Max(0, Math.Min(idx, Tabs.Count - 1))];
        else
            SelectedTab = null;
    }

    public void CloseOtherTabs(TabItemViewModel keepTab)
    {
        var toRemove = Tabs.Where(t => t != keepTab).ToList();
        foreach (var tab in toRemove)
            Tabs.Remove(tab);
        SelectedTab = keepTab;
        TabCount = Tabs.Count;
    }

    public void CloseAllTabs()
    {
        Tabs.Clear();
        SelectedTab = null;
        TabCount = 0;
    }

    #endregion

    #region 菜单/工具栏命令

    [RelayCommand]
    private void AddConnection()
    {
        var window = new Views.AddOrEditConnWindow
        {
            DataContext = new AddEditConnViewModel(_connectionService, new DbConnectionModel()),
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
        RefreshTreeCommand.Execute(null);
    }

    [RelayCommand]
    private async Task RefreshTree()
    {
        StatusMessage = "正在刷新...";
        await TreeViewModel.RefreshTree();
        await LoadConnectionCountAsync();
        StatusMessage = "刷新完成";
    }

    [RelayCommand]
    private async Task ExportConnections()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON文件|*.json",
            FileName = "db_connections.json"
        };
        if (dialog.ShowDialog() == true)
        {
            var json = await _connectionService.ExportConnectionsToJsonAsync();
            System.IO.File.WriteAllText(dialog.FileName, json);
            StatusMessage = "导出成功";
        }
    }

    [RelayCommand]
    private async Task ImportConnections()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "JSON文件|*.json"
        };
        if (dialog.ShowDialog() == true)
        {
            var json = System.IO.File.ReadAllText(dialog.FileName);
            await _connectionService.ImportConnectionsFromJsonAsync(json);
            await RefreshTree();
            StatusMessage = "导入成功";
        }
    }

    public void ExecuteSql()
    {
        if (SelectedTab?.Content is SqlQueryTabViewModel vm)
            vm.ExecuteCommand.Execute(null);
    }

    public void ExecuteSelectedSql()
    {
        if (SelectedTab?.Content is SqlQueryTabViewModel vm)
            vm.ExecuteSelectedCommand.Execute(null);
    }

    #endregion
}

public enum TabContentType
{
    SqlQuery,
    DataBrowser,
    TableDesign,
    MongoBrowser,
    RedisBrowser
}

public class TabItemViewModel : ObservableObject
{
    public string Header { get; set; } = "";
    public string IconKind { get; set; } = "FileDocument";
    public TabContentType ContentType { get; set; }
    public object Content { get; set; } = null!;
}