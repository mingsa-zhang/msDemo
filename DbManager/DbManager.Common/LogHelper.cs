using Serilog;
using Serilog.Events;

namespace DbManager.Common;

public static class LogHelper
{
    private static bool _initialized;

    public static void Initialize(string? logDir = null)
    {
        if (_initialized) return;

        var dir = logDir ?? AppConst.LogDir;
        Directory.CreateDirectory(dir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(dir, "dbmanager_.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        _initialized = true;
    }

    public static void Debug(string message, params object[] args) => Log.Debug(message, args);
    public static void Info(string message, params object[] args) => Log.Information(message, args);
    public static void Warn(string message, params object[] args) => Log.Warning(message, args);
    public static void Error(string message, params object[] args) => Log.Error(message, args);
    public static void Error(Exception ex, string message, params object[] args) => Log.Error(ex, message, args);
    public static void Fatal(string message, params object[] args) => Log.Fatal(message, args);
    public static void Fatal(Exception ex, string message, params object[] args) => Log.Fatal(ex, message, args);
}
