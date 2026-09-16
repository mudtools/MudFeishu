// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Authentication.MultiApp;

namespace Mud.Feishu.Abstractions;

/// <summary>
/// TMA-09 / P1-8 修复（D7 契约）：按应用构造认证 API 的工厂接口。
/// </summary>
/// <remarks>
/// <para>
/// 认证/取令牌请求此前通过 DI 单例 <c>IFeishuAuthentication</c> 获取，使用默认应用的命名 HttpClient。
/// 本工厂使认证链路也能走 per-app 端点，实现传输归属正确收口。
/// </para>
/// <para>
/// 默认实现 <see cref="PerAppFeishuAuthenticationFactory"/> 基于 <see cref="IFeishuHttpClientFactory.CreateBasic"/>。
/// </para>
/// </remarks>
public interface IFeishuAuthenticationFactory
{
    /// <summary>
    /// 为指定应用创建认证 API 实例。
    /// </summary>
    /// <param name="appKey">应用唯一标识。</param>
    /// <returns>使用该应用命名 HttpClient 的 <see cref="IFeishuAuthentication"/> 实例。</returns>
    IFeishuAuthentication Create(string appKey);
}

/// <summary>
/// TMA-09 默认实现：通过 <see cref="IFeishuHttpClientFactory.CreateBasic"/> 创建 per-app 认证客户端。
/// </summary>
internal sealed class PerAppFeishuAuthenticationFactory : IFeishuAuthenticationFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFeishuHttpClientFactory _httpClientFactory;
    private readonly ILogger<PerAppFeishuAuthenticationFactory>? _logger;

    public PerAppFeishuAuthenticationFactory(
        IServiceProvider serviceProvider,
        IFeishuHttpClientFactory httpClientFactory,
        ILogger<PerAppFeishuAuthenticationFactory>? logger = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger;
    }

    /// <inheritdoc />
    public IFeishuAuthentication Create(string appKey)
    {
        if (string.IsNullOrWhiteSpace(appKey))
            throw new ArgumentException("应用标识不能为空", nameof(appKey));

        var client = _httpClientFactory.CreateBasic(appKey);

        // 使用 ActivatorUtilities 从 DI 构造 IFeishuAuthentication 实例，
        // 传入 per-app 的 IEnhancedHttpClient 作为构造参数。
        // 注意：此处使用 ActivatorUtilities.CreateInstance 有 IL2026 风险（AOT 场景），
        // 若 AOT 门禁不允许，应通过 FeishuAppOptions.EnablePerAppAuthenticationClient=false 降级。
        try
        {
#pragma warning disable IL2050 // ActivatorUtilities.CreateInstance: Types from IServiceClrProvider are statically known
            return ActivatorUtilities.CreateInstance<IFeishuAuthentication>(
                _serviceProvider,
                client);
#pragma warning restore IL2050
        }
        catch (InvalidOperationException)
        {
            // 回退路径：IFeishuAuthentication 注册为 Mock 或已实例化的 Singleton，
            // ActivatorUtilities 无法构造。回退到 DI 解析已注册实例。
            // 此路径下认证请求使用默认应用端点（与 EnablePerAppAuthenticationClient=false 行为一致）。
            _logger?.LogWarning(
                "无法通过 ActivatorUtilities 构造 IFeishuAuthentication 实例（类型为接口或抽象类），" +
                "已回退到 DI 单例。应用 {AppKey} 的认证请求将使用默认应用端点。",
                appKey);
            return _serviceProvider.GetRequiredService<IFeishuAuthentication>();
        }
    }
}
