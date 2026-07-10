using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DbManager.Wpf.Helpers;

/// <summary>
/// 通用多行文本编辑弹窗（等宽字体，可选 JSON 格式化）。确定返回文本，取消返回 null。
/// </summary>
public static class TextEditDialog
{
    /// <summary>
    /// 弹出多行编辑器。返回编辑后的文本；取消返回 null。
    /// </summary>
    public static string? Show(Window? owner, string title, string initialValue, bool jsonFormat = true)
    {
        var root = new DockPanel { Margin = new Thickness(10) };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 8, 0, 0)
        };
        DockPanel.SetDock(buttons, Dock.Bottom);

        var box = new TextBox
        {
            Text = jsonFormat ? CellValueViewer.PrettyIfJson(initialValue) : initialValue,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            Padding = new Thickness(6)
        };

        var ok = new Button { Content = "确定", Width = 76, Height = 28, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        var cancel = new Button { Content = "取消", Width = 76, Height = 28, IsCancel = true };

        if (jsonFormat)
        {
            var fmt = new Button { Content = "格式化 JSON", Height = 28, Padding = new Thickness(10, 0, 10, 0), Margin = new Thickness(0, 0, 8, 0) };
            fmt.Click += (_, _) => box.Text = CellValueViewer.PrettyIfJson(box.Text);
            buttons.Children.Add(fmt);
        }
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);

        root.Children.Add(buttons);
        root.Children.Add(box);

        var win = new Window
        {
            Title = title,
            Width = 560,
            Height = 460,
            Content = root,
            Owner = owner,
            WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
        };

        ok.Click += (_, _) => win.DialogResult = true;

        return win.ShowDialog() == true ? box.Text : null;
    }
}
