using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace DbManager.Wpf.Converters;

/// <summary>
/// 单元格 NULL 显示：DBNull/null 显示为 "(NULL)"，否则原样文本。
/// </summary>
public class NullCellDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value == null || value == System.DBNull.Value ? "(NULL)" : value.ToString() ?? string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// 数据库类型 → MaterialDesign 图标（用于连接编辑窗的可视化类型选择）。
/// </summary>
public class DbTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var kind = value switch
        {
            DbManager.Core.Enums.DbTypeEnum.MySql or DbManager.Core.Enums.DbTypeEnum.MariaDB => PackIconKind.Database,
            DbManager.Core.Enums.DbTypeEnum.SqlServer => PackIconKind.DatabaseOutline,
            DbManager.Core.Enums.DbTypeEnum.PostgreSQL => PackIconKind.DatabaseSearch,
            DbManager.Core.Enums.DbTypeEnum.Oracle => PackIconKind.DatabaseEye,
            DbManager.Core.Enums.DbTypeEnum.SQLite => PackIconKind.FileOutline,
            DbManager.Core.Enums.DbTypeEnum.MongoDB => PackIconKind.Leaf,
            DbManager.Core.Enums.DbTypeEnum.Redis => PackIconKind.LightningBolt,
            DbManager.Core.Enums.DbTypeEnum.DB2 => PackIconKind.DatabaseClock,
            _ => PackIconKind.Database
        };
        return kind;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// 自动提交状态文本：true → "自动提交"，false → "手动事务"。
/// </summary>
public class AutoCommitTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? "自动提交" : "手动事务";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// 值是否为 DBNull/null（供样式触发器判断 NULL 单元格）。
/// </summary>
public class IsDbNullConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value == null || value == System.DBNull.Value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility.Visible;
    }
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility.Collapsed;
    }
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class NullToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class StringToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string colorStr && !string.IsNullOrEmpty(colorStr))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(colorStr);
                return new SolidColorBrush(color);
            }
            catch
            {
                return new SolidColorBrush(Colors.Gray);
            }
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class StringToPackIconKindConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string kindStr && !string.IsNullOrEmpty(kindStr))
        {
            if (Enum.TryParse<PackIconKind>(kindStr, out var kind))
                return kind;
        }
        return PackIconKind.FileDocument;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b ? !b : true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b ? !b : false;
    }
}

public class BoolToEditModeTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? "编辑中" : "编辑";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}