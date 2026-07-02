using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MeetingRoomBooking.Data;
using MeetingRoomBooking.Models;
using MeetingRoomBooking.Models.DTOs;

namespace MeetingRoomBooking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 获取所有待审批的预约
        /// </summary>
        [HttpGet("bookings/pending")]
        public async Task<ActionResult<IEnumerable<BookingDto>>> GetPendingBookings()
        {
            var bookings = await _context.Bookings
                
                
                .Where(b => b.Status == BookingStatus.Pending)
                .OrderBy(b => b.StartTime)
                .Select(b => new BookingDto
                {
                    Id = b.Id,
                    UserId = b.UserId,
                    UserName = b.User.Username,
                    RoomId = b.RoomId,
                    RoomName = b.Room.Name,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    Title = b.Title,
                    Description = b.Description,
                    AttendeeCount = b.AttendeeCount,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            return Ok(bookings);
        }

        /// <summary>
        /// 获取所有预约（管理员）
        /// </summary>
        [HttpGet("bookings")]
        public async Task<ActionResult<IEnumerable<BookingDto>>> GetAllBookings(
            [FromQuery] int? roomId,
            [FromQuery] BookingStatus? status,
            [FromQuery] DateTime? start,
            [FromQuery] DateTime? end)
        {
            var query = _context.Bookings
                
                
                
                .AsQueryable();

            if (roomId.HasValue)
            {
                query = query.Where(b => b.RoomId == roomId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(b => b.Status == status.Value);
            }

            if (start.HasValue)
            {
                query = query.Where(b => b.StartTime >= start.Value);
            }

            if (end.HasValue)
            {
                query = query.Where(b => b.EndTime <= end.Value);
            }

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new BookingDto
                {
                    Id = b.Id,
                    UserId = b.UserId,
                    UserName = b.User.Username,
                    RoomId = b.RoomId,
                    RoomName = b.Room.Name,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    Title = b.Title,
                    Description = b.Description,
                    AttendeeCount = b.AttendeeCount,
                    Status = b.Status,
                    RejectionReason = b.RejectionReason,
                    CreatedAt = b.CreatedAt,
                    ApproverName = b.Approver != null ? b.Approver.Username : null
                })
                .ToListAsync();

            return Ok(bookings);
        }

        /// <summary>
        /// 审批预约
        /// </summary>
        [HttpPost("bookings/{id}/approve")]
        public async Task<ActionResult<BookingDto>> ApproveBooking(int id, [FromBody] ApproveBookingDto dto)
        {
            var adminIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (adminIdClaim == null || !int.TryParse(adminIdClaim.Value, out int adminId))
            {
                return Unauthorized();
            }

            var booking = await _context.Bookings
                
                
                
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.Status != BookingStatus.Pending)
            {
                return BadRequest(new { message = "该预约已被处理" });
            }

            // 检查时间冲突（如果是批准）
            if (dto.Approved)
            {
                var hasConflict = await _context.Bookings
                    .Where(b => b.RoomId == booking.RoomId
                        && b.Id != id
                        && b.Status == BookingStatus.Approved
                        && ((booking.StartTime >= b.StartTime && booking.StartTime < b.EndTime)
                            || (booking.EndTime > b.StartTime && booking.EndTime <= b.EndTime)
                            || (booking.StartTime <= b.StartTime && booking.EndTime >= b.EndTime)))
                    .AnyAsync();

                if (hasConflict)
                {
                    return BadRequest(new { message = "该时间段存在时间冲突，无法批准" });
                }
            }

            booking.Status = dto.Approved ? BookingStatus.Approved : BookingStatus.Rejected;
            booking.RejectionReason = dto.Approved ? null : dto.Reason;
            booking.ApprovedBy = adminId;
            booking.ApprovedAt = DateTime.Now;
            booking.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new BookingDto
            {
                Id = booking.Id,
                UserId = booking.UserId,
                UserName = booking.User.Username,
                RoomId = booking.RoomId,
                RoomName = booking.Room.Name,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Title = booking.Title,
                Description = booking.Description,
                AttendeeCount = booking.AttendeeCount,
                Status = booking.Status,
                RejectionReason = booking.RejectionReason,
                CreatedAt = booking.CreatedAt,
                ApproverName = booking.Approver?.Username
            });
        }

        /// <summary>
        /// 获取使用率统计
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<UsageStatisticsDto>> GetStatistics(
            [FromQuery] DateTime? start,
            [FromQuery] DateTime? end)
        {
            var startDate = start ?? DateTime.Today.AddDays(-30);
            var endDate = end ?? DateTime.Today;

            // 获取所有已完成的预约
            var bookings = await _context.Bookings
                
                
                .Where(b => b.StartTime >= startDate && b.EndTime <= endDate && b.Status == BookingStatus.Approved)
                .ToListAsync();

            var totalBookings = bookings.Count;
            var approvedBookings = bookings.Count;

            // 计算会议室使用率
            var roomUsages = bookings
                .GroupBy(b => b.RoomId)
                .Select(g => new RoomUsageDto
                {
                    RoomId = g.First().Room.Id,
                    RoomName = g.First().Room.Name,
                    BookingCount = g.Count(),
                    TotalHours = (decimal)g.Sum(b => (b.EndTime - b.StartTime).TotalHours),
                    UtilizationRate = CalculateUtilizationRate(g.ToList(), startDate, endDate)
                })
                .OrderByDescending(r => r.BookingCount)
                .ToList();

            // 计算部门使用统计
            var departmentUsages = bookings
                .GroupBy(b => b.User.Department)
                .Select(g => new DepartmentUsageDto
                {
                    Department = g.Key,
                    BookingCount = g.Count(),
                    TotalHours = (decimal)g.Sum(b => (b.EndTime - b.StartTime).TotalHours),
                    Percentage = totalBookings > 0 ? (g.Count() * 100.0 / totalBookings) : 0
                })
                .OrderByDescending(d => d.BookingCount)
                .ToList();

            // 计算总体使用率
            var overallUtilizationRate = roomUsages.Any() ? roomUsages.Average(r => r.UtilizationRate) : 0;

            return Ok(new UsageStatisticsDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalBookings = totalBookings,
                ApprovedBookings = approvedBookings,
                OverallUtilizationRate = overallUtilizationRate,
                RoomUsages = roomUsages,
                DepartmentUsages = departmentUsages
            });
        }

        private decimal CalculateUtilizationRate(List<Booking> bookings, DateTime start, DateTime end)
        {
            var totalDays = (end - start).Days + 1;
            if (totalDays <= 0) return 0;

            var totalHours = bookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);
            var availableHours = totalDays * 8; // 假设每天8小时工作时间

            return availableHours > 0 ? (decimal)(totalHours / availableHours * 100) : 0;
        }
    }
}
