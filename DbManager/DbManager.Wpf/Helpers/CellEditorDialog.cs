using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DbManager.Core.Enums;
using Newtonsoft.Json;

namespace DbManager.Wpf.Helpers;

/// <summary>
/// 类型化单元格编辑弹窗：按列逻辑类型呈现不同编辑器
/// （日期用 DatePicker、JSON/文本多行并可格式化、二进制只读预览、其余单行文本），
/// 支持将值置为 NULL。数据浏览编辑模式下双击单元格调用。
/// </summary>
public static class CellEditorDialog
{
    /// <summary>
    /// 编辑结果。
    /// </summary>
    public sealed class EditResult
    {
        /// <summary>
        /// 用户是否确认
        /// </summary>
        public bool Confirmed { get; init; }

        /// <summary>
        /// 是否置为 NULL
        /// </summary>
        public bool IsNull { get; init; }

        /// <summary>
        /// 编辑后的文本值（IsNull 为 true 时忽略）
        /// </summary>
        public string Value { get; init; } = string.Empty;
    }

    /// <summary>
    /// 弹出类型化编辑器；返回编辑结果（取消时 Confirmed=false）。
    /// </summary>
    public static EditResult Show(Window? owner, string columnName, LogicalTypeEnum logicalType, bool nullable, string? currentValue, bool isCurrentNull)
    {
        var result = new EditResult();

        var rootPanel = new DockPanel { Margin = new Thickness(10) };

        // 顶部：类型说明
        var typeLabel = new TextBlock
        {
            Text = $"类型: {DescribeType(logicalType)}",
            Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 6)
        };
        DockPanel.SetDock(typeLabel, Dock.Top);
        rootPanel.Children.Add(typeLabel);

        // NULL 选项
        var nullCheck = new CheckBox
        {
            Content = "设为 NULL",
            IsChecked = isCurrentNull,
            IsEnabled = nullable,
            Margin = new Thickness(0, 0, 0, 6),
            VerticalContentAlignment = VerticalAlignment.Center
        };
        DockPanel.SetDock(nullCheck, Dock.Top);
        rootPanel.Children.Add(nullCheck);

        // 底部按钮区
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 8, 0, 0)
        };
        DockPanel.SetDock(buttonPanel, Dock.Bottom);
        var okButton = new Button { Content = "确定", Width = 72, Height = 28, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        var cancelButton = new Button { Content = "取消", Width = 72, Height = 28, IsCancel = true };
        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        rootPanel.Children.Add(buttonPanel);

        // 主编辑区（按类型构建）
        var editor = BuildEditor(logicalType, currentValue, out var valueGetter, out var editableRoot);
        rootPanel.Children.Add(editor);

        var win = new Window
        {
            Title = $"编辑单元格 - {columnName}",
            Width = 520,
            Height = logicalType is LogicalTypeEnum.Text or LogicalTypeEnum.Json or LogicalTypeEnum.Binary ? 420 : 200,
            Content = rootPanel,
            Owner = owner,
            WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
        };

        // NULL 勾选时禁用编辑区
        void SyncEnabled()
        {
            if (editableRoot != null)
            {
                editableRoot.IsEnabled = nullCheck.IsChecked != true;
            }
        }
        nullCheck.Checked += (_, _) => SyncEnabled();
        nullCheck.Unchecked += (_, _) => SyncEnabled();
        SyncEnabled();

        okButton.Click += (_, _) =>
        {
            result = new EditResult
            {
                Confirmed = true,
                IsNull = nullCheck.IsChecked == true,
                Value = nullCheck.IsChecked == true ? string.Empty : valueGetter()
            };
            win.DialogResult = true;
        };

        return win.ShowDialog() == true ? result : new EditResult { Confirmed = false };
    }

    /// <summary>
    /// 依逻辑类型构建编辑控件，并输出取值委托与可禁用根元素。
    /// </summary>
    private static UIElement BuildEditor(LogicalTypeEnum logicalType, string? currentValue, out Func<string> valueGetter, out FrameworkElement? editableRoot)
    {
        switch (logicalType)
        {
            case LogicalTypeEnum.DateTime:
                return BuildDateTimeEditor(currentValue, out valueGetter, out editableRoot);

            case LogicalTypeEnum.Binary:
                return BuildBinaryEditor(currentValue, out valueGetter, out editableRoot);

            case LogicalTypeEnum.Text:
            case LogicalTypeEnum.Json:
                return BuildMultilineEditor(currentValue, logicalType == LogicalTypeEnum.Json, out valueGetter, out editableRoot);

            default:
                return BuildSingleLineEditor(currentValue, out valueGetter, out editableRoot);
        }
    }

    private static UIElement BuildSingleLineEditor(string? currentValue, out Func<string> valueGetter, out FrameworkElement? editableRoot)
    {
        var box = new TextBox
        {
            Text = currentValue ?? string.Empty,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            Padding = new Thickness(4),
            VerticalAlignment = VerticalAlignment.Top
        };
        valueGetter = () => box.Text;
        editableRoot = box;
        return box;
    }

    private static UIElement BuildMultilineEditor(string? currentValue, bool isJson, out Func<string> valueGetter, out FrameworkElement? editableRoot)
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var box = new TextBox
        {
            Text = isJson ? CellValueViewer.PrettyIfJson(currentValue ?? string.Empty) : currentValue ?? string.Empty,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            Padding = new Thickness(4)
        };
        Grid.SetRow(box, 0);
        grid.Children.Add(box);

        if (isJson)
        {
            var formatBtn = new Button
            {
                Content = "格式化 JSON",
                HorizontalAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 6, 0, 0)
            };
            formatBtn.Click += (_, _) => box.Text = CellValueViewer.PrettyIfJson(box.Text);
            Grid.SetRow(formatBtn, 1);
            grid.Children.Add(formatBtn);
        }

        valueGetter = () => box.Text;
        editableRoot = grid;
        return grid;
    }

    private static UIElement BuildDateTimeEditor(string? currentValue, out Func<string> valueGetter, out FrameworkElement? editableRoot)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Top };

        DateTime? parsed = DateTime.TryParse(currentValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
            ? dt
            : null;

        var picker = new DatePicker
        {
            SelectedDate = parsed,
            Margin = new Thickness(0, 0, 0, 6)
        };

        var timeRow = new StackPanel { Orientation = Orientation.Horizontal };
        timeRow.Children.Add(new TextBlock { Text = "时间:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
        var timeBox = new TextBox
        {
            Text = parsed?.ToString("HH:mm:ss", CultureInfo.InvariantCulture) ?? "00:00:00",
            Width = 120,
            FontFamily = new FontFamily("Consolas"),
            Padding = new Thickness(4)
        };
        timeRow.Children.Add(timeBox);

        panel.Children.Add(picker);
        panel.Children.Add(timeRow);

        valueGetter = () =>
        {
            if (picker.SelectedDate is not { } d)
            {
                // 未选日期则回退为原始文本，避免丢值
                return currentValue ?? string.Empty;
            }

            var timePart = TimeSpan.TryParse(timeBox.Text, CultureInfo.InvariantCulture, out var ts) ? ts : TimeSpan.Zero;
            var combined = d.Date + timePart;
            // 无时间部分则仅输出日期
            return timePart == TimeSpan.Zero
                ? combined.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : combined.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        };
        editableRoot = panel;
        return panel;
    }

    private static UIElement BuildBinaryEditor(string? currentValue, out Func<string> valueGetter, out FrameworkElement? editableRoot)
    {
        var note = new TextBox
        {
            Text = string.IsNullOrEmpty(currentValue)
                ? "(二进制/BLOB 数据，暂不支持在此编辑，可勾选 NULL 清空)"
                : $"(二进制/BLOB 数据，长度约 {currentValue.Length} 字符，暂不支持在此编辑)\n\n{Preview(currentValue)}",
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(0x77, 0x77, 0x77)),
            Padding = new Thickness(4)
        };
        // 二进制不可编辑，取值恒为原值
        valueGetter = () => currentValue ?? string.Empty;
        editableRoot = null; // 不随 NULL 勾选禁用（本就只读）
        return note;
    }

    private static string Preview(string value)
        => value.Length <= 512 ? value : value[..512] + " …";

    private static string DescribeType(LogicalTypeEnum t) => t switch
    {
        LogicalTypeEnum.Text => "文本",
        LogicalTypeEnum.Number => "数值",
        LogicalTypeEnum.Boolean => "布尔",
        LogicalTypeEnum.DateTime => "日期时间",
        LogicalTypeEnum.Binary => "二进制/BLOB",
        LogicalTypeEnum.Json => "JSON",
        LogicalTypeEnum.Guid => "GUID",
        _ => "未知"
    };
}
