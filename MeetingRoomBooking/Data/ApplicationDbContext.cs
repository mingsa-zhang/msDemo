using Microsoft.EntityFrameworkCore;
using MeetingRoomBooking.Models;

namespace MeetingRoomBooking.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            // 禁用外键约束
            Database.SetCommandTimeout(30);
        }

        public DbSet<User> Users { get; set; }
        public DbSet<MeetingRoom> MeetingRooms { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<RoomUsageLog> RoomUsageLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User配置
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Department).HasMaxLength(100);

                // 忽略导航属性
                entity.Ignore(e => e.Bookings);
            });

            // MeetingRoom配置
            modelBuilder.Entity<MeetingRoom>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Location).HasMaxLength(200);
                entity.Property(e => e.Equipment).HasMaxLength(500);
                entity.Property(e => e.Description).HasMaxLength(500);

                // 忽略导航属性
                entity.Ignore(e => e.Bookings);
            });

            // Booking配置 - 不配置外键关系
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasIndex(e => new { e.RoomId, e.StartTime, e.EndTime });
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Status);

                entity.Property(e => e.Title).HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.RejectionReason).HasMaxLength(500);

                // 忽略导航属性，避免外键约束
                entity.Ignore(e => e.User);
                entity.Ignore(e => e.Room);
                entity.Ignore(e => e.Approver);
            });

            // RoomUsageLog配置 - 不配置外键关系
            modelBuilder.Entity<RoomUsageLog>(entity =>
            {
                entity.HasIndex(e => e.RoomId);
                entity.HasIndex(e => e.UsedDate);

                // 忽略导航属性
                entity.Ignore(e => e.Room);
                entity.Ignore(e => e.Booking);
            });
        }
    }
}
