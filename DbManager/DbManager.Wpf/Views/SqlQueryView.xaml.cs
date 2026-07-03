using System.Windows.Controls;
using System.Windows.Input;
using DbManager.Wpf.ViewModels;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Search;
using System.Xml;

namespace DbManager.Wpf.Views;

public partial class SqlQueryView : UserControl
{
    private SqlQueryTabViewModel? _viewModel;

    public SqlQueryView()
    {
        InitializeComponent();
        DataContextChanged += SqlQueryView_DataContextChanged;
        KeyDown += SqlQueryView_KeyDown;
    }

    private void SqlQueryView_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        _viewModel = DataContext as SqlQueryTabViewModel;
        if (_viewModel == null) return;

        LoadSqlHighlighting();

        SqlEditor.Text = _viewModel.SqlText;
        SqlEditor.TextChanged += (s, args) => _viewModel.SqlText = SqlEditor.Text;
        // 实时同步选中文本，使工具栏「运行选中」按钮也能拿到当前选区（不止 Ctrl+F5）
        SqlEditor.TextArea.SelectionChanged += (s, args) =>
        {
            if (_viewModel != null) _viewModel.SelectedSql = SqlEditor.SelectedText;
        };
        SqlEditor.Options.HighlightCurrentLine = true;
        SqlEditor.Options.ShowColumnRuler = false;
        SqlEditor.Options.ConvertTabsToSpaces = true;
        SqlEditor.Options.IndentationSize = 2;

        SearchPanel.Install(SqlEditor);
    }

    private void SqlQueryView_KeyDown(object sender, KeyEventArgs e)
    {
        if (_viewModel == null) return;

        if (e.Key == Key.F5 && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
        {
            _viewModel.SelectedSql = SqlEditor.SelectedText;
            _viewModel.ExecuteSelectedCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.F5)
        {
            _viewModel.ExecuteCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.F && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
        {
            OpenSearchPanel();
            e.Handled = true;
        }
        else if (e.Key == Key.H && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
        {
            OpenSearchPanel();
            e.Handled = true;
        }
        else if (e.Key == Key.OemQuestion && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
        {
            ToggleLineComment();
            e.Handled = true;
        }
        else if (e.Key == Key.U && e.KeyboardDevice.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            TransformSelectionCase(toUpper: true);
            e.Handled = true;
        }
        else if (e.Key == Key.L && e.KeyboardDevice.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            TransformSelectionCase(toUpper: false);
            e.Handled = true;
        }
    }

    /// <summary>
    /// 注释/取消注释选中行（无选中时作用于当前行）。整块统一：全为注释则取消，否则全部注释。
    /// </summary>
    private void ToggleLineComment()
    {
        var doc = SqlEditor.Document;
        int startOffset = SqlEditor.SelectionLength > 0 ? SqlEditor.SelectionStart : SqlEditor.CaretOffset;
        int endOffset = SqlEditor.SelectionLength > 0 ? SqlEditor.SelectionStart + SqlEditor.SelectionLength : SqlEditor.CaretOffset;

        var startLine = doc.GetLineByOffset(startOffset);
        var endLine = doc.GetLineByOffset(endOffset);

        var lines = new List<ICSharpCode.AvalonEdit.Document.DocumentLine>();
        for (var line = startLine; line != null && line.LineNumber <= endLine.LineNumber; line = line.NextLine)
        {
            lines.Add(line);
        }

        var contentLines = lines.Where(l => !string.IsNullOrWhiteSpace(doc.GetText(l))).ToList();
        if (contentLines.Count == 0) return;

        bool allCommented = contentLines.All(l => doc.GetText(l).TrimStart().StartsWith("--", StringComparison.Ordinal));

        doc.BeginUpdate();
        try
        {
            // 从后往前改，保证偏移量不失效
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                var line = lines[i];
                var text = doc.GetText(line);
                if (string.IsNullOrWhiteSpace(text)) continue;

                if (allCommented)
                {
                    int idx = text.IndexOf("--", StringComparison.Ordinal);
                    if (idx >= 0)
                    {
                        int removeLen = (idx + 2 < text.Length && text[idx + 2] == ' ') ? 3 : 2;
                        doc.Remove(line.Offset + idx, removeLen);
                    }
                }
                else
                {
                    int firstNonWs = 0;
                    while (firstNonWs < text.Length && char.IsWhiteSpace(text[firstNonWs])) firstNonWs++;
                    doc.Insert(line.Offset + firstNonWs, "-- ");
                }
            }
        }
        finally
        {
            doc.EndUpdate();
        }
    }

    /// <summary>
    /// 将选中文本转为大写或小写。
    /// </summary>
    private void TransformSelectionCase(bool toUpper)
    {
        if (SqlEditor.SelectionLength == 0) return;

        int start = SqlEditor.SelectionStart;
        int len = SqlEditor.SelectionLength;
        var text = SqlEditor.Document.GetText(start, len);
        var transformed = toUpper ? text.ToUpperInvariant() : text.ToLowerInvariant();
        SqlEditor.Document.Replace(start, len, transformed);
        SqlEditor.Select(start, transformed.Length);
    }

    private void ToggleComment_Click(object sender, System.Windows.RoutedEventArgs e) => ToggleLineComment();
    private void UpperCase_Click(object sender, System.Windows.RoutedEventArgs e) => TransformSelectionCase(toUpper: true);
    private void LowerCase_Click(object sender, System.Windows.RoutedEventArgs e) => TransformSelectionCase(toUpper: false);

    private void OpenSearchPanel()
    {
        if (!string.IsNullOrEmpty(SqlEditor.SelectedText))
        {
            var panel = SearchPanel.Install(SqlEditor);
            panel.SearchPattern = SqlEditor.SelectedText;
        }
    }

    private void FindReplace_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        OpenSearchPanel();
    }

    private void LoadSqlHighlighting()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = "DbManager.Wpf.Resources.SQL.xshd";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = XmlReader.Create(stream);
                SqlEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }
        }
        catch
        {
            SqlEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(".sql");
        }
    }
}