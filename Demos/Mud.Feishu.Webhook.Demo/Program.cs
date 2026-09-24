// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Interceptors;
using Mud.Feishu.Webhook.Demo;
using Mud.Feishu.Webhook.Demo.Handlers;
using Mud.Feishu.Webhook.Demo.Handlers.MultiApp;
using Mud.Feishu.Webhook.Demo.Interceptors;
using Mud.Feishu.Webhook.Demo.Interceptors.MultiApp;
using Mud.Feishu.Webhook.Demo.Services;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Debug)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Debug)
    .Enrich.FromLogContext()
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

// AOT: 为 Minimal API 的 JSON 请求/响应序列化注入源生成上下文
// （反射序列化在 Native AOT 下不可用，匿名类型已改为具名 DTO 并注册于 WebhookDemoJsonContext）
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Add(WebhookDemoJsonContext.Default);
});

// 注册演示服务
builder.Services.AddSingleton<DemoEventService>();

// 3.0.0-rc3 包已知问题：WHF-R2/C3 将 Webhook 中间件以无工厂 Singleton 注册，
// Development 宿主默认 ValidateOnBuild=true 时因 RequestDelegate 不可从 DI 解析而启动失败。
// 在该修复随新版本发布前（库内已改为工厂注册），这里关闭 ValidateOnBuild（仅影响开发期容器校验）。
builder.Host.UseDefaultServiceProvider(o => o.ValidateOnBuild = false);

// 注册飞书Webhook服务（多应用模式）
// 注意：多应用模式需要配置多个应用，每个应用有独立的配置
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration, "FeishuWebhook")
    // 添加全局拦截器（所有应用共享）
    .AddInterceptor<LoggingEventInterceptor>() // 日志拦截器（内置）
    .AddInterceptor<TelemetryEventInterceptor>(sp => new TelemetryEventInterceptor("Mud.Feishu.Webhook.Demo.MultiApp")) // 遥测拦截器（内置）
    .AddInterceptor<AuditLogInterceptor>() // 审计日志拦截器（自定义）
    .AddInterceptor<PerformanceMonitoringInterceptor>() // 性能监控拦截器（自定义）

    // 全局默认处理器（无 appKey 注册）：3.0.0-rc3 起 Build() 要求至少一个全局注册的处理器，
    // 否则启动失败——应用专属处理器（AddHandler<T>(appKey)）不得充当全局默认处理器。
    .AddHandler<DemoDepartmentEventHandler>()
    .AddHandler<DemoDepartmentDeleteEventHandler>()
    .AddHandler<DemoDepartmentUpdateEventHandler>()

    // 为 App1 添加处理器和拦截器（组织架构相关事件）
    .AddHandler<App1DepartmentEventHandler>("app1")
    .AddHandler<App1DepartmentDeleteEventHandler>("app1")
    .AddHandler<App1DepartmentUpdateEventHandler>("app1")
    .AddInterceptor<App1SpecificInterceptor>("app1") // App1 特定的拦截器

    // 为 App2 添加处理器和拦截器（审批相关事件）
    .AddHandler<App2ApprovalPassedEventHandler>("app2")
    .AddHandler<App2ApprovalRejectedEventHandler>("app2")
    .AddHandler<App2DepartmentDeleteEventHandler>("app2") // App2 部门删除事件处理器
    .AddInterceptor<App2SpecificInterceptor>("app2") // App2 特定的拦截器

    .Build();

var app = builder.Build();

// 添加多应用信息端点
app.MapMultiAppInfo();

// 添加诊断端点
app.MapDiagnostics();

// 添加测试端点（用于捕获飞书回调数据）
app.MapTestEndpoints();

// 添加飞书Webhook限流中间件（可选，推荐在生产环境启用）
app.UseFeishuRateLimit();

// 添加飞书Webhook中间件（自动注册多应用端点）
app.UseFeishuWebhook();

app.Run();
