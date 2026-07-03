using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbManager.Core.Adapters;
using DbManager.Core.Interfaces;
using DbManager.Core.Models;
using DbManager.Core.Services;
using DbManager.Wpf.Helpers;
using DbManager.Wpf.Views;
using System.Collections.ObjectModel;
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
    [ObservableProperty] private ObservableCollection<QueryResultTab> _resultTabs = new();
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

    /// <summary>
    /// 对单条裸 SELECT 应用"最大返回行数"限制（设置 &gt; 0 时）。
    /// 仅当：以 SELECT 开头、单条语句、未含 LIMIT/TOP/FETCH/ROWNUM 时才改写，避免破坏复杂语句。
    /// </summary>
    private string ApplyRowLimit(string sql)
    {
        var max = App.CurrentSettings?.MaxQueryRows ?? 0;
        if (max <= 0)
        {
            return sql;
        }

        var trimmed = sql.Trim().TrimEnd(';').Trim();
        if (trimmed.Contains(';'))
        {
            return sql; // 多语句不处理
        }
        if (!trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            return sql;
        }
        if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"\b(LIMIT|TOP|FETCH|ROWNUM)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            return sql;
        }

        try
        {
            return DialectProvider.GetDialect(_connection.DbType).Paginate(trimmed, 1, max);
        }
        catch
        {
            return sql;
        }
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
            var result = await _executeService.ExecuteQueryAsync(connectionString, ApplyRowLimit(sql), cancellationToken: _cts.Token);
            stopwatch.Stop();
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            history.ExecutionTimeMs = ExecutionTimeMs;

            if (result.IsSuccess)
            {
                // 仅含列的结果集才作为数据标签展示；无列的为非查询语句（增删改）。
                var dataSets = result.ResultSets.Where(rs => rs.Columns.Count > 0).ToList();
                ResultTabs = new ObservableCollection<QueryResultTab>(
                    dataSets.Select((rs, i) => new QueryResultTab
                    {
                        Header = dataSets.Count > 1 ? $"结果 {i + 1}" : "结果",
                        DataView = ConvertToDataTable(rs).DefaultView,
                        RowCount = rs.Rows.Count
                    }));
                // 兼容旧的单结果集绑定
                ResultDataView = ResultTabs.Count > 0 ? ResultTabs[0].DataView : null;

                if (dataSets.Count > 0)
                {
                    AffectedRows = dataSets.Sum(d => d.Rows.Count);
                    MessageText = dataSets.Count > 1
                        ? $"查询完成，{dataSets.Count} 个结果集，共 {AffectedRows} 行，耗时 {ExecutionTimeMs} ms"
                        : $"查询完成，{AffectedRows} 行，耗时 {ExecutionTimeMs} ms";
                }
                else
                {
                    AffectedRows = result.AffectedRows;
                    MessageText = $"执行成功，影响 {AffectedRows} 行，耗时 {ExecutionTimeMs} ms";
                }
                IsSuccess = true;
                history.IsSuccess = true;
                history.AffectedRows = AffectedRows;
            }
            else
            {
                ResultTabs = new ObservableCollection<QueryResultTab>();
                ResultDataView = null;
                MessageText = $"错误: {DbManager.Common.DbErrorTranslator.Translate(result.ErrorMessage)}";
                IsSuccess = false;
                history.IsSuccess = false;
            }
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            ResultTabs = new ObservableCollection<QueryResultTab>();
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
            ResultTabs = new ObservableCollection<QueryResultTab>();
            ResultDataView = null;
            MessageText = $"错误: {DbManager.Common.DbErrorTranslator.Translate(ex)}";
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
        ResultTabs = new ObservableCollection<QueryResultTab>();
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

        SqlText = SqlFormatter.Format(SqlText);
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

/// <summary>
/// 单个结果集标签（多语句执行时每个查询结果一个）。
/// </summary>
public class QueryResultTab
{
    /// <summary>
    /// 标签标题
    /// </summary>
    public string Header { get; set; } = "结果";

    /// <summary>
    /// 结果数据视图
    /// </summary>
    public DataView? DataView { get; set; }

    /// <summary>
    /// 行数
    /// </summary>
    public int RowCount { get; set; }
}