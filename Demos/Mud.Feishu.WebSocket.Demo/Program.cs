// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Interceptors;
using Mud.Feishu.Redis.Extensions;
using Mud.Feishu.WebSocket.Demo.Handlers;
using Mud.Feishu.WebSocket.Demo.Interceptors;
using Mud.Feishu.WebSocket.Services;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
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

// 配置Redis分布式去重服务
builder.Services.AddFeishuRedisDeduplicators(builder.Configuration);

// 注册多应用支持
builder.Services.AddFeishuApp(builder.Configuration);

// 配置飞书WebSocket服务（添加拦截器）
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration, "default")
                .AddInterceptor<LoggingEventInterceptor>() // 日志拦截器（内置）
                .AddInterceptor<WebSocketTelemetryInterceptor>() // 遥测拦截器（自定义）
                .AddInterceptor(sp => new RateLimitingInterceptor(
                    sp.GetRequiredService<ILogger<RateLimitingInterceptor>>(),
                    minIntervalMs: 50)) // 限流拦截器（自定义，50ms 间隔）
                .AddHandler<DemoDepartmentEventHandler>()
                .AddHandler<DemoDepartmentDeleteEventHandler>()
                .AddHandler<DemoDepartmentUpdateEventHandler>()
                .Build();

// 配置演示服务
builder.Services.AddSingleton<DemoEventService>();
builder.Services.AddHostedService<DemoEventBackgroundService>();

var app = builder.Build();

app.Run();