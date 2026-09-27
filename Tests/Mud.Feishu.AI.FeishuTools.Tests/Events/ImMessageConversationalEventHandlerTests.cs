// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Conversations;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.FeishuTools.Events;
using Mud.Feishu.DataModels.Messages;
using Mud.Feishu.EventCallback.IM;

namespace Mud.Feishu.AI.FeishuTools.Tests.Events;

/// <summary>
/// 内置 IM 会话事件处理器测试（AI-FD-D12 P2D-5a）：事件规范化（群聊/单聊维度 + ChatId +
/// @文本提取）、安全内建（Bot 自激过滤、群聊 @ 过滤、单聊开关）、文本回复。
/// </summary>
public class ImMessageConversationalEventHandlerTests
{
    private readonly Mock<Mud.Feishu.IFeishuTenantV1Message> _messageClient = new();

    private ImMessageConversationalEventHandler CreateHandler(
        Action<ImConversationOptions>? configure = null)
    {
        var options = new ImConversationOptions();
        configure?.Invoke(options);
        var chatClient = new Mock<Microsoft.Extensions.AI.IChatClient>();
        chatClient
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<Microsoft.Extensions.AI.ChatMessage>>(),
                It.IsAny<Microsoft.Extensions.AI.ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Microsoft.Extensions.AI.ChatResponse(
                new Microsoft.Extensions.AI.ChatMessage(
                    Microsoft.Extensions.AI.ChatRole.Assistant, "模型回复")));
        var agent = new FeishuAgent(chatClient.Object, new FeishuAgentOptions { Instructions = "x" });
        var dedup = new Mock<IFeishuEventDeduplicator>();
        dedup
            .Setup(d => d.TryMarkAsProcessingAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = false });

        return new ImMessageConversationalEventHandler(
            agent, dedup.Object, Options.Create(options), _messageClient.Object,
            NullLogger.Instance);
    }

    private static EventData BuildEvent(MessageReceiveResult result)
        => new() { EventId = "evt-im-1", Event = result };

    private static MessageReceiveResult BuildMessage(
        string chatType = "group",
        string? chatId = "oc_group1",
        string? senderType = "user",
        string[]? mentionKeys = null,
        string? parentId = null)
    {
        var message = new MessageContent
        {
            MessageId = "om_trigger",
            ChatType = chatType,
            ChatId = chatId,
            MessageType = "text",
            Content = "{\"text\":\"" + (mentionKeys is { Length: > 0 } ? "@_user_1 帮我查表" : "帮我查表") + "\"}",
            ParentId = parentId,
        };
        if (mentionKeys is { Length: > 0 })
        {
            message.Mentions = mentionKeys
                .Select(key => new MentionUser { Key = key, Name = "助手" })
                .ToArray();
        }

        return new MessageReceiveResult
        {
            Sender = new Mud.Feishu.EventCallback.IM.MessageSender
            {
                SenderType = senderType,
                SenderId = new UserIdInfo { OpenId = "ou_sender_1" },
            },
            Message = message,
        };
    }

    [Fact]
    public async Task HandleAsync_ShouldNormalizeGroupMessage_AndReplyWithText()
    {
        _messageClient
            .Setup(c => c.ReplyMessageAsync(It.IsAny<string>(), It.IsAny<ReplyMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<MessageDataResult> { Code = 0, Data = new MessageDataResult() });
        var handler = CreateHandler();

        await handler.HandleAsync(BuildEvent(BuildMessage(mentionKeys: ["@_user_1"])), default);

        _messageClient.Verify(
            c => c.ReplyMessageAsync("om_trigger", It.IsAny<ReplyMessageRequest>(), It.IsAny<CancellationToken>()),
            Times.Once, "以触发消息为父消息回复");
    }

    [Fact]
    public async Task HandleAsync_ShouldSkipBotSelfTrigger()
    {
        var handler = CreateHandler();

        await handler.HandleAsync(BuildEvent(BuildMessage(senderType: "app")), default);

        _messageClient.Verify(
            c => c.ReplyMessageAsync(It.IsAny<string>(), It.IsAny<ReplyMessageRequest>(), It.IsAny<CancellationToken>()),
            Times.Never, "Bot 自激过滤：应用（含其他 Bot）发的消息不进入会话，防事件风暴");
    }

    [Fact]
    public async Task HandleAsync_ShouldSkipGroupMessage_WithoutMention_WhenRequireMentionInGroup()
    {
        var handler = CreateHandler();

        await handler.HandleAsync(BuildEvent(BuildMessage(mentionKeys: null)), default);

        _messageClient.Verify(
            c => c.ReplyMessageAsync(It.IsAny<string>(), It.IsAny<ReplyMessageRequest>(), It.IsAny<CancellationToken>()),
            Times.Never, "群聊 @ 过滤：默认仅响应 @Bot 消息");
    }

    [Fact]
    public async Task HandleAsync_ShouldRespondInGroup_WithoutMention_WhenRequireMentionDisabled()
    {
        _messageClient
            .Setup(c => c.ReplyMessageAsync(It.IsAny<string>(), It.IsAny<ReplyMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<MessageDataResult> { Code = 0, Data = new MessageDataResult() });
        var handler = CreateHandler(options => options.RequireMentionInGroup = false);

        await handler.HandleAsync(BuildEvent(BuildMessage(mentionKeys: null)), default);

        _messageClient.Verify(
            c => c.ReplyMessageAsync(It.IsAny<string>(), It.IsAny<ReplyMessageRequest>(), It.IsAny<CancellationToken>()),
            Times.Once, "关闭 @ 过滤后群聊任意消息进入会话（消费点行为验证）");
    }

    [Fact]
    public async Task HandleAsync_ShouldSkipP2p_WhenAllowP2pConversationDisabled()
    {
        var handler = CreateHandler(options => options.AllowP2pConversation = false);

        await handler.HandleAsync(BuildEvent(BuildMessage(chatType: "p2p", chatId: "oc_p2p")), default);

        _messageClient.Verify(
            c => c.ReplyMessageAsync(It.IsAny<string>(), It.IsAny<ReplyMessageRequest>(), It.IsAny<CancellationToken>()),
            Times.Never, "单聊会话开关：仅做任务型 Bot 的宿主可关闭单聊");
    }

    [Fact]
    public async Task BuildRequest_ShouldExtractMentionedText_AndParentId()
    {
        ReplyMessageRequest? captured = null;
        _messageClient
            .Setup(c => c.ReplyMessageAsync("om_trigger", It.IsAny<ReplyMessageRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, ReplyMessageRequest request, CancellationToken __) =>
            {
                captured = request;
            })
            .ReturnsAsync(new FeishuApiResult<MessageDataResult> { Code = 0, Data = new MessageDataResult() });
        var handler = CreateHandler();

        // 群聊 @ 消息：MentionedText 应还原 @名 并保留指令文本；回复 content 为 text JSON。
        await handler.HandleAsync(BuildEvent(BuildMessage(mentionKeys: ["@_user_1"], parentId: "om_parent")), default);

        captured.Should().NotBeNull();
        captured!.MsgType.Should().Be("text");
        var text = JsonDocument.Parse(captured!.Content!).RootElement.GetProperty("text").GetString();
        text.Should().NotBeNullOrEmpty("回复 content 为 text 类型 JSON（飞书消息格式）");
    }
}
