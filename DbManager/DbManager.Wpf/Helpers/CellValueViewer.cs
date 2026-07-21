using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json;

namespace DbManager.Wpf.Helpers;

/// <summary>
/// 单元格完整内容查看弹窗：只读文本框展示，JSON 自动美化。数据浏览与查询结果共用。
/// </summary>
public static class CellValueViewer
{
    /// <summary>
    /// 弹窗显示单元格完整内容。
    /// </summary>
    public static void Show(Window? owner, string columnName, string value)
    {
        var textBox = new TextBox
        {
            Text = PrettyIfJson(value),
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            Margin = new Thickness(8)
        };
        // 鼠标滚轮无论落在文本区还是滚动条上都能滚动（而不是只能拖滚动条）
        textBox.PreviewMouseWheel += (s, e) =>
        {
            textBox.ScrollToVerticalOffset(textBox.VerticalOffset - e.Delta);
            e.Handled = true;
        };
        var win = new Window
        {
            Title = $"单元格内容 - {columnName}",
            Width = 520,
            Height = 420,
            Content = textBox,
            Owner = owner,
            WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
        };
        // 打开即聚焦并全选，可直接 Ctrl+C 复制或按方向键/滚轮浏览，不用先手动点一下
        win.Loaded += (_, _) =>
        {
            textBox.Focus();
            textBox.SelectAll();
        };
        win.ShowDialog();
    }

    /// <summary>
    /// 内容像 JSON 时格式化，否则原样返回。
    /// </summary>
    public static string PrettyIfJson(string text)
    {
        var t = text.TrimStart();
        if (t.StartsWith("{") || t.StartsWith("["))
        {
            try
            {
                return JsonConvert.SerializeObject(JsonConvert.DeserializeObject(text), Formatting.Indented);
            }
            catch
            {
                // 不是合法 JSON，原样返回
            }
        }
        return text;
    }
}
