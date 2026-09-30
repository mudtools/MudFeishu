// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Conversations;
using Mud.Feishu.Abstractions.EventHandlers;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.AI.Channels;
using Mud.Feishu.AI.Conversations;
using Mud.Feishu.AI.Events;
using Mud.Feishu.DataModels.Messages;
using Mud.Feishu.EventCallback.IM;

namespace Mud.Feishu.AI.FeishuTools.Events;

/// <summary>
/// 内置 IM 会话事件处理器（AI-FD-D12 P2D-5a，「零自定义接入」参考实现）：
/// <c>im.message.receive_v1</c> 消息 → 会话 → 模型 → 回复/流式，一行接入
/// （<c>AddFeishuImConversationHandler</c> + 通道 <c>AddHandler</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>事件规范化</b>（<see cref="ConversationalFeishuEventHandler{T}.BuildRequestAsync"/>）：
/// <c>Message.ChatType</c>（p2p/group）判定 <see cref="ConversationScope"/> 与会话主体
/// （群聊 = chat_id、单聊 = 发送者 open_id）；<c>Message.ChatId</c> 填
/// <see cref="ConversationRequest.ChatId"/>（P2D-2b，单聊流式解锁）；<c>Mentions</c> 提取
/// @文本（<see cref="ConversationRequest.MentionedText"/>）。
/// </para>
/// <para>
/// <b>安全内建</b>（通用逻辑权威化，非策略下放）：
/// Bot 自激过滤——<c>sender_type == "app"</c> 跳过（防 Bot 回复自己触发的事件风暴）；
/// 群聊 @ 过滤——<see cref="ImConversationOptions.RequireMentionInGroup"/> 默认仅响应
/// @Bot 消息（单聊不受影响）；单聊开关 <see cref="ImConversationOptions.AllowP2pConversation"/>。
/// 过滤在业务幂等标记<b>之前</b>……由于基类 <c>HandleAsync</c> 已密封，过滤落在
/// <see cref="ProcessBusinessLogicAsync"/> 入口：被过滤消息按「已消费」落幂等终态（不重投递）。
/// </para>
/// <para>
/// <b>回复</b>：<c>IFeishuTenantV1Message.ReplyMessageAsync</c> 文本回复（以触发消息为父消息）；
/// <b>流式</b>：注入 <see cref="IMessageChannel"/> 时启用（目标 = <see cref="ConversationRequest.ChatId"/>，
/// 卡片流通道经目标解析器取发送者 open_id），复用基类全部既有语义。
/// </para>
/// <para>
/// 事件订阅侧由宿主挂 WebSocket/Webhook 既有通道（不改变事件接入方式，已决策④）。
/// </para>
/// </remarks>
public sealed class ImMessageConversationalEventHandler(
    FeishuAgent agent,
    IFeishuEventDeduplicator businessDeduplicator,
    IOptions<ImConversationOptions> options,
    Mud.Feishu.IFeishuTenantV1Message messageClient,
    ILogger? logger = null,
    IReadOnlyList<IContextAssembler>? contextAssemblers = null,
    IFeishuToolContextAccessor? toolContextAccessor = null,
    IMessageChannel? messageChannel = null,
    IConversationGate? conversationGate = null,
    IAppKeyAccessor? appKeyAccessor = null)
    : ConversationalFeishuEventHandler<MessageReceiveResult>(
        agent, businessDeduplicator, logger, contextAssemblers, toolContextAccessor, messageChannel, conversationGate, appKeyAccessor)
{
    private const string ChatTypeGroup = "group";
    private const string SenderTypeApp = "app";

    private readonly ImConversationOptions _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
    private readonly Mud.Feishu.IFeishuTenantV1Message _messageClient = messageClient
        ?? throw new ArgumentNullException(nameof(messageClient));

    /// <inheritdoc />
    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        MessageReceiveResult? eventEntity,
        CancellationToken cancellationToken = default)
    {
        if (eventEntity is not null && TrySkipFiltered(eventEntity))
        {
            // 被过滤消息按「已消费」正常返回（幂等终态落 Completed，不重投递）。
            return;
        }

        await base.ProcessBusinessLogicAsync(eventData, eventEntity, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override Task<ConversationRequest> BuildRequestAsync(MessageReceiveResult eventData, CancellationToken cancellationToken)
    {
        if (eventData.Message is null)
        {
            throw new InvalidOperationException("im.message.receive_v1 事件缺少 message 字段，无法规范化会话请求");
        }

        var message = eventData.Message;
        var senderId = ExtractSenderId(eventData.Sender);
        var isGroup = string.Equals(message.ChatType, ChatTypeGroup, StringComparison.OrdinalIgnoreCase);

        // 会话键维度（P2D-2b）：群聊按 chat_id、单聊按发送者；回复/流式目标恒为 message.chat_id。
        // appKey 取自应用键上下文（多应用管线由事件通道经 IAppKeyAccessor 注入，见构造参数 appKeyAccessor）；
        // 缺失时的处置由基类 AllowMissingAppKey 分级决定（多应用宿主 fail-fast、单应用宿主降级告警）。
        var request = new ConversationRequest(
            AppKey: CurrentAppKey ?? string.Empty,
            Scope: isGroup ? ConversationScope.Group() : ConversationScope.P2P(),
            SubjectId: isGroup ? (message.ChatId ?? string.Empty) : senderId,
            SenderId: senderId,
            MessageId: message.MessageId ?? string.Empty,
            MentionedText: ExtractMentionedText(message),
            ChatId: message.ChatId,
            ParentId: message.ParentId);
        return Task.FromResult(request);
    }

    /// <inheritdoc />
    protected override async Task ReplyAsync(ConversationRequest request, string responseText, CancellationToken cancellationToken)
    {
        var outcome = FeishuApiResultReader.Read(await _messageClient
            .ReplyMessageAsync(request.MessageId, new ReplyMessageRequest
            {
                MsgType = "text",
                Content = new JsonObject { ["text"] = responseText }.ToJsonString(),
            }, cancellationToken)
            .ConfigureAwait(false));
        if (!outcome.Ok)
        {
            throw new InvalidOperationException(
                $"会话回复失败（messageId: {request.MessageId}）: {outcome.ErrorText ?? "返回空数据"}");
        }
    }

    /// <summary>安全过滤：命中返回 true（Bot 自激 / 群聊未 @ / 单聊关闭）。</summary>
    private bool TrySkipFiltered(MessageReceiveResult eventEntity)
    {
        var message = eventEntity.Message;
        if (message is null)
        {
            return false; // 交由 BuildRequest 的结构化失败路径上报。
        }

        var senderType = eventEntity.Sender?.SenderType;
        if (string.Equals(senderType, SenderTypeApp, StringComparison.OrdinalIgnoreCase))
        {
            // Bot 自激过滤：应用自身（含其他 Bot）发的消息不再进入会话，防回复风暴。
            return true;
        }

        var isGroup = string.Equals(message.ChatType, ChatTypeGroup, StringComparison.OrdinalIgnoreCase);
        if (isGroup)
        {
            if (_options.RequireMentionInGroup && (message.Mentions is not { Length: > 0 }))
            {
                return true; // 群聊 @ 过滤（默认仅响应 @Bot 消息）。
            }
        }
        else if (!_options.AllowP2pConversation)
        {
            return true; // 单聊会话开关（仅做任务型 Bot 的宿主可关闭）。
        }

        return false;
    }

    /// <summary>发送者 ID（open_id 优先；脱敏边界：仅取 ID 形态，不取昵称原文）。</summary>
    private static string ExtractSenderId(Mud.Feishu.EventCallback.IM.MessageSender? sender)
        => sender?.SenderId switch
        {
            { OpenId: { Length: > 0 } openId } => openId,
            { UserId: { Length: > 0 } userId } => userId,
            { UnionId: { Length: > 0 } unionId } => unionId,
            _ => string.Empty,
        };

    /// <summary>从 message.content（类型化 JSON）提取文本并把 @提及键替换为可读名。</summary>
    private static string? ExtractMentionedText(MessageContent? message)
    {
        if (message is null || string.IsNullOrEmpty(message.Content))
        {
            return null;
        }

        // netstandard2.0 的 BCL 缺少流转注解：Content 先局部化。
        var content = message.Content!;
        var text = message.MessageType == "text" && TryParseTextContent(content, out var parsed)
            ? parsed
            : content;

        foreach (var mention in message.Mentions ?? [])
        {
            var key = mention?.Key;
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            var name = string.IsNullOrWhiteSpace(mention?.Name) ? string.Empty : mention!.Name;
            text = text.Replace(key, name);
        }

        var trimmed = text.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static bool TryParseTextContent(string content, out string text)
    {
        text = string.Empty;
        try
        {
            if (JsonNode.Parse(content) is JsonObject obj && obj["text"] is JsonValue value
                && value.TryGetValue<string>(out var parsed))
            {
                text = parsed;
                return true;
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // 非法 JSON 按原文处理。
            // 有意静默（守卫白名单）：这是"消息非 text JSON"的正常分支，降级输出确定；记日志只会产生噪声。
        }

        return false;
    }
}
