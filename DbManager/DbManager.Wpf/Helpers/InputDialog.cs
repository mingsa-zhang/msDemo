using System.Windows;
using System.Windows.Controls;

namespace DbManager.Wpf.Helpers;

/// <summary>
/// 通用单行文本输入弹窗：确定返回文本，取消返回 null。
/// </summary>
public static class InputDialog
{
    /// <summary>
    /// 弹出输入框。返回用户输入（去空白）；取消或输入空白返回 null。
    /// </summary>
    public static string? Show(Window? owner, string title, string prompt, string initialValue = "")
    {
        var panel = new DockPanel { Margin = new Thickness(12) };

        var label = new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 8) };
        DockPanel.SetDock(label, Dock.Top);
        panel.Children.Add(label);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0)
        };
        DockPanel.SetDock(buttons, Dock.Bottom);

        var box = new TextBox
        {
            Text = initialValue,
            Padding = new Thickness(4),
            VerticalAlignment = VerticalAlignment.Top
        };
        panel.Children.Add(box);

        var win = new Window
        {
            Title = title,
            Width = 380,
            Height = 170,
            Content = panel,
            ResizeMode = ResizeMode.NoResize,
            Owner = owner,
            WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
        };

        var ok = new Button { Content = "确定", Width = 72, Height = 28, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        var cancel = new Button { Content = "取消", Width = 72, Height = 28, IsCancel = true };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);

        ok.Click += (_, _) => win.DialogResult = true;

        box.Focus();
        box.SelectAll();

        if (win.ShowDialog() != true)
        {
            return null;
        }

        var result = box.Text.Trim();
        return string.IsNullOrEmpty(result) ? null : result;
    }
}
