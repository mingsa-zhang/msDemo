using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using MeetingRoomBooking.Data;
using MeetingRoomBooking.Extensions;
using MeetingRoomBooking.Models;
using MeetingRoomBooking.Services;

var builder = WebApplication.CreateBuilder(args);

// 添加服务
builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddControllers();

// 注册JWT服务
builder.Services.AddScoped<IJwtService, JwtService>();

var app = builder.Build();

// 自动创建数据库和初始化数据
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();

    var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();

    // 创建默认管理员账号（如果不存在）
    if (!dbContext.Users.Any(u => u.Role == UserRole.Admin))
    {
        var admin = new User
        {
            Username = "admin",
            Email = "admin@company.com",
            PasswordHash = jwtService.HashPassword("admin123"),
            Department = "行政部",
            Role = UserRole.Admin,
            CreatedAt = DateTime.Now
        };
        dbContext.Users.Add(admin);
        dbContext.SaveChanges();

        System.Console.WriteLine("默认管理员账号已创建 - 用户名: admin, 密码: admin123");
    }

    // 创建默认会议室（如果不存在）
    if (!dbContext.MeetingRooms.Any())
    {
        var rooms = new[]
        {
            new MeetingRoom
            {
                Name = "第一会议室",
                Location = "1楼",
                Capacity = 10,
                Equipment = "投影仪、白板",
                Description = "小型会议室",
                IsActive = true,
                AvailableFrom = new TimeSpan(9, 0, 0),
                AvailableTo = new TimeSpan(18, 0, 0),
                CreatedAt = DateTime.Now
            },
            new MeetingRoom
            {
                Name = "第二会议室",
                Location = "2楼",
                Capacity = 20,
                Equipment = "投影仪、音响系统、白板",
                Description = "中型会议室",
                IsActive = true,
                AvailableFrom = new TimeSpan(9, 0, 0),
                AvailableTo = new TimeSpan(18, 0, 0),
                CreatedAt = DateTime.Now
            },
            new MeetingRoom
            {
                Name = "第三会议室",
                Location = "3楼",
                Capacity = 50,
                Equipment = "投影仪、音响系统、视频会议设备",
                Description = "大型会议室",
                IsActive = true,
                AvailableFrom = new TimeSpan(8, 0, 0),
                AvailableTo = new TimeSpan(20, 0, 0),
                CreatedAt = DateTime.Now
            }
        };

        dbContext.MeetingRooms.AddRange(rooms);
        dbContext.SaveChanges();

        System.Console.WriteLine("已创建3个默认会议室");
    }
}

// 配置HTTP请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "会议室预约管理系统 API v1");
        c.RoutePrefix = string.Empty; // 设置Swagger为根路径
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// 提供静态文件（前端页面）
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();
