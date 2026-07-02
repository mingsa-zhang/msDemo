using System;
using System.ComponentModel.DataAnnotations;

namespace CoreDemo3.Models
{
    /// <summary>
    /// 访客信息实体
    /// </summary>
    public class Visitor
    {
        public int Id { get; set; }

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

        /// <summary>
        /// 6位数字通行码
        /// </summary>
        [Required]
        [StringLength(6)]
        public string AccessCode { get; set; }

        /// <summary>
        /// 通行码有效期（当天23:59:59）
        /// </summary>
        public DateTime ExpiryTime { get; set; }

        /// <summary>
        /// 访客状态：0-已登记，1-已入场，2-已离场，3-已过期
        /// </summary>
        public int Status { get; set; } = 0;

        /// <summary>
        /// 入场时间
        /// </summary>
        public DateTime? CheckInTime { get; set; }

        /// <summary>
        /// 离场时间
        /// </summary>
        public DateTime? CheckOutTime { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedTime { get; set; } = DateTime.Now;
    }
}