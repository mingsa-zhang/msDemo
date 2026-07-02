namespace CoreDemo3.Models
{
    /// <summary>
    /// 通行码验证响应模型
    /// </summary>
    public class AccessCodeValidationResponse
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public VisitorInfo VisitorInfo { get; set; }
    }

    /// <summary>
    /// 访客信息简报
    /// </summary>
    public class VisitorInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string VisitedPerson { get; set; }
        public string VisitReason { get; set; }
        public int Status { get; set; }
        public string StatusText { get; set; }
    }
}