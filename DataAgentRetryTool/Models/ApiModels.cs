using Newtonsoft.Json;

namespace DataAgentRetryTool.Models;

/// <summary>
/// 正式环境查询请求参数
/// </summary>
public class ProductionQueryRequest
{
    [JsonProperty("TransferState")]
    public int TransferState { get; set; } = 4;

    [JsonProperty("BusinessTypeId")]
    public string BusinessTypeId { get; set; } = "1957628687581773824";

    [JsonProperty("BusinessTypeIdName")]
    public string BusinessTypeIdName { get; set; } = "【新-部】客户修改";

    [JsonProperty("PageIndex")]
    public int PageIndex { get; set; } = 1;

    [JsonProperty("PageSize")]
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// 测试环境查询请求参数
/// </summary>
public class TestQueryRequest
{
    [JsonProperty("TransferState")]
    public int TransferState { get; set; } = 4;

    [JsonProperty("BusinessTypeId")]
    public string BusinessTypeId { get; set; } = "1905147715017936896";

    [JsonProperty("BusinessTypeIdName")]
    public string BusinessTypeIdName { get; set; } = "【正式】客户修改";

    [JsonProperty("PageIndex")]
    public int PageIndex { get; set; } = 1;

    [JsonProperty("PageSize")]
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// 测试环境查询传输中记录请求参数（用于放弃）
/// </summary>
public class TestAbandonQueryRequest
{
    [JsonProperty("BusinessTypeId")]
    public string BusinessTypeId { get; set; } = "1905147715017936896";

    [JsonProperty("BusinessTypeIdName")]
    public string BusinessTypeIdName { get; set; } = "【正式】客户修改";

    [JsonProperty("FilterKeyName")]
    public string FilterKeyName { get; set; } = "ERPCustomerId";

    [JsonProperty("TransferState")]
    public int TransferState { get; set; } = 1;

    [JsonProperty("StartTime")]
    public string StartTime { get; set; } = string.Empty;

    [JsonProperty("EndTime")]
    public string EndTime { get; set; } = string.Empty;

    [JsonProperty("PageIndex")]
    public int PageIndex { get; set; } = 1;

    [JsonProperty("PageSize")]
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// 测试环境按时间条件查询请求参数（支持多状态）
/// </summary>
public class TestTimeQueryRequest
{
    [JsonProperty("BusinessTypeId")]
    public string BusinessTypeId { get; set; } = "1905147715017936896";

    [JsonProperty("BusinessTypeIdName")]
    public string BusinessTypeIdName { get; set; } = "【正式】客户修改";

    [JsonProperty("TransferState")]
    public int TransferState { get; set; } = 4;

    [JsonProperty("StartTime")]
    public DateTime? StartTime { get; set; }

    [JsonProperty("EndTime")]
    public DateTime? EndTime { get; set; }

    [JsonProperty("PageIndex")]
    public int PageIndex { get; set; } = 1;

    [JsonProperty("PageSize")]
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// 测试环境按客户ID查询传输中记录请求参数（用于重推）
/// </summary>
public class TestTransferringByIdQueryRequest
{
    [JsonProperty("BusinessTypeId")]
    public string BusinessTypeId { get; set; } = "1905147715017936896";

    [JsonProperty("BusinessTypeIdName")]
    public string BusinessTypeIdName { get; set; } = "【正式】客户修改";

    [JsonProperty("FilterKey")]
    public string FilterKey { get; set; } = "ERPCustomerId";

    [JsonProperty("FilterKeyName")]
    public string FilterKeyName { get; set; } = "ERPCustomerId";

    [JsonProperty("FilterValue")]
    public string FilterValue { get; set; } = string.Empty;

    [JsonProperty("TransferState")]
    public int TransferState { get; set; } = 1;

    [JsonProperty("PageIndex")]
    public int PageIndex { get; set; } = 1;

    [JsonProperty("PageSize")]
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// 查询返回结果
/// </summary>
public class QueryResponse
{
    [JsonProperty("IsSuccess")]
    public bool IsSuccess { get; set; }

    [JsonProperty("ErrorCode")]
    public int ErrorCode { get; set; }

    [JsonProperty("Msg")]
    public string Msg { get; set; } = string.Empty;

    [JsonProperty("Data")]
    public List<TransmissionRecord> Data { get; set; } = new();

    [JsonProperty("Count")]
    public int Count { get; set; }
}

/// <summary>
/// 传输记录
/// </summary>
public class TransmissionRecord
{
    [JsonProperty("Id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("BusinessGroupId")]
    public string BusinessGroupId { get; set; } = string.Empty;

    [JsonProperty("BusinessGroupCode")]
    public string BusinessGroupCode { get; set; } = string.Empty;

    [JsonProperty("BusinessGroupName")]
    public string BusinessGroupName { get; set; } = string.Empty;

    [JsonProperty("BusinessTypeId")]
    public string BusinessTypeId { get; set; } = string.Empty;

    [JsonProperty("BusinessTypeCode")]
    public string BusinessTypeCode { get; set; } = string.Empty;

    [JsonProperty("BusinessTypeName")]
    public string BusinessTypeName { get; set; } = string.Empty;

    [JsonProperty("AccessAppId")]
    public string AccessAppId { get; set; } = string.Empty;

    [JsonProperty("AccessAppCode")]
    public string AccessAppCode { get; set; } = string.Empty;

    [JsonProperty("AccessAppName")]
    public string AccessAppName { get; set; } = string.Empty;

    [JsonProperty("BusinessDataJson")]
    public string BusinessDataJson { get; set; } = string.Empty;

    [JsonProperty("TransferState")]
    public int TransferState { get; set; }

    [JsonProperty("TransferStateStr")]
    public string TransferStateStr { get; set; } = string.Empty;

    [JsonProperty("TransferStateDescription")]
    public string TransferStateDescription { get; set; } = string.Empty;

    [JsonProperty("ExecState")]
    public int ExecState { get; set; }

    [JsonProperty("ExecStateStr")]
    public string ExecStateStr { get; set; } = string.Empty;

    [JsonProperty("CreateTime")]
    public string CreateTime { get; set; } = string.Empty;

    [JsonProperty("CanRetry")]
    public bool CanRetry { get; set; }

    [JsonProperty("CanAbandon")]
    public bool CanAbandon { get; set; }
}

/// <summary>
/// 重推请求参数
/// </summary>
public class RetryRequest
{
    [JsonProperty("Id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("BusinessTypeCode")]
    public string BusinessTypeCode { get; set; } = string.Empty;

    [JsonProperty("AppCode")]
    public string AppCode { get; set; } = string.Empty;

    [JsonProperty("BusinessDataJson")]
    public string BusinessDataJson { get; set; } = string.Empty;
}

/// <summary>
/// 重推返回结果
/// </summary>
public class RetryResponse
{
    [JsonProperty("IsSuccess")]
    public bool IsSuccess { get; set; }

    [JsonProperty("ErrorCode")]
    public int ErrorCode { get; set; }

    [JsonProperty("Msg")]
    public string Msg { get; set; } = string.Empty;

    [JsonProperty("Data")]
    public object? Data { get; set; }
}

/// <summary>
/// 详情接口返回
/// </summary>
public class DetailResponse
{
    [JsonProperty("IsSuccess")]
    public bool IsSuccess { get; set; }

    [JsonProperty("ErrorCode")]
    public int ErrorCode { get; set; }

    [JsonProperty("Msg")]
    public string Msg { get; set; } = string.Empty;

    [JsonProperty("Data")]
    public DetailRecord Data { get; set; } = new();

    [JsonProperty("Count")]
    public int Count { get; set; }
}

/// <summary>
/// 详情记录
/// </summary>
public class DetailRecord
{
    [JsonProperty("Id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("BusinessTypeCode")]
    public string BusinessTypeCode { get; set; } = string.Empty;

    [JsonProperty("InApp")]
    public InAppInfo InApp { get; set; } = new();

    [JsonProperty("OutApps")]
    public List<OutAppInfo> OutApps { get; set; } = new();

    [JsonProperty("TransferState")]
    public int TransferState { get; set; }

    [JsonProperty("TransferStateStr")]
    public string TransferStateStr { get; set; } = string.Empty;
}

/// <summary>
/// 源应用信息
/// </summary>
public class InAppInfo
{
    [JsonProperty("AccessAppId")]
    public string AccessAppId { get; set; } = string.Empty;

    [JsonProperty("AccessAppCode")]
    public string AccessAppCode { get; set; } = string.Empty;

    [JsonProperty("AccessAppName")]
    public string AccessAppName { get; set; } = string.Empty;
}

/// <summary>
/// 目标应用信息
/// </summary>
public class OutAppInfo
{
    [JsonProperty("AccessAppId")]
    public string AccessAppId { get; set; } = string.Empty;

    [JsonProperty("AccessAppCode")]
    public string AccessAppCode { get; set; } = string.Empty;

    [JsonProperty("AccessAppName")]
    public string AccessAppName { get; set; } = string.Empty;

    [JsonProperty("TransferState")]
    public int TransferState { get; set; }

    [JsonProperty("TransferStateStr")]
    public string TransferStateStr { get; set; } = string.Empty;

    [JsonProperty("TransferStateDescription")]
    public string TransferStateDescription { get; set; } = string.Empty;
}

/// <summary>
/// 内层错误信息（TransferStateDescription 反序列化后的对象）
/// </summary>
public class ErrorDetail
{
    [JsonProperty("ClassName")]
    public string ClassName { get; set; } = string.Empty;

    [JsonProperty("Message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 取消推送请求
/// </summary>
public class AbandonRequest
{
    [JsonProperty("Id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("BusinessTypeCode")]
    public string BusinessTypeCode { get; set; } = string.Empty;

    [JsonProperty("AppCode")]
    public string AppCode { get; set; } = string.Empty;

    [JsonProperty("OutAppCode")]
    public string OutAppCode { get; set; } = string.Empty;

    [JsonProperty("BusinessDataJson")]
    public object? BusinessDataJson { get; set; }
}

/// <summary>
/// 取消推送返回
/// </summary>
public class AbandonResponse
{
    [JsonProperty("IsSuccess")]
    public bool IsSuccess { get; set; }

    [JsonProperty("ErrorCode")]
    public int ErrorCode { get; set; }

    [JsonProperty("Msg")]
    public string Msg { get; set; } = string.Empty;

    [JsonProperty("Data")]
    public object? Data { get; set; }

    [JsonProperty("Count")]
    public int Count { get; set; }
}