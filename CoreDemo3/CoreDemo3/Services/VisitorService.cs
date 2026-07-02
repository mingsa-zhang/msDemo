using CoreDemo3.Data;
using CoreDemo3.Models;
using Microsoft.EntityFrameworkCore;

namespace CoreDemo3.Services
{
    /// <summary>
    /// 访客服务实现
    /// </summary>
    public class VisitorService : IVisitorService
    {
        private readonly VisitorDbContext _context;
        private readonly Random _random = new Random();

        public VisitorService(VisitorDbContext context)
        {
            _context = context;
        }

        public async Task<VisitorRegisterResponse> RegisterVisitorAsync(VisitorRegisterRequest request)
        {
            try
            {
                // 检查是否已存在相同身份证号的访客
                var existingVisitor = await _context.Visitors
                    .FirstOrDefaultAsync(v => v.IdCard == request.IdCard &&
                                            v.CreatedTime.Date == DateTime.Today);

                if (existingVisitor != null)
                {
                    return new VisitorRegisterResponse
                    {
                        Success = false,
                        Message = "该身份证号今日已登记，请勿重复登记"
                    };
                }

                // 生成通行码
                var accessCode = await GenerateAccessCodeAsync();

                // 设置通行码有效期为当天23:59:59
                var expiryTime = DateTime.Today.AddDays(1).AddTicks(-1);

                var visitor = new Visitor
                {
                    Name = request.Name,
                    Phone = request.Phone,
                    IdCard = request.IdCard,
                    VisitReason = request.VisitReason,
                    VisitedPerson = request.VisitedPerson,
                    AccessCode = accessCode,
                    ExpiryTime = expiryTime,
                    Status = 0, // 已登记
                    CreatedTime = DateTime.Now,
                    UpdatedTime = DateTime.Now
                };

                _context.Visitors.Add(visitor);
                await _context.SaveChangesAsync();

                return new VisitorRegisterResponse
                {
                    VisitorId = visitor.Id,
                    AccessCode = accessCode,
                    Name = visitor.Name,
                    Phone = visitor.Phone,
                    ExpiryTime = expiryTime,
                    Success = true,
                    Message = "访客登记成功"
                };
            }
            catch (Exception ex)
            {
                return new VisitorRegisterResponse
                {
                    Success = false,
                    Message = $"访客登记失败：{ex.Message}"
                };
            }
        }

        public async Task<AccessCodeValidationResponse> ValidateAccessCodeAsync(string accessCode)
        {
            try
            {
                var visitor = await _context.Visitors
                    .FirstOrDefaultAsync(v => v.AccessCode == accessCode);

                if (visitor == null)
                {
                    return new AccessCodeValidationResponse
                    {
                        IsValid = false,
                        Message = "通行码不存在"
                    };
                }

                // 检查通行码是否过期
                if (DateTime.Now > visitor.ExpiryTime)
                {
                    // 更新状态为已过期
                    visitor.Status = 3;
                    visitor.UpdatedTime = DateTime.Now;
                    await _context.SaveChangesAsync();

                    return new AccessCodeValidationResponse
                    {
                        IsValid = false,
                        Message = "通行码已过期"
                    };
                }

                // 根据状态返回不同的验证结果
                var statusText = visitor.Status switch
                {
                    0 => "已登记",
                    1 => "已入场",
                    2 => "已离场",
                    3 => "已过期",
                    _ => "未知状态"
                };

                var isValid = visitor.Status switch
                {
                    0 => true,  // 已登记，可以入场
                    1 => false, // 已入场，不能再入场
                    2 => false, // 已离场
                    3 => false, // 已过期
                    _ => false
                };

                // 如果状态为已登记且验证通过，更新为已入场
                if (visitor.Status == 0 && isValid)
                {
                    visitor.Status = 1;
                    visitor.CheckInTime = DateTime.Now;
                    visitor.UpdatedTime = DateTime.Now;
                    await _context.SaveChangesAsync();
                    statusText = "已入场";
                }

                return new AccessCodeValidationResponse
                {
                    IsValid = isValid,
                    Message = isValid ? "通行码验证成功" : $"通行码状态：{statusText}",
                    VisitorInfo = new VisitorInfo
                    {
                        Id = visitor.Id,
                        Name = visitor.Name,
                        VisitedPerson = visitor.VisitedPerson,
                        VisitReason = visitor.VisitReason,
                        Status = visitor.Status,
                        StatusText = statusText
                    }
                };
            }
            catch (Exception ex)
            {
                return new AccessCodeValidationResponse
                {
                    IsValid = false,
                    Message = $"验证失败：{ex.Message}"
                };
            }
        }

        public async Task<List<Visitor>> GetVisitorsByTimeRangeAsync(DateTime startTime, DateTime endTime)
        {
            return await _context.Visitors
                .Where(v => v.CreatedTime >= startTime && v.CreatedTime <= endTime)
                .OrderByDescending(v => v.CreatedTime)
                .ToListAsync();
        }

        public async Task<string> GenerateAccessCodeAsync()
        {
            string accessCode;
            int maxAttempts = 100;
            int attempts = 0;

            do
            {
                accessCode = _random.Next(100000, 999999).ToString();
                attempts++;

                if (attempts >= maxAttempts)
                {
                    throw new InvalidOperationException("无法生成唯一的通行码，请稍后重试");
                }
            } while (await AccessCodeExistsAsync(accessCode));

            return accessCode;
        }

        public async Task<bool> AccessCodeExistsAsync(string accessCode)
        {
            return await _context.Visitors
                .AnyAsync(v => v.AccessCode == accessCode && v.ExpiryTime > DateTime.Now);
        }
    }
}