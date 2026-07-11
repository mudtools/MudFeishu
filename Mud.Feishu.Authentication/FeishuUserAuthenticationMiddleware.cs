// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions;
using Mud.HttpUtils;
using System.Diagnostics;
using System.Security.Claims;

namespace Mud.Feishu.Authentication;

/// <summary>
/// 飞书用户认证中间件
/// </summary>
/// <remarks>
/// <para>功能说明：</para>
/// <list type="bullet">
///   <item><description>从已认证的 ClaimsPrincipal 中提取飞书用户信息</description></item>
///   <item><description>设置 IFeishuCurrentUserContext 供后续请求处理使用</description></item>
///   <item><description>设置 IAppContextHolder 默认应用上下文作用域，确保 TokenManager 模式的 API 调用无需显式 BeginScope</description></item>
///   <item><description>请求结束后自动清理用户上下文和应用上下文作用域</description></item>
/// </list>
/// <para>中间件位置：</para>
/// 应放在 AuthenticationMiddleware 之后、AuthorizationMiddleware 之前：
/// <code>
/// app.UseAuthentication();
/// app.UseFeishuUserAuthentication();  // 此飞书用户认证中间件
/// app.UseAuthorization();
/// </code>
/// <para>默认 Claims 类型（可通过 FeishuUserAuthenticationOptions 自定义）：</para>
/// <list type="bullet">
///   <item><description>open_id - 飞书用户 OpenId（备选：ClaimTypes.NameIdentifier）</description></item>
///   <item><description>union_id - 飞书用户 UnionId</description></item>
///   <item><description>user_id - 业务系统用户ID</description></item>
///   <item><description>ClaimTypes.Name - 用户名称</description></item>
/// </list>
/// </remarks>
/// <remarks>
/// 初始化中间件
/// </remarks>
/// <param name="next">下一个中间件委托</param>
/// <param name="options">配置选项</param>
/// <param name="logger">日志记录器</param>
/// <exception cref="ArgumentNullException">当参数为 null 时抛出</exception>
public class FeishuUserAuthenticationMiddleware(
    RequestDelegate next,
    IOptions<FeishuUserAuthenticationOptions> options,
    ILogger<FeishuUserAuthenticationMiddleware> logger)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly FeishuUserAuthenticationOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<FeishuUserAuthenticationMiddleware> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// 处理 HTTP 请求
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <param name="userContext">用户上下文</param>
    public async Task InvokeAsync(HttpContext context, IFeishuCurrentUserContext userContext)
    {
        // 使用 Activity 进行分布式追踪
        using var activity = _options.EnableDistributedTracing
            ? FeishuUserAuthenticationActivitySource.Source.StartActivity("FeishuUserAuthentication", ActivityKind.Internal)
            : null;

        var user = context.User;
        IDisposable? appContextScope = null;

        if (user?.Identity?.IsAuthenticated == true)
        {
            // 提取用户信息（使用配置的 Claim 类型）
            var openId = ExtractClaimValue(user, _options.OpenIdClaimType, _options.OpenIdFallbackClaimType);
            var unionId = ExtractClaimValue(user, _options.UnionIdClaimType);
            var userId = ExtractClaimValue(user, _options.UserIdClaimType);
            var name = ExtractClaimValue(user, _options.NameClaimType);

            if (!string.IsNullOrEmpty(openId))
            {
                userContext.SetUser(openId!, unionId, userId, name);

                activity?.SetTag("user.open_id", openId);
                activity?.SetTag("user.union_id", unionId ?? "N/A");

                var logOpenId = _options.EnableSensitiveLog ? openId : MaskSensitiveInfo(openId!);
                var logUnionId = _options.EnableSensitiveLog ? unionId ?? "N/A" : (unionId != null ? MaskSensitiveInfo(unionId) : "N/A");
                // 使用 userContext.UserId 获取有效 UserId（当 Claim 中未提供 user_id 时回退到 OpenId）
                var effectiveUserId = userContext.UserId;
                var logUserId = effectiveUserId != null
                    ? (_options.EnableSensitiveLog ? effectiveUserId : MaskSensitiveInfo(effectiveUserId))
                    : "N/A";

                _logger.LogDebug("用户上下文已设置: OpenId={OpenId}, UnionId={UnionId}, UserId={UserId}",
                    logOpenId, logUnionId, logUserId);

                // 设置默认应用上下文作用域：当 IAppContextHolder.Current 为 null 时，
                // 自动切换到默认应用上下文，使 TokenManager 模式的飞书 API 调用无需显式 BeginScope。
                // 若 Current 已被显式设置（如多应用场景下通过 UseApp/BeginScope 切换），则不覆盖。
                appContextScope = TryBeginDefaultAppScope(context.RequestServices);
            }
            else
            {
                _logger.LogDebug("已认证用户但未找到 open_id claim");
                activity?.SetTag("user.authenticated", true);
                activity?.SetTag("user.open_id_missing", true);
            }
        }

        try
        {
            await _next(context);
        }
        finally
        {
            // 释放应用上下文作用域，恢复之前的上下文状态
            appContextScope?.Dispose();

            try
            {
                userContext.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理用户上下文时发生异常");
            }
        }
    }

    /// <summary>
    /// 尝试为当前请求创建默认应用上下文作用域。
    /// <para>
    /// 仅当 IAppContextHolder.Current 为 null 且 IFeishuAppManager 已注册时，
    /// 使用 BeginScope 切换到默认应用上下文。返回的 IDisposable 在请求结束时释放以恢复原状态。
    /// </para>
    /// </summary>
    /// <param name="serviceProvider">请求作用域的服务提供器</param>
    /// <returns>应用上下文作用域的 IDisposable；若无需创建则返回 null</returns>
    private IDisposable? TryBeginDefaultAppScope(IServiceProvider serviceProvider)
    {
        try
        {
            var appContextHolder = serviceProvider.GetService<IAppContextHolder>();
            var feishuAppManager = serviceProvider.GetService<IFeishuAppManager>();

            if (appContextHolder == null || feishuAppManager == null)
                return null;

            // 仅当当前上下文未被显式设置时才创建作用域，避免覆盖多应用场景下的显式切换
            if (appContextHolder.Current != null)
                return null;

            var defaultApp = feishuAppManager.GetDefaultApp();
            return appContextHolder.BeginScope(defaultApp);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "设置默认应用上下文作用域时发生异常，后续 TokenManager 模式 API 调用可能需要显式 BeginScope");
            return null;
        }
    }

    /// <summary>
    /// 提取 Claim 值
    /// </summary>
    /// <param name="principal">Claims 主体</param>
    /// <param name="claimTypes">Claim 类型（支持多个，按顺序尝试）</param>
    /// <returns>Claim 值，未找到返回 null</returns>
    private static string? ExtractClaimValue(ClaimsPrincipal principal, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = principal.FindFirst(claimType)?.Value;
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }
        return null;
    }

    /// <summary>
    /// 脱敏处理敏感信息
    /// </summary>
    /// <param name="value">原始值</param>
    /// <returns>脱敏后的值</returns>
    private static string MaskSensitiveInfo(string value) => Abstractions.Utilities.SensitiveDataUtils.MaskSensitiveData(value);

    /// <summary>
    /// ActivitySource 用于分布式追踪
    /// </summary>
    private static class FeishuUserAuthenticationActivitySource
    {
        public static readonly ActivitySource Source = new("Mud.Feishu.Authentication.UserContext");
    }
}
