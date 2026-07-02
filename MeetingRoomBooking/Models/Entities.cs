using System;
using System.Collections.Generic;

namespace MeetingRoomBooking.Models
{
    // 用户角色枚举
    public enum UserRole
    {
        Employee = 0,    // 普通员工
        Admin = 1        // 行政管理员
    }

    // 预约状态枚举
    public enum BookingStatus
    {
        Pending = 0,     // 待审批
        Approved = 1,    // 已批准
        Rejected = 2,    // 已拒绝
        Cancelled = 3    // 已取消
    }

    // 用户实体
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;  // 部门
        public UserRole Role { get; set; } = UserRole.Employee;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastLoginAt { get; set; } = DateTime.MinValue;

        // 导航属性
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }

    // 会议室实体
    public class MeetingRoom
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;  // 位置
        public int Capacity { get; set; }       // 容量
        public string Equipment { get; set; } = string.Empty;  // 设备（如投影仪、白板等）
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public TimeSpan AvailableFrom { get; set; } = new TimeSpan(9, 0, 0);  // 可预约开始时间
        public TimeSpan AvailableTo { get; set; } = new TimeSpan(18, 0, 0);    // 可预约结束时间
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // 导航属性
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }

    // 预约实体
    public class Booking
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int RoomId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Title { get; set; } = string.Empty;  // 会议主题
        public string Description { get; set; } = string.Empty;
        public int AttendeeCount { get; set; } = 0;  // 参会人数
        public BookingStatus Status { get; set; } = BookingStatus.Pending;
        public string RejectionReason { get; set; } = string.Empty;  // 拒绝原因
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.MinValue;
        public int ApprovedBy { get; set; } = 0;  // 审批人ID
        public DateTime ApprovedAt { get; set; } = DateTime.MinValue;

        // 导航属性
        public User User { get; set; }
        public MeetingRoom Room { get; set; }
        public User Approver { get; set; }
    }

    // 会议室使用记录（用于统计）
    public class RoomUsageLog
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public int BookingId { get; set; }
        public DateTime UsedDate { get; set; }
        public decimal DurationHours { get; set; }
        public int AttendeeCount { get; set; }
        public string Department { get; set; } = string.Empty;

        // 导航属性
        public MeetingRoom Room { get; set; }
        public Booking Booking { get; set; }
    }
}
