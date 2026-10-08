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
        Action<ImConversationOptions>? configure = null,
        string? appKey = null,
        IFeishuAppContextScopeFactory? scopeFactory = null)
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

        var appKeyAccessor = new Mock<IAppKeyAccessor>();
        appKeyAccessor.Setup(a => a.CurrentAppKey).Returns(appKey);

        return new ImMessageConversationalEventHandler(
            agent, dedup.Object, Options.Create(options), _messageClient.Object,
            NullLogger.Instance,
            appKeyAccessor: appKey is null ? null : appKeyAccessor.Object,
            appContextScopeFactory: scopeFactory);
    }

    private static EventData BuildEvent(MessageReceiveResult result)
        => new() { EventId = "evt-im-1", Event = result };

    private static MessageReceiveResult BuildMessage(
        string chatType = "group",
        string? chatId = "oc_group1",
        string? senderType = "user",
        string[]? mentionKeys = null,
        string? parentId = null,
        string mentionName = "助手")
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
                .Select(key => new MentionUser { Key = key, Name = mentionName })
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

    // ───────────────── R3-3：群聊「@ 到 Bot 本人」判定（Mentions 非空 ≠ @Bot） ─────────────────

    /// <summary>
    /// R3-3：配置 <see cref="ImConversationOptions.BotName"/> 后，只有 <c>mentions[].name</c>
    /// 与 Bot 显示名一致的 @ 才进入会话。
    /// </summary>
    /// <remarks>
    /// 缺陷原始形态：判定条件仅为 <c>mentions</c> 非空 ⇒ 「@ 别人」「@ 全体」都会触发 Bot 回复
    /// （过度响应，群内噪声）。本用例同时锁定「命中即响应」的正面路径。
    /// </remarks>
    [Fact]
    public async Task HandleAsync_ShouldRespondInGroup_WhenMentionTargetsBotName()
    {
        _messageClient
            .Setup(c => c.ReplyMessageAsync(It.IsAny<string>(), It.IsAny<ReplyMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<MessageDataResult> { Code = 0, Data = new MessageDataResult() });
        var handler = CreateHandler(options => options.BotName = "飞书助手");

        await handler.HandleAsync(
            BuildEvent(BuildMessage(mentionKeys: ["@_user_1"], mentionName: "飞书助手")), default);

        _messageClient.Verify(
            c => c.ReplyMessageAsync(It.IsAny<string>(), It.IsAny<ReplyMessageRequest>(), It.IsAny<CancellationToken>()),
            Times.Once, "@ 到 Bot 本人时正常进入会话");
    }

    [Fact]
    public async Task HandleAsync_ShouldSkipGroupMessage_WhenMentionDoesNotTargetBot()
    {
        var handler = CreateHandler(options => options.BotName = "飞书助手");

        await handler.HandleAsync(
            BuildEvent(BuildMessage(mentionKeys: ["@_user_1"], mentionName: "张三")), default);

        _messageClient.Verify(
            c => c.ReplyMessageAsync(It.IsAny<string>(), It.IsAny<ReplyMessageRequest>(), It.IsAny<CancellationToken>()),
            Times.Never, "@ 别人 / @ 全体（name 不等于 Bot 显示名）不得触发回复（R3-3）");
    }

    /// <summary>
    /// 未配置 <see cref="ImConversationOptions.BotName"/> 时保持旧行为（<c>mentions</c> 非空即视为 @Bot），
    /// 保证升级零破坏。
    /// </summary>
    [Fact]
    public async Task HandleAsync_ShouldKeepLegacyMentionSemantics_WhenBotNameNotConfigured()
    {
        _messageClient
            .Setup(c => c.ReplyMessageAsync(It.IsAny<string>(), It.IsAny<ReplyMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<MessageDataResult> { Code = 0, Data = new MessageDataResult() });
        var handler = CreateHandler();

        await handler.HandleAsync(
            BuildEvent(BuildMessage(mentionKeys: ["@_user_1"], mentionName: "张三")), default);

        _messageClient.Verify(
            c => c.ReplyMessageAsync(It.IsAny<string>(), It.IsAny<ReplyMessageRequest>(), It.IsAny<CancellationToken>()),
            Times.Once, "BotName 为 null 时不启用收紧（默认零破坏）");
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

    /// <summary>
    /// R3-1（P0）：多应用宿主下回复必须发生在<b>事件所属租户</b>的 SDK 作用域内。
    /// </summary>
    /// <remarks>
    /// 缺陷原始形态：<c>ReplyAsync</c> 直接调用生成的客户端，而生成客户端在无环境上下文时退化为
    /// 默认应用身份 ⇒ 以别的租户名义把回复发出去（TMA2-20）。本用例同时锁定三件事：
    /// 作用域<b>已建立</b>、建立<b>在</b>下发动作之前、且随回复结束<b>释放</b>。
    /// </remarks>
    [Fact]
    public async Task ReplyAsync_ShouldBeginAppScope_WhenAppKeyIsNotDefault()
    {
        var scopeFactory = new RecordingScopeFactory();
        string? activeDuringReply = null;
        _messageClient
            .Setup(c => c.ReplyMessageAsync(It.IsAny<string>(), It.IsAny<ReplyMessageRequest>(), It.IsAny<CancellationToken>()))
            .Callback(() => activeDuringReply = scopeFactory.ActiveAppKey)
            .ReturnsAsync(new FeishuApiResult<MessageDataResult> { Code = 0, Data = new MessageDataResult() });
        var handler = CreateHandler(appKey: "app-a", scopeFactory: scopeFactory);

        await handler.HandleAsync(BuildEvent(BuildMessage(mentionKeys: ["@_user_1"])), default);

        scopeFactory.Begun.Should().Equal(new[] { "app-a" }, "回复前必须切到事件携带的租户上下文（R3-1）");
        activeDuringReply.Should().Be(
            "app-a", "下发动作必须发生在目标租户作用域内，否则会退回默认应用身份（TMA2-20 跨租户错发）");
        scopeFactory.ActiveAppKey.Should().BeNull("作用域必须随回复结束释放，不得泄漏到后续事件");
    }

    /// <summary>测试替身：记录建立过的作用域并暴露「当前活跃 appKey」供回调断言。</summary>
    private sealed class RecordingScopeFactory : IFeishuAppContextScopeFactory
    {
        private readonly List<string> _begun = [];

        /// <summary>按建立顺序记录的全部 appKey。</summary>
        public IReadOnlyList<string> Begun => _begun;

        /// <summary>当前活跃 appKey（无活跃作用域时为 null）。</summary>
        public string? ActiveAppKey { get; private set; }

        /// <inheritdoc />
        public IDisposable BeginScope(string appKey)
        {
            _begun.Add(appKey);
            ActiveAppKey = appKey;
            return new Scope(this);
        }

        private sealed class Scope(RecordingScopeFactory owner) : IDisposable
        {
            public void Dispose() => owner.ActiveAppKey = null;
        }
    }
}
