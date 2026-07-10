using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbManager.Common;
using DbManager.Core.Adapters;
using DbManager.Core.Models;
using DbManager.Core.Services;
using DbManager.Wpf.Helpers;

namespace DbManager.Wpf.ViewModels;

/// <summary>
/// MongoDB 集合文档浏览（只读）：按 JSON 过滤分页展示文档。
/// </summary>
public partial class MongoBrowserViewModel : ObservableObject
{
    private readonly DbConnectionModel _connection;
    private readonly string _database;
    private readonly string _collection;
    private readonly MongoService _mongo = new();

    [ObservableProperty] private string _header = "";
    [ObservableProperty] private string _iconKind = "FileDocumentOutline";
    [ObservableProperty] private ObservableCollection<string> _documents = new();
    [ObservableProperty] private string _filterJson = string.Empty;
    [ObservableProperty] private int _pageIndex = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private long _totalCount;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public long TotalPages => TotalCount > 0 ? (long)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public string PageInfo => TotalPages > 0 ? $"第 {PageIndex}/{TotalPages} 页（共 {TotalCount} 篇）" : "无文档";

    public MongoBrowserViewModel(DbConnectionModel connection, string database, string collection)
    {
        _connection = connection;
        _database = database;
        _collection = collection;
        Header = $"{collection} (Mongo)";
        StatusMessage = $"{connection.Name} - {database} - {collection}";
        _ = LoadAsync();
    }

    private string GetConnectionString() => DbConnStringBuilder.BuildDecryptedConnectionString(_connection);

    private async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = "加载中...";
        try
        {
            var skip = (PageIndex - 1) * PageSize;
            var (docs, total) = await _mongo.QueryAsync(GetConnectionString(), _database, _collection, FilterJson, skip, PageSize);
            Documents = new ObservableCollection<string>(docs);
            TotalCount = total;
            StatusMessage = $"共 {total} 篇文档";
        }
        catch (Exception ex)
        {
            Documents = new ObservableCollection<string>();
            StatusMessage = $"加载失败: {DbErrorTranslator.Translate(ex)}";
            MessageTipHelper.Error($"查询失败: {DbErrorTranslator.Translate(ex)}");
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(PageInfo));
        }
    }

    [RelayCommand]
    private async Task ApplyFilter()
    {
        PageIndex = 1;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ClearFilter()
    {
        FilterJson = string.Empty;
        PageIndex = 1;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    private static Window? Owner => System.Windows.Application.Current.MainWindow;

    /// <summary>
    /// 删除文档（按 _id 定位，删除前确认）。
    /// </summary>
    [RelayCommand]
    private async Task DeleteDocument(string? documentJson)
    {
        if (string.IsNullOrWhiteSpace(documentJson))
        {
            return;
        }
        if (!MessageTipHelper.Confirm("确定删除该文档？此操作不可恢复。"))
        {
            return;
        }

        try
        {
            await _mongo.DeleteDocumentAsync(GetConnectionString(), _database, _collection, documentJson);
            MessageTipHelper.Success("文档已删除");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageTipHelper.Error($"删除失败: {DbErrorTranslator.Translate(ex)}");
        }
    }

    /// <summary>
    /// 编辑文档（编辑 JSON 后按原 _id 替换）。
    /// </summary>
    [RelayCommand]
    private async Task EditDocument(string? documentJson)
    {
        if (string.IsNullOrWhiteSpace(documentJson))
        {
            return;
        }

        var edited = TextEditDialog.Show(Owner, "编辑文档", documentJson);
        if (edited == null)
        {
            return;
        }

        try
        {
            await _mongo.ReplaceDocumentAsync(GetConnectionString(), _database, _collection, documentJson, edited);
            MessageTipHelper.Success("文档已更新");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageTipHelper.Error($"更新失败: {DbErrorTranslator.Translate(ex)}");
        }
    }

    /// <summary>
    /// 新增文档。
    /// </summary>
    [RelayCommand]
    private async Task InsertDocument()
    {
        var json = TextEditDialog.Show(Owner, "新增文档", "{\n  \n}");
        if (json == null)
        {
            return;
        }

        try
        {
            await _mongo.InsertDocumentAsync(GetConnectionString(), _database, _collection, json);
            MessageTipHelper.Success("文档已插入");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageTipHelper.Error($"插入失败: {DbErrorTranslator.Translate(ex)}");
        }
    }

    /// <summary>
    /// 导出当前页文档为 JSON 数组文件。
    /// </summary>
    [RelayCommand]
    private void ExportJson()
    {
        if (Documents.Count == 0)
        {
            MessageTipHelper.Warning("当前无文档可导出");
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON文件 (*.json)|*.json",
            FileName = $"{_collection}.json"
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var content = "[\n" + string.Join(",\n", Documents) + "\n]";
            System.IO.File.WriteAllText(dialog.FileName, content);
            MessageTipHelper.Success($"已导出 {Documents.Count} 篇文档");
        }
        catch (Exception ex)
        {
            MessageTipHelper.Error($"导出失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PreviousPage()
    {
        if (PageIndex > 1)
        {
            PageIndex--;
            await LoadAsync();
        }
    }

    [RelayCommand]
    private async Task NextPage()
    {
        if (PageIndex < TotalPages)
        {
            PageIndex++;
            await LoadAsync();
        }
    }
}
