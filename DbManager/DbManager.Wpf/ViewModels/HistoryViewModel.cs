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

    private List<SqlHistoryModel> _allHistories = new();

    [ObservableProperty] private List<SqlHistoryModel> _histories = new();
    [ObservableProperty] private SqlHistoryModel? _selectedHistory;
    [ObservableProperty] private int _historyCount;
    [ObservableProperty] private string _searchText = string.Empty;

    public HistoryViewModel(SqlHistoryService historyService, Action<string>? useSqlAction = null)
    {
        _historyService = historyService;
        _useSqlAction = useSqlAction;
        _ = LoadHistoriesAsync();
    }

    private async Task LoadHistoriesAsync()
    {
        _allHistories = await _historyService.LoadHistoriesAsync(100);
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Histories = _allHistories;
        }
        else
        {
            var kw = SearchText.Trim();
            Histories = _allHistories.Where(h =>
                (h.SqlText?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false)
                || (h.ConnectionName?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false)
                || (h.DatabaseName?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
        }
        HistoryCount = Histories.Count;
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        if (SelectedHistory?.FileName == null) return;

        _historyService.DeleteHistory(SelectedHistory.FileName);
        _allHistories.Remove(SelectedHistory);
        ApplyFilter();
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
        _allHistories = new List<SqlHistoryModel>();
        Histories = _allHistories;
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