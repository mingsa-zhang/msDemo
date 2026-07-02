using System.ComponentModel.DataAnnotations;

namespace CoreDemo3.Models
{
    /// <summary>
    /// 访客登记请求模型
    /// </summary>
    public class VisitorRegisterRequest
    {
        [Required(ErrorMessage = "姓名不能为空")]
        [StringLength(50, ErrorMessage = "姓名长度不能超过50个字符")]
        public string Name { get; set; }

        [Required(ErrorMessage = "手机号不能为空")]
        [RegularExpression(@"^1[3-9]\d{9}$", ErrorMessage = "手机号格式不正确")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "身份证号不能为空")]
        [RegularExpression(@"^[1-9]\d{5}(18|19|20)\d{2}(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])\d{3}[\dXx]$", ErrorMessage = "身份证号格式不正确")]
        public string IdCard { get; set; }

        [Required(ErrorMessage = "来访事由不能为空")]
        [StringLength(200, ErrorMessage = "来访事由长度不能超过200个字符")]
        public string VisitReason { get; set; }

        [Required(ErrorMessage = "被访人不能为空")]
        [StringLength(50, ErrorMessage = "被访人姓名长度不能超过50个字符")]
        public string VisitedPerson { get; set; }
    }
}