using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbManager.Core.Models;
using DbManager.Core.Services;

namespace DbManager.Wpf.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private AppSettingsModel _settings;

    [ObservableProperty] private bool _useDarkTheme;
    [ObservableProperty] private int _sqlFontSizeIndex;
    [ObservableProperty] private int _pageSizeIndex;
    [ObservableProperty] private bool _autoRefreshResults;
    [ObservableProperty] private bool _showNullMarker;

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _settings = settingsService.Settings;
        LoadFromSettings();
    }

    private void LoadFromSettings()
    {
        UseDarkTheme = _settings.UseDarkTheme;
        SqlFontSizeIndex = _settings.SqlFontSize - 11; // 11对应index 0
        PageSizeIndex = GetPageSizeIndex(_settings.DefaultPageSize);
        AutoRefreshResults = _settings.AutoRefreshResults;
        ShowNullMarker = _settings.ShowNullMarker;
    }

    private static int GetPageSizeIndex(int pageSize)
    {
        return pageSize switch
        {
            50 => 0,
            100 => 1,
            200 => 2,
            500 => 3,
            1000 => 4,
            _ => 1
        };
    }

    private static int GetPageSizeFromIndex(int index)
    {
        return index switch
        {
            0 => 50,
            1 => 100,
            2 => 200,
            3 => 500,
            4 => 1000,
            _ => 50
        };
    }

    [RelayCommand]
    private void Save()
    {
        _settings.UseDarkTheme = UseDarkTheme;
        _settings.SqlFontSize = SqlFontSizeIndex + 11;
        _settings.DefaultPageSize = GetPageSizeFromIndex(PageSizeIndex);
        _settings.AutoRefreshResults = AutoRefreshResults;
        _settings.ShowNullMarker = ShowNullMarker;
        _settingsService.SaveSettings(_settings);

        App.ApplyTheme(UseDarkTheme);
        App.CurrentSettings = _settings;

        CloseWindow();
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow();
    }

    private void CloseWindow()
    {
        foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
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