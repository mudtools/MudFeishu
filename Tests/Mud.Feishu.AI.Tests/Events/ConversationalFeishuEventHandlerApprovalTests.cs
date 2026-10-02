// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.AI.Tools;
using Mud.Feishu.AI.Events;
using Mud.Feishu.AI.Channels;
using Mud.Feishu.Abstractions;

namespace Mud.Feishu.AI.Tests.Events;

/// <summary>
/// P4-1：写工具经 <c>ApprovalRequiredAIFunction</c> 包装后，MAF 在<b>调用之前</b>产出
/// <see cref="ToolApprovalRequestContent"/>——处理器必须把它识别为「已发起人工确认」，
/// 提交宿主批准通道，并<b>不</b>把任何批准要素回灌给模型/用户。
/// </summary>
public class ConversationalFeishuEventHandlerApprovalTests
{
    private sealed class DemoEvent : IEventResult
    {
        public string MessageId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    private sealed class RecordingHandler(
        FeishuAgent agent,
        IFeishuEventDeduplicator deduplicator,
        IFeishuToolApprovalChannel? approvalChannel = null,
        IMessageChannel? messageChannel = null)
        : ConversationalFeishuEventHandler<DemoEvent>(
            agent, deduplicator, NullLogger.Instance, messageChannel: messageChannel, approvalChannel: approvalChannel)
    {
        public string? LastReply { get; private set; }

        protected override Task<ConversationRequest> BuildRequestAsync(DemoEvent eventData, CancellationToken cancellationToken)
            => Task.FromResult(new ConversationRequest(
                AppKey: "app-a",
                Scope: ConversationScope.Group(),
                SubjectId: "oc_1",
                SenderId: "ou_sender",
                MessageId: eventData.MessageId,
                MentionedText: eventData.Text));

        protected override Task ReplyAsync(ConversationRequest request, string responseText, CancellationToken cancellationToken)
        {
            LastReply = responseText;
            return Task.CompletedTask;
        }

        protected override Task<string> AssembleUserMessageAsync(ConversationRequest request, CancellationToken cancellationToken)
            => Task.FromResult("请把这条记录写进多维表格");
    }

    private sealed class CapturingChannel(Exception? failure = null) : IFeishuToolApprovalChannel
    {
        internal List<FrameworkToolApprovalRequest> Requests { get; } = [];

        public Task<string?> RequestApprovalAsync(ToolApprovalRequest request, CancellationToken cancellationToken = default)
            => failure is not null ? throw failure : Task.FromResult<string?>(null);

        public Task<string?> RequestFrameworkApprovalAsync(
            FrameworkToolApprovalRequest request, CancellationToken cancellationToken = default)
        {
            if (failure is not null)
            {
                throw failure;
            }

            Requests.Add(request);
            return Task.FromResult<string?>("approval-77");
        }
    }

    private static Mock<IFeishuEventDeduplicator> CreateDeduplicator()
    {
        var deduplicator = new Mock<IFeishuEventDeduplicator>();
        deduplicator.Setup(d => d.TryMarkAsProcessingAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = false });
        deduplicator.Setup(d => d.MarkAsCompletedAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return deduplicator;
    }

    /// <summary>模拟「写工具被拦下来」的首轮回应：内容里直接给出框架审批请求。</summary>
    private static Mock<IChatClient> CreateApprovalClient(string toolName = "bitable.add_record")
    {
        var call = new FunctionCallContent(
            "call-1", toolName, new Dictionary<string, object?> { ["table_id"] = "tbl_x" });
        var approvalRequest = new ToolApprovalRequestContent("ficc_call-1", call);

        var client = new Mock<IChatClient>();
        client
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, [approvalRequest])));
        return client;
    }

    private static EventData DemoEventData(string eventId) => new()
    {
        EventId = eventId,
        Event = new DemoEvent { MessageId = "om_1", Text = "新增一条记录" },
    };

    [Fact]
    public async Task HandleAsync_ShouldNotifyHost_AndReplyPending_WhenApprovalRequested()
    {
        var agent = new FeishuAgent(
            CreateApprovalClient().Object, new FeishuAgentOptions { Instructions = "x" });
        var channel = new CapturingChannel();
        var handler = new RecordingHandler(agent, CreateDeduplicator().Object, channel);

        await handler.HandleAsync(DemoEventData("evt-approval-1"), default);

        channel.Requests.Should().ContainSingle("待确认项必须提交宿主批准通道——这是批准的唯一入口");
        channel.Requests[0].ToolName.Should().Be("bitable.add_record");
        channel.Requests[0].RequestId.Should().Be("ficc_call-1",
            "RequestId 必须原样携带：回灌响应时要靠它把批准绑定到框架请求");
        channel.Requests[0].AppKey.Should().Be("app-a");
        channel.Requests[0].ConversationKey.Should().NotBeNullOrEmpty("会话键供宿主关联审批界面");

        handler.LastReply.Should().NotBeNullOrEmpty("必须给用户一个可见的『等待确认』答复，而不是静默");
    }

    [Fact]
    public async Task HandleAsync_ShouldNotLeakApprovalSecrets_IntoUserVisibleReply()
    {
        // R2-1 的同一条纪律在 P4-1 下同样适用：等待确认的答复不得携带任何批准要素。
        var agent = new FeishuAgent(
            CreateApprovalClient().Object, new FeishuAgentOptions { Instructions = "x" });
        var channel = new CapturingChannel();
        var handler = new RecordingHandler(agent, CreateDeduplicator().Object, channel);

        await handler.HandleAsync(DemoEventData("evt-approval-2"), default);

        handler.LastReply.Should().NotContain("ficc_call-1", "答复不得携带 requestId（否则模型可据此构造批准）");
        handler.LastReply.Should().NotContain("tbl_x", "答复不得回灌入参原文");
        handler.LastReply.Should().NotContain("approval-77", "答复不得回灌宿主关联号");
    }

    [Fact]
    public async Task HandleAsync_ShouldStillReplyPending_WhenApprovalChannelThrows()
    {
        // 通道故障 ⇒ fail-closed：写工具保持未执行，但用户仍收到「等待确认」，不静默、不自动放行。
        var agent = new FeishuAgent(
            CreateApprovalClient().Object, new FeishuAgentOptions { Instructions = "x" });
        var handler = new RecordingHandler(
            agent, CreateDeduplicator().Object, new CapturingChannel(new InvalidOperationException("通道故障")));

        var act = async () => await handler.HandleAsync(DemoEventData("evt-approval-3"), default);

        await act.Should().NotThrowAsync("通道故障不得冒到事件循环（按『无法发起确认』处理）");
        handler.LastReply.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task HandleAsync_ShouldNotifyApprovalRequests_WhenBeginFailsAndModelDefersToolCall()
    {
        // R5-1：流式 Begin 失败 → 回退非流式，该回退路径**必须与主非流式路径共用同一回合实现**。
        // 修复前形态：回退路径直接 `return (response.Text, false)`，审批请求被静默丢弃 ——
        // 用户收不到确认提示（空文本被 R2-9 守卫吞掉）、宿主批准通道收不到回调、写工具永久挂起。
        var agent = new FeishuAgent(
            CreateApprovalClient().Object, new FeishuAgentOptions { Instructions = "x" });
        var channel = new CapturingChannel();
        var messageChannel = new Mock<IMessageChannel>();
        messageChannel
            .Setup(c => c.BeginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("飞书占位消息创建失败（限流）"));

        var handler = new RecordingHandler(
            agent, CreateDeduplicator().Object, channel, messageChannel.Object);

        await handler.HandleAsync(DemoEventData("evt-approval-fallback"), default);

        channel.Requests.Should().ContainSingle("回退路径同样必须把待确认项提交宿主批准通道");
        channel.Requests[0].RequestId.Should().Be("ficc_call-1",
            "RequestId 必须原样携带（回灌响应靠它绑定框架请求）");
        handler.LastReply.Should().NotBeNullOrEmpty("用户必须收到『等待确认』答复，而不是被空回复守卫静默吞掉");
        handler.LastReply.Should().NotContain("ficc_call-1", "答复不得携带批准要素");
    }
}
