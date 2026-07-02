using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using DataAgentRetryTool.Models;
using Newtonsoft.Json;

namespace DataAgentRetryTool.Services;

/// <summary>
/// 数据传输记录服务
/// </summary>
public class DataTransmissionService
{
    private readonly HttpClient _httpClient;
    private string _token = string.Empty;
    private EnvironmentType _currentEnvironment = EnvironmentType.Production;

    // 环境地址配置
    private static readonly string ProductionBaseUrl = "https://apicenter.hysyyl.com/api/dataagentcenter/admin/v1";
    private static readonly string TestBaseUrl = "https://testapidataagent.hysyyl.com:30881/api/dataagentcenter/admin/v1";

    public DataTransmissionService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    /// <summary>
    /// 获取当前环境
    /// </summary>
    public EnvironmentType GetCurrentEnvironment() => _currentEnvironment;

    /// <summary>
    /// 设置环境类型
    /// </summary>
    public void SetEnvironment(EnvironmentType environment)
    {
        _currentEnvironment = environment;
    }

    /// <summary>
    /// 获取当前环境的BaseUrl
    /// </summary>
    private string GetBaseUrl()
    {
        return _currentEnvironment == EnvironmentType.Production
            ? ProductionBaseUrl
            : TestBaseUrl;
    }

    /// <summary>
    /// 设置授权Token
    /// </summary>
    public void SetToken(string token)
    {
        _token = token;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// 查询数据传输记录（正式环境 - 不分FilterValue）
    /// </summary>
    public async Task<QueryResponse?> QueryProductionRecordsAsync(int pageIndex = 1, int pageSize = 10)
    {
        var request = new ProductionQueryRequest
        {
            PageIndex = pageIndex,
            PageSize = pageSize
        };

        var json = JsonConvert.SerializeObject(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{GetBaseUrl()}/DataTransmissionRecord/PageList", content);
        var responseString = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<QueryResponse>(responseString);
    }

    /// <summary>
    /// 查询数据传输记录（测试环境 - 不分FilterValue）
    /// </summary>
    public async Task<QueryResponse?> QueryTestRecordsAsync(int pageIndex = 1, int pageSize = 10)
    {
        var request = new TestQueryRequest
        {
            PageIndex = pageIndex,
            PageSize = pageSize
        };

        var json = JsonConvert.SerializeObject(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{GetBaseUrl()}/DataTransmissionRecord/PageList", content);
        var responseString = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<QueryResponse>(responseString);
    }

    /// <summary>
    /// 查询传输中的记录（测试环境 - 用于放弃功能）
    /// </summary>
    public async Task<QueryResponse?> QueryTestTransferringRecordsAsync(string startTime, string endTime, int pageIndex = 1, int pageSize = 10)
    {
        var request = new TestAbandonQueryRequest
        {
            StartTime = startTime,
            EndTime = endTime,
            PageIndex = pageIndex,
            PageSize = pageSize
        };

        var json = JsonConvert.SerializeObject(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{GetBaseUrl()}/DataTransmissionRecord/PageList", content);
        var responseString = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<QueryResponse>(responseString);
    }

    /// <summary>
    /// 按时间条件查询记录（测试环境 - 支持多状态）
    /// </summary>
    public async Task<QueryResponse?> QueryTestByTimeAsync(DateTime? startTime, DateTime? endTime, int transferState, int pageIndex = 1, int pageSize = 10)
    {
        var request = new TestTimeQueryRequest
        {
            StartTime = startTime,
            EndTime = endTime,
            TransferState = transferState,
            PageIndex = pageIndex,
            PageSize = pageSize
        };

        var json = JsonConvert.SerializeObject(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{GetBaseUrl()}/DataTransmissionRecord/PageList", content);
        var responseString = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<QueryResponse>(responseString);
    }

    /// <summary>
    /// 按客户ID查询传输中记录（测试环境 - 用于重推）
    /// </summary>
    public async Task<QueryResponse?> QueryTestTransferringByIdAsync(string filterValue, int pageIndex = 1, int pageSize = 10)
    {
        var request = new TestTransferringByIdQueryRequest
        {
            FilterValue = filterValue,
            PageIndex = pageIndex,
            PageSize = pageSize
        };

        var json = JsonConvert.SerializeObject(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{GetBaseUrl()}/DataTransmissionRecord/PageList", content);
        var responseString = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<QueryResponse>(responseString);
    }

    /// <summary>
    /// 重推数据传输记录
    /// </summary>
    public async Task<RetryResponse?> RetryAsync(TransmissionRecord record)
    {
        var request = new RetryRequest
        {
            Id = record.Id,
            BusinessTypeCode = record.BusinessTypeCode,
            AppCode = record.AccessAppCode,
            BusinessDataJson = record.BusinessDataJson
        };

        var json = JsonConvert.SerializeObject(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{GetBaseUrl()}/DataTransmissionRecord/Retry", content);
        var responseString = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<RetryResponse>(responseString);
    }

    /// <summary>
    /// 根据Id查询详情
    /// </summary>
    public async Task<DetailResponse?> GetDetailAsync(string id)
    {
        var response = await _httpClient.GetAsync(
            $"{GetBaseUrl()}/DataTransmissionRecord/Detail?id={id}&_t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
        var responseString = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<DetailResponse>(responseString);
    }

    /// <summary>
    /// 取消推送
    /// </summary>
    public async Task<AbandonResponse?> AbandonAsync(AbandonRequest request)
    {
        var json = JsonConvert.SerializeObject(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            $"{GetBaseUrl()}/DataTransmissionRecord/Abandon", content);
        var responseString = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<AbandonResponse>(responseString);
    }
}