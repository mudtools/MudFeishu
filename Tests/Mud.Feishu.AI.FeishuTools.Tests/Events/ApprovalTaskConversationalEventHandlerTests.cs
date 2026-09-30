// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.EventHandlers;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.AI.FeishuTools.Events;
using Mud.Feishu.DataModels.Messages;
using Mud.Feishu.EventCallback.Approval;

namespace Mud.Feishu.AI.FeishuTools.Tests.Events;

/// <summary>
/// R2-01b：审批任务事件处理器的投递与"触发文本入模型"链路。
/// </summary>
/// <remarks>
/// 本类与 <see cref="TaskUpdatedConversationalEventHandlerTests"/> 同批——两者是同一处缺陷的
/// 两个实例（<c>MentionedText</c> 不进用户消息 ⇒ 模型从未被调用；平台返回错误码时静默成功）。
/// 只修其中一个、把另一个留给"下次"，正是本轮根因 R-C 批评的处置方式。
/// </remarks>
public class ApprovalTaskConversationalEventHandlerTests
{
    private const string OperatorOpenId = "ou_operator";

    /// <summary>触发文本必须到达模型，且回复必须真投递到操作人 open_id。</summary>
    [Fact]
    public async Task HandleAsync_ShouldSendTriggerTextToModel_AndDeliverToOperator()
    {
        var (handler, messageClient, chatClient, _) = CreateHandler();
        messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<MessageDataResult> { Code = 0, Data = new MessageDataResult { MessageId = "om_ok" } });

        var captured = new List<IEnumerable<ChatMessage>>();
        chatClient
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>((messages, _, _) => captured.Add(messages))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "模型回答")));

        await handler.HandleAsync(ApprovalEvent("evt-1"), default);

        captured.Should().NotBeEmpty("触发文本非空时模型必须被调用（R2-01b：此前用户消息恒空导致从未调用）");
        var userText = string.Join(
            "\n",
            captured.SelectMany(static batch => batch)
                .Where(static m => m.Role == ChatRole.User)
                .Select(static m => m.Text));
        userText.Should().Contain("AP-1", "审批通知文本必须进入用户消息");

        messageClient.Verify(
            c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.ReceiveId == OperatorOpenId),
                "open_id",
                It.IsAny<CancellationToken>()),
            Times.Once,
            "回复必须真实下发到操作人 open_id");
    }

    /// <summary>回复 content 必须是合法 JSON 且文本可<b>无损</b>往返（含引号/换行/制表符等需转义字符）。</summary>
    [Fact]
    public async Task HandleAsync_ShouldEscapeReplyContent_Losslessly()
    {
        // 缺陷原始形态：手写 `Replace("\\","\\\\").Replace("\"","\\\"")` 只处理两种字符，
        // 模型回复里的真实换行（\n）会让 content 变成非法 JSON（未转义的控制字符）。
        const string ReplyWithEscapes = "第一行\n第二行\t带\"引号\"与\\反斜杠";

        var (handler, messageClient, _, _) = CreateHandler(replyText: ReplyWithEscapes);
        SendMessageRequest? sent = null;
        messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<SendMessageRequest, string, CancellationToken>((request, _, _) => sent = request)
            .ReturnsAsync(new FeishuApiResult<MessageDataResult> { Code = 0, Data = new MessageDataResult { MessageId = "om_ok" } });

        await handler.HandleAsync(ApprovalEvent("evt-1"), default);

        sent.Should().NotBeNull();
        using var document = System.Text.Json.JsonDocument.Parse(sent!.Content);
        document.RootElement.GetProperty("text").GetString().Should().Be(
            ReplyWithEscapes, "content 必须经 JsonObject 构造（转义完整、可无损往返）");
    }

    /// <summary>平台返回错误码时不得静默成功——必须上抛以触发幂等回滚 + 重投递。</summary>
    [Fact]
    public async Task HandleAsync_ShouldRollback_WhenPlatformRejectsDelivery()
    {
        var (handler, messageClient, _, deduplicator) = CreateHandler();
        messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<MessageDataResult> { Code = 230002, Msg = "目标不可达" });

        var act = async () => await handler.HandleAsync(ApprovalEvent("evt-1"), default);

        await act.Should().ThrowAsync<InvalidOperationException>(
            "平台拒绝时必须上抛——此前实现不检查 outcome，会静默把本轮判为已消费，用户永远收不到答复");
        deduplicator.Verify(
            d => d.RollbackProcessingAsync(BusinessKey("evt-1"), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "回滚必须用带命名空间的真实业务键（用裸 EventId 断言会恒不命中，从而掩盖缺陷）");
    }

    /// <summary>未注册消息客户端时应在调用模型之前短路（回复通道不可用 ⇒ 调模型纯属白花）。</summary>
    [Fact]
    public async Task HandleAsync_ShouldSkipModel_WhenMessageClientIsMissing()
    {
        var (handler, _, chatClient, _) = CreateHandler(withMessageClient: false);

        await handler.HandleAsync(ApprovalEvent("evt-1"), default);

        chatClient.Verify(
            c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "回复通道不可用时可投递性在调用模型前即可判定，必须在模型调用前短路");
    }

    /// <summary>自动通过类型的审批任务 open_id 为空 ⇒ 无可投递操作人，同样应在模型调用前短路。</summary>
    [Fact]
    public async Task HandleAsync_ShouldSkipModel_WhenOperatorOpenIdIsEmpty()
    {
        var (handler, _, chatClient, _) = CreateHandler();
        var eventData = ApprovalEvent("evt-1");
        ((ApprovalTaskResult)eventData.Event!).OpenId = null;
        ((ApprovalTaskResult)eventData.Event!).UserId = null;

        await handler.HandleAsync(eventData, default);

        chatClient.Verify(
            c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "无收件人（自动通过类型）时必须在模型调用前短路");
    }

    // ────────── 辅助 ──────────

    private static (
        ApprovalTaskConversationalEventHandler Handler,
        Mock<Mud.Feishu.IFeishuTenantV1Message> MessageClient,
        Mock<IChatClient> ChatClient,
        Mock<IFeishuEventDeduplicator> Deduplicator) CreateHandler(
        bool withMessageClient = true,
        string replyText = "模型回答")
    {
        var chatClient = new Mock<IChatClient>();
        chatClient
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, replyText)));

        var agent = new FeishuAgent(chatClient.Object, new FeishuAgentOptions { Instructions = "x" });
        var deduplicator = CreateDeduplicator();
        var messageMock = new Mock<Mud.Feishu.IFeishuTenantV1Message>();

        var handler = new ApprovalTaskConversationalEventHandler(
            agent,
            deduplicator.Object,
            withMessageClient ? messageMock.Object : null,
            logger: NullLogger.Instance);

        return (handler, messageMock, chatClient, deduplicator);
    }

    private static Mock<IFeishuEventDeduplicator> CreateDeduplicator()
    {
        var deduplicator = new Mock<IFeishuEventDeduplicator>();
        deduplicator
            .Setup(d => d.TryMarkAsProcessingAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = false });
        deduplicator
            .Setup(d => d.MarkAsCompletedAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        deduplicator
            .Setup(d => d.RollbackProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return deduplicator;
    }

    /// <summary>业务去重键（自带命名空间；断言回滚时必须用真实键，用裸 EventId 会恒不命中）。</summary>
    private static string BusinessKey(string eventId) => $"feishu.agent.approval_task:{eventId}";

    private static EventData ApprovalEvent(string eventId) => new()
    {
        EventId = eventId,
        Event = new ApprovalTaskResult
        {
            OpenId = OperatorOpenId,
            ApprovalCode = "AP-1",
            InstanceCode = "inst-1",
            TaskId = "task-1",
            Status = "PENDING",
        },
    };
}
