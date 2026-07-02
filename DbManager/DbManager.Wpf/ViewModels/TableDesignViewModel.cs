using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbManager.Common;
using DbManager.Core.Adapters;
using DbManager.Core.Enums;
using DbManager.Core.Interfaces;
using DbManager.Core.Models;

namespace DbManager.Wpf.ViewModels;

public partial class TableDesignViewModel : ObservableObject
{
    private readonly DbConnectionModel _connection;
    private readonly string _databaseName;
    private readonly string _tableName;
    private readonly IDbMetadataService _metadataService;
    private readonly IDbExecuteService _executeService;
    private List<TableColumnModel> _originalColumns = new();

    [ObservableProperty] private string _header = "";
    [ObservableProperty] private string _iconKind = "TableEdit";
    [ObservableProperty] private int _connectionId;
    [ObservableProperty] private string _databaseName2 = "";
    [ObservableProperty] private string _tableName2 = "";

    [ObservableProperty] private List<TableColumnModel> _columns = new();
    [ObservableProperty] private TableColumnModel? _selectedColumn;
    [ObservableProperty] private string _previewSql = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasChanges;

    public string TableName => _tableName;

    public TableDesignViewModel(DbConnectionModel connection, string databaseName, string tableName)
    {
        _connection = connection;
        _databaseName = databaseName;
        _tableName = tableName;
        ConnectionId = connection.Id;
        DatabaseName2 = databaseName;
        TableName2 = tableName;
        _metadataService = App.MetadataFactory.Create(connection.DbType);
        _executeService = App.ExecuteFactory.Create(connection.DbType);
        Header = $"{tableName} (设计)";
        _ = LoadColumnsAsync();
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
            Columns = await _metadataService.GetColumnsAsync(connectionString, _databaseName, _tableName);
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
        try
        {
            var connectionString = GetConnectionString();
            PreviewSql = await _metadataService.GetCreateTableSqlAsync(connectionString, _databaseName, _tableName);
        }
        catch (Exception ex)
        {
            PreviewSql = $"-- 获取建表SQL失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task Save()
    {
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
            foreach (var sql in alterSqls)
            {
                var execResult = await _executeService.ExecuteQueryAsync(connectionString, sql);
                if (!execResult.IsSuccess)
                {
                    Helpers.MessageTipHelper.Error($"执行失败: {execResult.ErrorMessage}\nSQL: {sql}");
                    return;
                }
            }

            Helpers.MessageTipHelper.Success("表结构修改成功");
            await LoadColumnsAsync();
        }
        catch (Exception ex)
        {
            Helpers.MessageTipHelper.Error($"保存失败: {ex.Message}");
        }
    }

    private List<string> GenerateAlterTableSql()
    {
        var sqls = new List<string>();
        var quotedTable = QuoteTableName();

        // 新增列
        foreach (var col in Columns.Where(c => c.IsNew && !c.IsDeleted))
        {
            var colDef = BuildColumnDefinition(col);
            sqls.Add($"ALTER TABLE {quotedTable} ADD COLUMN {QuoteColumn(col.ColumnName)} {colDef}");
        }

        // 删除列
        foreach (var col in Columns.Where(c => c.IsDeleted && !c.IsNew))
        {
            sqls.Add($"ALTER TABLE {quotedTable} DROP COLUMN {QuoteColumn(col.ColumnName)}");
        }

        // 修改列
        foreach (var col in Columns.Where(c => !c.IsNew && !c.IsDeleted))
        {
            var original = _originalColumns.FirstOrDefault(o => o.ColumnName == col.ColumnName);
            if (original == null) continue;

            var changes = new List<string>();
            if (original.DataType != col.DataType || original.MaxLength != col.MaxLength)
                changes.Add($"TYPE {BuildTypeString(col)}");
            if (original.IsNullable != col.IsNullable)
                changes.Add(col.IsNullable ? "DROP NOT NULL" : "SET NOT NULL");
            if (original.DefaultValue != col.DefaultValue)
                changes.Add(col.DefaultValue == null ? "DROP DEFAULT" : $"SET DEFAULT {FormatDefault(col.DefaultValue)}");

            if (changes.Count > 0)
            {
                // 不同数据库语法不同，简化为通用 ALTER COLUMN
                var alterClauses = changes.Select(c => $"ALTER COLUMN {QuoteColumn(col.ColumnName)} {c}");
                sqls.Add($"ALTER TABLE {quotedTable} {string.Join(", ", alterClauses)}");
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

    private string GetAutoIncrementSyntax()
    {
        return _connection.DbType switch
        {
            DbTypeEnum.MySql or DbTypeEnum.MariaDB => "AUTO_INCREMENT",
            DbTypeEnum.SqlServer => "IDENTITY(1,1)",
            DbTypeEnum.SQLite => "AUTOINCREMENT",
            _ => "AUTO_INCREMENT"
        };
    }

    private static string FormatDefault(string? defaultValue)
    {
        if (string.IsNullOrEmpty(defaultValue)) return "NULL";
        if (defaultValue.StartsWith("'") && defaultValue.EndsWith("'")) return defaultValue;
        return $"'{defaultValue}'";
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
            DbTypeEnum.PostgreSQL or DbTypeEnum.SQLite => $"\"{col}\"",
            _ => col
        };
    }
}