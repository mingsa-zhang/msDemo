using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MeetingRoomBooking.Models.DTOs
{
    // 登录请求DTO
    public class LoginRequestDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    // 注册请求DTO
    public class RegisterRequestDto
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [StringLength(100)]
        public string Department { get; set; } = string.Empty;
    }

    // 认证响应DTO
    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public UserDto User { get; set; } = new UserDto();
    }

    // 用户DTO
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public UserRole Role { get; set; }
    }

    // 会议室DTO
    public class MeetingRoomDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string Equipment { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string AvailableFrom { get; set; } = string.Empty;
        public string AvailableTo { get; set; } = string.Empty;
    }

    // 创建/更新会议室DTO
    public class CreateMeetingRoomDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [Range(1, 100)]
        public int Capacity { get; set; }

        [StringLength(500)]
        public string Equipment { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public string AvailableFrom { get; set; } = string.Empty;
        public string AvailableTo { get; set; } = string.Empty;
    }

    // 预约DTO
    public class BookingDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int AttendeeCount { get; set; }
        public BookingStatus Status { get; set; }
        public string RejectionReason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string ApproverName { get; set; } = string.Empty;
    }

    // 创建预约DTO
    public class CreateBookingDto
    {
        [Required]
        public int RoomId { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Range(1, 100)]
        public int AttendeeCount { get; set; }
    }

    // 审批预约DTO
    public class ApproveBookingDto
    {
        [Required]
        public bool Approved { get; set; }

        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
    }

    // 会议室空闲时间查询DTO
    public class RoomAvailabilityQueryDto
    {
        public int RoomId { get; set; }
        public DateTime Date { get; set; }
    }

    // 会议室可用时间段DTO
    public class RoomAvailabilityDto
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public List<TimeSlotDto> AvailableSlots { get; set; } = new List<TimeSlotDto>();
        public List<BookingDto> ExistingBookings { get; set; } = new List<BookingDto>();
    }

    // 时间段DTO
    public class TimeSlotDto
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsAvailable { get; set; }
    }

    // 使用率统计DTO
    public class UsageStatisticsDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalBookings { get; set; }
        public int ApprovedBookings { get; set; }
        public decimal OverallUtilizationRate { get; set; }
        public List<RoomUsageDto> RoomUsages { get; set; } = new List<RoomUsageDto>();
        public List<DepartmentUsageDto> DepartmentUsages { get; set; } = new List<DepartmentUsageDto>();
    }

    // 会议室使用统计DTO
    public class RoomUsageDto
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public int BookingCount { get; set; }
        public decimal TotalHours { get; set; }
        public decimal UtilizationRate { get; set; }
    }

    // 部门使用统计DTO
    public class DepartmentUsageDto
    {
        public string Department { get; set; } = string.Empty;
        public int BookingCount { get; set; }
        public decimal TotalHours { get; set; }
        public double Percentage { get; set; }
    }
}
