using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbManager.Core.Abstractions;
using DbManager.Core.Enums;

namespace DbManager.Wpf.ViewModels;

/// <summary>
/// 可视化筛选构建器：把结构化条件（列/运算符/值 + AND·OR 连接）生成为 WHERE 片段。
/// 标识符经方言层引用，值做安全字面量转义（单引号翻倍、数值不加引号），替代手写 WHERE。
/// </summary>
public partial class FilterBuilderViewModel : ObservableObject
{
    private readonly IDialect _dialect;
    private readonly Func<string, LogicalTypeEnum> _typeResolver;

    /// <summary>
    /// 可选列清单
    /// </summary>
    public IReadOnlyList<string> Columns { get; }

    /// <summary>
    /// 运算符清单（所有行共用）
    /// </summary>
    public IReadOnlyList<FilterOperatorOption> Operators { get; } = FilterOperatorOption.All;

    /// <summary>
    /// 连接符清单
    /// </summary>
    public IReadOnlyList<string> Connectors { get; } = new[] { "AND", "OR" };

    [ObservableProperty] private ObservableCollection<FilterConditionRow> _rows = new();
    [ObservableProperty] private string _previewWhere = string.Empty;

    /// <summary>
    /// 确认后生成的 WHERE 片段（不含 WHERE 关键字）
    /// </summary>
    public string ResultWhere { get; private set; } = string.Empty;

    public FilterBuilderViewModel(IReadOnlyList<string> columns, IDialect dialect, Func<string, LogicalTypeEnum> typeResolver)
    {
        Columns = columns;
        _dialect = dialect;
        _typeResolver = typeResolver;
        AddRow();
    }

    [RelayCommand]
    private void AddRow()
    {
        var row = new FilterConditionRow
        {
            Column = Columns.Count > 0 ? Columns[0] : string.Empty,
            Operator = Operators[0],
            Connector = "AND"
        };
        row.PropertyChanged += (_, _) => UpdatePreview();
        Rows.Add(row);
        UpdatePreview();
    }

    [RelayCommand]
    private void RemoveRow(FilterConditionRow? row)
    {
        if (row != null)
        {
            Rows.Remove(row);
        }
        UpdatePreview();
    }

    private void UpdatePreview() => PreviewWhere = BuildWhere();

    /// <summary>
    /// 生成 WHERE 片段：逐行拼装并以各行连接符连接（首行连接符忽略）。
    /// </summary>
    public string BuildWhere()
    {
        var parts = new List<string>();
        var first = true;
        foreach (var row in Rows)
        {
            var clause = BuildClause(row);
            if (string.IsNullOrEmpty(clause))
            {
                continue;
            }

            if (!first)
            {
                parts.Add(row.Connector);
            }
            parts.Add(clause);
            first = false;
        }
        return string.Join(" ", parts);
    }

    private string BuildClause(FilterConditionRow row)
    {
        if (string.IsNullOrWhiteSpace(row.Column))
        {
            return string.Empty;
        }

        var col = _dialect.Quoter.Quote(row.Column);
        var op = row.Operator;
        var type = _typeResolver(row.Column);

        switch (op.Kind)
        {
            case FilterOperatorKind.IsNull:
                return $"{col} IS NULL";
            case FilterOperatorKind.IsNotNull:
                return $"{col} IS NOT NULL";
            case FilterOperatorKind.Contains:
                return $"{col} LIKE {QuoteLiteral($"%{row.Value}%")}";
            case FilterOperatorKind.NotContains:
                return $"{col} NOT LIKE {QuoteLiteral($"%{row.Value}%")}";
            case FilterOperatorKind.StartsWith:
                return $"{col} LIKE {QuoteLiteral($"{row.Value}%")}";
            case FilterOperatorKind.In:
                var items = (row.Value ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(v => FormatLiteral(v, type));
                var joined = string.Join(", ", items);
                return string.IsNullOrEmpty(joined) ? string.Empty : $"{col} IN ({joined})";
            default:
                // 比较运算符：= <> > >= < <=
                return $"{col} {op.Sql} {FormatLiteral(row.Value ?? string.Empty, type)}";
        }
    }

    /// <summary>
    /// 依逻辑类型格式化值字面量：数值/布尔按原义，其余加引号并转义单引号。
    /// </summary>
    private static string FormatLiteral(string value, LogicalTypeEnum type)
    {
        switch (type)
        {
            case LogicalTypeEnum.Number:
                return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _)
                    ? value
                    : QuoteLiteral(value);
            case LogicalTypeEnum.Boolean:
                var b = value.Trim().ToLowerInvariant();
                return b is "true" or "false" or "1" or "0" ? b : QuoteLiteral(value);
            default:
                return QuoteLiteral(value);
        }
    }

    private static string QuoteLiteral(string value) => $"'{value.Replace("'", "''")}'";

    /// <summary>
    /// 确认：固化生成结果。
    /// </summary>
    public void Confirm() => ResultWhere = BuildWhere();
}

/// <summary>
/// 单条筛选条件行。
/// </summary>
public partial class FilterConditionRow : ObservableObject
{
    [ObservableProperty] private string _connector = "AND";
    [ObservableProperty] private string _column = string.Empty;
    [ObservableProperty] private FilterOperatorOption _operator = FilterOperatorOption.All[0];
    [ObservableProperty] private string _value = string.Empty;
}

/// <summary>
/// 筛选运算符类别。
/// </summary>
public enum FilterOperatorKind
{
    Compare,
    Contains,
    NotContains,
    StartsWith,
    In,
    IsNull,
    IsNotNull
}

/// <summary>
/// 运算符选项：显示名 + SQL 片段 + 类别。
/// </summary>
public sealed class FilterOperatorOption
{
    /// <summary>
    /// 显示名
    /// </summary>
    public string Display { get; init; } = string.Empty;

    /// <summary>
    /// 比较类运算符的 SQL 符号（非比较类忽略）
    /// </summary>
    public string Sql { get; init; } = string.Empty;

    /// <summary>
    /// 类别
    /// </summary>
    public FilterOperatorKind Kind { get; init; }

    /// <summary>
    /// 全部可选运算符
    /// </summary>
    public static IReadOnlyList<FilterOperatorOption> All { get; } = new[]
    {
        new FilterOperatorOption { Display = "等于 =", Sql = "=", Kind = FilterOperatorKind.Compare },
        new FilterOperatorOption { Display = "不等于 <>", Sql = "<>", Kind = FilterOperatorKind.Compare },
        new FilterOperatorOption { Display = "大于 >", Sql = ">", Kind = FilterOperatorKind.Compare },
        new FilterOperatorOption { Display = "大于等于 >=", Sql = ">=", Kind = FilterOperatorKind.Compare },
        new FilterOperatorOption { Display = "小于 <", Sql = "<", Kind = FilterOperatorKind.Compare },
        new FilterOperatorOption { Display = "小于等于 <=", Sql = "<=", Kind = FilterOperatorKind.Compare },
        new FilterOperatorOption { Display = "包含", Kind = FilterOperatorKind.Contains },
        new FilterOperatorOption { Display = "不包含", Kind = FilterOperatorKind.NotContains },
        new FilterOperatorOption { Display = "开头是", Kind = FilterOperatorKind.StartsWith },
        new FilterOperatorOption { Display = "在列表(逗号分隔)", Kind = FilterOperatorKind.In },
        new FilterOperatorOption { Display = "为空", Kind = FilterOperatorKind.IsNull },
        new FilterOperatorOption { Display = "不为空", Kind = FilterOperatorKind.IsNotNull }
    };
}
