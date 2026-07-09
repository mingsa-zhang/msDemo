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

    // 事件：供MainWindow订阅（表相关事件带 schema）
    public event Action<int, string>? OpenSqlQueryRequested;
    public event Action<int, string, string, string?>? OpenDataBrowserRequested;
    public event Action<int, string, string, string?>? OpenTableDesignRequested;
    // 新建表：连接Id、库、schema
    public event Action<int, string, string?>? OpenNewTableRequested;

    public DbTreeViewModel(IDbTreeNavigateService navigateService)
    {
        _navigateService = navigateService;
    }

    /// <summary>
    /// 失效指定连接的元数据缓存（单节点刷新）。
    /// </summary>
    public void InvalidateConnectionCache(int connectionId) => _navigateService.InvalidateConnection(connectionId);

    public async Task RefreshTree()
    {
        var connections = await _navigateService.GetConnectionNodesAsync();
        // 全量刷新：失效所有连接的元数据缓存，确保重新查库
        foreach (var c in connections)
        {
            _navigateService.InvalidateConnection(c.ConnectionId);
        }
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

    internal void RequestOpenDataBrowser(int connectionId, string database, string tableName, string? schema = null)
    {
        OpenDataBrowserRequested?.Invoke(connectionId, database, tableName, schema);
    }

    internal void RequestOpenTableDesign(int connectionId, string database, string tableName, string? schema = null)
    {
        OpenTableDesignRequested?.Invoke(connectionId, database, tableName, schema);
    }

    internal void RequestOpenNewTable(int connectionId, string database, string? schema = null)
    {
        OpenNewTableRequested?.Invoke(connectionId, database, schema);
    }
}

public partial class DbTreeNodeViewModel : ObservableObject
{
    private readonly IDbTreeNavigateService _navigateService;
    private readonly DbTreeNodeModel _model;

    public MainWindowViewModel? MainViewModel { get; set; }

    public int ConnectionId => _model.ConnectionId;
    public string? DatabaseName => _model.DatabaseName;
    public string? SchemaName => _model.SchemaName;
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

    /// <summary>
    /// 构建"表/视图/存储过程/函数"四个对象组节点，并把所属 schema 透传给子查询。
    /// </summary>
    private List<DbTreeNodeModel> BuildObjectGroups(string? database, string? schema) => new()
    {
        new() { DisplayName = "表", NodeType = TreeNodeType.TableGroup, ConnectionId = ConnectionId, DatabaseName = database, SchemaName = schema, IconKind = "FolderTable", IconColor = "#2196F3" },
        new() { DisplayName = "视图", NodeType = TreeNodeType.ViewGroup, ConnectionId = ConnectionId, DatabaseName = database, SchemaName = schema, IconKind = "EyeOutline", IconColor = "#4CAF50" },
        new() { DisplayName = "存储过程", NodeType = TreeNodeType.ProcedureGroup, ConnectionId = ConnectionId, DatabaseName = database, SchemaName = schema, IconKind = "CogOutline", IconColor = "#FF9800" },
        new() { DisplayName = "函数", NodeType = TreeNodeType.FunctionGroup, ConnectionId = ConnectionId, DatabaseName = database, SchemaName = schema, IconKind = "FunctionVariant", IconColor = "#9C27B0" }
    };

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
                    // Schema 敏感库（PG/SqlServer）先展开 Schema 层；无 Schema 概念的库直接展开对象组。
                    var schemaNodes = await _navigateService.GetSchemaNodesAsync(ConnectionId, DatabaseName!);
                    children = schemaNodes.Count > 0
                        ? schemaNodes.ToList()
                        : BuildObjectGroups(DatabaseName, null);
                    break;

                case TreeNodeType.Schema:
                    children = BuildObjectGroups(DatabaseName, SchemaName);
                    break;

                case TreeNodeType.TableGroup:
                    var tableNodes = await _navigateService.GetTableNodesAsync(ConnectionId, DatabaseName!, SchemaName);
                    foreach (var t in tableNodes) { t.IconKind = "Table"; t.IconColor = "#2196F3"; }
                    children = tableNodes.ToList();
                    break;

                case TreeNodeType.ViewGroup:
                    var viewNodes = await _navigateService.GetViewNodesAsync(ConnectionId, DatabaseName!, SchemaName);
                    foreach (var v in viewNodes) { v.IconKind = "EyeOutline"; v.IconColor = "#4CAF50"; }
                    children = viewNodes.ToList();
                    break;

                case TreeNodeType.ProcedureGroup:
                    var procNodes = await _navigateService.GetStoredProcedureNodesAsync(ConnectionId, DatabaseName!, SchemaName);
                    foreach (var p in procNodes) { p.IconKind = "CogOutline"; p.IconColor = "#FF9800"; }
                    children = procNodes.ToList();
                    break;

                case TreeNodeType.FunctionGroup:
                    var funcNodes = await _navigateService.GetFunctionNodesAsync(ConnectionId, DatabaseName!, SchemaName);
                    foreach (var f in funcNodes) { f.IconKind = "FunctionVariant"; f.IconColor = "#9C27B0"; }
                    children = funcNodes.ToList();
                    break;

                case TreeNodeType.Table:
                    // 表展开为「字段」「索引」两个子组（对标 Navicat）
                    children = new List<DbTreeNodeModel>
                    {
                        new() { DisplayName = "字段", NodeType = TreeNodeType.ColumnGroup, ConnectionId = ConnectionId, DatabaseName = DatabaseName, SchemaName = SchemaName, ObjectName = ObjectName, IconKind = "FormatListBulleted", IconColor = "#607D8B" },
                        new() { DisplayName = "索引", NodeType = TreeNodeType.IndexGroup, ConnectionId = ConnectionId, DatabaseName = DatabaseName, SchemaName = SchemaName, ObjectName = ObjectName, IconKind = "KeyOutline", IconColor = "#FF9800" },
                        new() { DisplayName = "外键", NodeType = TreeNodeType.ForeignKeyGroup, ConnectionId = ConnectionId, DatabaseName = DatabaseName, SchemaName = SchemaName, ObjectName = ObjectName, IconKind = "KeyLink", IconColor = "#9C27B0" }
                    };
                    break;

                case TreeNodeType.ColumnGroup:
                    var colNodes = await _navigateService.GetColumnNodesAsync(ConnectionId, DatabaseName!, ObjectName!, SchemaName);
                    foreach (var c in colNodes) { c.IconKind = "TextBoxOutline"; c.IconColor = "#607D8B"; }
                    children = colNodes.ToList();
                    break;

                case TreeNodeType.IndexGroup:
                    var idxNodes = await _navigateService.GetIndexNodesAsync(ConnectionId, DatabaseName!, ObjectName!, SchemaName);
                    children = idxNodes.ToList();
                    break;

                case TreeNodeType.ForeignKeyGroup:
                    var fkNodes = await _navigateService.GetForeignKeyNodesAsync(ConnectionId, DatabaseName!, ObjectName!, SchemaName);
                    children = fkNodes.ToList();
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