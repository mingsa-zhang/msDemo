using System.Windows.Controls;
using DbManager.Wpf.ViewModels;

namespace DbManager.Wpf.Views;

public partial class ConnListPage : UserControl
{
    public ConnListPage()
    {
        InitializeComponent();
    }

    private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is ConnListViewModel vm && vm.SelectedConnection != null)
        {
            vm.EditConnectionCommand.Execute(null);
        }
    }
}