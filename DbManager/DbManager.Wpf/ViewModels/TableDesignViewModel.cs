using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbManager.Common;
using DbManager.Core.Abstractions;
using DbManager.Core.Adapters;
using DbManager.Core.Enums;
using DbManager.Core.Interfaces;
using DbManager.Core.Models;
using DbManager.Core.Services;

namespace DbManager.Wpf.ViewModels;

public partial class TableDesignViewModel : ObservableObject
{
    private readonly DbConnectionModel _connection;
    private readonly string _databaseName;
    private string _tableName;
    private readonly string? _schema;
    private readonly IDbMetadataService _metadataService;
    private readonly IDbExecuteService _executeService;
    private readonly IDialect _dialect;
    private readonly Action? _onSaved;
    private List<TableColumnModel> _originalColumns = new();

    [ObservableProperty] private string _header = "";
    [ObservableProperty] private string _iconKind = "TableEdit";
    [ObservableProperty] private int _connectionId;
    [ObservableProperty] private string _databaseName2 = "";
    [ObservableProperty] private string _tableName2 = "";
    [ObservableProperty] private bool _isCreateMode;

    [ObservableProperty] private List<TableColumnModel> _columns = new();
    [ObservableProperty] private TableColumnModel? _selectedColumn;
    [ObservableProperty] private string _previewSql = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasChanges;

    public string TableName => _tableName;

    public TableDesignViewModel(DbConnectionModel connection, string databaseName, string tableName, string? schema = null)
    {
        _connection = connection;
        _databaseName = databaseName;
        _tableName = tableName;
        _schema = schema;
        ConnectionId = connection.Id;
        DatabaseName2 = databaseName;
        TableName2 = tableName;
        _metadataService = App.MetadataFactory.Create(connection.DbType);
        _executeService = App.ExecuteFactory.Create(connection.DbType);
        _dialect = DialectProvider.GetDialect(connection.DbType);
        Header = $"{tableName} (设计)";
        _ = LoadColumnsAsync();
    }

    /// <summary>
    /// 新建表模式：表名可编辑，保存生成并执行 CREATE TABLE。
    /// </summary>
    public TableDesignViewModel(DbConnectionModel connection, string databaseName, string? schema, bool isCreateMode, Action? onSaved = null)
    {
        _connection = connection;
        _databaseName = databaseName;
        _tableName = string.Empty;
        _schema = schema;
        _onSaved = onSaved;
        IsCreateMode = isCreateMode;
        ConnectionId = connection.Id;
        DatabaseName2 = databaseName;
        TableName2 = string.Empty;
        _metadataService = App.MetadataFactory.Create(connection.DbType);
        _executeService = App.ExecuteFactory.Create(connection.DbType);
        _dialect = DialectProvider.GetDialect(connection.DbType);
        Header = "新建表";

        // 预置一个 id 主键自增列，便于直接使用
        Columns = new List<TableColumnModel>
        {
            new()
            {
                ColumnName = "id",
                DataType = "INT",
                IsNullable = false,
                IsPrimaryKey = true,
                IsAutoIncrement = true,
                IsNew = true
            }
        };
        SelectedColumn = Columns[0];
    }

    private string GetConnectionString()
    {
        return DbConnStringBuilder.BuildDecryptedConnectionString(_connection);
    }

    private async Task LoadColumnsAsync()
    {
        IsLoading = true;
        try
        {
            var connectionString = GetConnectionString();
            Columns = await _metadataService.GetColumnsAsync(connectionString, _databaseName, _tableName, _schema);
            _originalColumns = Columns.Select(c => c.Clone()).ToList();
            HasChanges = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void AddColumn()
    {
        var newCol = new TableColumnModel
        {
            ColumnName = $"new_col_{Columns.Count + 1}",
            DataType = "VARCHAR",
            MaxLength = 255,
            IsNullable = true,
            IsNew = true
        };
        Columns = new List<TableColumnModel>(Columns) { newCol };
        SelectedColumn = newCol;
        HasChanges = true;
    }

    [RelayCommand]
    private void DeleteColumn()
    {
        if (SelectedColumn == null)
        {
            Helpers.MessageTipHelper.Warning("请先选择要删除的字段");
            return;
        }

        if (SelectedColumn.IsNew)
        {
            Columns = Columns.Where(c => c != SelectedColumn).ToList();
            SelectedColumn = null;
            HasChanges = true;
            return;
        }

        SelectedColumn.IsDeleted = true;
        Columns = Columns.ToList(); // 触发刷新
        SelectedColumn = null;
        HasChanges = true;
    }

    [RelayCommand]
    private async Task GeneratePreviewSql()
    {
        // 新建模式：本地按方言层生成 CREATE TABLE 预览
        if (IsCreateMode)
        {
            PreviewSql = TryBuildCreateTableSql(out var error) ?? $"-- {error}";
            return;
        }

        try
        {
            var connectionString = GetConnectionString();
            PreviewSql = await _metadataService.GetCreateTableSqlAsync(connectionString, _databaseName, _tableName, _schema);
        }
        catch (Exception ex)
        {
            PreviewSql = $"-- 获取建表SQL失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (IsCreateMode)
        {
            await CreateTableAsync();
            return;
        }

        var alterSqls = GenerateAlterTableSql();
        if (alterSqls.Count == 0)
        {
            Helpers.MessageTipHelper.Warning("没有需要保存的变更");
            return;
        }

        PreviewSql = string.Join("\n\n", alterSqls);

        var result = System.Windows.MessageBox.Show(
            $"将执行以下 {alterSqls.Count} 条ALTER语句：\n\n{PreviewSql}\n\n确定执行？",
            "确认保存", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            var connectionString = GetConnectionString();
            // 跳过注释行（如 SQLite 不支持修改列的提示）
            var executable = alterSqls.Where(s => !s.TrimStart().StartsWith("--")).ToList();
            if (executable.Count == 0)
            {
                Helpers.MessageTipHelper.Warning("当前数据库不支持所请求的修改（详见预览注释）");
                return;
            }

            foreach (var sql in executable)
            {
                var execResult = await _executeService.ExecuteNonQueryAsync(connectionString, sql);
                if (!execResult.IsSuccess)
                {
                    Helpers.MessageTipHelper.Error($"执行失败: {DbErrorTranslator.Translate(execResult.ErrorMessage)}\nSQL: {sql}");
                    return;
                }
            }

            Helpers.MessageTipHelper.Success("表结构修改成功");
            await LoadColumnsAsync();
        }
        catch (Exception ex)
        {
            Helpers.MessageTipHelper.Error($"保存失败: {DbErrorTranslator.Translate(ex)}");
        }
    }

    /// <summary>
    /// 新建表：生成 CREATE TABLE 并执行，成功后切换到编辑模式并回调刷新树。
    /// </summary>
    private async Task CreateTableAsync()
    {
        var sql = TryBuildCreateTableSql(out var error);
        if (sql == null)
        {
            Helpers.MessageTipHelper.Warning(error);
            return;
        }

        PreviewSql = sql;

        var confirm = System.Windows.MessageBox.Show(
            $"将执行以下建表语句：\n\n{sql}\n\n确定执行？",
            "确认新建表", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
        if (confirm != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var connectionString = GetConnectionString();
            var execResult = await _executeService.ExecuteNonQueryAsync(connectionString, sql);
            if (!execResult.IsSuccess)
            {
                Helpers.MessageTipHelper.Error($"建表失败: {DbErrorTranslator.Translate(execResult.ErrorMessage)}");
                return;
            }

            Helpers.MessageTipHelper.Success("表创建成功");

            // 切换为编辑模式：后续继续改结构走 ALTER
            _tableName = TableName2.Trim();
            IsCreateMode = false;
            Header = $"{_tableName} (设计)";
            _onSaved?.Invoke();
            await LoadColumnsAsync();
        }
        catch (Exception ex)
        {
            Helpers.MessageTipHelper.Error($"建表失败: {DbErrorTranslator.Translate(ex)}");
        }
    }

    /// <summary>
    /// 按方言层拼装 CREATE TABLE；校验失败返回 null 并输出错误原因。
    /// </summary>
    private string? TryBuildCreateTableSql(out string error)
    {
        error = string.Empty;
        var tableName = TableName2.Trim();
        if (string.IsNullOrEmpty(tableName))
        {
            error = "请先输入表名";
            return null;
        }

        var effectiveColumns = Columns.Where(c => !c.IsDeleted && !string.IsNullOrWhiteSpace(c.ColumnName)).ToList();
        if (effectiveColumns.Count == 0)
        {
            error = "请至少定义一个字段";
            return null;
        }

        var specs = effectiveColumns.Select(c => new CreateTableColumnSpec
        {
            QuotedColumn = QuoteColumn(c.ColumnName),
            TypeString = BuildTypeString(c),
            IsNullable = c.IsNullable,
            IsPrimaryKey = c.IsPrimaryKey,
            IsAutoIncrement = c.IsAutoIncrement,
            DefaultLiteral = c.DefaultValue == null ? null : FormatDefault(c.DefaultValue)
        }).ToList();

        var qualifiedTable = _dialect.QualifyTable(_databaseName, _schema, tableName);
        return _dialect.BuildCreateTable(qualifiedTable, specs);
    }

    private List<string> GenerateAlterTableSql()
    {
        var sqls = new List<string>();
        var quotedTable = QuoteTableName();

        // 新增列（各库 ADD 语法由方言层处理）
        foreach (var col in Columns.Where(c => c.IsNew && !c.IsDeleted))
        {
            sqls.Add(_dialect.BuildAddColumn(quotedTable, QuoteColumn(col.ColumnName), BuildColumnDefinition(col)));
        }

        // 删除列
        foreach (var col in Columns.Where(c => c.IsDeleted && !c.IsNew))
        {
            sqls.Add(_dialect.BuildDropColumn(quotedTable, QuoteColumn(col.ColumnName)));
        }

        // 修改列（各库 ALTER/MODIFY 语法差异由方言层处理）
        foreach (var col in Columns.Where(c => !c.IsNew && !c.IsDeleted))
        {
            var original = _originalColumns.FirstOrDefault(o => o.ColumnName == col.ColumnName);
            if (original == null) continue;

            var spec = new ColumnAlterSpec
            {
                QuotedColumn = QuoteColumn(col.ColumnName),
                NewTypeString = BuildTypeString(col),
                IsNullable = col.IsNullable,
                DefaultLiteral = col.DefaultValue == null ? null : FormatDefault(col.DefaultValue),
                FullDefinition = BuildColumnDefinition(col),
                TypeChanged = original.DataType != col.DataType || original.MaxLength != col.MaxLength,
                NullabilityChanged = original.IsNullable != col.IsNullable,
                DefaultChanged = original.DefaultValue != col.DefaultValue
            };

            if (spec.HasAnyChange)
            {
                sqls.AddRange(_dialect.BuildAlterColumn(quotedTable, spec));
            }
        }

        return sqls;
    }

    private string BuildColumnDefinition(TableColumnModel col)
    {
        var parts = new List<string> { BuildTypeString(col) };
        if (!col.IsNullable) parts.Add("NOT NULL");
        if (col.IsAutoIncrement) parts.Add(GetAutoIncrementSyntax());
        if (col.DefaultValue != null) parts.Add($"DEFAULT {FormatDefault(col.DefaultValue)}");
        return string.Join(" ", parts);
    }

    private string BuildTypeString(TableColumnModel col)
    {
        if (col.MaxLength > 0 && NeedsLength(col.DataType))
            return $"{col.DataType}({col.MaxLength})";
        return col.DataType;
    }

    private static bool NeedsLength(string dataType)
    {
        var typesNeedingLength = new[] { "VARCHAR", "CHAR", "NVARCHAR", "NCHAR", "DECIMAL", "NUMERIC" };
        return typesNeedingLength.Any(t => dataType.StartsWith(t, StringComparison.OrdinalIgnoreCase));
    }

    private string GetAutoIncrementSyntax() => _dialect.AutoIncrementKeyword();

    private static string FormatDefault(string? defaultValue)
    {
        if (string.IsNullOrEmpty(defaultValue)) return "NULL";
        if (defaultValue.StartsWith("'") && defaultValue.EndsWith("'")) return defaultValue;
        return $"'{defaultValue}'";
    }

    // 走方言层：库/schema/表统一限定并加引号（schema 感知，杜绝硬编码 public/dbo）
    private string QuoteTableName() => _dialect.QualifyTable(_databaseName, _schema, _tableName);

    private string QuoteColumn(string col) => _dialect.Quoter.Quote(col);

}