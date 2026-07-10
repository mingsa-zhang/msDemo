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
            TreeNodeType.Group => "GroupMenu",
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
                // MongoDB 库无 SQL 查询语义，双击展开即可
                if (node.DbType == DbManager.Core.Enums.DbTypeEnum.MongoDB)
                {
                    node.IsExpanded = !node.IsExpanded;
                }
                else
                {
                    ViewModel?.RequestOpenSqlQuery(node.ConnectionId, node.DatabaseName ?? "");
                }
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

    #endregion
}