// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FeishuFileServer.Data;
using FeishuFileServer.Extensions;
using FeishuFileServer.Middleware;
using FeishuFileServer.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 配置 Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "FeishuFileServer")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .WriteTo.File(
        path: "logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 30
    )
    .CreateLogger();

builder.Host.UseSerilog();

// 添加 appsettings.local.json 配置文件（用于本地开发配置）
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddFeishuServices(builder.Configuration);
builder.Services.AddCorsConfiguration(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// 添加飞书用户上下文服务
builder.Services.AddFeishuUserContext(o =>
{
    o.OpenIdClaimType = "open_id";
    o.UnionIdClaimType = "union_id";
    o.UserIdClaimType = "user_id";
    o.NameClaimType = "name";
    o.EnableSensitiveLog = true;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FeishuFileDbContext>();

    var dbPath = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(dbPath))
    {
        var dataSourceMatch = System.Text.RegularExpressions.Regex.Match(dbPath, @"Data Source=(.+?)(?:;|$)");
        if (dataSourceMatch.Success)
        {
            var dbFilePath = dataSourceMatch.Groups[1].Value;
            var dbDirectory = Path.GetDirectoryName(dbFilePath);
            if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
            {
                Directory.CreateDirectory(dbDirectory);
                Log.Information("创建数据库目录: {Directory}", dbDirectory);
            }
        }
    }

    await dbContext.Database.MigrateAsync();

    // 初始化管理员用户
    await InitializeAdminUserAsync(dbContext);
}

async Task InitializeAdminUserAsync(FeishuFileDbContext dbContext)
{
    if (!await dbContext.Users.AnyAsync(u => u.Role == UserRole.Admin))
    {
        var adminUser = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            DisplayName = "管理员",
            Email = "admin@example.com",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(adminUser);
        await dbContext.SaveChangesAsync();
    }
}

if (app.Environment.IsDevelopment())
{
    // OpenAPI 文档端点由 Microsoft.AspNetCore.OpenApi 包提供（.NET 9+ 使用 AddOpenApi()/MapOpenApi()）
}

app.UseGlobalExceptionHandler();

app.UseRateLimiter(100);

app.UseCors("DefaultPolicy");

app.UseAuthentication();
app.UseFeishuUserAuthentication(); // 飞书用户认证中间件
app.UseAuthorization();

app.MapControllers();

try
{
    Log.Information("Starting FeishuFileServer...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
