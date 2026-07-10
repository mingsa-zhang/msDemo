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
    [ObservableProperty] private string _ttlText = string.Empty;
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
            var info = await _redis.GetValueAsync(GetConnectionString(), Database, key);
            ValueType = info.Type;
            ValueText = info.Value;
            TtlText = info.Ttl;
        }
        catch (Exception ex)
        {
            ValueType = string.Empty;
            TtlText = string.Empty;
            ValueText = $"读取失败: {DbErrorTranslator.Translate(ex)}";
        }
    }

    private static System.Windows.Window? Owner => System.Windows.Application.Current.MainWindow;

    /// <summary>
    /// 删除当前选中键（删除前确认）。
    /// </summary>
    [RelayCommand]
    private async Task DeleteKey()
    {
        if (string.IsNullOrEmpty(SelectedKey))
        {
            MessageTipHelper.Warning("请先选择一个键");
            return;
        }
        if (!MessageTipHelper.Confirm($"确定删除键「{SelectedKey}」？此操作不可恢复。"))
        {
            return;
        }

        var key = SelectedKey;
        try
        {
            await _redis.DeleteKeyAsync(GetConnectionString(), Database, key);
            MessageTipHelper.Success("键已删除");
            ValueText = string.Empty;
            ValueType = string.Empty;
            TtlText = string.Empty;
            await LoadKeysAsync();
        }
        catch (Exception ex)
        {
            MessageTipHelper.Error($"删除失败: {DbErrorTranslator.Translate(ex)}");
        }
    }

    /// <summary>
    /// 编辑当前 String 键的值（仅 string 类型可编辑）。
    /// </summary>
    [RelayCommand]
    private async Task EditValue()
    {
        if (string.IsNullOrEmpty(SelectedKey))
        {
            MessageTipHelper.Warning("请先选择一个键");
            return;
        }
        if (!string.Equals(ValueType, "String", StringComparison.OrdinalIgnoreCase))
        {
            MessageTipHelper.Warning("仅支持编辑 String 类型的值");
            return;
        }

        var edited = TextEditDialog.Show(Owner, $"编辑值 - {SelectedKey}", ValueText, jsonFormat: false);
        if (edited == null)
        {
            return;
        }

        try
        {
            await _redis.SetStringAsync(GetConnectionString(), Database, SelectedKey, edited);
            MessageTipHelper.Success("值已更新");
            await LoadValueAsync(SelectedKey);
        }
        catch (Exception ex)
        {
            MessageTipHelper.Error($"更新失败: {DbErrorTranslator.Translate(ex)}");
        }
    }

    /// <summary>
    /// 设置/清除当前键的 TTL（输入秒数，留空或 0 表示持久化）。
    /// </summary>
    [RelayCommand]
    private async Task SetTtl()
    {
        if (string.IsNullOrEmpty(SelectedKey))
        {
            MessageTipHelper.Warning("请先选择一个键");
            return;
        }

        var input = InputDialog.Show(Owner, "设置 TTL", "过期秒数（输入 0 = 永久；取消不改）：");
        if (input == null)
        {
            return;
        }

        TimeSpan? ttl = int.TryParse(input, out var secs) && secs > 0 ? TimeSpan.FromSeconds(secs) : null;
        try
        {
            await _redis.SetTtlAsync(GetConnectionString(), Database, SelectedKey, ttl);
            MessageTipHelper.Success(ttl == null ? "已设为永久" : $"TTL 已设为 {secs}s");
            await LoadValueAsync(SelectedKey);
        }
        catch (Exception ex)
        {
            MessageTipHelper.Error($"设置失败: {DbErrorTranslator.Translate(ex)}");
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
