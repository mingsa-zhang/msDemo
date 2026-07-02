using System.ComponentModel.DataAnnotations;

namespace CoreDemo3.Models
{
    /// <summary>
    /// 通行码验证请求模型
    /// </summary>
    public class AccessCodeValidationRequest
    {
        [Required(ErrorMessage = "通行码不能为空")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "通行码必须为6位数字")]
        public string AccessCode { get; set; }
    }
}