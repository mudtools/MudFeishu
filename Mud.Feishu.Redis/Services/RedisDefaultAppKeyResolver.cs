// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Redis.Configuration;

namespace Mud.Feishu.Redis.Services;

/// <summary>
/// 「默认应用 AppKey」解析的单一出口（R2-04/R2-09）。
/// </summary>
/// <remarks>
/// <para>
/// 解析优先级：<c>FeishuApps</c> 中 <c>IsDefault = true</c> 的应用 &gt; 首个应用 &gt;
/// <see cref="RedisOptions.AppKey"/>（默认 <c>default</c>）。
/// </para>
/// <para>
/// 消费者：① SeqID 的 <c>scopeKey</c> 合成；② <c>RedisTokenStore</c>/<c>RedisUserTokenStore</c>
/// 具体类型注册的键前缀（ADR-14：与工厂同前缀，避免"按类型解析"写到另一套键空间）。
/// </para>
/// <para>
/// 设计约束：必须发生在**解析期**（工厂委托内）——文档化的调用顺序是「Redis 先于 <c>AddFeishuApp</c>」，
/// 绑定期拿不到应用列表；读取 <see cref="IOptionsMonitor{T}"/>（与 <c>FeishuAppManager</c> 同形）而非
/// <c>IFeishuAppManager</c>，避免触发默认应用懒加载装配的副作用，且用 <c>GetService</c>
/// 保证宿主未接多应用时不硬失败。
/// </para>
/// </remarks>
internal static class RedisDefaultAppKeyResolver
{
    /// <summary>
    /// 解析默认应用 AppKey（绝不返回空白串）。
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="options">Redis 配置（最终回落值来源）</param>
    /// <returns>默认应用 AppKey；无多应用配置时回落 <see cref="RedisOptions.AppKey"/></returns>
    public static string Resolve(IServiceProvider serviceProvider, RedisOptions options)
    {
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        var appConfigs = serviceProvider.GetService<IOptionsMonitor<List<FeishuAppConfig>>>()?.CurrentValue
            ?? serviceProvider.GetService<IOptions<List<FeishuAppConfig>>>()?.Value;

        var defaultAppKey = appConfigs?.FirstOrDefault(c => c.IsDefault)?.AppKey
            ?? appConfigs?.FirstOrDefault()?.AppKey;

        if (!string.IsNullOrWhiteSpace(defaultAppKey))
            return defaultAppKey!;

        return string.IsNullOrWhiteSpace(options.AppKey) ? "default" : options.AppKey;
    }
}
