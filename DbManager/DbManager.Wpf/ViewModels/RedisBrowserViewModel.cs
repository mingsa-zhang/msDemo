using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbManager.Common;
using DbManager.Core.Adapters;
using DbManager.Core.Models;
using DbManager.Core.Services;
using DbManager.Wpf.Helpers;

namespace DbManager.Wpf.ViewModels;

/// <summary>
/// Redis 键浏览（只读）：按模式扫描键，点击键按类型展示值。
/// </summary>
public partial class RedisBrowserViewModel : ObservableObject
{
    private readonly DbConnectionModel _connection;
    private readonly RedisService _redis = new();

    [ObservableProperty] private string _header = "";
    [ObservableProperty] private string _iconKind = "LightningBolt";
    [ObservableProperty] private int _database;
    [ObservableProperty] private string _pattern = "*";
    [ObservableProperty] private int _keyLimit = 500;
    [ObservableProperty] private ObservableCollection<string> _keys = new();
    [ObservableProperty] private string? _selectedKey;
    [ObservableProperty] private string _valueType = string.Empty;
    [ObservableProperty] private string _valueText = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public ObservableCollection<int> DatabaseOptions { get; } = new(Enumerable.Range(0, 16));

    public RedisBrowserViewModel(DbConnectionModel connection)
    {
        _connection = connection;
        Header = $"{connection.Name} (Redis)";
        StatusMessage = connection.Name;
        _ = LoadKeysAsync();
    }

    private string GetConnectionString() => DbConnStringBuilder.BuildDecryptedConnectionString(_connection);

    private async Task LoadKeysAsync()
    {
        IsLoading = true;
        StatusMessage = "扫描键中...";
        try
        {
            var keys = await _redis.ListKeysAsync(GetConnectionString(), Database, Pattern, KeyLimit);
            Keys = new ObservableCollection<string>(keys);
            StatusMessage = keys.Count >= KeyLimit ? $"已加载前 {KeyLimit} 个键（可能更多）" : $"共 {keys.Count} 个键";
        }
        catch (Exception ex)
        {
            Keys = new ObservableCollection<string>();
            StatusMessage = $"加载失败: {DbErrorTranslator.Translate(ex)}";
            MessageTipHelper.Error($"扫描失败: {DbErrorTranslator.Translate(ex)}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedKeyChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _ = LoadValueAsync(value);
        }
    }

    private async Task LoadValueAsync(string key)
    {
        try
        {
            var (type, text) = await _redis.GetValueAsync(GetConnectionString(), Database, key);
            ValueType = type;
            ValueText = text;
        }
        catch (Exception ex)
        {
            ValueType = string.Empty;
            ValueText = $"读取失败: {DbErrorTranslator.Translate(ex)}";
        }
    }

    [RelayCommand]
    private async Task Search()
    {
        await LoadKeysAsync();
    }

    [RelayCommand]
    private async Task Refresh() => await LoadKeysAsync();

    partial void OnDatabaseChanged(int value)
    {
        _ = LoadKeysAsync();
    }
}
