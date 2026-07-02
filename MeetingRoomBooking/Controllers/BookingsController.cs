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
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 辅助方法：手动加载用户名
        private async Task<string> GetUserName(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.Username ?? "未知用户";
        }

        // 辅助方法：手动加载会议室名称
        private async Task<string> GetRoomName(int roomId)
        {
            var room = await _context.MeetingRooms.FindAsync(roomId);
            return room?.Name ?? "未知会议室";
        }

        /// <summary>
        /// 检查时间冲突
        /// </summary>
        private async Task<bool> HasTimeConflict(int roomId, DateTime startTime, DateTime endTime, int? excludeBookingId = null)
        {
            return await _context.Bookings
                .Where(b => b.RoomId == roomId
                    && b.Status != BookingStatus.Cancelled
                    && b.Status != BookingStatus.Rejected
                    && (excludeBookingId == null || b.Id != excludeBookingId)
                    && ((startTime >= b.StartTime && startTime < b.EndTime)
                        || (endTime > b.StartTime && endTime <= b.EndTime)
                        || (startTime <= b.StartTime && endTime >= b.EndTime)))
                .AnyAsync();
        }

        /// <summary>
        /// 获取会议室空闲时间表
        /// </summary>
        [HttpGet("availability")]
        public async Task<ActionResult<IEnumerable<RoomAvailabilityDto>>> GetAvailability([FromQuery] RoomAvailabilityQueryDto query)
        {
            var date = query.Date == DateTime.MinValue ? DateTime.Today : query.Date;
            var roomsQuery = _context.MeetingRooms.Where(r => r.IsActive);

            if (query.RoomId > 0)
            {
                roomsQuery = roomsQuery.Where(r => r.Id == query.RoomId);
            }

            var rooms = await roomsQuery.ToListAsync();
            var result = new List<RoomAvailabilityDto>();

            foreach (var room in rooms)
            {
                // 获取该房间当天的所有已批准预约
                var bookingsData = await _context.Bookings
                    .Where(b => b.RoomId == room.Id
                        && b.StartTime.Date == date
                        && b.Status == BookingStatus.Approved)
                    .OrderBy(b => b.StartTime)
                    .ToListAsync();

                var bookings = new List<BookingDto>();
                foreach (var b in bookingsData)
                {
                    bookings.Add(new BookingDto
                    {
                        Id = b.Id,
                        UserId = b.UserId,
                        UserName = await GetUserName(b.UserId),
                        RoomId = b.RoomId,
                        RoomName = room.Name,
                        StartTime = b.StartTime,
                        EndTime = b.EndTime,
                        Title = b.Title,
                        Status = b.Status
                    });
                }

                // 计算可用时间段
                var availableSlots = new List<TimeSlotDto>();
                var dayStart = date.Add(room.AvailableFrom);
                var dayEnd = date.Add(room.AvailableTo);

                var currentTime = dayStart;
                foreach (var booking in bookings)
                {
                    if (currentTime < booking.StartTime)
                    {
                        availableSlots.Add(new TimeSlotDto
                        {
                            StartTime = currentTime,
                            EndTime = booking.StartTime,
                            IsAvailable = true
                        });
                    }
                    currentTime = booking.EndTime;
                }

                if (currentTime < dayEnd)
                {
                    availableSlots.Add(new TimeSlotDto
                    {
                        StartTime = currentTime,
                        EndTime = dayEnd,
                        IsAvailable = true
                    });
                }

                result.Add(new RoomAvailabilityDto
                {
                    RoomId = room.Id,
                    RoomName = room.Name,
                    AvailableSlots = availableSlots,
                    ExistingBookings = bookings
                });
            }

            return Ok(result);
        }

        /// <summary>
        /// 提交预约申请
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<BookingDto>> CreateBooking([FromBody] CreateBookingDto dto)
        {
            // 获取当前用户ID
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "未授权访问" });
            }

            // 验证用户是否存在
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return BadRequest(new { message = $"用户不存在: UserId={userId}" });
            }

            // 验证会议室是否存在
            var room = await _context.MeetingRooms.FindAsync(dto.RoomId);
            if (room == null || !room.IsActive)
            {
                return BadRequest(new { message = "会议室不存在或不可用" });
            }

            // 验证时间
            if (dto.StartTime >= dto.EndTime)
            {
                return BadRequest(new { message = "结束时间必须大于开始时间" });
            }

            if (dto.StartTime < DateTime.Now)
            {
                return BadRequest(new { message = "不能预约过去的时间" });
            }

            // 检查时间冲突
            if (await HasTimeConflict(dto.RoomId, dto.StartTime, dto.EndTime))
            {
                return BadRequest(new { message = "该时间段已被预约" });
            }

            var booking = new Booking
            {
                UserId = userId,
                RoomId = dto.RoomId,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Title = dto.Title,
                Description = dto.Description,
                AttendeeCount = dto.AttendeeCount,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.Now
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // 手动构建返回的DTO
            var resultDto = new BookingDto
            {
                Id = booking.Id,
                UserId = booking.UserId,
                UserName = await GetUserName(booking.UserId),
                RoomId = booking.RoomId,
                RoomName = await GetRoomName(booking.RoomId),
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Title = booking.Title,
                Description = booking.Description,
                AttendeeCount = booking.AttendeeCount,
                Status = booking.Status,
                RejectionReason = booking.RejectionReason,
                CreatedAt = booking.CreatedAt
            };

            return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, resultDto);
        }

        /// <summary>
        /// 获取我的预约记录
        /// </summary>
        [HttpGet("my")]
        public async Task<ActionResult<IEnumerable<BookingDto>>> GetMyBookings()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            var bookingsData = await _context.Bookings
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var bookings = new List<BookingDto>();
            foreach (var b in bookingsData)
            {
                bookings.Add(new BookingDto
                {
                    Id = b.Id,
                    UserId = b.UserId,
                    UserName = await GetUserName(b.UserId),
                    RoomId = b.RoomId,
                    RoomName = await GetRoomName(b.RoomId),
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    Title = b.Title,
                    Description = b.Description,
                    AttendeeCount = b.AttendeeCount,
                    Status = b.Status,
                    RejectionReason = b.RejectionReason,
                    CreatedAt = b.CreatedAt,
                    ApproverName = b.ApprovedBy > 0 ? await GetUserName(b.ApprovedBy) : null
                });
            }

            return Ok(bookings);
        }

        /// <summary>
        /// 获取单个预约
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<BookingDto>> GetBooking(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);

            if (booking == null)
            {
                return NotFound();
            }

            var dto = new BookingDto
            {
                Id = booking.Id,
                UserId = booking.UserId,
                UserName = await GetUserName(booking.UserId),
                RoomId = booking.RoomId,
                RoomName = await GetRoomName(booking.RoomId),
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Title = booking.Title,
                Description = booking.Description,
                AttendeeCount = booking.AttendeeCount,
                Status = booking.Status,
                RejectionReason = booking.RejectionReason,
                CreatedAt = booking.CreatedAt,
                ApproverName = booking.ApprovedBy > 0 ? await GetUserName(booking.ApprovedBy) : null
            };

            return Ok(dto);
        }

        /// <summary>
        /// 取消预约
        /// </summary>
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            // 只能取消自己的预约
            if (booking.UserId != userId)
            {
                var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role);
                if (roleClaim?.Value != "Admin")
                {
                    return Forbid();
                }
            }

            // 检查是否可以取消（会议开始前2小时内不可取消）
            if (booking.StartTime.Subtract(DateTime.Now).TotalHours < 2)
            {
                return BadRequest(new { message = "会议开始前2小时内不可取消" });
            }

            booking.Status = BookingStatus.Cancelled;
            booking.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
