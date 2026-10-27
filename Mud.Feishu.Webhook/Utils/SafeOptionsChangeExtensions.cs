// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Configuration;

namespace Mud.Feishu.Webhook.Utils;

/// <summary>
/// <see cref="IOptionsMonitor{TOptions}.OnChange"/> 的**唯一**安全包装：保证热更回调零异常外泄。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么必须包装</b>：<c>OptionsMonitor</c> 在配置重载线程上同步调用回调且**自身不兜底**，
/// 回调体内抛出的异常会沿 <c>IConfiguration</c> 的变更通知链上抛到文件监视线程/线程池，
/// 在 .NET Core 上直接<b>终止进程</b>。而回调恰恰最容易踩到"新配置非法"——
/// 任何在回调内读 <c>CurrentValue</c> 的写法，都会在"热更成非法配置"时抛
/// <c>InvalidOperationException</c> / <c>OptionsValidationException</c>。
/// </para>
/// <para>
/// <b>两条纪律</b>：① 回调体内**禁止**读 <c>_options.CurrentValue</c>——只用回调参数给出的新值，
/// 需要"变更前值"时维护快照（构造期用 <c>CurrentValue</c> 播种）；
/// ② 回调一律经本方法注册，不得直接调用 <c>OnChange</c>（由 <c>HotReloadCallbackContractGuards</c> 锁定）。
/// </para>
/// <para>
/// <b>为什么不做成泛型扩展</b>：本模块只监视 <see cref="FeishuWebhookOptions"/> 一种类型；
/// 泛型版本会在 AOT 分析下触发 <c>IL2091</c>（<c>IOptionsMonitor&lt;TOptions&gt;</c> 要求
/// <c>PublicParameterlessConstructor</c> 注解），为三个调用点引入条件编译不值得。
/// </para>
/// </remarks>
internal static class SafeOptionsChangeExtensions
{
    /// <summary>
    /// 注册 <see cref="FeishuWebhookOptions"/> 的变更回调，并保证回调体内的任何异常都不会外泄到配置重载线程。
    /// </summary>
    /// <param name="monitor">配置监视器</param>
    /// <param name="logger">日志（可为 null：无日志宿主下静默丢弃，但仍绝不上抛）</param>
    /// <param name="callback">回调体（新配置 + 变更来源名；<b>禁止</b>在体内读 <c>CurrentValue</c>）</param>
    /// <returns>取消订阅句柄（宿主关停时释放）</returns>
    public static IDisposable? OnChangeSafe(
        this IOptionsMonitor<FeishuWebhookOptions> monitor,
        ILogger? logger,
        Action<FeishuWebhookOptions, string?> callback)
    {
        if (monitor is null) throw new ArgumentNullException(nameof(monitor));
        if (callback is null) throw new ArgumentNullException(nameof(callback));

        return monitor.OnChange((newOptions, name) =>
        {
            try
            {
                callback(newOptions, name);
            }
            catch (Exception ex)
            {
                // 绝不上抛：OnChange 的异常会传播到配置重载的发起线程并终止进程。
                // 既有管道不受影响（旧 Options 实例仍在服务），但失败必须可诊断。
                logger?.LogError(ex,
                    "配置变更回调执行失败（已忽略，既有管道继续使用旧配置）。来源: {ChangeSource}", name);
            }
        });
    }
}
