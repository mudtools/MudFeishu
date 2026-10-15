// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.AgentTools;
using Mud.Feishu.AI.Conversations;
using Mud.Feishu.AI.Events;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.Messages;

namespace Mud.Feishu.AI.Tests.Events;

/// <summary>
/// T2 群聊历史上下文装配器用例。
/// </summary>
/// <remarks>
/// 覆盖：群聊注入 / 单聊跳过 / 无 chatId 跳过 / 拉取失败降级 / 去重 / 三级闸 / Bot 过滤 / untrusted 标注。
/// </remarks>
public class GroupHistoryContextAssemblerTests
{
    private readonly Mock<Mud.Feishu.IFeishuTenantV1Message> _messageClient = new();
    private readonly Mock<IFeishuAppContextScopeFactory> _scopeFactory = new();

    public GroupHistoryContextAssemblerTests()
    {
        _scopeFactory
            .Setup(f => f.BeginScope(It.IsAny<string>()))
            .Returns(new DisposableScope());
    }

    private GroupHistoryContextAssembler CreateSut()
        => new(_messageClient.Object, _scopeFactory.Object);

    private static ConversationRequest GroupRequest(string? chatId = "oc_test", string? messageId = "om_current", string? parentId = null)
        => new("appA", ConversationScope.Group(), "oc_test", "ou_sender", messageId, "hello", ChatId: chatId, ParentId: parentId);

    private static ConversationRequest P2PRequest()
        => new("appA", ConversationScope.P2P(), "ou_sender", "ou_sender", "om_current", "hello", ChatId: "oc_test");

    private static HistoryMessageData Msg(string messageId, string? senderType = "user", string? content = "text content", string? createTime = "1700000000")
        => new()
        {
            MessageId = messageId,
            MsgType = "text",
            CreateTime = createTime,
            Sender = new MessageSender { Id = "ou_" + messageId, SenderType = senderType },
            Body = new MessageBody { Content = content },
        };

    private void SetupHistory(params HistoryMessageData[] messages)
    {
        var pageResult = new FeishuApiPageListResult<HistoryMessageData>
        {
            Code = 0,
            Msg = "ok",
            Data = new ApiPageListResult<HistoryMessageData> { Items = messages.ToList(), HasMore = false },
        };
        SetupGetHistory(pageResult);
    }

    private void SetupGetHistory(FeishuApiPageListResult<HistoryMessageData>? result)
    {
        _messageClient
            .Setup(m => m.GetHistoryMessageAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }

    private void SetupGetHistoryThrows(Exception ex)
    {
        _messageClient
            .Setup(m => m.GetHistoryMessageAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
    }

    [Fact]
    public async Task Assemble_GroupChat_WithHistory_ShouldInjectMessages()
    {
        // Arrange
        SetupHistory(Msg("om_1", content: "{\"text\":\"hello world\"}"), Msg("om_2", content: "{\"text\":\"second message\"}"));

        // Act
        var result = await CreateSut().AssembleAsync(GroupRequest());

        // Assert
        result.Should().NotBeNull("群聊场景应注入历史消息");
        result.Should().Contain("hello world");
        result.Should().Contain("second message");
    }

    [Fact]
    public async Task Assemble_P2PChat_ShouldReturnNull()
    {
        // Arrange
        SetupHistory(Msg("om_1"));

        // Act
        var result = await CreateSut().AssembleAsync(P2PRequest());

        // Assert
        result.Should().BeNull("单聊不走群聊历史装配器——会话历史已足够");
    }

    [Fact]
    public async Task Assemble_NoChatId_ShouldReturnNull()
    {
        // Arrange
        SetupHistory(Msg("om_1"));

        // Act
        var result = await CreateSut().AssembleAsync(GroupRequest(chatId: null));

        // Assert
        result.Should().BeNull("无 chatId 直接返回 null（A15 零行为差异）");
    }

    [Fact]
    public async Task Assemble_NoMessageId_ShouldReturnNull()
    {
        // Arrange
        SetupHistory(Msg("om_1"));

        // Act
        var result = await CreateSut().AssembleAsync(GroupRequest(messageId: ""));

        // Assert
        result.Should().BeNull("无 messageId 直接返回 null");
    }

    [Fact]
    public async Task Assemble_ApiError_ShouldReturnNull_NotThrow()
    {
        // Arrange
        var errorResult = new FeishuApiPageListResult<HistoryMessageData>
        {
            Code = 99991663,
            Msg = "permission denied",
            Data = null,
        };
        SetupGetHistory(errorResult);

        // Act
        var result = await CreateSut().AssembleAsync(GroupRequest());

        // Assert
        result.Should().BeNull("拉取失败/无权限应记日志并返回 null——不阻断对话");
    }

    [Fact]
    public async Task Assemble_Exception_ShouldReturnNull_NotThrow()
    {
        // Arrange
        SetupGetHistoryThrows(new InvalidOperationException("network error"));

        // Act
        var result = await CreateSut().AssembleAsync(GroupRequest());

        // Assert
        result.Should().BeNull("异常应被捕获并返回 null——不阻断对话");
    }

    [Fact]
    public async Task Assemble_ShouldExcludeCurrentMessage_AndParentMessage()
    {
        // Arrange: om_current is the current message, om_parent is the parent (quote)
        SetupHistory(
            Msg("om_1", content: "{\"text\":\"first\"}"),
            Msg("om_parent", content: "{\"text\":\"parent\"}"),
            Msg("om_current", content: "{\"text\":\"current\"}"));

        // Act
        var result = await CreateSut().AssembleAsync(GroupRequest(messageId: "om_current", parentId: "om_parent"));

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("first");
        result.Should().NotContain("current", "本轮消息应被去重排除");
        result.Should().NotContain("parent", "引用父消息应被去重排除（由 QuoteMessage 装配器负责）");
    }

    [Fact]
    public async Task Assemble_ShouldFilterBotMessages()
    {
        // Arrange
        SetupHistory(
            Msg("om_1", senderType: "user", content: "{\"text\":\"user message\"}"),
            Msg("om_2", senderType: "app", content: "{\"text\":\"bot message\"}"));

        // Act
        var result = await CreateSut().AssembleAsync(GroupRequest());

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("user message");
        result.Should().NotContain("bot message", "Bot 自身消息（sender_type=app）应被过滤");
    }

    [Fact]
    public async Task Assemble_ShouldIncludeUntrustedHeader()
    {
        // Arrange
        SetupHistory(Msg("om_1", content: "{\"text\":\"hello\"}"));

        // Act
        var result = await CreateSut().AssembleAsync(GroupRequest());

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(ContextBudgets.UntrustedHeader, "群聊上文必须带 untrusted 标注（防提示注入）");
    }

    [Fact]
    public async Task Assemble_PerMessageLength_TruncationShouldApply()
    {
        // Arrange: a message longer than GroupHistoryPerMessageLength
        var longText = new string('x', ContextBudgets.GroupHistoryPerMessageLength + 100);
        SetupHistory(Msg("om_1", content: "{\"text\":\"" + longText + "\"}"));

        // Act
        var result = await CreateSut().AssembleAsync(GroupRequest());

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(AssemblerBudgets.TruncationMarker, "超单条闸应截断并追加标记");
    }

    [Fact]
    public async Task Assemble_TotalLength_TruncationShouldApply()
    {
        // Arrange: many messages that together exceed GroupHistoryTotalLength
        var messages = Enumerable.Range(0, 20)
            .Select(i => Msg("om_" + i, content: "{\"text\":\"" + new string('y', ContextBudgets.GroupHistoryPerMessageLength) + "\"}"))
            .ToArray();
        SetupHistory(messages);

        // Act
        var result = await CreateSut().AssembleAsync(GroupRequest());

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(AssemblerBudgets.TruncationMarker, "超总预算闸应截断并追加标记");
    }

    [Fact]
    public async Task Assemble_EmptyHistory_ShouldReturnNull()
    {
        // Arrange
        SetupHistory();

        // Act
        var result = await CreateSut().AssembleAsync(GroupRequest());

        // Assert
        result.Should().BeNull("拉取到空列表应返回 null");
    }

    [Fact]
    public async Task Assemble_Order_ShouldBe30()
    {
        // Act & Assert
        var sut = CreateSut();
        sut.Order.Should().Be(30, "GroupHistory Order=30（SenderInfo=10 → QuoteMessage=20 → GroupHistory=30 → Knowledge=100）");
    }

    private sealed class DisposableScope : IDisposable
    {
        public void Dispose() { }
    }
}
