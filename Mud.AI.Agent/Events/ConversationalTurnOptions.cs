// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.Abstractions.Approvals;
using Mud.AI.Abstractions.Conversations;

namespace Mud.AI.Agent.Events;

/// <summary>
/// 回合管线的<b>产品事实与产品策略</b>注入面（① 参数化 + ③ 委托注入的选项形态）。
/// </summary>
/// <remarks>
/// <para>
/// <b>承载两类内容</b>：
/// <list type="bullet">
/// <item><b>产品事实</b>（① 式）：状态键字面量（outbox 补发键是 <c>feishu.agent.*</c> 产品命名面，
/// 字面量归产品侧所有，本类型只以参数承载）；</item>
/// <item><b>产品策略</b>（③ 式）：请求准入校验（多租户分级等）、投递可行性判定、流式目标解析、
/// 用户消息装配、审批待确认文案——这些在产品侧是<b>可覆写钩子</b>（virtual），管线以委托消费，
/// 由薄壳把自身虚方法以方法组形态注入（虚分派在每次调用时发生 ⇒ 派生类覆写自动生效）。</item>
/// </list>
/// </para>
/// <para>
/// <b>各成员的缺省语义</b>（<see langword="null"/> = 使用管线内建默认）：
/// 状态键为空 ⇒ outbox 补发整体禁用；策略委托为空 ⇒ 使用管线内建默认实现
/// （与产品薄壳的历史默认行为逐字节一致）。
/// </para>
/// </remarks>
public sealed class ConversationalTurnOptions
{
    /// <summary>
    /// P4-4（outbox）：「已生成但未确认送达」的回复文本状态键（<b>产品事实</b>，与
    /// <see cref="PendingReplyTurnStateKey"/> 成对）。<see langword="null"/> ⇒ outbox 整体禁用。
    /// </summary>
    public string? PendingReplyStateKey { get; init; }

    /// <summary>
    /// P4-4（outbox）：<see cref="PendingReplyStateKey"/> 所属轮次的标识状态键（<b>产品事实</b>）。
    /// </summary>
    public string? PendingReplyTurnStateKey { get; init; }

    /// <summary>
    /// 请求准入校验（多租户分级等产品策略；可抛异常 fail-fast）。
    /// </summary>
    /// <remarks>
    /// 在<b>任何</b>其他阶段（会话键构造、闸门、会话存取）之前执行——
    /// 与既有行为一致：appKey 缺失的 fail-fast 发生在建立会话作用域之前。
    /// </remarks>
    public Action<ConversationRequest>? ValidateRequestPolicies { get; init; }

    /// <summary>
    /// 下发可行性前置判定（R2-01）：返回非空 = 本轮不可投递（管线按「已消费」短路）。
    /// <see langword="null"/> ⇒ 无前置判定（保持既有默认行为）。
    /// </summary>
    public Func<ConversationRequest, CancellationToken, Task<string?>>? FindDeliveryBlockAsync { get; init; }

    /// <summary>
    /// 流式回复目标解析（R5-3 两段式：通道解析器优先，回退 chat_id 解析）。
    /// <see langword="null"/> ⇒ 管线内建默认。
    /// </summary>
    public Func<ConversationRequest, string?>? ResolveStreamTarget { get; init; }

    /// <summary>
    /// 流式回复的目标会话 ID 解析（chat_id 优先、群聊回退会话主体）。
    /// <see langword="null"/> ⇒ 管线内建默认。
    /// </summary>
    public Func<ConversationRequest, string?>? ResolveStreamTargetChatId { get; init; }

    /// <summary>
    /// 用户消息装配（装配器循环 + 触发文本拼装；R2-01b：触发文本<b>必须</b>进入模型）。
    /// <see langword="null"/> ⇒ 管线内建默认（与产品薄壳的历史默认实现逐字节一致）。
    /// </summary>
    public Func<ConversationRequest, CancellationToken, Task<string>>? AssembleUserMessageAsync { get; init; }

    /// <summary>
    /// 「等待人工确认」的用户可见答复文案（不含任何批准要素——fail-closed 前提）。
    /// <see langword="null"/> ⇒ 管线内建默认文案。
    /// </summary>
    public Func<IReadOnlyList<FrameworkToolApprovalRequest>, string>? BuildApprovalPendingReply { get; init; }
}
