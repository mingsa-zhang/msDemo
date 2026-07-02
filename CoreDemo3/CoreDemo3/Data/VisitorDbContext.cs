using CoreDemo3.Models;
using Microsoft.EntityFrameworkCore;

namespace CoreDemo3.Data
{
    /// <summary>
    /// 访客数据库上下文
    /// </summary>
    public class VisitorDbContext : DbContext
    {
        public VisitorDbContext(DbContextOptions<VisitorDbContext> options) : base(options)
        {
        }

        public DbSet<Visitor> Visitors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置Visitor实体
            modelBuilder.Entity<Visitor>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Phone)
                    .IsRequired()
                    .HasMaxLength(11);

                entity.Property(e => e.IdCard)
                    .IsRequired()
                    .HasMaxLength(18);

                entity.Property(e => e.VisitReason)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.VisitedPerson)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.AccessCode)
                    .IsRequired()
                    .HasMaxLength(6);

                // 为AccessCode创建索引以提高查询性能
                entity.HasIndex(e => e.AccessCode)
                    .IsUnique();

                // 为Phone和IdCard创建索引
                entity.HasIndex(e => e.Phone);
                entity.HasIndex(e => e.IdCard);

                // 为CreatedTime创建索引以支持时间范围查询
                entity.HasIndex(e => e.CreatedTime);
            });
        }
    }
}