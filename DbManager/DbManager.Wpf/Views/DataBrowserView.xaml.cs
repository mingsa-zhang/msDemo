using System.Data;
using System.Linq;
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
}
