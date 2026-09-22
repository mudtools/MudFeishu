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

        // TMR2-P1-1：per-app 客户端必须装配到**真实实现类型**上。
        // 修复前使用 ActivatorUtilities.CreateInstance<IFeishuAuthentication>(...)——
        // T 为接口时该方法恒抛 InvalidOperationException（无公共构造可选中），
        // 于是每次都走 catch 降级到 GetRequiredService<IFeishuAuthentication>()（默认应用端点），
        // 使 EnablePerAppAuthenticationClient=true（默认）名存实亡：
        // 非默认应用的取令牌请求被发往默认应用端点（多区域部署下即凭据发往错区域）。
        //
        // 生成的实现 Internal.FeishuAuthentication 与本体同程序集，可编译期直引，
        // 无需反射 ⇒ 无 IL2026/IL3050/IL2072 增量，AOT 与 netstandard2.0 全 TFM 可用。
        // 构造参数与生成 ctor 顺序一致：
        //   (IEnhancedHttpClient httpClient, IHttpRequestExecutor executor,
        //    IHttpResponseCache? cacheProvider = null, IResiliencePolicyResolver? resilienceResolver = null,
        //    IHttpContentSerializer? contentSerializer = null, ILogger? logger = null)
        // 生成器若变更参数集，此处**编译期失败**（正向），另有守卫 8 提供更明确的失败信息。
        //
        // 依赖解析：IHttpRequestExecutor / IHttpContentSerializer 由 AddMudHttpClient 与
        // 生成注册（AddAuthenticationWebApiHttpClient）提供；IHttpResponseCache / IResiliencePolicyResolver /
        // ILogger<T> 为可空可选项，未注册时传 null 与生成类默认值语义一致。
        // 说明：本工厂为 Singleton 且持有根 IServiceProvider；上述依赖在注册侧均为
        // Transient/Singleton（非 Scoped），ValidateScopes=true 的容器下解析安全。
        try
        {
            return new Internal.FeishuAuthentication(
                client,
                _serviceProvider.GetRequiredService<IHttpRequestExecutor>(),
                _serviceProvider.GetService<IHttpResponseCache>(),
                _serviceProvider.GetService<IResiliencePolicyResolver>(),
                _serviceProvider.GetService<IHttpContentSerializer>(),
                _serviceProvider.GetService<ILogger<Internal.FeishuAuthentication>>());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // 装配失败必须显式失败（fail-fast），不得静默回退到默认应用端点——
            // “装配不上就用错端点”正是本缺陷的原始形态。此处仅补充 AppKey 上下文以便定位。
            _logger?.LogError(ex,
                "per-app 认证客户端装配失败（应用 {AppKey}）。请检查 IHttpRequestExecutor 等基础服务是否已注册" +
                "（AddFeishuApp 会注册），或改用 FeishuAppOptions.EnablePerAppAuthenticationClient=false 显式选择默认端点。",
                appKey);
            throw;
        }
    }
}
