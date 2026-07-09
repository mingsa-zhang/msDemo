using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DbManager.Wpf.ViewModels;

namespace DbManager.Wpf.Views;

public partial class DataBrowserView : UserControl
{
    public DataBrowserView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 把当前选中行同步给 ViewModel，供「删除行」使用。
    /// </summary>
    private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is DataBrowserViewModel vm && sender is DataGrid grid)
        {
            vm.SetSelectedRows(grid.SelectedItems.OfType<DataRowView>());
        }
    }

    /// <summary>
    /// 行号：把 1 基序号写入行头。
    /// </summary>
    private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        e.Row.Header = (e.Row.GetIndex() + 1).ToString();
    }

    /// <summary>
    /// "复制为"按钮：左键点击即展开其下拉菜单。
    /// </summary>
    private void CopyAsButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is Button btn && btn.ContextMenu is { } menu)
        {
            menu.PlacementTarget = btn;
            menu.IsOpen = true;
        }
    }

    /// <summary>
    /// 列显隐：按当前 DataGrid 列动态生成可勾选菜单，切换列可见性。
    /// </summary>
    private void ColumnToggle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;

        var menu = new ContextMenu { PlacementTarget = btn };
        foreach (var col in DataGrid.Columns)
        {
            var item = new MenuItem
            {
                Header = col.Header?.ToString() ?? string.Empty,
                IsCheckable = true,
                IsChecked = col.Visibility == Visibility.Visible,
                StaysOpenOnClick = true
            };
            var captured = col;
            item.Click += (s, _) =>
            {
                captured.Visibility = ((MenuItem)s!).IsChecked ? Visibility.Visible : Visibility.Collapsed;
            };
            menu.Items.Add(item);
        }
        menu.IsOpen = menu.Items.Count > 0;
    }

    /// <summary>
    /// 双击单元格：只读模式弹窗查看完整内容（JSON 美化）；编辑模式弹出类型化编辑器。
    /// </summary>
    private void DataGrid_CellDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataGrid.CurrentCell.Item is not DataRowView drv || DataGrid.CurrentCell.Column is not { } col) return;

        var colName = col.SortMemberPath;
        if (string.IsNullOrEmpty(colName) || !drv.Row.Table.Columns.Contains(colName)) return;

        var raw = drv.Row[colName];
        var isNull = raw == null || raw == DBNull.Value;

        // 编辑模式：类型化编辑器
        if (!DataGrid.IsReadOnly && DataContext is DataBrowserViewModel vm)
        {
            var logicalType = vm.GetColumnLogicalType(colName);
            var nullable = vm.IsColumnNullable(colName);
            var current = isNull ? null : raw!.ToString();

            var result = Helpers.CellEditorDialog.Show(Window.GetWindow(this), colName, logicalType, nullable, current, isNull);
            if (!result.Confirmed) return;

            // 结束当前单元格的内建编辑，避免与直接写值冲突
            DataGrid.CommitEdit(DataGridEditingUnit.Row, true);
            drv.Row[colName] = result.IsNull ? (object)DBNull.Value : result.Value;
            e.Handled = true;
            return;
        }

        // 只读模式：查看完整内容
        var text = isNull ? "(NULL)" : raw!.ToString() ?? string.Empty;
        Helpers.CellValueViewer.Show(Window.GetWindow(this), colName, text);
    }
}
