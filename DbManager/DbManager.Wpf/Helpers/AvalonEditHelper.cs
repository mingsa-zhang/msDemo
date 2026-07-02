using System.Windows;
using ICSharpCode.AvalonEdit;

namespace DbManager.Wpf.Helpers;

public static class AvalonEditHelper
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.RegisterAttached(
            "Text",
            typeof(string),
            typeof(AvalonEditHelper),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

    public static string GetText(DependencyObject obj) => (string)obj.GetValue(TextProperty);
    public static void SetText(DependencyObject obj, string value) => obj.SetValue(TextProperty, value);

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextEditor editor)
        {
            if (editor.Text != (string)e.NewValue)
                editor.Text = (string)e.NewValue;

            editor.TextChanged -= EditorOnTextChanged;
            editor.TextChanged += EditorOnTextChanged;
        }
    }

    private static void EditorOnTextChanged(object? sender, EventArgs e)
    {
        if (sender is TextEditor editor)
        {
            SetText(editor, editor.Text);
        }
    }
}