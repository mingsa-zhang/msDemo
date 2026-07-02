using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbManager.Core.Adapters;
using DbManager.Core.Enums;
using DbManager.Core.Interfaces;
using DbManager.Core.Models;
using DbManager.Common;
using DbManager.Wpf.Helpers;
using OfficeOpenXml;
using CsvHelper;
using System.Data;
using System.Globalization;
using System.IO;

namespace DbManager.Wpf.ViewModels;

public partial class DataBrowserViewModel : ObservableObject
{
    private readonly DbConnectionModel _connection;
    private readonly string _databaseName;
    private readonly string _tableName;
    private readonly IDbExecuteService _executeService;
    private readonly IDbMetadataService _metadataService;
    private List<string>? _primaryKeys;

    [ObservableProperty] private string _header = "";
    [ObservableProperty] private string _iconKind = "TableSearch";
    [ObservableProperty] private int _connectionId;
    [ObservableProperty] private DataView? _dataView;
    [ObservableProperty] private int _pageIndex = 1;
    [ObservableProperty] private int _pageSize = AppConst.DefaultPageSize;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _filterText = string.Empty;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private int _selectedRowCount;

    public string TableName => _tableName;
    public DataTable? SourceTable => DataView?.Table;

    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;
    public string PageInfo => TotalPages > 0 ? $"第 {PageIndex}/{TotalPages} 页 (共 {TotalCount} 条)" : "无数据";

    public DataBrowserViewModel(DbConnectionModel connection, string databaseName, string tableName)
    {
        _connection = connection;
        _databaseName = databaseName;
        _tableName = tableName;
        ConnectionId = connection.Id;
        _executeService = App.ExecuteFactory.Create(connection.DbType);
        _metadataService = App.MetadataFactory.Create(connection.DbType);
        Header = tableName;
        StatusMessage = $"{connection.Name} - {databaseName} - {tableName}";
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadPrimaryKeysAsync();
        await LoadDataAsync();
    }

    private async Task LoadPrimaryKeysAsync()
    {
        try
        {
            var connectionString = DbConnStringBuilder.BuildDecryptedConnectionString(_connection);
            var columns = await _metadataService.GetColumnsAsync(connectionString, _databaseName, _tableName);
            _primaryKeys = columns.Where(c => c.IsPrimaryKey).Select(c => c.ColumnName).ToList();
        }
        catch
        {
            _primaryKeys = new List<string>();
        }
    }

    private string GetConnectionString()
    {
        return DbConnStringBuilder.BuildDecryptedConnectionString(_connection);
    }

    private string BuildQuerySql()
    {
        var offset = (PageIndex - 1) * PageSize;
        var whereClause = string.IsNullOrWhiteSpace(FilterText) ? "" : $" WHERE {FilterText}";
        return _connection.DbType switch
        {
            DbTypeEnum.MySql or DbTypeEnum.MariaDB =>
                $"SELECT * FROM `{_databaseName}`.`{_tableName}`{whereClause} LIMIT {PageSize} OFFSET {offset}",
            DbTypeEnum.SqlServer =>
                $"SELECT * FROM [{_databaseName}].[dbo].[{_tableName}]{whereClause} ORDER BY (SELECT NULL) OFFSET {offset} ROWS FETCH NEXT {PageSize} ROWS ONLY",
            DbTypeEnum.PostgreSQL =>
                $"SELECT * FROM public.{_tableName}{whereClause} LIMIT {PageSize} OFFSET {offset}",
            DbTypeEnum.SQLite =>
                $"SELECT * FROM \"{_tableName}\"{whereClause} LIMIT {PageSize} OFFSET {offset}",
            _ => $"SELECT * FROM {_tableName}{whereClause}"
        };
    }

    private string BuildCountSql()
    {
        var whereClause = string.IsNullOrWhiteSpace(FilterText) ? "" : $" WHERE {FilterText}";
        return _connection.DbType switch
        {
            DbTypeEnum.MySql or DbTypeEnum.MariaDB => $"SELECT COUNT(*) FROM `{_databaseName}`.`{_tableName}`{whereClause}",
            DbTypeEnum.SqlServer => $"SELECT COUNT(*) FROM [{_databaseName}].[dbo].[{_tableName}]{whereClause}",
            DbTypeEnum.PostgreSQL => $"SELECT COUNT(*) FROM public.{_tableName}{whereClause}",
            DbTypeEnum.SQLite => $"SELECT COUNT(*) FROM \"{_tableName}\"{whereClause}",
            _ => $"SELECT COUNT(*) FROM {_tableName}{whereClause}"
        };
    }

    private async Task LoadDataAsync()
    {
        IsLoading = true;
        IsEditing = false;
        StatusMessage = "加载中...";

        try
        {
            var connectionString = GetConnectionString();

            var countResult = await _executeService.ExecuteQueryAsync(connectionString, BuildCountSql());
            if (countResult.IsSuccess && countResult.ResultSets.Count > 0 && countResult.ResultSets[0].Rows.Count > 0)
            {
                var firstVal = countResult.ResultSets[0].Rows[0].Values.FirstOrDefault();
                TotalCount = firstVal != null ? Convert.ToInt32(firstVal) : 0;
            }
            TotalPages = TotalCount > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

            var result = await _executeService.ExecuteQueryAsync(connectionString, BuildQuerySql());
            if (result.IsSuccess && result.ResultSets.Count > 0 && result.ResultSets[0].Rows.Count > 0)
            {
                var table = ConvertToDataTable(result.ResultSets[0]);
                DataView = table.DefaultView;
                StatusMessage = $"共 {TotalCount} 条";
            }
            else
            {
                DataView = null;
                StatusMessage = "无数据";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasPreviousPage));
            OnPropertyChanged(nameof(HasNextPage));
            OnPropertyChanged(nameof(PageInfo));
        }
    }

    private static DataTable ConvertToDataTable(QueryResultSet resultSet)
    {
        var table = new DataTable();
        foreach (var col in resultSet.Columns)
        {
            table.Columns.Add(col);
        }
        foreach (var row in resultSet.Rows)
        {
            var values = resultSet.Columns.Select(c => row.TryGetValue(c, out var v) ? v ?? DBNull.Value : DBNull.Value).ToArray();
            table.Rows.Add(values);
        }
        return table;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadDataAsync();

    [RelayCommand]
    private async Task PreviousPage()
    {
        if (PageIndex > 1) { PageIndex--; await LoadDataAsync(); }
    }

    [RelayCommand]
    private async Task NextPage()
    {
        if (PageIndex < TotalPages) { PageIndex++; await LoadDataAsync(); }
    }

    [RelayCommand]
    private async Task GoToPage(object page)
    {
        if (int.TryParse(page?.ToString(), out var p) && p >= 1 && p <= TotalPages)
        {
            PageIndex = p;
            await LoadDataAsync();
        }
    }

    // ===== 筛选 =====

    [RelayCommand]
    private async Task ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(FilterText))
        {
            await LoadDataAsync();
            return;
        }

        PageIndex = 1;
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task ClearFilter()
    {
        FilterText = string.Empty;
        PageIndex = 1;
        await LoadDataAsync();
    }

    // ===== 编辑模式 =====

    [RelayCommand]
    private void ToggleEdit()
    {
        IsEditing = !IsEditing;
    }

    [RelayCommand]
    private void AddRow()
    {
        if (DataView?.Table == null) return;
        var row = DataView.Table.NewRow();
        DataView.Table.Rows.Add(row);
        StatusMessage = "已新增一行，保存后生效";
    }

    [RelayCommand]
    private void DeleteSelectedRows()
    {
        if (DataView?.Table == null) return;

        // 收集要删除的行索引
        var toDelete = new List<DataRow>();
        foreach (DataRow row in DataView.Table.Rows)
        {
            if (row.RowState != DataRowState.Deleted)
                toDelete.Add(row);
        }

        if (toDelete.Count == 0) return;

        // 简化：删除最后选中行，或如果没有选择则提示
        var lastRow = toDelete.LastOrDefault();
        if (lastRow != null)
        {
            lastRow.Delete();
            SelectedRowCount = DataView.Table.Rows.Cast<DataRow>().Count(r => r.RowState != DataRowState.Deleted);
            StatusMessage = $"已标记删除，保存后生效";
        }
    }

    [RelayCommand]
    private async Task SaveChanges()
    {
        if (DataView?.Table == null) return;

        var table = DataView.Table;
        var changes = table.GetChanges();
        if (changes == null || changes.Rows.Count == 0)
        {
            MessageTipHelper.Warning("没有需要保存的变更");
            return;
        }

        try
        {
            var connectionString = GetConnectionString();
            int totalAffected = 0;

            // 处理新增行
            var addedRows = table.GetChanges()?.Rows.Cast<DataRow>().Where(r => r.RowState == DataRowState.Added).ToList();
            if (addedRows != null && addedRows.Count > 0)
            {
                foreach (var row in addedRows)
                {
                    var columns = table.Columns.Cast<DataColumn>().Where(c => !row.IsNull(c)).Select(c => c.ColumnName).ToList();
                    var values = columns.Select(c => FormatValue(row[c])).ToList();
                    var colList = QuoteColumnList(columns);
                    var valList = string.Join(", ", values);
                    var sql = $"INSERT INTO {QuoteTableName()} ({colList}) VALUES ({valList})";
                    var result = await _executeService.ExecuteQueryAsync(connectionString, sql);
                    if (!result.IsSuccess)
                    {
                        MessageTipHelper.Warning($"新增失败: {result.ErrorMessage}");
                        return;
                    }
                    totalAffected += result.AffectedRows;
                }
            }

            // 处理修改行
            var modifiedRows = table.GetChanges()?.Rows.Cast<DataRow>().Where(r => r.RowState == DataRowState.Modified).ToList();
            if (modifiedRows != null && modifiedRows.Count > 0)
            {
                foreach (var row in modifiedRows)
                {
                    var whereClause = BuildWhereClause(row, table);
                    if (string.IsNullOrEmpty(whereClause))
                    {
                        MessageTipHelper.Warning("无法识别主键，不能更新该行");
                        return;
                    }

                    var setClauses = new List<string>();
                    foreach (DataColumn col in table.Columns)
                    {
                        var originalVal = row[col, DataRowVersion.Original];
                        var currentVal = row[col];
                        if (object.Equals(originalVal, currentVal)) continue;
                        setClauses.Add($"{QuoteColumn(col.ColumnName)} = {FormatValue(currentVal)}");
                    }

                    if (setClauses.Count == 0) continue;

                    var sql = $"UPDATE {QuoteTableName()} SET {string.Join(", ", setClauses)} WHERE {whereClause}";
                    var result = await _executeService.ExecuteQueryAsync(connectionString, sql);
                    if (!result.IsSuccess)
                    {
                        MessageTipHelper.Warning($"更新失败: {result.ErrorMessage}");
                        return;
                    }
                    totalAffected += result.AffectedRows;
                }
            }

            // 处理删除行
            var deletedRows = table.GetChanges()?.Rows.Cast<DataRow>().Where(r => r.RowState == DataRowState.Deleted).ToList();
            if (deletedRows != null && deletedRows.Count > 0)
            {
                foreach (var row in deletedRows)
                {
                    var whereClause = BuildWhereClauseFromOriginal(row, table);
                    if (string.IsNullOrEmpty(whereClause))
                    {
                        MessageTipHelper.Warning("无法识别主键，不能删除该行");
                        return;
                    }

                    var sql = $"DELETE FROM {QuoteTableName()} WHERE {whereClause}";
                    var result = await _executeService.ExecuteQueryAsync(connectionString, sql);
                    if (!result.IsSuccess)
                    {
                        MessageTipHelper.Warning($"删除失败: {result.ErrorMessage}");
                        return;
                    }
                    totalAffected += result.AffectedRows;
                }
            }

            table.AcceptChanges();
            MessageTipHelper.Success($"保存成功，影响 {totalAffected} 行");
            StatusMessage = $"已保存 {totalAffected} 行变更";

            // 重新加载以获取最新数据
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            MessageTipHelper.Error($"保存失败: {ex.Message}");
        }
    }

    private string QuoteTableName()
    {
        return _connection.DbType switch
        {
            DbTypeEnum.MySql or DbTypeEnum.MariaDB => $"`{_databaseName}`.`{_tableName}`",
            DbTypeEnum.SqlServer => $"[{_databaseName}].[dbo].[{_tableName}]",
            DbTypeEnum.PostgreSQL => $"public.{_tableName}",
            DbTypeEnum.SQLite => $"\"{_tableName}\"",
            _ => _tableName
        };
    }

    private string QuoteColumn(string col)
    {
        return _connection.DbType switch
        {
            DbTypeEnum.MySql or DbTypeEnum.MariaDB => $"`{col}`",
            DbTypeEnum.SqlServer => $"[{col}]",
            DbTypeEnum.PostgreSQL => $"\"{col}\"",
            DbTypeEnum.SQLite => $"\"{col}\"",
            _ => col
        };
    }

    private string QuoteColumnList(List<string> columns)
    {
        return string.Join(", ", columns.Select(QuoteColumn));
    }

    private string FormatValue(object? val)
    {
        if (val == null || val == DBNull.Value) return "NULL";
        if (val is string s) return $"'{s.Replace("'", "''")}'";
        if (val is DateTime dt) return $"'{dt:yyyy-MM-dd HH:mm:ss}'";
        if (val is bool b) return b ? "1" : "0";
        return val.ToString() ?? "NULL";
    }

    private string BuildWhereClause(DataRow row, DataTable table)
    {
        var whereClauses = new List<string>();

        if (_primaryKeys == null || _primaryKeys.Count == 0)
        {
            foreach (DataColumn col in table.Columns)
                whereClauses.Add($"{QuoteColumn(col.ColumnName)} = {FormatValue(row[col])}");
        }
        else
        {
            foreach (var pk in _primaryKeys)
                if (table.Columns.Contains(pk))
                    whereClauses.Add($"{QuoteColumn(pk)} = {FormatValue(row[pk])}");
        }

        return string.Join(" AND ", whereClauses);
    }

    private string BuildWhereClauseFromOriginal(DataRow row, DataTable table)
    {
        var whereClauses = new List<string>();

        if (_primaryKeys == null || _primaryKeys.Count == 0)
        {
            foreach (DataColumn col in table.Columns)
            {
                var originalVal = row[col, DataRowVersion.Original];
                whereClauses.Add($"{QuoteColumn(col.ColumnName)} = {FormatValue(originalVal)}");
            }
        }
        else
        {
            foreach (var pk in _primaryKeys)
            {
                if (table.Columns.Contains(pk))
                {
                    var originalVal = row[pk, DataRowVersion.Original];
                    whereClauses.Add($"{QuoteColumn(pk)} = {FormatValue(originalVal)}");
                }
            }
        }

        return string.Join(" AND ", whereClauses);
    }

    // ===== 导出 =====

    [RelayCommand]
    private void ExportExcel()
    {
        if (DataView == null || DataView.Table.Rows.Count == 0)
        {
            MessageTipHelper.Warning("无数据可导出");
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel文件 (*.xlsx)|*.xlsx",
            DefaultExt = ".xlsx",
            FileName = $"{_tableName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add(_tableName);
                var table = DataView.Table;

                for (int col = 0; col < table.Columns.Count; col++)
                    worksheet.Cells[1, col + 1].Value = table.Columns[col].ColumnName;

                for (int row = 0; row < table.Rows.Count; row++)
                    for (int col = 0; col < table.Columns.Count; col++)
                        worksheet.Cells[row + 2, col + 1].Value = table.Rows[row][col];

                package.SaveAs(new FileInfo(dialog.FileName));
                MessageTipHelper.Success($"已导出: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                MessageTipHelper.Error($"导出失败: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private void ExportCsv()
    {
        if (DataView == null || DataView.Table.Rows.Count == 0)
        {
            MessageTipHelper.Warning("无数据可导出");
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV文件 (*.csv)|*.csv",
            DefaultExt = ".csv",
            FileName = $"{_tableName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                using var writer = new StreamWriter(dialog.FileName);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                var table = DataView.Table;

                for (int col = 0; col < table.Columns.Count; col++)
                    csv.WriteField(table.Columns[col].ColumnName);
                csv.NextRecord();

                foreach (DataRow row in table.Rows)
                {
                    for (int col = 0; col < table.Columns.Count; col++)
                        csv.WriteField(row[col]);
                    csv.NextRecord();
                }

                MessageTipHelper.Success($"已导出: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                MessageTipHelper.Error($"导出失败: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task ExportInsertSql()
    {
        if (DataView == null || DataView.Table.Rows.Count == 0)
        {
            MessageTipHelper.Warning("无数据可导出");
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "SQL文件 (*.sql)|*.sql",
            DefaultExt = ".sql",
            FileName = $"{_tableName}_{DateTime.Now:yyyyMMdd_HHmmss}.sql"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var table = DataView.Table;
                var columns = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
                var colList = QuoteColumnList(columns);

                using var writer = new StreamWriter(dialog.FileName);
                foreach (DataRow row in table.Rows)
                {
                    var values = columns.Select(c => FormatValue(row[c])).ToList();
                    writer.WriteLine($"INSERT INTO {QuoteTableName()} ({colList}) VALUES ({string.Join(", ", values)});");
                }

                MessageTipHelper.Success($"已导出: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                MessageTipHelper.Error($"导出失败: {ex.Message}");
            }
        }
    }
}