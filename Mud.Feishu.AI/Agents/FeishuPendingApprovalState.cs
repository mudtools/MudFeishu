// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.AI;

namespace Mud.Feishu.AI.Agents;

/// <summary>
/// 框架（MAF <c>ApprovalResponseBindingChatClient</c>）记录的「待人工确认的工具调用」状态桥（R5-11 / R5-12）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么读取框架自身的记录、而不是 SDK 另建一份</b>（R5 审查的架构裁决）：
/// MAF 的绑定层在<b>模型产出审批请求的那一轮</b>就把模型原始 <see cref="ToolApprovalRequestContent"/>
/// （含真实 <see cref="ToolCallContent"/>）写进<b>会话状态袋</b>，并且在<b>下一轮</b>按
/// <c>RequestId</c> 绑定人类响应——这是批准资格的唯一权威来源（"批准只能来自框架记录过的请求"）。
/// SDK 若再存一份自己的副本，会产生<b>两个真相来源</b>：MAF 消费/清理的时机（成功轮之后、
/// callId 冲突毒化、会话重建）SDK 无法完整观测，副本必然漂移——漂移的后果是 SDK 以为「仍待确认」
/// 而框架已遗忘，续跑轮被绑定层丢弃响应 ⇒ 框架抛错。故本类只做「读取 + 清理」的桥接，
/// <b>不新增任何 SDK 状态键</b>（对齐 R5 §0.4「不新增配置键」的克制精神）。
/// </para>
/// <para>
/// <b>对框架内部键的耦合与守卫</b>：<see cref="FrameworkStateKey"/> 取自 MAF
/// <c>Microsoft.Agents.AI.ApprovalResponseBindingChatClient.StateBagKey</c>（该类型为 internal，
/// 无法在编译期引用）。此耦合由契约守卫用例
/// <c>AgentContractGuards.FrameworkPendingApprovalStateKey_ShouldMatchMachineAgentsAiConstant</c>
/// 以反射方式锁定：MAF 改名即用例转红，避免静默失效。
/// </para>
/// <para>
/// <b>读失败一律按「无待确认项」处理</b>（R4-7 同款收口：状态袋值是惰性反序列化的，
/// 坏值只在首次类型化读取时抛，不得让本桥成为新的毒化点；取消原样传播）。
/// </para>
/// </remarks>
internal static class FeishuPendingApprovalState
{
    /// <summary>
    /// 框架绑定层的待审批请求状态键（MAF <c>ApprovalResponseBindingChatClient.StateBagKey</c> 的值）。
    /// </summary>
    /// <remarks>改名即失去 HITL 续跑能力，故由反射契约守卫锁定（见类型注释）。</remarks>
    internal const string FrameworkStateKey = "_pendingApprovalRequests";

    /// <summary>
    /// 读取框架记录的待审批请求（坏值 ⇒ 空集合，绝不抛）。
    /// </summary>
    /// <param name="session">MAF 会话。</param>
    /// <returns>待审批请求；无记录或不可用时为空集合。</returns>
    internal static IReadOnlyList<ToolApprovalRequestContent> Read(AgentSession session)
    {
        if (session is null)
        {
            return [];
        }

        try
        {
            // options 传 null ⇒ AgentAbstractionsJsonUtilities.DefaultOptions（框架自身同款解析链）。
            if (session.StateBag.TryGetValue<List<ToolApprovalRequestContent>>(
                    FrameworkStateKey, out var pending, null)
                && pending is { Count: > 0 })
            {
                return pending;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // 坏值按「无待确认项」处理：消费方全部 fail-closed（续跑抛错、放弃路径清键），
            // 不会因读失败而放行任何写工具（R4-7 收口纪律）。
            // 有意静默（守卫白名单）：本类是 internal 纯函数，无日志面；读失败的所有后果都是 fail-closed，
            // 且注入日志会把「框架状态不可读」这一瞬时事实膨胀成每轮噪声。
        }

        return [];
    }

    /// <summary>会话中是否存在框架记录的待审批请求。</summary>
    /// <param name="session">MAF 会话。</param>
    /// <returns>存在返回 <see langword="true"/>。</returns>
    internal static bool HasAny(AgentSession session) => Read(session).Count > 0;

    /// <summary>
    /// 会话是否处于「有待确认的写操作」状态（R5-12 放弃判定的判据）。
    /// </summary>
    /// <remarks>
    /// <b>两个判据取并集</b>，二者不可互相替代：
    /// <list type="bullet">
    /// <item><b>历史中存在孤儿审批请求</b>（非 <c>InformationalOnly</c>）：会让框架在<b>整段入站历史</b>的
    /// 配对校验中抛错，是「会话被毒化」的直接原因，必须摘除；</item>
    /// <item><b>框架仍记录着待审批请求</b>：历史可能已被历史裁剪窗挤掉（看不到孤儿），但记录仍在——
    /// 若不清空，<b>迟到的批准</b>仍会被绑定层接受并把一轮已被用户放弃的写操作真正执行（越权方向），
    /// 必须一并清空。</item>
    /// </list>
    /// </remarks>
    /// <param name="session">MAF 会话。</param>
    /// <returns>处于待确认状态返回 <see langword="true"/>。</returns>
    internal static bool HasPending(AgentSession session)
        => HasAny(session) || HasOrphanApprovalRequests(session);

    /// <summary>会话历史中是否存在框架尚未处理的孤儿审批请求。</summary>
    /// <param name="session">MAF 会话。</param>
    /// <returns>存在返回 <see langword="true"/>。</returns>
    internal static bool HasOrphanApprovalRequests(AgentSession session)
    {
        try
        {
            if (!session.TryGetInMemoryChatHistory(out var history, FeishuAgent.ChatHistoryStateKey, null)
                || history is null)
            {
                return false;
            }

            foreach (var message in history)
            {
                foreach (var content in message.Contents)
                {
                    if (IsOrphanApprovalRequest(content))
                    {
                        return true;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // 历史不可用时按「无孤儿」处理：后续模型调用会以框架自身的错误暴露问题（fail-closed）。
            // 有意静默（守卫白名单）：同上——无日志面，且后果一律 fail-closed。
        }

        return false;
    }

    private static bool IsOrphanApprovalRequest(AIContent content)
        => content is ToolApprovalRequestContent { ToolCall: FunctionCallContent { InformationalOnly: false } };

    /// <summary>
    /// 按 <c>RequestId</c> 取框架记录的待审批请求（R5-11 续跑的唯一入参来源）。
    /// </summary>
    /// <param name="session">MAF 会话。</param>
    /// <param name="requestId">框架请求标识（<c>ficc_{callId}</c>）。</param>
    /// <param name="request">命中的待审批请求。</param>
    /// <returns>命中返回 <see langword="true"/>。</returns>
    internal static bool TryFind(AgentSession session, string requestId, out ToolApprovalRequestContent? request)
    {
        request = null;
        if (string.IsNullOrEmpty(requestId))
        {
            return false;
        }

        foreach (var item in Read(session))
        {
            if (string.Equals(item.RequestId, requestId, StringComparison.Ordinal))
            {
                request = item;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// <b>放弃</b>尚未应答的审批：从会话历史中摘除孤儿审批请求内容，并清空框架待审批记录（R5-12）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>必须做的理由</b>：MEAI <c>FunctionInvokingChatClient</c> 在处理入站消息时对
    /// <b>整段输入历史</b>做一次配对校验——历史里只要残留「非 <c>InformationalOnly</c> 的
    /// <see cref="ToolApprovalRequestContent"/> 且无对应响应」，就直接
    /// <c>InvalidOperationException</c>（"ToolApprovalRequestContent found with FunctionCall.CallId(s) …
    /// that have no matching ToolApprovalResponseContent."）。宿主不续跑时该孤儿会随历史落库，
    /// 于是<b>该会话此后每一轮都抛</b>（异常早于任何新消息落库 ⇒ 永远不会自愈）⇒
    /// 幂等回滚 + 重投递 = 事件永久毒化循环。
    /// </para>
    /// <para>
    /// 只摘除 <c>InformationalOnly == false</c> 的审批请求（与框架校验集合完全一致）；
    /// 只含该类内容的助手消息整条移除（留一条「只有工具调用、没有结果」的助手消息会被
    /// OpenAI 兼容端点 400 拒绝）。清空框架记录是为了让<b>迟到的批准</b>在绑定层被丢弃
    /// （fail-closed），而不会把一轮已放弃的写操作重新执行。
    /// </para>
    /// </remarks>
    /// <param name="session">MAF 会话。</param>
    /// <returns>被摘除的孤儿审批请求条数。</returns>
    internal static int Abandon(AgentSession session)
    {
        if (session is null)
        {
            return 0;
        }

        var removed = 0;
        try
        {
            removed = StripOrphanApprovalRequests(session);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // 历史本身不可用 ⇒ 无从摘除（保持 fail-closed）。框架记录的清空**照常执行**：
            // 宁可让迟到批准在绑定层被丢弃，也不能让它把一轮已放弃的写操作重新放行。
            // 有意静默（守卫白名单）：调用点在事件层，已按返回值记 Warning（摘除条数），
            // 此处重复记日志只会在同一次故障上叠加两条噪声。
        }

        session.StateBag.TryRemoveValue(FrameworkStateKey);
        return removed;
    }

    private static int StripOrphanApprovalRequests(AgentSession session)
    {
        if (!session.TryGetInMemoryChatHistory(out var history, FeishuAgent.ChatHistoryStateKey, null)
            || history is null
            || history.Count == 0)
        {
            return 0;
        }

        List<ChatMessage>? rebuilt = null;
        var removed = 0;

        for (var i = 0; i < history.Count; i++)
        {
            var message = history[i];
            List<AIContent>? kept = null;

            foreach (var content in message.Contents)
            {
                if (IsOrphanApprovalRequest(content))
                {
                    removed++;
                    kept ??= [];
                    continue;
                }

                kept?.Add(content);
            }

            if (kept is null)
            {
                // 本条消息无孤儿审批：原样保留（仅在已发生改动时重建列表）。
                rebuilt?.Add(message);
                continue;
            }

            rebuilt ??= [.. history.Take(i)];

            if (kept.Count > 0)
            {
                var clone = message.Clone();
                clone.Contents = kept;
                rebuilt.Add(clone);
            }
        }

        if (rebuilt is not null)
        {
            session.SetInMemoryChatHistory(rebuilt, FeishuAgent.ChatHistoryStateKey, null);
        }

        return removed;
    }
}
