using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DbManager.Wpf.ViewModels;
using Newtonsoft.Json;

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
    /// 只读模式下双击单元格，弹窗查看完整内容（JSON 自动美化）。编辑模式让位给单元格编辑。
    /// </summary>
    private void DataGrid_CellDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (!DataGrid.IsReadOnly) return; // 编辑模式不拦截
        if (DataGrid.CurrentCell.Item is not DataRowView drv || DataGrid.CurrentCell.Column is not { } col) return;

        var colName = col.SortMemberPath;
        if (string.IsNullOrEmpty(colName) || !drv.Row.Table.Columns.Contains(colName)) return;

        var raw = drv.Row[colName];
        var text = raw == null || raw == DBNull.Value ? "(NULL)" : raw.ToString() ?? string.Empty;
        ShowCellValue(colName, PrettyIfJson(text));
    }

    private static string PrettyIfJson(string text)
    {
        var t = text.TrimStart();
        if (t.StartsWith("{") || t.StartsWith("["))
        {
            try { return JsonConvert.SerializeObject(JsonConvert.DeserializeObject(text), Formatting.Indented); }
            catch { /* 不是合法 JSON，原样返回 */ }
        }
        return text;
    }

    private void ShowCellValue(string title, string value)
    {
        var textBox = new TextBox
        {
            Text = value,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            Margin = new Thickness(8)
        };
        var win = new Window
        {
            Title = $"单元格内容 - {title}",
            Width = 520,
            Height = 420,
            Content = textBox,
            Owner = Window.GetWindow(this),
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        win.ShowDialog();
    }
}
