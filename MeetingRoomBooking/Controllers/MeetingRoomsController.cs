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
    public class MeetingRoomsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MeetingRoomsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 获取所有会议室
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MeetingRoomDto>>> GetMeetingRooms()
        {
            var rooms = await _context.MeetingRooms
                .Where(r => r.IsActive)
                .Select(r => new MeetingRoomDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Location = r.Location,
                    Capacity = r.Capacity,
                    Equipment = r.Equipment,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    AvailableFrom = r.AvailableFrom.ToString(@"hh\:mm"),
                    AvailableTo = r.AvailableTo.ToString(@"hh\:mm")
                })
                .ToListAsync();

            return Ok(rooms);
        }

        /// <summary>
        /// 根据ID获取会议室
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<MeetingRoomDto>> GetMeetingRoom(int id)
        {
            var room = await _context.MeetingRooms.FindAsync(id);

            if (room == null)
            {
                return NotFound();
            }

            return Ok(new MeetingRoomDto
            {
                Id = room.Id,
                Name = room.Name,
                Location = room.Location,
                Capacity = room.Capacity,
                Equipment = room.Equipment,
                Description = room.Description,
                IsActive = room.IsActive,
                AvailableFrom = room.AvailableFrom.ToString(@"hh\:mm"),
                AvailableTo = room.AvailableTo.ToString(@"hh\:mm")
            });
        }

        /// <summary>
        /// 创建会议室（仅管理员）
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<MeetingRoomDto>> CreateMeetingRoom([FromBody] CreateMeetingRoomDto dto)
        {
            var room = new MeetingRoom
            {
                Name = dto.Name,
                Location = dto.Location,
                Capacity = dto.Capacity,
                Equipment = dto.Equipment,
                Description = dto.Description,
                IsActive = dto.IsActive,
                AvailableFrom = TimeSpan.TryParse(dto.AvailableFrom, out var from) ? from : new TimeSpan(9, 0, 0),
                AvailableTo = TimeSpan.TryParse(dto.AvailableTo, out var to) ? to : new TimeSpan(18, 0, 0),
                CreatedAt = DateTime.Now
            };

            _context.MeetingRooms.Add(room);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMeetingRoom), new { id = room.Id }, new MeetingRoomDto
            {
                Id = room.Id,
                Name = room.Name,
                Location = room.Location,
                Capacity = room.Capacity,
                Equipment = room.Equipment,
                Description = room.Description,
                IsActive = room.IsActive,
                AvailableFrom = room.AvailableFrom.ToString(@"hh\:mm"),
                AvailableTo = room.AvailableTo.ToString(@"hh\:mm")
            });
        }

        /// <summary>
        /// 更新会议室（仅管理员）
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateMeetingRoom(int id, [FromBody] CreateMeetingRoomDto dto)
        {
            var room = await _context.MeetingRooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            room.Name = dto.Name;
            room.Location = dto.Location;
            room.Capacity = dto.Capacity;
            room.Equipment = dto.Equipment;
            room.Description = dto.Description;
            room.IsActive = dto.IsActive;
            room.AvailableFrom = TimeSpan.TryParse(dto.AvailableFrom, out var from) ? from : new TimeSpan(9, 0, 0);
            room.AvailableTo = TimeSpan.TryParse(dto.AvailableTo, out var to) ? to : new TimeSpan(18, 0, 0);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// 删除会议室（仅管理员）
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMeetingRoom(int id)
        {
            var room = await _context.MeetingRooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            // 软删除
            room.IsActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// 获取会议室使用记录（仅管理员）
        /// </summary>
        [HttpGet("{id}/usage")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<BookingDto>>> GetRoomUsage(int id, [FromQuery] DateTime? start, [FromQuery] DateTime? end)
        {
            var room = await _context.MeetingRooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            var query = _context.Bookings
                
                .Where(b => b.RoomId == id && b.Status == BookingStatus.Approved);

            if (start.HasValue)
            {
                query = query.Where(b => b.StartTime >= start.Value);
            }

            if (end.HasValue)
            {
                query = query.Where(b => b.EndTime <= end.Value);
            }

            var bookings = await query
                .OrderByDescending(b => b.StartTime)
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
    }
}
