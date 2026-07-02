using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbManager.Core.Adapters;
using DbManager.Core.Interfaces;
using DbManager.Core.Models;
using DbManager.Core.Services;
using DbManager.Wpf.Helpers;
using DbManager.Wpf.Views;
using System.Data;
using System.Diagnostics;

namespace DbManager.Wpf.ViewModels;

public partial class SqlQueryTabViewModel : ObservableObject
{
    private readonly DbConnectionModel _connection;
    private readonly string _databaseName;
    private readonly IDbExecuteService _executeService;
    private readonly SqlHistoryService _historyService;
    private static int _historyIdCounter = 0;
    private CancellationTokenSource? _cts;

    [ObservableProperty] private string _sqlText = string.Empty;
    [ObservableProperty] private string _selectedSql = string.Empty;
    [ObservableProperty] private bool _isExecuting;
    [ObservableProperty] private long _executionTimeMs;
    [ObservableProperty] private int _affectedRows;
    [ObservableProperty] private string _messageText = string.Empty;
    [ObservableProperty] private bool _isSuccess = true;
    [ObservableProperty] private DataView? _resultDataView;
    [ObservableProperty] private string _currentFilePath = string.Empty;

    public DbConnectionModel Connection => _connection;

    public SqlQueryTabViewModel(DbConnectionModel connection, string databaseName)
    {
        _connection = connection;
        _databaseName = databaseName;
        _executeService = App.ExecuteFactory.Create(connection.DbType);
        _historyService = new SqlHistoryService();
    }

    private string GetConnectionString()
    {
        return DbConnStringBuilder.BuildDecryptedConnectionString(_connection);
    }

    [RelayCommand]
    private async Task Execute()
    {
        if (string.IsNullOrWhiteSpace(SqlText)) return;
        await ExecuteSqlAsync(SqlText);
    }

    [RelayCommand]
    private async Task ExecuteSelected()
    {
        if (string.IsNullOrWhiteSpace(SelectedSql)) return;
        await ExecuteSqlAsync(SelectedSql);
    }

    private async Task ExecuteSqlAsync(string sql)
    {
        IsExecuting = true;
        _cts = new CancellationTokenSource();
        var stopwatch = Stopwatch.StartNew();
        var history = new SqlHistoryModel
        {
            Id = ++_historyIdCounter,
            ConnectionId = _connection.Id,
            ConnectionName = _connection.Name,
            DatabaseName = _databaseName,
            SqlText = sql,
            ExecuteTime = DateTime.Now
        };

        try
        {
            var connectionString = GetConnectionString();
            var result = await _executeService.ExecuteQueryAsync(connectionString, sql, cancellationToken: _cts.Token);
            stopwatch.Stop();
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            history.ExecutionTimeMs = ExecutionTimeMs;

            if (result.IsSuccess && result.ResultSets.Count > 0 && result.ResultSets[0].Rows.Count > 0)
            {
                var table = ConvertToDataTable(result.ResultSets[0]);
                ResultDataView = table.DefaultView;
                AffectedRows = result.ResultSets[0].Rows.Count;
                MessageText = $"查询完成，{AffectedRows} 行，耗时 {ExecutionTimeMs} ms";
                IsSuccess = true;
                history.IsSuccess = true;
                history.AffectedRows = AffectedRows;
            }
            else if (result.IsSuccess)
            {
                ResultDataView = null;
                AffectedRows = result.AffectedRows;
                MessageText = $"执行成功，影响 {AffectedRows} 行，耗时 {ExecutionTimeMs} ms";
                IsSuccess = true;
                history.IsSuccess = true;
                history.AffectedRows = AffectedRows;
            }
            else
            {
                ResultDataView = null;
                MessageText = $"错误: {result.ErrorMessage}";
                IsSuccess = false;
                history.IsSuccess = false;
            }
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            ResultDataView = null;
            MessageText = "查询已被取消";
            IsSuccess = true;
            history.IsSuccess = true;
            history.ExecutionTimeMs = ExecutionTimeMs;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            ResultDataView = null;
            MessageText = $"错误: {ex.Message}";
            IsSuccess = false;
            history.IsSuccess = false;
            history.ExecutionTimeMs = ExecutionTimeMs;
        }
        finally
        {
            IsExecuting = false;
            _cts = null;
        }

        // 保存历史记录
        await _historyService.SaveHistoryAsync(history);
    }

    [RelayCommand]
    private void StopExecution()
    {
        _cts?.Cancel();
    }

    [RelayCommand]
    private void Clear()
    {
        SqlText = string.Empty;
        ResultDataView = null;
        MessageText = string.Empty;
        AffectedRows = 0;
        ExecutionTimeMs = 0;
    }

    [RelayCommand]
    private void SaveScript()
    {
        if (string.IsNullOrWhiteSpace(SqlText))
        {
            MessageTipHelper.Warning("SQL内容为空");
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "SQL文件 (*.sql)|*.sql|所有文件 (*.*)|*.*",
            DefaultExt = ".sql",
            FileName = !string.IsNullOrEmpty(CurrentFilePath) ? System.IO.Path.GetFileName(CurrentFilePath) : "query.sql",
            InitialDirectory = !string.IsNullOrEmpty(CurrentFilePath) ? System.IO.Path.GetDirectoryName(CurrentFilePath) : DbManager.Common.AppConst.ScriptsDir
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                System.IO.File.WriteAllText(dialog.FileName, SqlText);
                CurrentFilePath = dialog.FileName;
                MessageTipHelper.Success($"已保存: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                MessageTipHelper.Error($"保存失败: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private void OpenScript()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "SQL文件 (*.sql)|*.sql|所有文件 (*.*)|*.*",
            DefaultExt = ".sql",
            InitialDirectory = !string.IsNullOrEmpty(CurrentFilePath) ? System.IO.Path.GetDirectoryName(CurrentFilePath) : DbManager.Common.AppConst.ScriptsDir
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                SqlText = System.IO.File.ReadAllText(dialog.FileName);
                CurrentFilePath = dialog.FileName;
                MessageText = $"已打开: {dialog.FileName}";
            }
            catch (Exception ex)
            {
                MessageTipHelper.Error($"打开失败: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private void ShowHistory()
    {
        var window = new HistoryWindow
        {
            DataContext = new HistoryViewModel(_historyService, sql => SqlText = sql),
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
    }

    [RelayCommand]
    private void FormatSql()
    {
        if (string.IsNullOrWhiteSpace(SqlText)) return;

        // 简单格式化：关键字大写
        var keywords = new[] { "SELECT", "FROM", "WHERE", "AND", "OR", "ORDER", "BY", "GROUP", "HAVING",
            "INSERT", "UPDATE", "DELETE", "INTO", "VALUES", "SET", "JOIN", "LEFT", "RIGHT", "INNER", "ON",
            "CREATE", "TABLE", "ALTER", "DROP", "INDEX", "PRIMARY", "KEY", "FOREIGN", "REFERENCES",
            "NOT", "NULL", "DEFAULT", "AUTO_INCREMENT", "IDENTITY", "LIKE", "IN", "BETWEEN", "IS",
            "AS", "DISTINCT", "COUNT", "SUM", "AVG", "MAX", "MIN", "CASE", "WHEN", "THEN", "ELSE", "END",
            "UNION", "ALL", "EXCEPT", "INTERSECT", "LIMIT", "OFFSET", "FETCH", "FIRST", "NEXT", "ROWS",
            "BEGIN", "COMMIT", "ROLLBACK", "TRANSACTION", "EXEC", "EXECUTE", "PROCEDURE", "FUNCTION" };

        var result = SqlText;
        foreach (var kw in keywords)
        {
            result = System.Text.RegularExpressions.Regex.Replace(
                result,
                $@"\b{kw.ToLowerInvariant()}\b",
                kw,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        SqlText = result;
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
}