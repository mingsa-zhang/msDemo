using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbManager.Core.Models;
using DbManager.Core.Services;
using System.Windows;

namespace DbManager.Wpf.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly SqlHistoryService _historyService;
    private readonly Action<string>? _useSqlAction;

    [ObservableProperty] private List<SqlHistoryModel> _histories = new();
    [ObservableProperty] private SqlHistoryModel? _selectedHistory;
    [ObservableProperty] private int _historyCount;

    public HistoryViewModel(SqlHistoryService historyService, Action<string>? useSqlAction = null)
    {
        _historyService = historyService;
        _useSqlAction = useSqlAction;
        _ = LoadHistoriesAsync();
    }

    private async Task LoadHistoriesAsync()
    {
        Histories = await _historyService.LoadHistoriesAsync(100);
        HistoryCount = Histories.Count;
    }

    [RelayCommand]
    private void UseSql()
    {
        if (SelectedHistory != null && _useSqlAction != null)
        {
            _useSqlAction(SelectedHistory.SqlText);
            CloseWindow();
        }
    }

    [RelayCommand]
    private void ClearAll()
    {
        _historyService.ClearAllHistories();
        Histories = new List<SqlHistoryModel>();
        HistoryCount = 0;
    }

    [RelayCommand]
    private void Close()
    {
        CloseWindow();
    }

    private void CloseWindow()
    {
        foreach (Window window in System.Windows.Application.Current.Windows)
        {
            if (window.DataContext == this)
            {
                window.DialogResult = true;
                window.Close();
                return;
            }
        }
    }
}