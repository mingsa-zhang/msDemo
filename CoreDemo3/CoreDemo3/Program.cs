using CoreDemo3.Data;
using CoreDemo3.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// 配置SQLite数据库
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? "Data Source=visitor.db";
builder.Services.AddDbContext<VisitorDbContext>(options =>
    options.UseSqlite(connectionString));

// 注册服务
builder.Services.AddScoped<IVisitorService, VisitorService>();

// 添加Swagger支持
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "访客管理系统 API",
        Version = "v1",
        Description = "自助终端访客信息登记和通行码生成系统"
    });
});

// 添加跨域支持
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 确保数据库已创建
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<VisitorDbContext>();
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 启用静态文件服务（用于前端页面）
app.UseStaticFiles();

// 启用跨域
app.UseCors("AllowAll");

app.UseRouting();

app.MapControllers();

// 默认路由重定向到首页
app.MapGet("/", () => Results.Redirect("/index.html"));

// 健康检查端点
app.MapGet("/health", () => new
{
    Status = "Healthy",
    Timestamp = DateTime.Now,
    Version = "1.0.0"
});

app.Run();
