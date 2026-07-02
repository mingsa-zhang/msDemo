using System;
using Microsoft.AspNetCore.Mvc;
using CoreDemo2.Services;
using CoreDemo2.Models;

namespace CoreDemo2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StringEscapeController : ControllerBase
    {
        private readonly IStringEscapeService _escapeService;

        public StringEscapeController(IStringEscapeService escapeService)
        {
            _escapeService = escapeService;
        }

        [HttpPost("escape")]
        public ActionResult<StringOperationResponse> EscapeString([FromBody] StringOperationRequest request)
        {
            try
            {
                var output = _escapeService.Escape(request.Input ?? string.Empty);

                return Ok(new StringOperationResponse
                {
                    Input = request.Input,
                    Output = output,
                    Operation = "escape",
                    Success = true,
                    Message = "字符串转义成功"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new StringOperationResponse
                {
                    Input = request.Input,
                    Output = string.Empty,
                    Operation = "escape",
                    Success = false,
                    Message = $"字符串转义失败: {ex.Message}"
                });
            }
        }

        [HttpPost("unescape")]
        public ActionResult<StringOperationResponse> UnescapeString([FromBody] StringOperationRequest request)
        {
            try
            {
                var output = _escapeService.Unescape(request.Input ?? string.Empty);

                return Ok(new StringOperationResponse
                {
                    Input = request.Input,
                    Output = output,
                    Operation = "unescape",
                    Success = true,
                    Message = "字符串反转义成功"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new StringOperationResponse
                {
                    Input = request.Input,
                    Output = string.Empty,
                    Operation = "unescape",
                    Success = false,
                    Message = $"字符串反转义失败: {ex.Message}"
                });
            }
        }
    }
}