// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Agents;

/// <summary>
/// 工具人工确认（HITL）的<b>批准回灌续跑</b>闭环（R5-11）。
/// </summary>
/// <remarks>
/// <para>
/// <b>要解决的问题</b>：写工具经 MEAI <c>ApprovalRequiredAIFunction</c> 包装后由框架
/// <c>FunctionInvokingChatClient</c> 在调用前转成审批请求，SDK 把要素交给宿主批准通道；但「批准之后如何继续」
/// 此前完全靠宿主自行领悟——而续跑轮<b>不在事件流内</b>，<c>IFeishuToolContextAccessor</c> 的
/// <c>AsyncLocal</c> 执行上下文（唯一建立点在 <c>ConversationalFeishuEventHandler</c>）并不存在，
/// 于是宿主即使正确回灌批准，工具仍会因缺少租户上下文而 100% 结构化拒绝——
/// <see cref="Mud.Feishu.AI.Events.ConversationalFeishuEventHandler{T}"/> 向用户承诺的「确认后会自动继续」不可达。
/// </para>
/// <para>
/// <b>本方法把四个易错点收进 SDK 一次调用</b>：同键加载会话 → 取框架记录的原始审批请求
/// （<b>批准资格的唯一权威</b>：MAF 绑定层在发起轮写入会话状态袋，按 <c>RequestId</c> 绑定人类响应）→
/// 重建工具执行上下文（<b>批准 ≠ 放行</b>，多租户隔离禁止默认 appKey 兜底，TMA2-20）→ 经框架绑定层续跑并落库。
/// </para>
/// <para>
/// <b>设计约束（R4-6 / R5 §0.5 否决项 4）</b>：不向 <see cref="FeishuAgent"/> 构造函数注入任何工具链依赖——
/// 扩展方法 + 调用点显式传参，依赖显式、构造面零变更，也不改变事件层的分层定位。
/// </para>
/// </remarks>
public static class FeishuAgentApprovalExtensions
{
    /// <summary>
    /// 回灌一次人工批准（或拒绝）并续跑同一会话（R5-11）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>调用时机</b>：宿主在自有界面（飞书卡片/工单/审批单）拿到人的决定后调用<b>一次</b>；
    /// 实现内不得阻塞等待人类操作（<c>RequestFrameworkApprovalAsync</c> 的会话语义即「登记后异步返回」）。
    /// </para>
    /// <para>
    /// <b>会话串行</b>：续跑轮与事件轮共享同一会话状态，传入 <paramref name="conversationGate"/>
    /// （<c>AddFeishuAgent</c> 默认注册的实例）可与事件路径同款闸门串行；为 <see langword="null"/>
    /// 时不串行化，由调用方自行保证互斥（与事件层 <c>ConversationGate is null</c> 的既有语义同口径）。
    /// </para>
    /// <para>
    /// <b>续跑轮可能再产出新的审批请求</b>（模型连发多个写工具）：此时经 <paramref name="approvalChannel"/>
    /// 再次提交宿主（fail-closed 语义与事件层完全一致——通道缺席即只记 Warning，绝不自动放行），
    /// 返回的 <see cref="AgentResponse"/> 文本为空，宿主应提示用户「仍需人工确认」。
    /// </para>
    /// <para>
    /// <b>宿主仍需自己把回复投递给用户</b>：回复通道属派生事件处理器（<c>ReplyAsync</c>），
    /// 本方法只负责模型轮次与会话落库。
    /// </para>
    /// <para><b>fail-closed 边界（全部显式抛错，绝不静默降级或凭空放行）</b>：</para>
    /// <list type="bullet">
    /// <item><paramref name="toolContextAccessor"/> 为 <see langword="null"/> ⇒ 装配缺陷，显式抛错；</item>
    /// <item><paramref name="approval"/> 的关键要素（<c>RequestId</c>/<c>ConversationKey</c>/<c>AppKey</c>）缺失
    /// ⇒ <see cref="ArgumentException"/>——无键即无法定位会话与绑定；</item>
    /// <item>框架已不再记录该 <c>RequestId</c>（会话已重建、该确认已被放弃或已回灌过）⇒
    /// <see cref="InvalidOperationException"/>——<b>绝不凭空构造批准响应</b>，请引导用户重新发起。</item>
    /// </list>
    /// </remarks>
    /// <param name="agent">目标 Agent（须与发起确认时同一实例/同一会话存储）。</param>
    /// <param name="toolContextAccessor">工具执行上下文访问器（宿主持有；续跑轮必须重建租户上下文）。</param>
    /// <param name="approval">宿主批准通道收到的框架审批请求要素（须原样回传）。</param>
    /// <param name="approved">是否批准。<see langword="false"/> 同样走完整流程（拒绝结果回填模型）。</param>
    /// <param name="reason">批准/拒绝原因（可空；会回填给模型，不得包含敏感信息）。</param>
    /// <param name="conversationGate">会话闸门（可空；建议传入与事件层同款实例以保证同会话串行）。</param>
    /// <param name="approvalChannel">宿主批准通道（可空；续跑轮再产出审批请求时使用）。</param>
    /// <param name="logger">日志（可空）。</param>
    /// <param name="pendingApprovalStore">
    /// R7 / C4a：待确认快照存储（可空）。传入时启用<b>幂等消费</b>——同一 <c>RequestId</c> 的批准只生效一次；
    /// 不存在 / 已过期 / 已被消费三者一律 fail-closed（丢弃迟到批准）。未传入则保持既有行为。
    /// </param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>续跑轮的模型回应（含可能新增的审批请求内容）。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="agent"/> 或 <paramref name="approval"/> 为 null。</exception>
    /// <exception cref="ArgumentException">审批请求要素不完整（缺 RequestId / ConversationKey / AppKey）。</exception>
    /// <exception cref="InvalidOperationException">工具上下文访问器缺失，或该审批请求已失效（fail-closed）。</exception>
    public static async Task<AgentResponse> RunApprovalContinuationAsync(
        this FeishuAgent agent,
        IFeishuToolContextAccessor toolContextAccessor,
        FrameworkToolApprovalRequest approval,
        bool approved,
        string? reason = null,
        IConversationGate? conversationGate = null,
        IFeishuToolApprovalChannel? approvalChannel = null,
        ILogger? logger = null,

        // R7 / C4a：待确认快照存储（可空 ⇒ 不做幂等消费，保持既有行为）。
        // 追加为**末尾可选参数**：既有调用点全部使用命名实参，源码兼容。
        IFeishuPendingApprovalStore? pendingApprovalStore = null,
        CancellationToken cancellationToken = default)
    {
        if (agent is null)
        {
            throw new ArgumentNullException(nameof(agent));
        }

        if (toolContextAccessor is null)
        {
            // 装配缺陷显式抛错：静默跳过上下文会让续跑轮的写工具 fail-closed 拒绝，
            // 表现为「批准了却不生效」且仅日志可见——宁可启动期/调用点直接失败。
            throw new InvalidOperationException(
                $"续跑人工确认需要 {nameof(IFeishuToolContextAccessor)} 以重建租户执行上下文（批准 ≠ 放行，TMA2-20）——"
                + "请从容器解析（FeishuTools 的 AddFeishuTools* 已 Singleton 注册）后传入");
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

        if (string.IsNullOrWhiteSpace(approval.AppKey))
        {
            throw new ArgumentException("审批请求缺少 AppKey——多租户隔离禁止默认应用兜底（TMA2-20）", nameof(approval));
        }

        var conversationKey = approval.ConversationKey!;

        IConversationGateHandle? gateHandle = null;
        try
        {
            if (conversationGate is not null)
            {
                gateHandle = await conversationGate.AcquireAsync(conversationKey, cancellationToken).ConfigureAwait(false);
            }

            var session = await agent.GetOrCreateSessionAsync(conversationKey, cancellationToken).ConfigureAwait(false);

            if (!FeishuPendingApprovalState.TryFind(session, approval.RequestId!, out var pending) || pending is null)
            {
                // 会话可能已被坏值自愈重建、该确认已被放弃（R5-12）、或已回灌过。
                // 绝不凭空构造批准响应：那会把「人类批准过的调用」重新指向一个未被展示过的工具调用。
                throw new InvalidOperationException(
                    $"待人工确认的工具调用已失效（requestId: {approval.RequestId}）——"
                    + "会话可能已被重建，或该确认已被放弃/已处理。请引导用户重新发起该操作（fail-closed，不凭空放行）");
            }

            // R7 / C4a：快照幂等消费（仅在宿主装配了 IFeishuPendingApprovalStore 时生效）。
            // 放在「框架记录已确认存在」之后：此时续跑必然发生，消费失败只可能是
            // 「不存在 / 已过期 / 已被消费」三种情形 —— 一律按「迟到的批准」丢弃（fail-closed，绝不重放）。
            if (pendingApprovalStore is not null)
            {
                var consumable = await pendingApprovalStore
                    .TryConsumeAsync(approval.AppKey!, approval.RequestId!, cancellationToken)
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
            using var _toolScope = toolContextAccessor.Begin(new FeishuToolContext(
                approval.AppKey,
                conversationKey,
                ChatId: approval.ChatId,
                UserId: approval.UserId));

            var response = await agent
                .RunAsync(messages, session, options: null, cancellationToken)
                .ConfigureAwait(false);

            // 续跑轮可能再产出审批请求（模型连发多个写工具）：与事件层同款口径提交宿主 + 记 Warning。
            var nextPending = FeishuApprovalRequestProjector.FromMessages(
                response.Messages,
                approval.AppKey,
                conversationKey,
                approval.ChatId,
                approval.UserId);

            if (nextPending.Count > 0)
            {
                await FeishuApprovalRequestProjector
                    .NotifyAsync(approvalChannel, pendingApprovalStore, logger, nextPending, cancellationToken)
                    .ConfigureAwait(false);
            }

            // 落库含框架绑定层在本次续跑中消费掉的审批记录与新生效的历史（失败即抛出，重试幂等：
            // 框架在运行失败时不消费记录，宿主持同一 approval 再次调用即可）。
            await agent.SaveSessionAsync(conversationKey, session, cancellationToken).ConfigureAwait(false);

            return response;
        }
        finally
        {
            gateHandle?.Dispose();
        }
    }
}
