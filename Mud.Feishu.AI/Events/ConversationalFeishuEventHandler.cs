// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Conversations;
using Mud.Feishu.AI.Diagnostics;

namespace Mud.Feishu.AI.Events;

/// <summary>
/// 会话式事件处理器基类：飞书事件 → 会话 → 模型 → 回复 的入口（路线图主线① M1）。
/// </summary>
/// <remarks>
/// <para>
/// 继承 <see cref="IdempotentFeishuEventHandler{T}"/> 复用 EventId 幂等（同一事件不二次调模型、
/// 不重复扣费）；业务去重键自带命名空间（D 系契约：禁止裸 EventId）。
/// </para>
/// <para>
/// 流程（总体设计 §4）：BuildRequest（事件规范化）→ <see cref="ConversationKeyBuilder"/>
/// → 取/建会话 → 上下文装配 → <c>FeishuAgent.RunAsync</c> → 持久化会话 →
/// <see cref="ReplyAsync"/>。回复机制由派生类实现（SDK 不预设消息通道，
/// 典型实现经 <c>IFeishuTenantV1Message.ReplyMessageAsync</c> 回复）。
/// </para>
/// </remarks>
/// <typeparam name="T">强类型事件 DTO（须实现 <see cref="IEventResult"/>）。</typeparam>
/// <remarks>
/// 构造注入 <see cref="IFeishuToolContextAccessor"/>（可空）时，模型工具调用期间
/// 的执行上下文（appKey/chat/user）经异步流注入工具执行链（多租户隔离事实来源，TMA2-20）。
/// </remarks>
public abstract class ConversationalFeishuEventHandler<T>(
    FeishuAgent agent,
    IFeishuEventDeduplicator businessDeduplicator,
    ILogger? logger = null,
    IReadOnlyList<IContextAssembler>? contextAssemblers = null,
    IFeishuToolContextAccessor? toolContextAccessor = null) : IdempotentFeishuEventHandler<T>(businessDeduplicator, logger ?? NullLogger.Instance)
    where T : class, IEventResult, new()
{
    private readonly FeishuAgent _agent = agent ?? throw new ArgumentNullException(nameof(agent));

    /// <summary>已注册的上下文装配器（按 <see cref="IContextAssembler.Order"/> 排序）。</summary>
    protected IReadOnlyList<IContextAssembler> ContextAssemblers { get; } = contextAssemblers ?? [];

    /// <summary>工具执行上下文访问器（可空；未注入时工具链在执行期拿不到上下文会结构化拒绝）。</summary>
    protected IFeishuToolContextAccessor? ToolContextAccessor { get; } = toolContextAccessor;

    /// <summary>
    /// 把强类型事件规范化为会话请求（群聊/单聊维度选择、会话主体提取）。
    /// </summary>
    /// <param name="eventData">强类型事件 DTO。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>会话请求。</returns>
    protected abstract Task<ConversationRequest> BuildRequestAsync(T eventData, CancellationToken cancellationToken);

    /// <summary>
    /// 把模型回复送达飞书（消息回复/卡片，由派生类选择通道）。
    /// </summary>
    /// <param name="request">会话请求。</param>
    /// <param name="responseText">模型最终回答文本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    protected abstract Task ReplyAsync(ConversationRequest request, string responseText, CancellationToken cancellationToken);

    /// <inheritdoc />
    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        T? eventEntity,
        CancellationToken cancellationToken = default)
    {
        if (eventEntity is null)
        {
            _logger.LogWarning("事件 {EventId} 缺少强类型实体，跳过会话处理", eventData.EventId);
            return;
        }

        var request = await BuildRequestAsync(eventEntity, cancellationToken).ConfigureAwait(false);
        var conversationKey = ConversationKeyBuilder.Build(request.AppKey, request.Scope, request.SubjectId);

        using var activity = FeishuAgentDiagnostics.StartConversationActivity(conversationKey, request.AppKey);

        var session = await _agent.GetOrCreateSessionAsync(conversationKey, cancellationToken).ConfigureAwait(false);
        var userMessage = await AssembleUserMessageAsync(request, cancellationToken).ConfigureAwait(false);

        // 工具执行上下文沿异步流注入（RunAsync 内模型发起的 tool_call 可读到 appKey/chat/user）。
        using var _toolScope = ToolContextAccessor?.Begin(new FeishuToolContext(
            request.AppKey,
            conversationKey,
            ChatId: request.Scope.IsGroup ? request.SubjectId : null,
            UserId: request.SenderId));

        var response = await _agent.RunAsync(userMessage, session, options: null, cancellationToken).ConfigureAwait(false);
        await _agent.SaveSessionAsync(conversationKey, session, cancellationToken).ConfigureAwait(false);

        await ReplyAsync(request, response.Text, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 覆写业务去重键：自带命名空间（<c>feishu.agent.conversation:{EventId}</c>），禁止裸 EventId。
    /// </summary>
    /// <param name="eventData">事件原始数据。</param>
    /// <returns>业务去重键。</returns>
    protected override string? GetBusinessKey(EventData eventData)
        => string.IsNullOrEmpty(eventData.EventId)
            ? null
            : $"feishu.agent.conversation:{eventData.EventId}";

    /// <summary>
    /// 拼接用户消息：默认仅装配器产物；派生类可覆写追加固定前缀/额外上下文。
    /// </summary>
    /// <param name="request">会话请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>用户消息文本。</returns>
    protected virtual async Task<string> AssembleUserMessageAsync(ConversationRequest request, CancellationToken cancellationToken)
    {
        var fragments = new List<string>();
        foreach (var assembler in ContextAssemblers.OrderBy(a => a.Order))
        {
            try
            {
                var fragment = await assembler.AssembleAsync(request, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(fragment))
                {
                    // netstandard2.0 的 BCL 不带 IsNullOrWhiteSpace 的 NotNullWhen 注解，需显式断言。
                    fragments.Add(fragment!);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // 单个装配器失败不中断事件处理（异常隔离，对齐事件派发隔离精神）。
                _logger.LogWarning(ex, "上下文装配器 {Assembler} 失败，已跳过", assembler.GetType().Name);
            }
        }

        return string.Join("\n", fragments);
    }
}
