using System.Windows;
using DbManager.Wpf.ViewModels;

namespace DbManager.Wpf.Views;

/// <summary>
/// 可视化筛选构建器窗口。确认后从 ViewModel.ResultWhere 取生成的 WHERE 片段。
/// </summary>
public partial class FilterBuilderWindow : Window
{
    private readonly FilterBuilderViewModel _viewModel;

    public FilterBuilderWindow(FilterBuilderViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    /// <summary>
    /// 生成的 WHERE 片段（不含 WHERE 关键字）。
    /// </summary>
    public string ResultWhere => _viewModel.ResultWhere;

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Confirm();
        DialogResult = true;
    }
}
