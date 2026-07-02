using System.Windows;
using System.Windows.Controls;
using DbManager.Wpf.ViewModels;

namespace DbManager.Wpf.Views;

public partial class AddOrEditConnWindow : Window
{
    private AddEditConnViewModel? _viewModel;

    public AddOrEditConnWindow()
    {
        InitializeComponent();
        Loaded += AddOrEditConnWindow_Loaded;
    }

    private void AddOrEditConnWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _viewModel = DataContext as AddEditConnViewModel;
        if (_viewModel == null) return;

        // 关系型数据库密码 → Connection.Password
        HookPasswordBox("PasswordBox", pwd => _viewModel.Connection.Password = pwd, () => _viewModel.Connection.Password);
        // Oracle密码 → Connection.Password（Oracle也用Password字段）
        HookPasswordBox("OraclePasswordBox", pwd => _viewModel.Connection.Password = pwd, () => _viewModel.Connection.Password);
        // MongoDB密码 → Connection.Password
        HookPasswordBox("MongoPasswordBox", pwd => _viewModel.Connection.Password = pwd, () => _viewModel.Connection.Password);
        // Redis密码 → Connection.RedisPassword
        HookPasswordBox("RedisPasswordBox", pwd => _viewModel.Connection.RedisPassword = pwd, () => _viewModel.Connection.RedisPassword);
    }

    private void HookPasswordBox(string name, Action<string> setter, Func<string> getter)
    {
        var pb = FindName(name) as PasswordBox;
        if (pb == null) return;

        // 回填已有密码
        var existing = getter();
        if (!string.IsNullOrEmpty(existing))
            pb.Password = existing;

        pb.PasswordChanged += (s, e) => setter(pb.Password);
    }
}