using System.Windows;

namespace DbManager.Wpf.Helpers;

public static class MessageTipHelper
{
    public static void Success(string message)
    {
        MessageBox.Show(message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public static void Error(string message)
    {
        MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public static void Warning(string message)
    {
        MessageBox.Show(message, "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public static bool Confirm(string message)
    {
        return MessageBox.Show(message, "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }
}