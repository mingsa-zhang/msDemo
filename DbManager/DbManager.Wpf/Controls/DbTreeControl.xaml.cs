using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DbManager.Common;
using DbManager.Core.Adapters;
using DbManager.Core.Enums;
using DbManager.Core.Interfaces;
using DbManager.Core.Models;
using DbManager.Core.Services;
using DbManager.Wpf.ViewModels;

namespace DbManager.Wpf.Controls;

public partial class DbTreeControl : UserControl
{
    private DbTreeViewModel? ViewModel => DataContext as DbTreeViewModel;

    public DbTreeControl()
    {
        InitializeComponent();
    }

    private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (ViewModel != null && e.NewValue is DbTreeNodeViewModel node)
        {
            ViewModel.SelectedNode = node;
        }
    }

    private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is TreeViewItem item && item.DataContext is DbTreeNodeViewModel node)
        {
            if (!node.IsLoaded) _ = node.LoadChildrenAsync();
        }
    }

    private void TreeViewItem_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is TreeViewItem item && item.DataContext is DbTreeNodeViewModel node)
        {
            item.IsSelected = true;
            item.ContextMenu = GetContextMenuForNode(node);
            e.Handled = true;
        }
    }

    private ContextMenu? GetContextMenuForNode(DbTreeNodeViewModel node)
    {
        string key = node.NodeType switch
        {
            TreeNodeType.Group => "ConnGroupMenu",
            TreeNodeType.Connection => "ConnectionMenu",
            TreeNodeType.Database => "DatabaseMenu",
            TreeNodeType.Schema => "GroupMenu",
            TreeNodeType.Table => "TableMenu",
            TreeNodeType.View => "ViewMenu",
            TreeNodeType.Procedure => "ProcedureMenu",
            TreeNodeType.Function => "FunctionMenu",
            TreeNodeType.TableGroup => "TableGroupMenu",
            TreeNodeType.ViewGroup => "GroupMenu",
            TreeNodeType.ProcedureGroup => "GroupMenu",
            TreeNodeType.FunctionGroup => "GroupMenu",
            TreeNodeType.ColumnGroup => "GroupMenu",
            TreeNodeType.IndexGroup => "GroupMenu",
            TreeNodeType.ForeignKeyGroup => "GroupMenu",
            TreeNodeType.Column => "ColumnMenu",
            _ => ""
        };

        if (string.IsNullOrEmpty(key)) return null;

        var treeView = FindTreeView();
        if (treeView != null && treeView.Resources[key] is ContextMenu menu)
            return menu;

        return null;
    }

    private TreeView? FindTreeView()
    {
        return DbTreeView;
    }

    private void TreeViewItem_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is TreeViewItem item && item.DataContext is DbTreeNodeViewModel node)
        {
            HandleDoubleClick(node);
            e.Handled = true;
        }
    }

    private void HandleDoubleClick(DbTreeNodeViewModel node)
    {
        switch (node.NodeType)
        {
            case TreeNodeType.Table:
            case TreeNodeType.View:
                ViewModel?.RequestOpenDataBrowser(node.ConnectionId, node.DatabaseName ?? "", node.ObjectName ?? "", node.SchemaName);
                break;
            case TreeNodeType.Collection:
                // MongoDB 集合：打开文档浏览
                ViewModel?.RequestOpenMongoBrowser(node.ConnectionId, node.DatabaseName ?? "", node.ObjectName ?? "");
                break;
            case TreeNodeType.Database:
                // 双击库：展开/折叠查看下级对象（表/Schema 等），与"连接"节点的双击行为一致；
                // 新建查询走右键菜单"新建查询"，不占用双击语义
                node.IsExpanded = !node.IsExpanded;
                break;
            case TreeNodeType.Connection:
                // Redis 连接：双击打开键浏览器；其余展开
                if (node.DbType == DbManager.Core.Enums.DbTypeEnum.Redis)
                {
                    ViewModel?.RequestOpenRedisBrowser(node.ConnectionId);
                }
                else
                {
                    node.IsExpanded = !node.IsExpanded;
                }
                break;
        }
    }

    #region 右键菜单事件

    private DbTreeNodeViewModel? GetClickedNode(object sender)
    {
        if (sender is FrameworkElement fe && fe.DataContext is DbTreeNodeViewModel node)
            return node;
        return null;
    }

    private void Menu_NewQuery(object sender, RoutedEventArgs e)
    {
        if (GetClickedNode(sender) is { } node)
            ViewModel?.RequestOpenSqlQuery(node.ConnectionId, node.DatabaseName ?? "");
    }

    private async void Menu_EditConnection(object sender, RoutedEventArgs e)
    {
        if (GetClickedNode(sender) is not { } node) return;

        // async void 事件处理器：捕获异常避免未处理异常导致应用崩溃
        try
        {
            var connectionService = App.ConnectionService;
            var connection = await connectionService.GetConnectionByIdAsync(node.ConnectionId);
            if (connection == null) return;

            var window = new Views.AddOrEditConnWindow
            {
                DataContext = new AddEditConnViewModel(connectionService, connection),
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
            _ = ViewModel?.RefreshAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"编辑连接失败: {DbManager.Common.DbErrorTranslator.Translate(ex)}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Menu_DeleteConnection(object sender, RoutedEventArgs e)
    {
        if (GetClickedNode(sender) is { } node)
        {
            var result = MessageBox.Show($"确定删除连接「{node.DisplayName}」？", "确认删除",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                _ = App.ConnectionService.DeleteConnectionAsync(node.ConnectionId);
                SshTunnelManager.CloseForConnection(node.ConnectionId);
                _ = ViewModel?.RefreshAsync();
            }
        }
    }

    private void Menu_ViewData(object sender, RoutedEventArgs e)
    {
        if (GetClickedNode(sender) is { } node)
            ViewModel?.RequestOpenDataBrowser(node.ConnectionId, node.DatabaseName ?? "", node.ObjectName ?? "", node.SchemaName);
    }

    private void Menu_DesignTable(object sender, RoutedEventArgs e)
    {
        if (GetClickedNode(sender) is { } node)
            ViewModel?.RequestOpenTableDesign(node.ConnectionId, node.DatabaseName ?? "", node.ObjectName ?? "", node.SchemaName);
    }

    private void Menu_NewTable(object sender, RoutedEventArgs e)
    {
        if (GetClickedNode(sender) is { } node)
            ViewModel?.RequestOpenNewTable(node.ConnectionId, node.DatabaseName ?? "", node.SchemaName);
    }

    private void Menu_CopyName(object sender, RoutedEventArgs e)
    {
        if (GetClickedNode(sender) is { } node && node.ObjectName != null)
        {
            Clipboard.SetText(node.ObjectName);
        }
    }

    private async void Menu_ViewDdl(object sender, RoutedEventArgs e)
    {
        if (GetClickedNode(sender) is not { } node || node.ObjectName is null) return;

        try
        {
            var conn = await App.ConnectionService.GetConnectionByIdAsync(node.ConnectionId);
            if (conn == null) return;
            var metadataService = App.MetadataFactory.Create(conn.DbType);
            var connStr = DbConnStringBuilder.BuildDecryptedConnectionString(conn);
            var ddl = await metadataService.GetCreateTableSqlAsync(connStr, node.DatabaseName ?? "", node.ObjectName, node.SchemaName);

            if (string.IsNullOrWhiteSpace(ddl))
            {
                MessageBox.Show("未能获取建表 SQL。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try { Clipboard.SetText(ddl); } catch { /* 剪贴板偶发占用，忽略 */ }
            MessageBox.Show($"{ddl}\n\n（已复制到剪贴板）", $"建表 SQL - {node.ObjectName}",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"获取建表 SQL 失败: {DbManager.Common.DbErrorTranslator.Translate(ex)}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Menu_Refresh(object sender, RoutedEventArgs e)
    {
        if (GetClickedNode(sender) is { } node)
        {
            // 先失效该连接的元数据缓存，确保刷新是真正重新查库
            ViewModel?.InvalidateConnectionCache(node.ConnectionId);
            node.IsLoaded = false;
            node.Children.Clear();
            _ = node.LoadChildrenAsync();
        }
    }

    private async void Menu_TruncateTable(object sender, RoutedEventArgs e)
    {
        if (GetClickedNode(sender) is not { } node) return;

        var result = MessageBox.Show($"确定截断表「{node.ObjectName}」？\n此操作将删除所有数据且不可恢复！", "确认截断",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            var conn = await App.ConnectionService.GetConnectionByIdAsync(node.ConnectionId);
            if (conn == null) return;
            var executeService = App.ExecuteFactory.Create(conn.DbType);
            var connStr = DbConnStringBuilder.BuildDecryptedConnectionString(conn);
            var dialect = DialectProvider.GetDialect(conn.DbType);
            var qualifiedTable = dialect.QualifyTable(node.DatabaseName, node.SchemaName, node.ObjectName ?? "");
            // SQLite 不支持 TRUNCATE，用 DELETE 代替
            var sql = conn.DbType == DbTypeEnum.SQLite
                ? $"DELETE FROM {qualifiedTable}"
                : $"TRUNCATE TABLE {qualifiedTable}";
            var execResult = await executeService.ExecuteQueryAsync(connStr, sql);
            if (execResult.IsSuccess)
                MessageBox.Show("截断成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show($"截断失败: {DbManager.Common.DbErrorTranslator.Translate(execResult.ErrorMessage)}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"截断失败: {DbManager.Common.DbErrorTranslator.Translate(ex)}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 顶部"新建连接"：打开连接编辑窗，关闭后刷新树。
    /// </summary>
    private async void Btn_NewConnection(object sender, RoutedEventArgs e)
    {
        try
        {
            var window = new Views.AddOrEditConnWindow
            {
                DataContext = new AddEditConnViewModel(App.ConnectionService, new DbManager.Core.Models.DbConnectionModel()),
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
            if (ViewModel != null)
            {
                await ViewModel.RefreshAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"新建连接失败: {DbManager.Common.DbErrorTranslator.Translate(ex)}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 在指定分组下新建连接（预选该分组）。
    /// </summary>
    private async void Menu_NewConnectionFromGroup(object sender, RoutedEventArgs e)
    {
        var groupId = GetClickedNode(sender)?.Id ?? 0;
        try
        {
            var model = new DbManager.Core.Models.DbConnectionModel { GroupId = groupId };
            var window = new Views.AddOrEditConnWindow
            {
                DataContext = new AddEditConnViewModel(App.ConnectionService, model),
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
            if (ViewModel != null) await ViewModel.RefreshAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"新建连接失败: {DbManager.Common.DbErrorTranslator.Translate(ex)}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 测试连接：按数据库类型走对应服务。
    /// </summary>
    private async void Menu_TestConnection(object sender, RoutedEventArgs e)
    {
        if (GetClickedNode(sender) is not { } node) return;

        try
        {
            var conn = await App.ConnectionService.GetConnectionByIdAsync(node.ConnectionId);
            if (conn == null) return;
            var connStr = DbConnStringBuilder.BuildDecryptedConnectionString(conn);

            if (conn.DbType == DbTypeEnum.MongoDB)
            {
                await new MongoService().TestAsync(connStr);
                MessageBox.Show("MongoDB 连接成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (conn.DbType == DbTypeEnum.Redis)
            {
                await new RedisService().TestAsync(connStr);
                MessageBox.Show("Redis 连接成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var metadataService = App.MetadataFactory.Create(conn.DbType);
            var databases = await metadataService.GetDatabasesAsync(connStr);
            MessageBox.Show($"连接成功，发现 {databases.Count} 个数据库", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"连接失败: {DbManager.Common.DbErrorTranslator.Translate(ex)}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 新建分组。
    /// </summary>
    private async void Menu_NewGroup(object sender, RoutedEventArgs e)
    {
        var name = Helpers.InputDialog.Show(Window.GetWindow(this), "新建分组", "请输入分组名称：");
        if (name == null) return;

        try
        {
            await App.ConnectionService.AddGroupAsync(new DbManager.Core.Models.DbConnectionGroupModel { Name = name });
            if (ViewModel != null) await ViewModel.RefreshAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"新建分组失败: {DbManager.Common.DbErrorTranslator.Translate(ex)}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 重命名分组。
    /// </summary>
    private async void Menu_RenameGroup(object sender, RoutedEventArgs e)
    {
        if (GetClickedNode(sender) is not { } node || node.Id <= 0) return;

        var newName = Helpers.InputDialog.Show(Window.GetWindow(this), "重命名分组", "请输入新的分组名称：", node.DisplayName);
        if (newName == null || newName == node.DisplayName) return;

        try
        {
            var groups = await App.ConnectionService.GetAllGroupsAsync();
            var target = groups.FirstOrDefault(g => g.Id == node.Id);
            if (target == null)
            {
                MessageBox.Show("分组不存在，可能已被删除", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            target.Name = newName;
            await App.ConnectionService.UpdateGroupAsync(target);
            if (ViewModel != null) await ViewModel.RefreshAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"重命名失败: {DbManager.Common.DbErrorTranslator.Translate(ex)}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 删除分组（组内连接解组，不删连接）。
    /// </summary>
    private async void Menu_DeleteGroup(object sender, RoutedEventArgs e)
    {
        if (GetClickedNode(sender) is not { } node || node.Id <= 0) return;

        var result = MessageBox.Show($"确定删除分组「{node.DisplayName}」？组内连接将变为未分组（不会删除连接）。",
            "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            await App.ConnectionService.DeleteGroupAsync(node.Id);
            if (ViewModel != null) await ViewModel.RefreshAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"删除分组失败: {DbManager.Common.DbErrorTranslator.Translate(ex)}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion
}