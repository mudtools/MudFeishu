// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Webbook.Configuration;
using Mud.Feishu.Webbook.Controllers;
using Mud.Feishu.Webbook.Middleware;
using Mud.Feishu.Webbook.Services;
using Mud.Feishu.Webbook.Models;

namespace Mud.Feishu.Webbook.Extensions;

/// <summary>
/// 服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加飞书 Webbook 事件接收与处理服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFeishuWebbook(
        this IServiceCollection services,
        Action<FeishuWebbookOptions>? configureOptions = null)
    {
        // 配置选项
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            // 尝试从配置文件中读取
            services.Configure<FeishuWebbookOptions>(options =>
            {
                // 默认配置
                options.RoutePrefix = "feishu/webbook";
                options.AutoRegisterEndpoint = true;
                options.EnableRequestLogging = true;
                options.EnableExceptionHandling = true;
                options.EventHandlingTimeoutMs = 30000;
                options.MaxConcurrentEvents = 10;
                options.EnablePerformanceMonitoring = false;
                options.AllowedHttpMethods = new HashSet<string> { "POST" };
                options.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
                options.ValidateSourceIP = false;
                options.AllowedSourceIPs = new HashSet<string>();
            });
        }

        // 注册核心服务
        services.TryAddScoped<IFeishuEventValidator, FeishuEventValidator>();
        services.TryAddScoped<IFeishuEventDecryptor, FeishuEventDecryptor>();
        services.TryAddScoped<IFeishuWebbookService, FeishuWebbookService>();
        
        // 注册性能监控组件
        services.TryAddSingleton<MetricsCollector>();

        // 注册事件处理器工厂（从抽象层）
        services.TryAddScoped<IFeishuEventHandlerFactory, DefaultFeishuEventHandlerFactory>();

        // 添加控制器支持（可选）
        if (services.Any(s => s.ServiceType == typeof(Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider)))
        {
            services.AddControllers()
                .AddApplicationPart(typeof(FeishuWebbookController).Assembly);
        }

        // 添加健康检查支持
        services.AddHealthChecks()
            .AddCheck<FeishuWebbookHealthCheck>("feishu-webbook");

        return services;
    }

    /// <summary>
    /// 添加飞书 Webbook 事件接收与处理服务（带配置节）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="sectionName">配置节名称</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFeishuWebbook(
        this IServiceCollection services,
        string sectionName = "FeishuWebbook")
    {
        services.Configure<FeishuWebbookOptions>(options =>
        {
            // 从配置文件绑定
            var configuration = services.BuildServiceProvider().GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
            configuration.GetSection(sectionName).Bind(options);
        });

        return services.AddFeishuWebbook(null);
    }

    /// <summary>
    /// 添加自定义事件处理器
    /// </summary>
    /// <typeparam name="THandler">事件处理器类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFeishuEventHandler<THandler>(this IServiceCollection services)
        where THandler : class, IFeishuEventHandler
    {
        services.TryAddScoped<IFeishuEventHandler, THandler>();
        return services;
    }

    /// <summary>
    /// 添加多个自定义事件处理器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="handlerTypes">事件处理器类型列表</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFeishuEventHandlers(
        this IServiceCollection services,
        params Type[] handlerTypes)
    {
        foreach (var handlerType in handlerTypes)
        {
            if (typeof(IFeishuEventHandler).IsAssignableFrom(handlerType))
            {
                services.TryAddScoped(typeof(IFeishuEventHandler), handlerType);
            }
            else
            {
                throw new ArgumentException($"Type {handlerType.Name} does not implement IFeishuEventHandler");
            }
        }

        return services;
    }
}

/// <summary>
/// 应用程序构建器扩展方法
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// 使用飞书 Webbook 中间件
    /// </summary>
    /// <param name="builder">应用程序构建器</param>
    /// <param name="configureOptions">配置选项</param>
    /// <returns>应用程序构建器</returns>
    public static IApplicationBuilder UseFeishuWebbook(
        this IApplicationBuilder builder,
        Action<FeishuWebbookOptions>? configureOptions = null)
    {
        // 如果提供了配置选项，则应用它们
        if (configureOptions != null)
        {
            var options = builder.ApplicationServices.GetRequiredService<IOptions<FeishuWebbookOptions>>();
            configureOptions(options.Value);
        }

        var options = builder.ApplicationServices.GetRequiredService<IOptions<FeishuWebbookOptions>>().Value;

        if (options.AutoRegisterEndpoint)
        {
            builder.UseMiddleware<FeishuWebbookMiddleware>();
        }

        return builder;
    }

    /// <summary>
    /// 使用飞书 Webbook 中间件并映射路由
    /// </summary>
    /// <param name="builder">应用程序构建器</param>
    /// <returns>应用程序构建器</returns>
    public static IApplicationBuilder UseFeishuWebbookWithRouting(this IApplicationBuilder builder)
    {
        var options = builder.ApplicationServices.GetRequiredService<IOptions<FeishuWebbookOptions>>().Value;

        builder.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments($"/{options.RoutePrefix}", StringComparison.OrdinalIgnoreCase))
            {
                // 创建中间件实例
                var middleware = new FeishuWebbookMiddleware(
                    next,
                    builder.ApplicationServices.GetRequiredService<IFeishuWebbookService>(),
                    builder.ApplicationServices.GetRequiredService<Microsoft.Extensions.Logging.ILogger<FeishuWebbookMiddleware>>(),
                    builder.ApplicationServices.GetRequiredService<IOptions<FeishuWebbookOptions>>());

                await middleware.InvokeAsync(context);
            }
            else
            {
                await next();
            }
        });

        return builder;
    }
}

/// <summary>
/// 飞书 Webbook 健康检查
/// </summary>
public class FeishuWebbookHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly IFeishuWebbookService _webbookService;

    public FeishuWebbookHealthCheck(IFeishuWebbookService webbookService)
    {
        _webbookService = webbookService;
    }

    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 这里可以添加具体的健康检查逻辑
            // 例如检查服务是否正常运行，依赖项是否可用等
            
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("飞书 Webbook 服务运行正常");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                "飞书 Webbook 服务异常", ex);
        }
    }
}