// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.Abstractions.Approvals;
using Mud.AI.Agent.Events;

namespace Mud.AI.Agent.Approvals;

/// <summary>
/// 工具人工确认（HITL）的<b>批准回灌续跑</b>闭环（R5-11）：同键加载会话 → 取框架记录的原始审批请求 →
/// 重建工具执行上下文 → 经框架绑定层续跑并落库。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么必须代劳</b>：续跑轮不在工具执行上下文内（<c>AsyncLocal</c> 唯一建立点在事件层）——宿主即使正确回灌批准，工具仍会 100% 结构化拒绝。
/// <b>参数注入而非构造注入</b>：依赖显式、构造面零变更；闸门经产品委托（<paramref name="acquireGate"/>）消费，中立侧不命名闸门类型，满额上抛的产品异常原样传播。
/// </para>
/// <para>
/// <b>fail-closed 边界（全部显式抛错，绝不静默降级或凭空放行）</b>：<paramref name="scopeFactory"/> 为 <see langword="null"/> ⇒ 装配缺陷抛错；
/// <paramref name="approval"/> 缺 <c>RequestId</c>/<c>ConversationKey</c>/<c>TenantKey</c> ⇒ <see cref="ArgumentException"/>；
/// 框架已不再记录该 <c>RequestId</c> ⇒ <see cref="InvalidOperationException"/>（引导用户重新发起，绝不凭空构造批准响应）。
/// </para>
/// <para>
/// <b>续跑轮可能再产出新审批请求</b>：经 <paramref name="approvalChannel"/> 再次提交宿主（通道缺席即只记 Warning）；
/// <b>回复下发不在闭环内</b>——投递属派生事件处理器职责，本方法只负责模型轮次与会话落库。
/// </para>
/// </remarks>
public static class AgentApprovalContinuation
{
    /// <summary>
    /// 回灌一次人工批准（或拒绝）并续跑同一会话（R5-11）。
    /// </summary>
    /// <param name="agent">目标 Agent（须与发起确认时同一实例/同一会话存储）。</param>
    /// <param name="sessions">会话取/建/存端口（发起轮与续跑轮必须同键）。</param>
    /// <param name="scopeFactory">产品作用域端口（续跑轮必须重建租户上下文）。</param>
    /// <param name="approval">宿主批准通道收到的框架审批请求要素（须原样回传）。</param>
    /// <param name="approved">是否批准。<see langword="false"/> 同样走完整流程（拒绝结果回填模型）。</param>
    /// <param name="reason">批准/拒绝原因（可空；会回填给模型，不得包含敏感信息）。</param>
    /// <param name="acquireGate">会话闸门获取委托（可空；产品侧传入其闸门实现以与事件路径同款串行）。</param>
    /// <param name="approvalChannel">宿主批准通道（可空；续跑轮再产出审批请求时使用）。</param>
    /// <param name="logger">日志（可空）。</param>
    /// <param name="pendingApprovalStore">
    /// 待确认快照存储（可空）。传入时启用<b>幂等消费</b>——同一 <c>RequestId</c> 的批准只生效一次；
    /// 不存在 / 已过期 / 已被消费三者一律 fail-closed（丢弃迟到批准）。未传入则保持既有行为。
    /// </param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>续跑轮的模型回应（含可能新增的审批请求内容）。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="agent"/>、<paramref name="sessions"/> 或 <paramref name="approval"/> 为 null。</exception>
    /// <exception cref="ArgumentException">审批请求要素不完整（缺 RequestId / ConversationKey / TenantKey）。</exception>
    /// <exception cref="InvalidOperationException">产品作用域缺失，或该审批请求已失效（fail-closed）。</exception>
    public static async Task<AgentResponse> RunAsync(
        AIAgent agent,
        IAgentSessionFacade sessions,
        ITurnScopeFactory scopeFactory,
        FrameworkToolApprovalRequest approval,
        bool approved,
        string? reason = null,
        Func<string, CancellationToken, Task<IDisposable?>>? acquireGate = null,
        IToolApprovalChannel? approvalChannel = null,
        ILogger? logger = null,
        IPendingApprovalStore? pendingApprovalStore = null,
        CancellationToken cancellationToken = default)
    {
        if (agent is null)
        {
            throw new ArgumentNullException(nameof(agent));
        }

        if (sessions is null)
        {
            throw new ArgumentNullException(nameof(sessions));
        }

        if (scopeFactory is null)
        {
            // 装配缺陷显式抛错：静默跳过上下文会让续跑轮的写工具 fail-closed 拒绝，
            // 表现为「批准了却不生效」且仅日志可见——宁可调用点直接失败。
            throw new InvalidOperationException(
                $"续跑人工确认需要 {nameof(ITurnScopeFactory)} 以重建租户执行上下文（批准 ≠ 放行）——"
                    + "请从容器解析产品实现后传入");
        }

        if (approval is null)
        {
            throw new ArgumentNullException(nameof(approval));
        }

        if (string.IsNullOrWhiteSpace(approval.RequestId))
        {
            throw new ArgumentException("审批请求缺少 RequestId——框架绑定层按它把批准绑定到原始请求", nameof(approval));
        }

        if (string.IsNullOrWhiteSpace(approval.ConversationKey))
        {
            throw new ArgumentException("审批请求缺少 ConversationKey——无法定位待续跑的会话", nameof(approval));
        }

        if (string.IsNullOrWhiteSpace(approval.TenantKey))
        {
            throw new ArgumentException("审批请求缺少 TenantKey——多租户隔离禁止默认租户兜底", nameof(approval));
        }

        var conversationKey = approval.ConversationKey!;

        IDisposable? gateHandle = null;
        try
        {
            if (acquireGate is not null)
            {
                gateHandle = await acquireGate(conversationKey, cancellationToken).ConfigureAwait(false);
            }

            var session = await sessions.GetOrCreateSessionAsync(conversationKey, cancellationToken).ConfigureAwait(false);

            var pendingState = new PendingApprovalState(sessions.HistoryStateKey);
            if (!pendingState.TryFind(session, approval.RequestId!, out var pending) || pending is null)
            {
                // 会话可能已被坏值自愈重建、该确认已被放弃（R5-12）、或已回灌过。
                // 绝不凭空构造批准响应：那会把「人类批准过的调用」重新指向一个未被展示过的工具调用。
                throw new InvalidOperationException(
                    $"待人工确认的工具调用已失效（requestId: {approval.RequestId}）——"
                        + "会话可能已被重建，或该确认已被放弃/已处理。请引导用户重新发起该操作（fail-closed，不凭空放行）");
            }

            // R7 / C4a：快照幂等消费（仅在宿主装配了 IPendingApprovalStore 时生效）。
            // 放在「框架记录已确认存在」之后：此时续跑必然发生，消费失败只可能是
            // 「不存在 / 已过期 / 已被消费」三种情形 —— 一律按「迟到的批准」丢弃（fail-closed，绝不重放）。
            if (pendingApprovalStore is not null)
            {
                var consumable = await pendingApprovalStore
                    .TryConsumeAsync(approval.TenantKey!, approval.RequestId!, cancellationToken)
                    .ConfigureAwait(false);

                if (!consumable)
                {
                    throw new InvalidOperationException(
                        $"待人工确认项已过期或已被处理（requestId: {approval.RequestId}）——"
                            + "同一确认只生效一次；过期项自动放弃，不重放（fail-closed）");
                }
            }

            // CreateResponse 由框架记录的原始请求派生：CallId 与入参同源，
            // 免去宿主自行拼装 ToolApprovalResponseContent 时「调用与批准不一致」的风险。
            var responseContent = pending.CreateResponse(approved, reason);
            var messages = new List<ChatMessage>(1) { new(ChatRole.User, [responseContent]) };

            // 续跑轮不在事件流内：工具执行上下文必须在此显式重建（否则工具 fail-closed 结构化拒绝）。
            using var _toolScope = scopeFactory.BeginToolScope(new TurnScopeContext(
                approval.TenantKey,
                conversationKey,
                approval.ChatId,
                approval.UserId));

            var response = await agent
                .RunAsync(messages, session, options: null, cancellationToken)
                .ConfigureAwait(false);

            // 续跑轮可能再产出审批请求（模型连发多个写工具）：与事件层同款口径提交宿主 + 记 Warning。
            var nextPending = ApprovalRequestProjector.FromMessages(
                response.Messages,
                approval.TenantKey,
                conversationKey,
                approval.ChatId,
                approval.UserId);

            if (nextPending.Count > 0)
            {
                await ApprovalRequestProjector
                    .NotifyAsync(approvalChannel, pendingApprovalStore, logger, nextPending, cancellationToken)
                    .ConfigureAwait(false);
            }

            // 落库含框架绑定层在本次续跑中消费掉的审批记录与新生效的历史（失败即抛出，重试幂等：
            // 框架在运行失败时不消费记录，宿主持同一 approval 再次调用即可）。
            await sessions.SaveSessionAsync(conversationKey, session, cancellationToken).ConfigureAwait(false);

            return response;
        }
        finally
        {
            gateHandle?.Dispose();
        }
    }

}
