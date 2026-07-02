using System.Windows;
using System.Windows.Input;
using DbManager.Core.Services;
using DbManager.Wpf.ViewModels;

namespace DbManager.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        KeyDown += MainWindow_KeyDown;
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5 && Keyboard.Modifiers == ModifierKeys.Control)
        {
            ViewModel?.ExecuteSelectedSql();
            e.Handled = true;
        }
        else if (e.Key == Key.F5)
        {
            ViewModel?.ExecuteSql();
            e.Handled = true;
        }
        else if (e.Key == Key.R && Keyboard.Modifiers == ModifierKeys.Control)
        {
            ViewModel?.RefreshTreeCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.W && Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (ViewModel?.SelectedTab is { } tab)
            {
                ViewModel.CloseTabCommand.Execute(tab);
                e.Handled = true;
            }
        }
        else if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
        {
            ViewModel?.AddConnectionCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Q && Keyboard.Modifiers == ModifierKeys.Control)
        {
            Menu_NewQuery(sender, e);
            e.Handled = true;
        }
    }

    private void CloseTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is TabItemViewModel tab)
        {
            ViewModel?.CloseTabCommand.Execute(tab);
        }
    }

    private void CloseOtherTabs_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is TabItemViewModel tab)
        {
            ViewModel?.CloseOtherTabs(tab);
        }
    }

    private void CloseAllTabs_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.CloseAllTabs();
    }

    private void Menu_NewConnection(object sender, RoutedEventArgs e)
    {
        ViewModel?.AddConnectionCommand.Execute(null);
    }

    private void Menu_NewConnection_Click(object sender, RoutedEventArgs e)
    {
        Menu_NewConnection(sender, e);
    }

    private void Menu_NewQuery(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            var selectedNode = ViewModel.TreeViewModel.SelectedNode;
            if (selectedNode != null && selectedNode.ConnectionId > 0)
                _ = ViewModel.OpenSqlQueryAsync(selectedNode.ConnectionId, selectedNode.DatabaseName ?? "");
            else
                _ = ViewModel.OpenSqlQueryAsync(-1, "");
        }
    }

    private void Menu_NewQuery_Click(object sender, RoutedEventArgs e)
    {
        Menu_NewQuery(sender, e);
    }

    private void Menu_Refresh(object sender, RoutedEventArgs e)
    {
        ViewModel?.RefreshTreeCommand.Execute(null);
    }

    private void Menu_ToggleLeftPanel(object sender, RoutedEventArgs e)
    {
        LeftPanel.Visibility = LeftPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Menu_Settings(object sender, RoutedEventArgs e)
    {
        var settingsService = new SettingsService();
        var win = new Views.SettingsWindow
        {
            DataContext = new SettingsViewModel(settingsService),
            Owner = this
        };
        win.ShowDialog();
    }

    private void Menu_CloseTab(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedTab is { } tab)
        {
            ViewModel.CloseTabCommand.Execute(tab);
        }
    }

    private void Menu_About(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("DbManager v1.0\n多数据库管理工具", "关于", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Menu_Exit(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
