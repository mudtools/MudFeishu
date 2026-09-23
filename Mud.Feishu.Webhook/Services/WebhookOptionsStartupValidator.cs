// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Utils;

namespace Mud.Feishu.Webhook.Services;

/// <summary>
/// 宿主启动期强制构建并校验 <see cref="FeishuWebhookOptions"/>、并输出**启动 Summary 日志**的托管服务。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它</b>：<c>FeishuWebhookOptions</c> 经 <c>IOptionsMonitor</c> <b>惰性</b>构建，
/// 因此 PostConfigure 中的 <c>Validate()</c> 与去重形态检查（R3-P0-1 / R3-P0-4）默认只在
/// <b>首个请求</b>时才执行——配置错误表现为"服务已起来并已开始收流量，然后第一个请求 500"，
/// 比启动失败更糟。
/// </para>
/// <para>
/// 修复前，Options 之所以"恰好"在启动期被构建，只是因为
/// <see cref="FeishuWebhookConcurrencyService"/> 的构造函数顺手读了一次
/// <c>CurrentValue</c>——一个无人知晓的隐式副作用。本类型把这件事变成<b>显式机制</b>。
/// </para>
/// <para>
/// <b>为什么不用 <c>AddOptions&lt;T&gt;().ValidateOnStart()</c></b>：该扩展方法在 net6.0 目标下
/// 同时存在于 <c>Microsoft.Extensions.Hosting</c> 引用程序集与 <c>Microsoft.Extensions.Options</c>
/// 包中，产生 <c>CS0121</c> 二义性错误。托管服务方式在所有 TFM（含 netstandard2.0）上行为一致。
/// </para>
/// <para>
/// <b>R3-FEAT-4</b>：本服务同时承担"启动 Summary 日志"（每进程一次）——打印去重实现形态、
/// 时间戳容差与重放窗口不变量校验结果，与 R3-P0-1 的启动期阻断、健康检查的数据项
/// 共同构成"启动可见 + 运行可见 + 指标可见"三层。
/// </para>
/// </remarks>
internal sealed class WebhookOptionsStartupValidator : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WebhookOptionsStartupValidator>? _logger;

    public WebhookOptionsStartupValidator(
        IServiceProvider serviceProvider,
        ILogger<WebhookOptionsStartupValidator>? logger = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger;
    }

    /// <summary>
    /// 在宿主启动时强制构建一次 <see cref="FeishuWebhookOptions"/>，
    /// 使 <c>Validate()</c> 与去重形态检查的失败表现为<b>启动失败</b>而非首个请求失败；
    /// 随后输出一次启动 Summary 日志。
    /// </summary>
    /// <remarks>
    /// 此处<b>故意不捕获异常</b>：配置错误的正确处置是让宿主启动失败，而不是降级放行。
    /// </remarks>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // 取 CurrentValue 即触发 Options 构建（PostConfigure → Validate() + 形态检查）。
        // 结果本身无需使用，故显式丢弃。
        var options = _serviceProvider.GetRequiredService<IOptionsMonitor<FeishuWebhookOptions>>().CurrentValue;

        _logger?.LogDebug("FeishuWebhookOptions 启动期校验通过");

        WriteStartupSummary(options);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 输出一次启动 Summary（R3-FEAT-4）：去重实现形态、时间戳容差、重放窗口不变量。
    /// </summary>
    /// <remarks>
    /// <b>不变量</b>：<c>NonceTtl ≥ TimestampToleranceSeconds</c>——否则 Nonce 已过期、
    /// 时间戳容差窗口尚未关闭的区间内重放攻击可行。该不变量跨工程（TTL 属 Redis /
    /// `FeishuDeduplication` 节，容差属 Webhook）无法在单一库内联断言，此处以启动日志显式提示。
    /// </remarks>
    private void WriteStartupSummary(FeishuWebhookOptions options)
    {
        var nonceForm = _serviceProvider.GetService<IFeishuNonceDistributedDeduplicator>()
            is null or FeishuNonceDistributedDeduplicator
            ? "InMemory"
            : "Distributed";

        var eventForm = _serviceProvider.GetService<IFeishuEventDeduplicator>()
            is null or FeishuEventDeduplicator
            ? "InMemory"
            : "Distributed";

        var tolerance = options.TimestampToleranceSeconds;
        var nonceTtl = options.NonceTtlSeconds;
        var invariantOk = nonceTtl is null || nonceTtl.Value >= tolerance;

        _logger?.LogInformation(
            "飞书 Webhook 启动自检 | Nonce 去重: {NonceForm} | 事件去重: {EventForm} | " +
            "时间戳容差: {Tolerance}s | Nonce TTL: {NonceTtl} | 重放窗口不变量(TTL ≥ 容差): {Invariant} | 应用数: {AppCount}",
            nonceForm,
            eventForm,
            tolerance,
            nonceTtl?.ToString() ?? "(未配置)",
            invariantOk ? "满足" : "**不满足**",
            options.Apps.Count);

        if (!invariantOk)
        {
            _logger?.LogWarning(
                "重放窗口不变量不满足：Nonce TTL {NonceTtl}s < 时间戳容差 {Tolerance}s。" +
                "在 Nonce 过期后、容差窗口结束前的区间内重放攻击可行，请将 NonceTtl 调至不小于容差",
                nonceTtl!.Value, tolerance);
        }

        WriteRegistrySummary();
    }

    /// <summary>
    /// 输出一次"处理器/拦截器注册自检"汇总（§8.3）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>与 §8.3 原文的偏离（有意为之）</b>：原文要求"检出'声明了非空
    /// <c>SupportedEventType</c> 但从未在任一 app 下注册'的处理器并告警"。该判定
    /// <b>不可实现</b>：<c>SupportedEventType</c> 是实例属性，<c>Build()</c> 阶段既无实例、
    /// 也无法从根容器解析 <c>Scoped</c> 处理器（会触发 Captive Dependency 校验）。
    /// 强行实现只会产出误报。
    /// </para>
    /// <para>
    /// <b>替代方案（等价运维价值、零误报）</b>：
    /// ① 启动期打印"每个 app 注册了哪些处理器类型"——注册错 app 一眼可见；
    /// ② 运行期由 R3-P1-1 在事件真正被全部跳过时输出 Warning（精确命中，无误报）。
    /// 二者组合覆盖"处理器配错导致事件静默消失"的全部可观测需求。
    /// </para>
    /// </remarks>
    private void WriteRegistrySummary()
    {
        var handlerRegistry = _serviceProvider.GetService<FeishuWebhookHandlerRegistry>();
        var interceptorRegistry = _serviceProvider.GetService<FeishuWebhookInterceptorRegistry>();
        if (handlerRegistry is null && interceptorRegistry is null)
            return;

        var appKeys = new List<string>();
        if (handlerRegistry != null)
            appKeys.AddRange(handlerRegistry.GetAllAppKeys());
        if (interceptorRegistry != null)
            appKeys.AddRange(interceptorRegistry.GetAllAppKeys());

        foreach (var appKey in appKeys.Distinct(StringComparer.Ordinal))
        {
            var handlers = handlerRegistry?.GetHandlers(appKey) ?? Array.Empty<Type>();
            var interceptors = interceptorRegistry?.GetInterceptors(appKey) ?? Array.Empty<Type>();

            _logger?.LogInformation(
                "应用 {AppKey} 注册自检：处理器 [{Handlers}]，拦截器 [{Interceptors}]",
                appKey,
                handlers.Count == 0 ? "(无)" : string.Join(", ", handlers.Select(t => t.Name)),
                interceptors.Count == 0 ? "(无)" : string.Join(", ", interceptors.Select(t => t.Name)));
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
