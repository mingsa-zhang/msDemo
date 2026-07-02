using CoreDemo3.Models;
using CoreDemo3.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoreDemo3.Controllers
{
    /// <summary>
    /// 访客管理控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class VisitorController : ControllerBase
    {
        private readonly IVisitorService _visitorService;
        private readonly ILogger<VisitorController> _logger;

        public VisitorController(IVisitorService visitorService, ILogger<VisitorController> logger)
        {
            _visitorService = visitorService;
            _logger = logger;
        }

        /// <summary>
        /// 访客登记接口
        /// </summary>
        /// <param name="request">访客登记信息</param>
        /// <returns>登记结果</returns>
        [HttpPost("register")]
        public async Task<ActionResult<VisitorRegisterResponse>> RegisterVisitor([FromBody] VisitorRegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new VisitorRegisterResponse
                    {
                        Success = false,
                        Message = "输入数据无效：" + string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))
                    });
                }

                var result = await _visitorService.RegisterVisitorAsync(request);

                if (result.Success)
                {
                    _logger.LogInformation($"访客登记成功：{result.Name}，通行码：{result.AccessCode}");
                    return Ok(result);
                }
                else
                {
                    _logger.LogWarning($"访客登记失败：{result.Message}");
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "访客登记时发生异常");
                return StatusCode(500, new VisitorRegisterResponse
                {
                    Success = false,
                    Message = "服务器内部错误，请稍后重试"
                });
            }
        }

        /// <summary>
        /// 验证通行码接口
        /// </summary>
        /// <param name="request">验证请求</param>
        /// <returns>验证结果</returns>
        [HttpPost("validate")]
        public async Task<ActionResult<AccessCodeValidationResponse>> ValidateAccessCode([FromBody] AccessCodeValidationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new AccessCodeValidationResponse
                    {
                        IsValid = false,
                        Message = "输入数据无效：" + string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))
                    });
                }

                var result = await _visitorService.ValidateAccessCodeAsync(request.AccessCode);

                if (result.IsValid)
                {
                    _logger.LogInformation($"通行码验证成功：{request.AccessCode}");
                }
                else
                {
                    _logger.LogWarning($"通行码验证失败：{request.AccessCode}，原因：{result.Message}");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"验证通行码时发生异常：{request.AccessCode}");
                return StatusCode(500, new AccessCodeValidationResponse
                {
                    IsValid = false,
                    Message = "服务器内部错误，请稍后重试"
                });
            }
        }

        /// <summary>
        /// 查询访客记录接口
        /// </summary>
        /// <param name="startTime">开始时间（格式：yyyy-MM-dd HH:mm:ss）</param>
        /// <param name="endTime">结束时间（格式：yyyy-MM-dd HH:mm:ss）</param>
        /// <returns>访客记录列表</returns>
        [HttpGet("records")]
        public async Task<ActionResult<List<Visitor>>> GetVisitorRecords(
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null)
        {
            try
            {
                // 如果没有指定时间范围，默认查询当天的记录
                var start = startTime ?? DateTime.Today;
                var end = endTime ?? DateTime.Today.AddDays(1).AddTicks(-1);

                if (start > end)
                {
                    return BadRequest("开始时间不能大于结束时间");
                }

                // 限制查询范围不超过31天
                if ((end - start).TotalDays > 31)
                {
                    return BadRequest("查询时间范围不能超过31天");
                }

                var records = await _visitorService.GetVisitorsByTimeRangeAsync(start, end);

                _logger.LogInformation($"查询访客记录：{start:yyyy-MM-dd HH:mm:ss} 至 {end:yyyy-MM-dd HH:mm:ss}，共{records.Count}条记录");

                return Ok(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询访客记录时发生异常");
                return StatusCode(500, "服务器内部错误，请稍后重试");
            }
        }

        /// <summary>
        /// 获取访客统计信息
        /// </summary>
        /// <param name="date">日期（格式：yyyy-MM-dd），默认为今天</param>
        /// <returns>统计信息</returns>
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetVisitorStatistics([FromQuery] DateTime? date = null)
        {
            try
            {
                var targetDate = date ?? DateTime.Today;
                var startTime = targetDate.Date;
                var endTime = targetDate.Date.AddDays(1).AddTicks(-1);

                var records = await _visitorService.GetVisitorsByTimeRangeAsync(startTime, endTime);

                var statistics = new
                {
                    Date = targetDate.ToString("yyyy-MM-dd"),
                    TotalCount = records.Count,
                    RegisteredCount = records.Count(v => v.Status == 0),
                    CheckedInCount = records.Count(v => v.Status == 1),
                    CheckedOutCount = records.Count(v => v.Status == 2),
                    ExpiredCount = records.Count(v => v.Status == 3)
                };

                _logger.LogInformation($"获取访客统计：{targetDate:yyyy-MM-dd}，总计{statistics.TotalCount}人");

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取访客统计信息时发生异常");
                return StatusCode(500, "服务器内部错误，请稍后重试");
            }
        }
    }
}