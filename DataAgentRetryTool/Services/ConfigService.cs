using System.IO;
using Newtonsoft.Json;

namespace DataAgentRetryTool.Services;

/// <summary>
/// 环境类型
/// </summary>
public enum EnvironmentType
{
    Production,  // 正式环境
    Test         // 测试环境
}

/// <summary>
/// 配置管理服务
/// </summary>
public class ConfigService
{
    private static readonly string ConfigPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "config.json");

    /// <summary>
    /// 保存Token（指定环境）
    /// </summary>
    public void SaveToken(string token, EnvironmentType environment)
    {
        var config = LoadConfig();
        if (environment == EnvironmentType.Production)
        {
            config.ProductionToken = token;
        }
        else
        {
            config.TestToken = token;
        }
        config.CurrentEnvironment = environment;
        var json = JsonConvert.SerializeObject(config, Formatting.Indented);
        File.WriteAllText(ConfigPath, json);
    }

    /// <summary>
    /// 加载Token（指定环境）
    /// </summary>
    public string LoadToken(EnvironmentType environment)
    {
        var config = LoadConfig();
        return environment == EnvironmentType.Production
            ? config.ProductionToken
            : config.TestToken;
    }

    /// <summary>
    /// 加载当前环境的Token
    /// </summary>
    public string LoadCurrentToken()
    {
        var config = LoadConfig();
        return config.CurrentEnvironment == EnvironmentType.Production
            ? config.ProductionToken
            : config.TestToken;
    }

    /// <summary>
    /// 获取当前环境
    /// </summary>
    public EnvironmentType GetCurrentEnvironment()
    {
        var config = LoadConfig();
        return config.CurrentEnvironment;
    }

    /// <summary>
    /// 设置当前环境（不改变Token）
    /// </summary>
    public void SetCurrentEnvironment(EnvironmentType environment)
    {
        var config = LoadConfig();
        config.CurrentEnvironment = environment;
        var json = JsonConvert.SerializeObject(config, Formatting.Indented);
        File.WriteAllText(ConfigPath, json);
    }

    /// <summary>
    /// 加载完整配置
    /// </summary>
    private ConfigModel LoadConfig()
    {
        if (!File.Exists(ConfigPath))
        {
            return new ConfigModel();
        }

        try
        {
            var json = File.ReadAllText(ConfigPath);
            var config = JsonConvert.DeserializeObject<ConfigModel>(json);
            return config ?? new ConfigModel();
        }
        catch
        {
            return new ConfigModel();
        }
    }
}

/// <summary>
/// 配置模型
/// </summary>
public class ConfigModel
{
    [JsonProperty("CurrentEnvironment")]
    public EnvironmentType CurrentEnvironment { get; set; } = EnvironmentType.Production;

    [JsonProperty("ProductionToken")]
    public string ProductionToken { get; set; } = string.Empty;

    [JsonProperty("TestToken")]
    public string TestToken { get; set; } = string.Empty;
}