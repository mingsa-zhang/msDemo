using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DbManager.Common;
using DbManager.Core.Adapters;
using DbManager.Core.Enums;
using DbManager.Core.Interfaces;
using DbManager.Core.Models;
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
            TreeNodeType.Table => "TableMenu",
            TreeNodeType.View => "ViewMenu",
            TreeNodeType.Procedure => "ProcedureMenu",
            TreeNodeType.Function => "FunctionMenu",
            TreeNodeType.TableGroup => "GroupMenu",
            TreeNodeType.ViewGroup => "GroupMenu",
            TreeNodeType.ProcedureGroup => "GroupMenu",
            TreeNodeType.FunctionGroup => "GroupMenu",
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
                ViewModel?.RequestOpenDataBrowser(node.ConnectionId, node.DatabaseName ?? "", node.ObjectName ?? "");
                break;
            case TreeNodeType.Database:
                ViewModel?.RequestOpenSqlQuery(node.ConnectionId, node.DatabaseName ?? "");
                break;
            case TreeNodeType.Connection:
                node.IsExpanded = !node.IsExpanded;
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
        if (GetClickedNode(sender) is { } node)
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
            ViewModel?.RequestOpenDataBrowser(node.ConnectionId, node.DatabaseName ?? "", node.ObjectName ?? "");
    }

    private void Menu_DesignTable(object sender, RoutedEventArgs e)
    {
        if (GetClickedNode(sender) is { } node)
            ViewModel?.RequestOpenTableDesign(node.ConnectionId, node.DatabaseName ?? "", node.ObjectName ?? "");
    }

    private void Menu_CopyName(object sender, RoutedEventArgs e)
    {
        if (GetClickedNode(sender) is { } node && node.ObjectName != null)
        {
            Clipboard.SetText(node.ObjectName);
        }
    }

    private void Menu_Refresh(object sender, RoutedEventArgs e)
    {
        if (GetClickedNode(sender) is { } node)
        {
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
            var sql = conn.DbType switch
            {
                DbTypeEnum.MySql or DbTypeEnum.MariaDB => $"TRUNCATE TABLE `{node.DatabaseName}`.`{node.ObjectName}`",
                DbTypeEnum.SqlServer => $"TRUNCATE TABLE [{node.DatabaseName}].[dbo].[{node.ObjectName}]",
                DbTypeEnum.PostgreSQL => $"TRUNCATE TABLE public.{node.ObjectName}",
                DbTypeEnum.SQLite => $"DELETE FROM \"{node.ObjectName}\"",
                _ => $"TRUNCATE TABLE {node.ObjectName}"
            };
            var execResult = await executeService.ExecuteQueryAsync(connStr, sql);
            if (execResult.IsSuccess)
                MessageBox.Show("截断成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show($"截断失败: {execResult.ErrorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"截断失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion
}