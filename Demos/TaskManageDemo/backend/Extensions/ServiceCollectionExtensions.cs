// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Mud.Feishu.Abstractions.Interceptors;
using TaskManageDemo.Backend.Data;
using TaskManageDemo.Backend.EventHandlers;
using TaskManageDemo.Backend.Interceptors;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Services;
using TaskManageDemo.Backend.Services.Approval;
using TaskManageDemo.Backend.Services.Auth;
using TaskManageDemo.Backend.Services.Background;
using TaskManageDemo.Backend.Services.Caching;
using TaskManageDemo.Backend.Services.Feishu;
using TaskManageDemo.Backend.Services.Search;
using TaskManageDemo.Backend.Services.Statistics;
using TaskManageDemo.Backend.Services.Sync;
using TaskManageDemo.Backend.Services.Templates;
using TaskManageDemo.Backend.Services.Transaction;
using TaskManageDemo.Backend.Services.History;

namespace TaskManageDemo.Backend.Extensions;

/// <summary>
/// 服务集合扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加数据库服务
    /// </summary>
    public static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=TaskManage.db";

        services.AddDbContext<TaskManageDbContext>(options =>
        {
            if (connectionString.Contains(":memory:") || connectionString.Contains("InMemory"))
            {
                options.UseInMemoryDatabase("TestDatabase");
            }
            else
            {
                options.UseSqlite(connectionString, sqliteOptions =>
                {
                    sqliteOptions.MigrationsAssembly("TaskManageDemo.Backend");
                });
            }

            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
        });

        return services;
    }

    /// <summary>
    /// 添加飞书服务
    /// </summary>
    public static IServiceCollection AddFeishuServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var feishuAppsSection = configuration.GetSection("FeishuApps");
        if (!feishuAppsSection.Exists())
        {
            return services;
        }

        services.AddFeishuApp(configuration, "FeishuApps");

        services.CreateFeishuServicesBuilder()
            .AddModules(FeishuModule.Organization, FeishuModule.Approval)
            .AddTaskApi()
            .AddMessageApi()
            .Build();

        return services;
    }

    /// <summary>
    /// 添加Webhook服务
    /// </summary>
    public static IServiceCollection AddWebhookServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.CreateFeishuWebhookServiceBuilder(configuration, "FeishuWebhook")
            .AddInterceptor<LoggingEventInterceptor>()
            .AddInterceptor<TelemetryEventInterceptor>(sp => new TelemetryEventInterceptor("TaskManageDemo.Backend"))
            .AddInterceptor<AuditLogInterceptor>()
            .AddInterceptor<PerformanceMonitoringInterceptor>()
            .AddHandler<UserCreatedEventHandler>()
            .AddHandler<UserUpdatedEventHandler>()
            .AddHandler<FeishuTaskUpdatedEventHandler>()
            .AddHandler<FeishuApprovalInstanceEventHandler>()
            .Build();

        return services;
    }

    /// <summary>
    /// 添加业务服务
    /// </summary>
    public static IServiceCollection AddBusinessServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 飞书服务
        services.AddScoped<IFeishuTaskService, FeishuTaskService>();
        services.AddScoped<IFeishuTaskListService, FeishuTaskListService>();
        services.AddScoped<IFeishuNotificationService, FeishuNotificationService>();
        services.AddScoped<IFeishuAuthService, FeishuAuthService>();
        services.AddScoped<ITaskSyncService, TaskSyncService>();
        services.AddScoped<ITaskSearchService, TaskSearchService>();
        services.AddScoped<ITaskTemplateService, TaskTemplateService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IApprovalService, ApprovalService>();
        services.AddScoped<IFeishuApproval, FeishuApprovalAdapter>();
        services.AddScoped<IDepartmentSyncService, DepartmentSyncService>();
        services.AddScoped<IFeishuDepartmentApi, FeishuDepartmentApiAdapter>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<ILocalAuthService, LocalAuthService>();

        // HttpClient
        services.AddHttpClient();

        // 事务服务
        services.AddScoped<ITransactionService, TransactionService>();

        // 任务历史服务
        services.AddScoped<ITaskHistoryService, TaskHistoryService>();

        // 任务服务（带缓存装饰器）
        services.AddScoped<TaskService>();
        services.AddScoped<ITaskService>(sp =>
        {
            var innerService = sp.GetRequiredService<TaskService>();
            var cache = sp.GetRequiredService<IMemoryCache>();
            var logger = sp.GetRequiredService<ILogger<CachedTaskService>>();
            return new CachedTaskService(innerService, cache, logger);
        });

        // OAuth 和 JWT 服务
        var oauthOptions = configuration.GetSection("OAuth").Get<OAuthOptions>() ?? new OAuthOptions();
        services.AddSingleton<IStateStorageService>(_ =>
            new StateStorageService(TimeSpan.FromMinutes(oauthOptions.StateExpirationMinutes)));

        services.AddSingleton<IJwtTokenService>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OAuthOptions>>().Value;
            return new JwtTokenService(options.Jwt);
        });

        // 注册后台服务
        services.AddHostedService<StateCleanupService>();

        services.AddScheduledTasks();

        return services;
    }
}
