// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.Agent.Events;

namespace Mud.Feishu.AI.Agents;

/// <summary>
/// 工具人工确认（HITL）的<b>批准回灌续跑</b>入口（R5-11；N2-c 改为产品侧薄转发，④式）。
/// </summary>
/// <remarks>
/// <para>
/// <b>N2-c 中立化</b>：闭环机制（四个易错点：同键加载会话 → 取框架记录的原始审批请求 →
/// 重建工具执行上下文 → 续跑并落库）已迁 <c>Mud.AI.Agent.Approvals.AgentApprovalContinuation</c>。
/// 本类只做<b>产品事实的适配</b>：
/// <list type="bullet">
/// <item>飞书工具上下文访问器（<see cref="IFeishuToolContextAccessor"/>）→ 中立
/// <see cref="Mud.AI.Agent.Events.ITurnScopeFactory"/>（<see cref="FeishuTurnScopeFactory"/>）；</item>
/// <item>飞书会话闸门（<c>IConversationGate</c>，已发布 SDK 的宿主可替换契约）→ ③ 式委托
/// <c>acquireGate</c>（方案 v2 §5.4.1：中立侧不命名产品闸门类型）；</item>
/// <item>批准通道门面 <see cref="IFeishuToolApprovalChannel"/>（继承中立
/// <c>IToolApprovalChannel</c>）原样透传。</item>
/// </list>
/// </para>
/// <para>
/// <b>宿主调用点零改</b>：方法签名（含全部可选参数与顺序）与中立化前逐字一致。
/// </para>
/// <para>
/// <b>fail-closed 边界</b>与改造前一致（见中立实现的 remarks）：缺租户上下文/缺关键要素/
/// 框架记录已失效一律显式抛错，绝不静默降级或凭空放行。
/// </para>
/// </remarks>
public static class FeishuAgentApprovalExtensions
{
    /// <summary>
    /// 回灌一次人工批准（或拒绝）并续跑同一会话（R5-11）。
    /// </summary>
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
    /// <exception cref="ArgumentException">审批请求要素不完整（缺 RequestId / ConversationKey / TenantKey）。</exception>
    /// <exception cref="InvalidOperationException">工具上下文访问器缺失，或该审批请求已失效（fail-closed）。</exception>
    public static Task<AgentResponse> RunApprovalContinuationAsync(
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
        IPendingApprovalStore? pendingApprovalStore = null,
        CancellationToken cancellationToken = default)
    {
        if (toolContextAccessor is null)
        {
            // 装配缺陷显式抛错（改造前语义逐字保留）：静默跳过上下文会让续跑轮的写工具 fail-closed 拒绝，
            // 表现为「批准了却不生效」且仅日志可见——宁可调用点直接失败。
            throw new InvalidOperationException(
                $"续跑人工确认需要 {nameof(IFeishuToolContextAccessor)} 以重建租户执行上下文（批准 ≠ 放行，TMA2-20）——"
                    + "请从容器解析（FeishuTools 的 AddFeishuTools* 已 Singleton 注册）后传入");
        }

        // ③ 式委托：中立侧不命名产品闸门类型；闸门缺席即不串行化（与事件层 ConversationGate is null 同口径）。
        Func<string, CancellationToken, Task<IDisposable?>>? acquireGate = conversationGate is null
            ? null
            : async (key, ct) => (IDisposable?)await conversationGate
                .AcquireAsync(key, ct)
                .ConfigureAwait(false);

        return AgentApprovalContinuation.RunAsync(
            agent,
            agent,
            new FeishuTurnScopeFactory(toolContextAccessor),
            approval,
            approved,
            reason,
            acquireGate,
            approvalChannel,
            logger,
            pendingApprovalStore,
            cancellationToken);
    }
}
