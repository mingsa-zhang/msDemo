using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbManager.Core.Interfaces;
using DbManager.Core.Models;
using System.Collections.ObjectModel;

namespace DbManager.Wpf.ViewModels;

public partial class DbTreeViewModel : ObservableObject
{
    private readonly IDbTreeNavigateService _navigateService;

    public MainWindowViewModel? MainViewModel { get; set; }

    [ObservableProperty] private ObservableCollection<DbTreeNodeViewModel> _nodes = new();
    [ObservableProperty] private DbTreeNodeViewModel? _selectedNode;
    [ObservableProperty] private string _searchText = string.Empty;

    // 事件：供MainWindow订阅
    public event Action<int, string>? OpenSqlQueryRequested;
    public event Action<int, string, string>? OpenDataBrowserRequested;
    public event Action<int, string, string>? OpenTableDesignRequested;

    public DbTreeViewModel(IDbTreeNavigateService navigateService)
    {
        _navigateService = navigateService;
    }

    public async Task RefreshTree()
    {
        var connections = await _navigateService.GetConnectionNodesAsync();
        var groups = await App.ConnectionService.GetAllGroupsAsync();

        if (groups.Count == 0)
        {
            Nodes = new ObservableCollection<DbTreeNodeViewModel>(
                connections.Select(c => new DbTreeNodeViewModel(c, _navigateService) { MainViewModel = MainViewModel }));
        }
        else
        {
            var groupedNodes = new ObservableCollection<DbTreeNodeViewModel>();
            var allConns = await App.ConnectionService.GetAllConnectionsAsync();
            var groupedConnIds = new HashSet<int>();

            foreach (var group in groups.OrderBy(g => g.SortOrder).ThenBy(g => g.Name))
            {
                var groupConnIds = allConns.Where(c => c.GroupId == group.Id).Select(c => c.Id).ToHashSet();

                var groupNode = new DbTreeNodeModel
                {
                    DisplayName = group.Name,
                    NodeType = TreeNodeType.Group,
                    IconKind = "FolderOutline",
                    IconColor = "#FF9800"
                };
                var groupVm = new DbTreeNodeViewModel(groupNode, _navigateService) { MainViewModel = MainViewModel };

                var childConns = connections.Where(c => groupConnIds.Contains(c.ConnectionId)).ToList();
                groupVm.Children = new ObservableCollection<DbTreeNodeViewModel>(
                    childConns.Select(c => new DbTreeNodeViewModel(c, _navigateService) { MainViewModel = MainViewModel }));

                groupedNodes.Add(groupVm);
                foreach (var c in childConns) groupedConnIds.Add(c.ConnectionId);
            }

            var ungrouped = connections.Where(c => !groupedConnIds.Contains(c.ConnectionId)).ToList();
            foreach (var c in ungrouped)
                groupedNodes.Add(new DbTreeNodeViewModel(c, _navigateService) { MainViewModel = MainViewModel });

            Nodes = groupedNodes;
        }
    }

    public async Task RefreshAsync()
    {
        await RefreshTree();
    }

    [RelayCommand]
    private async Task Search()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await RefreshTree();
            return;
        }
        await RefreshTree();
        var filtered = Nodes.Where(n => n.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();
        Nodes = new ObservableCollection<DbTreeNodeViewModel>(filtered);
    }

    internal void RequestOpenSqlQuery(int connectionId, string database)
    {
        OpenSqlQueryRequested?.Invoke(connectionId, database);
    }

    internal void RequestOpenDataBrowser(int connectionId, string database, string tableName)
    {
        OpenDataBrowserRequested?.Invoke(connectionId, database, tableName);
    }

    internal void RequestOpenTableDesign(int connectionId, string database, string tableName)
    {
        OpenTableDesignRequested?.Invoke(connectionId, database, tableName);
    }
}

public partial class DbTreeNodeViewModel : ObservableObject
{
    private readonly IDbTreeNavigateService _navigateService;
    private readonly DbTreeNodeModel _model;

    public MainWindowViewModel? MainViewModel { get; set; }

    public int ConnectionId => _model.ConnectionId;
    public string? DatabaseName => _model.DatabaseName;
    public string? ObjectName => _model.ObjectName;
    public TreeNodeType NodeType => _model.NodeType;
    public string? IconKind => _model.IconKind;
    public string IconColor => _model.IconColor;

    [ObservableProperty] private string _displayName;
    [ObservableProperty] private bool _isExpanded;
    [ObservableProperty] private ObservableCollection<DbTreeNodeViewModel> _children = new();
    [ObservableProperty] private bool _isLoaded;
    [ObservableProperty] private bool _isConnected;

    public DbTreeNodeViewModel(DbTreeNodeModel model, IDbTreeNavigateService navigateService)
    {
        _model = model;
        _navigateService = navigateService;
        _displayName = model.DisplayName;
    }

    partial void OnIsExpandedChanged(bool value)
    {
        if (value && !IsLoaded) _ = LoadChildrenAsync();
    }

    public async Task LoadChildrenAsync()
    {
        try
        {
            List<DbTreeNodeModel> children;
            switch (NodeType)
            {
                case TreeNodeType.Connection:
                    var databases = await _navigateService.GetDatabaseNodesAsync(ConnectionId);
                    children = databases.ToList();
                    IsConnected = true;
                    break;

                case TreeNodeType.Database:
                    children = new List<DbTreeNodeModel>
                    {
                        new() { DisplayName = "表", NodeType = TreeNodeType.TableGroup, ConnectionId = ConnectionId, DatabaseName = DatabaseName, IconKind = "FolderTable", IconColor = "#2196F3" },
                        new() { DisplayName = "视图", NodeType = TreeNodeType.ViewGroup, ConnectionId = ConnectionId, DatabaseName = DatabaseName, IconKind = "EyeOutline", IconColor = "#4CAF50" },
                        new() { DisplayName = "存储过程", NodeType = TreeNodeType.ProcedureGroup, ConnectionId = ConnectionId, DatabaseName = DatabaseName, IconKind = "CogOutline", IconColor = "#FF9800" },
                        new() { DisplayName = "函数", NodeType = TreeNodeType.FunctionGroup, ConnectionId = ConnectionId, DatabaseName = DatabaseName, IconKind = "FunctionVariant", IconColor = "#9C27B0" }
                    };
                    break;

                case TreeNodeType.TableGroup:
                    var tableNodes = await _navigateService.GetTableNodesAsync(ConnectionId, DatabaseName!);
                    foreach (var t in tableNodes) { t.IconKind = "Table"; t.IconColor = "#2196F3"; }
                    children = tableNodes.ToList();
                    break;

                case TreeNodeType.ViewGroup:
                    var viewNodes = await _navigateService.GetViewNodesAsync(ConnectionId, DatabaseName!);
                    foreach (var v in viewNodes) { v.IconKind = "EyeOutline"; v.IconColor = "#4CAF50"; }
                    children = viewNodes.ToList();
                    break;

                case TreeNodeType.ProcedureGroup:
                    var procNodes = await _navigateService.GetStoredProcedureNodesAsync(ConnectionId, DatabaseName!);
                    foreach (var p in procNodes) { p.IconKind = "CogOutline"; p.IconColor = "#FF9800"; }
                    children = procNodes.ToList();
                    break;

                case TreeNodeType.FunctionGroup:
                    var funcNodes = await _navigateService.GetFunctionNodesAsync(ConnectionId, DatabaseName!);
                    foreach (var f in funcNodes) { f.IconKind = "FunctionVariant"; f.IconColor = "#9C27B0"; }
                    children = funcNodes.ToList();
                    break;

                case TreeNodeType.Table:
                    var colNodes = await _navigateService.GetColumnNodesAsync(ConnectionId, DatabaseName!, ObjectName!);
                    foreach (var c in colNodes) { c.IconKind = "TextBoxOutline"; c.IconColor = "#607D8B"; }
                    children = colNodes.ToList();
                    break;

                case TreeNodeType.IndexGroup:
                    var idxNodes = await _navigateService.GetIndexNodesAsync(ConnectionId, DatabaseName!, ObjectName!);
                    children = idxNodes.ToList();
                    break;

                default:
                    return;
            }

            Children = new ObservableCollection<DbTreeNodeViewModel>(
                children.Select(c => new DbTreeNodeViewModel(c, _navigateService) { MainViewModel = MainViewModel }));
            IsLoaded = true;
        }
        catch (Exception ex)
        {
            Children = new ObservableCollection<DbTreeNodeViewModel>();
            DisplayName = $"{DisplayName} (加载失败)";
            System.Diagnostics.Debug.WriteLine($"树节点加载失败: {ex.Message}");
        }
    }
}