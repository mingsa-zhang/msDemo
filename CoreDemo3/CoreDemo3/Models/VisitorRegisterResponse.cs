namespace CoreDemo3.Models
{
    /// <summary>
    /// 访客登记响应模型
    /// </summary>
    public class VisitorRegisterResponse
    {
        public int VisitorId { get; set; }
        public string AccessCode { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public DateTime ExpiryTime { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}