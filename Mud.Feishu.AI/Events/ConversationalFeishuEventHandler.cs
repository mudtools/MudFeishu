// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Channels;
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
/// <para>
/// 流式回复（Phase 2 §3.1）：注入 <see cref="IMessageChannel"/> 且会话主体可解析出 chat_id
/// （群聊默认取 <see cref="ConversationRequest.SubjectId"/>；单聊经
/// <see cref="ResolveStreamTargetChatId"/> 覆写解析，缺省回退非流式）时，改走
/// <c>RunStreamingAsync</c>——通道 Begin 占位 → 增量写入 → Flush 收尾，不再调
/// <see cref="ReplyAsync"/>。通道 Begin 失败回退非流式（模型尚未调用，零重复成本）；
/// 流中失败向上传播（幂等键回滚，事件重投递）。
/// </para>
/// </remarks>
/// <typeparam name="T">强类型事件 DTO（须实现 <see cref="IEventResult"/>）。</typeparam>
public abstract class ConversationalFeishuEventHandler<T>(
    FeishuAgent agent,
    IFeishuEventDeduplicator businessDeduplicator,
    ILogger? logger = null,
    IReadOnlyList<IContextAssembler>? contextAssemblers = null,
    IFeishuToolContextAccessor? toolContextAccessor = null,
    IMessageChannel? messageChannel = null) : IdempotentFeishuEventHandler<T>(businessDeduplicator, logger ?? NullLogger.Instance)
    where T : class, IEventResult, new()
{
    private readonly FeishuAgent _agent = agent ?? throw new ArgumentNullException(nameof(agent));

    /// <summary>已注册的上下文装配器（按 <see cref="IContextAssembler.Order"/> 排序）。</summary>
    protected IReadOnlyList<IContextAssembler> ContextAssemblers { get; } = contextAssemblers ?? [];

    /// <summary>工具执行上下文访问器（可空；未注入时工具链在执行期拿不到上下文会结构化拒绝）。</summary>
    protected IFeishuToolContextAccessor? ToolContextAccessor { get; } = toolContextAccessor;

    /// <summary>流式回复通道（可空；未注入时保持非流式回复路径，Phase 2 §3.1）。</summary>
    protected IMessageChannel? MessageChannel { get; } = messageChannel;

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

        var (responseText, streamed) = await RunConversationAsync(
            request, session, userMessage, activity, cancellationToken).ConfigureAwait(false);

        await _agent.SaveSessionAsync(conversationKey, session, cancellationToken).ConfigureAwait(false);

        // 流式路径已由通道送达（Begin/Write/Flush），不再走派生类回复。
        if (!streamed)
        {
            await ReplyAsync(request, responseText, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 运行一轮对话：注入流式通道且可解析 chat_id 时走流式（Begin → 增量 → Flush），
    /// 否则非流式单次 Run。流式 Begin 失败回退非流式；流中失败向上传播（幂等回滚重投递）。
    /// </summary>
    /// <returns>（模型最终回答文本, 是否已经流式送达）。</returns>
    private async Task<(string ResponseText, bool Streamed)> RunConversationAsync(
        ConversationRequest request,
        AgentSession session,
        string userMessage,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        var streamChatId = MessageChannel is not null ? ResolveStreamTargetChatId(request) : null;
        if (MessageChannel is null || string.IsNullOrEmpty(streamChatId))
        {
            var response = await _agent.RunAsync(userMessage, session, options: null, cancellationToken).ConfigureAwait(false);
            return (response.Text, false);
        }

        string messageId;
        try
        {
            messageId = await MessageChannel.BeginAsync(request.AppKey, streamChatId!, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Begin 失败：模型尚未调用，回退非流式（零重复成本，Phase 2 §3.1）。
            _logger.LogWarning(ex, "流式占位消息创建失败，回退非流式回复（appKey: {AppKey}, chatId: {ChatId}）",
                request.AppKey, streamChatId);
            var response = await _agent.RunAsync(userMessage, session, options: null, cancellationToken).ConfigureAwait(false);
            return (response.Text, false);
        }

        var fullText = new StringBuilder();
        await foreach (var update in _agent
            .RunStreamingAsync(userMessage, session, options: null, cancellationToken)
            .ConfigureAwait(false))
        {
            var delta = update.Text;
            if (string.IsNullOrEmpty(delta))
            {
                continue;
            }

            fullText.Append(delta);
            // 单次写入失败由通道实现隔离（不中断模型流，接口契约）。
            await MessageChannel.WriteStreamAsync(request.AppKey, streamChatId!, messageId, delta, cancellationToken).ConfigureAwait(false);
        }

        await MessageChannel.FlushAsync(request.AppKey, streamChatId!, messageId, cancellationToken).ConfigureAwait(false);
        activity?.AddTag(FeishuAgentDiagnostics.TagStreamed, true);
        return (fullText.ToString(), true);
    }

    /// <summary>
    /// 解析流式回复的目标 chat_id：群聊默认为会话主体（chat_id）；返回 <see langword="null"/>
    /// 表示本次事件不适用流式（如单聊未覆写解析），回退非流式路径。
    /// </summary>
    /// <param name="request">规范化会话请求。</param>
    /// <returns>目标 chat_id（可空）。</returns>
    protected virtual string? ResolveStreamTargetChatId(ConversationRequest request)
        => request.Scope.IsGroup ? request.SubjectId : null;

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
