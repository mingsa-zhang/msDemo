using DbManager.Core.Models;
using DbManager.Common;
using Newtonsoft.Json;

namespace DbManager.Core.Services;

public class SettingsService
{
    private readonly string _settingsFilePath;
    private AppSettingsModel _settings;

    public SettingsService()
    {
        _settingsFilePath = Path.Combine(AppConst.AppDataDir, "settings.json");
        _settings = LoadSettings();
    }

    public AppSettingsModel Settings => _settings;

    public AppSettingsModel LoadSettings()
    {
        if (!File.Exists(_settingsFilePath))
            return new AppSettingsModel();

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            return JsonConvert.DeserializeObject<AppSettingsModel>(json) ?? new AppSettingsModel();
        }
        catch
        {
            return new AppSettingsModel();
        }
    }

    public void SaveSettings(AppSettingsModel settings)
    {
        _settings = settings;
        var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
        File.WriteAllText(_settingsFilePath, json);
    }
}