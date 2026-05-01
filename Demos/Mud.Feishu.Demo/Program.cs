// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Authentication;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        rollOnFileSizeLimit: true,
        fileSizeLimitBytes: 10 * 1024 * 1024, // 10 MB
        retainedFileCountLimit: 7, // 保留 7 天的日志
        encoding: System.Text.Encoding.UTF8
    ));

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
#if net8 || net9
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName);
});
#endif

#if net10
builder.Services.AddOpenApi();
#endif

// 添加飞书服务 - 使用多应用模式
builder.Services.AddFeishuApp(builder.Configuration, "FeishuApps");

// 注册API服务
builder.Services.CreateFeishuServicesBuilder()
                .AddModules(FeishuModule.All)
                .Build();

// 使用 Mud.Feishu.Authentication 中的用户上下文服务
builder.Services.AddFeishuUserContext(o =>
{
    o.OpenIdClaimType = "open_id"; // 根据飞书的用户信息调整
    o.UnionIdClaimType = "union_id"; // 根据飞书的用户信息调整
    o.UserIdClaimType = "user_id";
    o.NameClaimType = "name";
    o.EnableSensitiveLog = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
#if net8 || net9
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
    });

    // 重定向根路径到 Swagger UI
    app.MapGet("/", () => Results.Redirect("/swagger/index.html"));
#endif
}

app.UseHttpsRedirection();
app.UseAuthentication();
// 使用飞书用户认证中间件
app.UseFeishuUserAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();