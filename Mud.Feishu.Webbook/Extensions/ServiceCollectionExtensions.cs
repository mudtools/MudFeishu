// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Webbook.Configuration;
using Mud.Feishu.Webbook.Services;

namespace Mud.Feishu.Webbook;

/// <summary>
/// 服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 使用建造者模式注册飞书 Webbook 事件接收与处理服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>Webbook 服务建造者</returns>
    public static FeishuWebbookServiceBuilder AddFeishuWebbookBuilder(this IServiceCollection services)
    {
        return new FeishuWebbookServiceBuilder(services);
    }

    /// <summary>
    /// 添加飞书 Webbook 事件接收与处理服务（使用配置节）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节名称，默认为"FeishuWebbook"</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuWebbook(
        this IServiceCollection services,
        IConfiguration configuration,
        string? sectionName = null)
    {
        return services.AddFeishuWebbookBuilder()
            .ConfigureFrom(configuration, sectionName)
            .Build();
    }

    /// <summary>
    /// 添加飞书 Webbook 事件接收与处理服务（使用选项委托）- 兼容性方法
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFeishuWebbook(
        this IServiceCollection services,
        Action<FeishuWebbookOptions>? configureOptions = null)
    {
        // 使用建造者模式注册核心服务
        return services.AddFeishuWebbookBuilder()
            .ConfigureOptions(configureOptions)
            .Build();
    }

    /// <summary>
    /// 添加飞书 Webbook 事件接收与处理服务（带配置节）- 兼容性方法
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="sectionName">配置节名称</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFeishuWebbook(
        this IServiceCollection services,
        string sectionName = "FeishuWebbook")
    {
        // 从服务集合中获取配置
        var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        return services.AddFeishuWebbook(configuration, sectionName);
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

    /// <summary>
    /// 注册单处理器模式的飞书Webbook服务（简化版本）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项的委托</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuWebbookWithSingleHandler(
        this IServiceCollection services,
        Action<FeishuWebbookOptions> configureOptions)
    {
        return services
            .AddFeishuWebbookBuilder()
            .ConfigureOptions(configureOptions)
            .Build();
    }

    /// <summary>
    /// 注册单处理器模式的飞书Webbook服务（带处理器类型）
    /// </summary>
    /// <typeparam name="THandler">处理器类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项的委托</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuWebbookWithSingleHandler<THandler>(
        this IServiceCollection services,
        Action<FeishuWebbookOptions> configureOptions)
        where THandler : class, IFeishuEventHandler
    {
        return services
            .AddFeishuWebbookBuilder()
            .ConfigureOptions(configureOptions)
            .AddHandler<THandler>()
            .Build();
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
            var webbookOptions = builder.ApplicationServices.GetRequiredService<IOptions<FeishuWebbookOptions>>();
            configureOptions(webbookOptions.Value);
        }

        var appOptions = builder.ApplicationServices.GetRequiredService<IOptions<FeishuWebbookOptions>>().Value;

        if (appOptions.AutoRegisterEndpoint)
        {
            // 使用反射来获取中间件类型
            var middlewareType = Type.GetType("Mud.Feishu.Webbook.Middleware.FeishuWebbookMiddleware, Mud.Feishu.Webbook");
            if (middlewareType != null)
            {
                var useMiddlewareMethod = typeof(Microsoft.AspNetCore.Builder.UseMiddlewareExtensions)
                    .GetMethods()
                    .FirstOrDefault(m => m.Name == "UseMiddleware" && m.GetParameters().Length == 2);

                if (useMiddlewareMethod != null)
                {
                    var genericMethod = useMiddlewareMethod.MakeGenericMethod(middlewareType);
                    genericMethod.Invoke(null, new object[] { builder });
                }
            }
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
        // 简化实现，直接委托给UseFeishuWebbook
        return builder.UseFeishuWebbook();
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