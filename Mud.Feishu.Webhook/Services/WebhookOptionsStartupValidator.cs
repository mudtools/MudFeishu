// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mud.Feishu.Webhook.Configuration;

namespace Mud.Feishu.Webhook.Services;

/// <summary>
/// 宿主启动期强制构建并校验 <see cref="FeishuWebhookOptions"/> 的托管服务（R3-P0-5）。
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
    /// 使 <c>Validate()</c> 与去重形态检查的失败表现为<b>启动失败</b>而非首个请求失败。
    /// </summary>
    /// <remarks>
    /// 此处<b>故意不捕获异常</b>：配置错误的正确处置是让宿主启动失败，而不是降级放行。
    /// </remarks>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // 取 CurrentValue 即触发 Options 构建（PostConfigure → Validate() + 形态检查）。
        // 结果本身无需使用，故显式丢弃。
        _ = _serviceProvider.GetRequiredService<IOptionsMonitor<FeishuWebhookOptions>>().CurrentValue;

        _logger?.LogDebug("FeishuWebhookOptions 启动期校验通过");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
