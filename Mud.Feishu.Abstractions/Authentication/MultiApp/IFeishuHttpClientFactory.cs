// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mud.Feishu.Abstractions.Authentication.MultiApp;

/// <summary>
/// 飞书 HTTP 客户端工厂，负责为指定应用构造 <see cref="IEnhancedHttpClient"/>。
/// </summary>
/// <remarks>
/// <para>
/// <b>ARC-2 Step 1</b>：此前 <c>FeishuAppManager.CreateAppContext</c> 在内部散装手工装配
/// （8 处 <c>GetService</c> + 2 处 <c>new</c>），与 <c>AddMudHttpClient</c> 的注册路径形成配置双源。
/// 本接口把装配收敛到唯一实现 <see cref="FeishuHttpClientFactory"/>，
/// 使「注册路径」与「创建路径」共享同一份 <see cref="EnhancedHttpClientOptions"/> 基线，
/// 同时为多应用配置热更新（ARC-1）提供可替换的客户端创建入口。
/// </para>
/// <para>
/// <b>为什么不能完全改用组件 <c>IEnhancedHttpClientFactory.CreateClient</c></b>：
/// <c>TokenRecoveryEnhancedClient</c> 是 <c>sealed</c>，唯一构造函数要求
/// <c>IHttpClientFactory + clientName + TokenRecoveryExecutor</c>，<b>无法包装</b>一个既有的
/// <see cref="IEnhancedHttpClient"/>；且组件未提供任何把令牌恢复挂进 DI/HttpClient 管道的扩展方法。
/// 因此「手工 new」是组件当前的唯一可行接入方式，本接口收敛的是<b>装配参数来源</b>，而非 new 动作本身。
/// </para>
/// </remarks>
public interface IFeishuHttpClientFactory
{
    /// <summary>
    /// 创建带令牌恢复（401 自动恢复）装饰的客户端，供业务 API 使用。
    /// </summary>
    /// <param name="appKey">应用唯一标识。</param>
    /// <param name="recoveryExecutor">令牌恢复执行器。</param>
    /// <returns>带恢复能力的增强 HTTP 客户端。</returns>
    IEnhancedHttpClient Create(string appKey, TokenRecoveryExecutor recoveryExecutor);

    /// <summary>
    /// 创建不带令牌恢复（401 自动恢复）装饰的基础客户端。
    /// </summary>
    /// <remarks>
    /// 适用于「不得递归触发令牌请求」的场景（例如使用方自行调用认证接口获取令牌）。
    /// <para>
    /// <b>注意</b>：<c>IFeishuAuthentication</c> 的注册路径（ARC-6）已改为从 DI 解析源生成的实现，
    /// 其 HttpClient 由 <c>AddMudHttpClient</c> 提供，<b>不再经由本方法</b>；
    /// 本方法当前没有仓库内的生产调用方，保留为装配能力出口。
    /// </para>
    /// </remarks>
    /// <param name="appKey">应用唯一标识。</param>
    /// <returns>基础增强 HTTP 客户端。</returns>
    IEnhancedHttpClient CreateBasic(string appKey);

    /// <summary>
    /// 构建指定应用的 <see cref="EnhancedHttpClientOptions"/>。
    /// 以 DI 中注册的 <see cref="EnhancedHttpClientOptions"/> 编程式基线为准，
    /// 再叠加 DI 解析的拦截器/掩码器/日志器与 per-client 配置覆盖，
    /// 与组件 <c>CreateEnhancedClient</c> 的装配结果保持一致。
    /// </summary>
    EnhancedHttpClientOptions CreateOptions(string appKey);
}

/// <summary>
/// <see cref="IFeishuHttpClientFactory"/> 的默认实现。
/// </summary>
public class FeishuHttpClientFactory : IFeishuHttpClientFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 初始化 <see cref="FeishuHttpClientFactory"/> 实例。
    /// </summary>
    public FeishuHttpClientFactory(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    /// <inheritdoc />
    public IEnhancedHttpClient Create(string appKey, TokenRecoveryExecutor recoveryExecutor)
    {
        if (recoveryExecutor == null) throw new ArgumentNullException(nameof(recoveryExecutor));

        return new TokenRecoveryEnhancedClient(
            _serviceProvider.GetRequiredService<IHttpClientFactory>(),
            BuildClientName(appKey),
            recoveryExecutor,
            _serviceProvider.GetService<IEncryptionProvider>(),
            CreateOptions(appKey));
    }

    /// <inheritdoc />
    public IEnhancedHttpClient CreateBasic(string appKey)
        => new HttpClientFactoryEnhancedClient(
            _serviceProvider.GetRequiredService<IHttpClientFactory>(),
            BuildClientName(appKey),
            _serviceProvider.GetService<IEncryptionProvider>(),
            CreateOptions(appKey));

    /// <inheritdoc />
    public EnhancedHttpClientOptions CreateOptions(string appKey)
    {
        var clientName = BuildClientName(appKey);

        // ARC-2：以 DI 中注册的 EnhancedHttpClientOptions 编程式配置为基线（与 AddMudHttpClient 路径同源），
        // 避免此前"手工 new EnhancedHttpClientOptions"导致 10 个字段静默取默认值的问题。
        var baseline = _serviceProvider.GetService<IOptions<EnhancedHttpClientOptions>>()?.Value;
        var options = baseline != null ? Clone(baseline) : new EnhancedHttpClientOptions();

        options.Logger = _serviceProvider.GetService<ILogger<HttpClientFactoryEnhancedClient>>() ?? options.Logger;
        options.RequestInterceptors = _serviceProvider.GetServices<IHttpRequestInterceptor>();
        options.ResponseInterceptors = _serviceProvider.GetServices<IHttpResponseInterceptor>();
        options.SensitiveDataMasker = _serviceProvider.GetService<ISensitiveDataMasker>() ?? options.SensitiveDataMasker;

        var optionsMonitor = _serviceProvider.GetService<IOptionsMonitor<MudHttpClientApplicationOptions>>();
        if (optionsMonitor != null && optionsMonitor.CurrentValue.Clients.TryGetValue(clientName, out var clientOptions))
        {
            options.AllowCustomBaseUrls = clientOptions.AllowCustomBaseUrls;
        }

        return options;
    }

    /// <summary>
    /// 构建命名客户端名称，与 <c>AddMudHttpClient</c> 注册时使用的名称保持一致。
    /// </summary>
    internal static string BuildClientName(string appKey) => $"feishu-{appKey}";

    /// <summary>
    /// 复制基线配置。
    /// <see cref="EnhancedHttpClientOptions"/> 为 sealed 且未提供公开克隆方法，
    /// 此处按字段显式复制；新增字段时必须同步补充，并由
    /// <c>FeishuHttpClientFactoryTests</c> 的字段等价性测试守护。
    /// </summary>
    private static EnhancedHttpClientOptions Clone(EnhancedHttpClientOptions source) => new()
    {
        Logger = source.Logger,
        RequestInterceptors = source.RequestInterceptors,
        ResponseInterceptors = source.ResponseInterceptors,
        SensitiveDataMasker = source.SensitiveDataMasker,
        AllowCustomBaseUrls = source.AllowCustomBaseUrls,
        RequestBodySerialization = source.RequestBodySerialization,
        ExceptionRedactor = source.ExceptionRedactor,
        MaxExceptionContentLength = source.MaxExceptionContentLength,
        CaptureRequestContent = source.CaptureRequestContent,
        UrlResolution = source.UrlResolution,
        MaxSuccessResponseBytes = source.MaxSuccessResponseBytes,
        HttpRequestMessageOptions = source.HttpRequestMessageOptions,
        // Mud.HttpUtils 2.0.5 新增：应用访问授权器。由 FeishuHttpClientFactoryTests 的
        // 属性契约守卫发现——若不在此同步，该能力会在 MudFeishu 路径上被静默丢弃。
        AppAccessAuthorizer = source.AppAccessAuthorizer,
#if NET6_0_OR_GREATER
        HttpVersion = source.HttpVersion,
        HttpVersionPolicy = source.HttpVersionPolicy,
#endif
#if NET8_0_OR_GREATER
        JsonTypeInfoResolver = source.JsonTypeInfoResolver,
#endif
    };
}
