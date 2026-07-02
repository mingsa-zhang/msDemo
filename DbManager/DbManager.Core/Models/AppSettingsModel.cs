namespace DbManager.Core.Models;

public class AppSettingsModel
{
    public bool UseDarkTheme { get; set; }
    public int SqlFontSize { get; set; } = 13;
    public int DefaultPageSize { get; set; } = 50;
    public bool AutoRefreshResults { get; set; }
    public bool ShowNullMarker { get; set; } = true;
    public int DefaultConnectTimeout { get; set; } = 30;
    public int DefaultCommandTimeout { get; set; } = 60;
}