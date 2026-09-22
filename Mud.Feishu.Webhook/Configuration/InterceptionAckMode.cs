// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook.Configuration;

/// <summary>
/// 事件被前置拦截器中断（拦截器的 <c>BeforeHandleAsync</c> 返回 <c>false</c>）时，
/// 对飞书平台表达的确认语义。
/// </summary>
/// <remarks>
/// <para>
/// 设计口径与 <see cref="NonceFailureMode"/> 同形：这是「安全 / 可用二选一」的宿主级策略，
/// 属于<b>配置</b>而非类型——因此不通过新增拦截器接口（如 <c>IFeishuEventInterceptorV2</c>）来表达，
/// 避免双接口分派与既有实现的破坏性变更。<c>IFeishuEventInterceptor</c> 的
/// <c>BeforeHandleAsync</c> 仍返回 <see cref="bool"/>，其 <c>false</c> 的最终语义由本枚举决定。
/// </para>
/// <para>
/// 语义对照（详见方案 §4.2 的 ACK/HTTP 决策表）：
/// <list type="bullet">
/// <item><description><see cref="Ack"/>（默认）：拦截 = 有意消费 → HTTP 200，事件落去重标记，
/// 飞书<b>不再</b>重推。适用于黑名单、内部限流、重复投递过滤等「已决定不处理」的场景。</description></item>
/// <item><description><see cref="Retryable"/>：拦截 = 暂时无法处理 → HTTP 503，<b>不</b>落去重标记，
/// 由飞书按重推策略稍后重投。适用于「依赖的下游暂不可用，稍后再试」的场景。</description></item>
/// </list>
/// </para>
/// <para>
/// 两种终态都会写入指标（<c>intercepted</c> / <c>intercepted_retryable</c>），并可由
/// <c>AfterHandleAsync</c> 通过 <c>EventHandlingOutcomeException.OutcomeKind</c> 判别。
/// </para>
/// </remarks>
public enum InterceptionAckMode
{
    /// <summary>
    /// 已消费（默认）：响应 HTTP 200，事件写入去重完成标记，飞书不再重推。
    /// </summary>
    Ack = 0,

    /// <summary>
    /// 要求重推：响应 HTTP 503，不写入去重标记，由飞书稍后重投本事件。
    /// </summary>
    /// <remarks>
    /// 注意：此模式下事件<b>不会</b>写入失败事件存储（<c>IFailedEventStore</c>）——
    /// 拦截不是业务失败，重推由飞书平台侧的重推策略驱动，而非 SDK 侧的失败重投服务。
    /// </remarks>
    Retryable = 1
}
