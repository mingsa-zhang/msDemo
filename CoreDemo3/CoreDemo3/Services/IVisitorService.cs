using CoreDemo3.Models;

namespace CoreDemo3.Services
{
    /// <summary>
    /// 访客服务接口
    /// </summary>
    public interface IVisitorService
    {
        /// <summary>
        /// 注册访客信息
        /// </summary>
        /// <param name="request">访客登记请求</param>
        /// <returns>登记结果</returns>
        Task<VisitorRegisterResponse> RegisterVisitorAsync(VisitorRegisterRequest request);

        /// <summary>
        /// 验证通行码
        /// </summary>
        /// <param name="accessCode">通行码</param>
        /// <returns>验证结果</returns>
        Task<AccessCodeValidationResponse> ValidateAccessCodeAsync(string accessCode);

        /// <summary>
        /// 根据时间范围查询访客记录
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>访客记录列表</returns>
        Task<List<Visitor>> GetVisitorsByTimeRangeAsync(DateTime startTime, DateTime endTime);

        /// <summary>
        /// 生成唯一的6位数字通行码
        /// </summary>
        /// <returns>通行码</returns>
        Task<string> GenerateAccessCodeAsync();

        /// <summary>
        /// 检查通行码是否已存在
        /// </summary>
        /// <param name="accessCode">通行码</param>
        /// <returns>是否存在</returns>
        Task<bool> AccessCodeExistsAsync(string accessCode);
    }
}