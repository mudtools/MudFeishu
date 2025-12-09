// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webbook.Configuration;

namespace Mud.Feishu.Webbook;

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
                var useMiddlewareMethod = typeof(UseMiddlewareExtensions)
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
public class FeishuWebbookHealthCheck : IHealthCheck
{
    private readonly IFeishuWebbookService _webbookService;

    /// <inheritdoc />
    public FeishuWebbookHealthCheck(IFeishuWebbookService webbookService)
    {
        _webbookService = webbookService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return HealthCheckResult.Healthy("飞书 Webbook 服务运行正常");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("飞书 Webbook 服务异常", ex);
        }
    }
}