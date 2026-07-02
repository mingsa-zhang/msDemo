using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using CoreDemo2.Services;
using System.IO;
using System.Threading.Tasks;

namespace CoreDemo2
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            // 注册字符串转义服务
            services.AddScoped<IStringEscapeService, StringEscapeService>();

            // 添加Swagger文档支持
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "CoreDemo2 API", Version = "v1" });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CoreDemo2 API v1"));
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();

            // 启用静态文件
            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // 默认路由处理
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/")
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    var indexPath = Path.Combine(env.ContentRootPath, "wwwroot", "index.html");

                    if (File.Exists(indexPath))
                    {
                        var content = File.ReadAllText(indexPath);
                        await context.Response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes(content));
                        return;
                    }
                }
                await next();
            });
        }
    }
}