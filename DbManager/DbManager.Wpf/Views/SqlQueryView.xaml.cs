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
    }

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