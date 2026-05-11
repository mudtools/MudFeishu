// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Authentication;


namespace Microsoft.Extensions.DependencyInjection;


/// <summary>
/// 飞书用户认证服务扩展方法
/// </summary>
/// <remarks>
/// 提供飞书用户上下文和认证中间件的注册扩展方法。
/// </remarks>
public static class FeishuUserAuthenticationExtensions
{
    /// <summary>
    /// 添加飞书用户上下文服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合实例，支持链式调用</returns>
    /// <remarks>
    /// <para>注册内容：</para>
    /// <list type="bullet">
    ///   <item><description>IFeishuCurrentUserContext - 注册为 Singleton（覆盖 AddFeishuApp 中的默认实现）</description></item>
    ///   <item><description>ICurrentUserContext - 注册为 Singleton（覆盖 AddFeishuApp 中的默认实现）</description></item>
    ///   <item><description>FeishuUserAuthenticationOptions - 配置选项</description></item>
    /// </list>
    /// <para>此方法会覆盖 <see cref="Mud.Feishu.Abstractions.FeishuServiceCollectionExtensions.AddFeishuAppBaseServices"/> 中注册的默认实现，
    /// 应在 <c>services.AddFeishuApp()</c> 之后调用。</para>
    /// <para>使用示例：</para>
    /// <code>
    /// services.AddFeishuApp(builder.Configuration, "FeishuApps");
    /// services.AddFeishuUserContext();
    /// </code>
    /// </remarks>
    public static IServiceCollection AddFeishuUserContext(this IServiceCollection services)
    {
        return services.AddFeishuUserContext(_ => { });
    }

    /// <summary>
    /// 添加飞书用户上下文服务并配置选项
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">配置委托</param>
    /// <returns>服务集合实例，支持链式调用</returns>
    /// <remarks>
    /// <para>注册内容：</para>
    /// <list type="bullet">
    ///   <item><description>IFeishuCurrentUserContext - 注册为 Singleton（覆盖 AddFeishuApp 中的默认实现）</description></item>
    ///   <item><description>ICurrentUserContext - 注册为 Singleton（覆盖 AddFeishuApp 中的默认实现）</description></item>
    ///   <item><description>FeishuUserAuthenticationOptions - 配置选项</description></item>
    /// </list>
    /// <para>此方法会覆盖 <see cref="Mud.Feishu.Abstractions.FeishuServiceCollectionExtensions.AddFeishuAppBaseServices"/> 中注册的默认实现，
    /// 应在 <c>services.AddFeishuApp()</c> 之后调用。</para>
    /// <para>使用示例：</para>
    /// <code>
    /// services.AddFeishuApp(builder.Configuration, "FeishuApps");
    /// services.AddFeishuUserContext(options =>
    /// {
    ///     options.OpenIdClaimType = "custom_open_id";
    ///     options.EnableSensitiveLog = false;
    /// });
    /// </code>
    /// </remarks>
    public static IServiceCollection AddFeishuUserContext(this IServiceCollection services, Action<FeishuUserAuthenticationOptions> configure)
    {
        services.Configure(configure);

        services.AddSingleton<IValidateOptions<FeishuUserAuthenticationOptions>, FeishuUserAuthenticationOptions>();

        services.RemoveAll<IFeishuCurrentUserContext>();
        services.AddSingleton<IFeishuCurrentUserContext, CurrentUserContext>();

        services.RemoveAll<Mud.HttpUtils.ICurrentUserContext>();
        services.AddSingleton<Mud.HttpUtils.ICurrentUserContext, CurrentUserContext>();

        return services;
    }

    /// <summary>
    /// 使用飞书用户认证中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>应用程序构建器实例，支持链式调用</returns>
    /// <remarks>
    /// <para>中间件顺序：</para>
    /// 应在 UseAuthentication() 之后、UseAuthorization() 之前调用：
    /// <code>
    /// app.UseAuthentication();
    /// app.UseFeishuUserAuthentication();  // 添加此中间件
    /// app.UseAuthorization();
    /// </code>
    /// <para>前置条件：</para>
    /// <list type="bullet">
    ///   <item><description>已调用 services.AddFeishuUserContext()</description></item>
    ///   <item><description>已配置 JWT 或其他认证方案</description></item>
    /// </list>
    /// </remarks>
    public static IApplicationBuilder UseFeishuUserAuthentication(this IApplicationBuilder app)
    {
        return app.UseMiddleware<FeishuUserAuthenticationMiddleware>();
    }
}
