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
using Mud.Feishu.AI.Conversations;
using Mud.Feishu.AI.Tools.Events;
using Mud.Feishu.DataModels.Messages;
using Mud.Feishu.DataModels.Tasks;
using Mud.Feishu.EventCallback.Task;

namespace Mud.Feishu.AI.Tools.Tests.Events;

/// <summary>
/// R2-01：任务更新事件的回复链路——<b>可投递则投递、不可投递则在调用模型之前短路</b>。
/// </summary>
/// <remarks>
/// <para>
/// 缺陷原始形态（本用例集锁定的事实）：该类此前是 <c>sealed</c> 且 <c>ReplyAsync</c> 只写日志——
/// 每个 <c>task.task.updated_v1</c> 事件消耗<b>一次完整模型调用</b>（计费 + 延迟 + 一轮历史落库），
/// 而回答只进日志，用户永远收不到。无异常、无告警、无指标。
/// </para>
/// <para>
/// 本用例集的核心断言是 <see cref="HandleAsync_ShouldSkipModel_WhenTaskHasNoDeliverableTarget"/> 与
/// <see cref="HandleAsync_ShouldSkipModel_WhenMessageClientIsMissing"/> 里的
/// <c>GetResponseAsync ... Times.Never</c>——它直接锁死"白跑模型"这一成本。
/// </para>
/// </remarks>
public class TaskUpdatedConversationalEventHandlerTests
{
    private const string AssigneeOpenId = "ou_assignee";

    // ────────── ① 可投递 ⇒ 真实投递（且只投一次） ──────────

    [Fact]
    public async Task HandleAsync_ShouldDeliverToAssignee_WhenTaskHasAssignee()
    {
        var (handler, messageClient, chatClient, _) = CreateHandler(TaskWithAssignee());
        messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<MessageDataResult> { Code = 0, Data = new MessageDataResult { MessageId = "om_sent" } });

        await handler.HandleAsync(TaskEvent("evt-1"), default);

        chatClient.Verify(
            c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        messageClient.Verify(
            c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.ReceiveId == AssigneeOpenId && r.MsgType == "text"),
                "open_id",
                It.IsAny<CancellationToken>()),
            Times.Once,
            "可投递时必须真实下发到负责人 open_id（send 计数 = 1）");
    }

    /// <summary>
    /// 投递目标携带的是<b>负责人</b>而非创建者（两者不同时必须取负责人）。
    /// </summary>
    [Fact]
    public async Task HandleAsync_ShouldPreferAssigneeOverCreator()
    {
        var task = new TaskOperationResult
        {
            Task = new TaskInfo
            {
                Creator = new TaskMemberInfo { Id = "ou_creator", Role = "creator", Type = "user" },
                Members =
                [
                    new TaskMemberInfo { Id = AssigneeOpenId, Role = "assignee", Type = "user" },
                ],
            },
        };

        var (handler, messageClient, _, _) = CreateHandler(task);
        messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<MessageDataResult> { Code = 0, Data = new MessageDataResult { MessageId = "om_sent" } });

        await handler.HandleAsync(TaskEvent("evt-1"), default);

        messageClient.Verify(
            c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.ReceiveId == AssigneeOpenId), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// R2-01b（本轮审查新增发现）：事件触发文本必须真的到达模型。
    /// </summary>
    /// <remarks>
    /// 缺陷原始形态：<c>MentionedText</c> 此前<b>不进</b>用户消息（基类默认只拼装配器产物），
    /// 于是无装配器的宿主上用户消息恒空、空消息守卫直接短路——模型<b>根本不会被调用</b>，
    /// 本处理器端到端完全 no-op。该缺陷比"白跑一次模型"更严重，且此前无任何用例覆盖。
    /// </remarks>
    [Fact]
    public async Task HandleAsync_ShouldSendTriggerTextToModel()
    {
        var (handler, messageClient, chatClient, _) = CreateHandler(TaskWithAssignee());
        messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<MessageDataResult> { Code = 0, Data = new MessageDataResult { MessageId = "om_sent" } });

        var captured = new List<IEnumerable<ChatMessage>>();
        chatClient
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>((messages, _, _) => captured.Add(messages))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "模型回答")));

        await handler.HandleAsync(TaskEvent("evt-1"), default);

        captured.Should().NotBeEmpty("触发文本非空时模型必须被调用（此前用户消息恒空导致从未调用）");
        var userText = string.Join(
            "\n",
            captured.SelectMany(static batch => batch)
                .Where(static m => m.Role == ChatRole.User)
                .Select(static m => m.Text));

        userText.Should().Contain("task-1", "任务更新的通知文本必须进入用户消息，否则模型看不到任何事件信息");
        userText.Should().Contain("任务详情发生变化", "obj_type 的语义映射必须随触发文本一并送去（模型据此判断发生了什么）");
    }

    // ────────── ② 不可投递 ⇒ 模型零调用（本任务的核心断言） ──────────

    [Fact]
    public async Task HandleAsync_ShouldSkipModel_WhenTaskHasNoDeliverableTarget()
    {
        // 任务既无负责人也无创建者 ⇒ 解析不出收件人。
        var (handler, messageClient, chatClient, _) = CreateHandler(new TaskOperationResult { Task = new TaskInfo() });

        await handler.HandleAsync(TaskEvent("evt-1"), default);

        chatClient.Verify(
            c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "解析不出投递目标时必须在调用模型之前短路——否则每个事件白花一次完整模型调用");
        messageClient.Verify(
            c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldSkipModel_WhenMessageClientIsMissing()
    {
        var (handler, _, chatClient, _) = CreateHandler(TaskWithAssignee(), withMessageClient: false);

        await handler.HandleAsync(TaskEvent("evt-1"), default);

        chatClient.Verify(
            c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "未注册消息客户端时回复通道不可用，必须在调用模型之前短路");
    }

    [Fact]
    public async Task HandleAsync_ShouldSkipModel_WhenTaskClientIsMissing()
    {
        var (handler, _, chatClient, _) = CreateHandler(TaskWithAssignee(), withTaskClient: false);

        await handler.HandleAsync(TaskEvent("evt-1"), default);

        chatClient.Verify(
            c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "未注册任务客户端时无法解析收件人（事件不带 open_id），必须在调用模型之前短路");
    }

    // ────────── ③ 下发失败 ⇒ 幂等回滚（重投递时由 outbox 补发，不重跑模型） ──────────

    [Fact]
    public async Task HandleAsync_ShouldRollbackDeduplication_WhenDeliveryFails()
    {
        var (handler, messageClient, chatClient, deduplicator) = CreateHandler(TaskWithAssignee());
        messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<MessageDataResult> { Code = 99991, Msg = "下发被平台拒绝" });

        var act = async () => await handler.HandleAsync(TaskEvent("evt-1"), default);

        await act.Should().ThrowAsync<InvalidOperationException>("下发失败必须上抛，使基类走幂等回滚 + 重投递");
        deduplicator.Verify(
            d => d.RollbackProcessingAsync(BusinessKey("evt-1"), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "下发失败未回滚幂等标记会让事件被永久判为「已消费」，用户永远收不到回复");
        chatClient.Verify(
            c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "本轮确实调用过模型（失败发生在投递阶段）——重投递由 outbox 补发同一文本，不重跑模型");
    }

    // ────────── 辅助 ──────────

    /// <summary>
    /// 构造被测处理器及其可断言协作件。
    /// </summary>
    /// <param name="task">任务详情（决定能否解析出收件人）。</param>
    /// <param name="withTaskClient">是否注册任务客户端（<see langword="false"/> 模拟软缺席）。</param>
    /// <param name="withMessageClient">是否注册消息客户端（<see langword="false"/> 模拟回复通道不可用）。</param>
    private static (
        TaskUpdatedConversationalEventHandler Handler,
        Mock<Mud.Feishu.IFeishuTenantV1Message> MessageClient,
        Mock<IChatClient> ChatClient,
        Mock<IFeishuEventDeduplicator> Deduplicator) CreateHandler(
        TaskOperationResult? task = null,
        bool withTaskClient = true,
        bool withMessageClient = true)
    {
        var chatClient = new Mock<IChatClient>();
        chatClient
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "模型回答")));

        var agent = new FeishuAgent(chatClient.Object, new FeishuAgentOptions { Instructions = "x" });
        var deduplicator = CreateDeduplicator();
        var messageMock = new Mock<Mud.Feishu.IFeishuTenantV1Message>();

        var handler = new TaskUpdatedConversationalEventHandler(
            agent,
            deduplicator.Object,
            withMessageClient ? messageMock.Object : null,
            withTaskClient ? BuildTaskClient(task) : null,
            scopeFactory: null,
            logger: NullLogger.Instance);

        return (handler, messageMock, chatClient, deduplicator);
    }

    private static Mud.Feishu.IFeishuTenantV2Task BuildTaskClient(TaskOperationResult? task)
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV2Task>();
        client
            .Setup(c => c.GetTaskByIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<TaskOperationResult> { Code = 0, Data = task ?? TaskWithAssignee() });
        return client.Object;
    }

    private static TaskOperationResult TaskWithAssignee() => new()
    {
        Task = new TaskInfo
        {
            Guid = "task-1",
            Members = [new TaskMemberInfo { Id = AssigneeOpenId, Role = "assignee", Type = "user" }],
        },
    };

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
    private static string BusinessKey(string eventId) => $"feishu.agent.task_updated:{eventId}";

    private static EventData TaskEvent(string eventId) => new()
    {
        EventId = eventId,
        Event = new TaskUpdatedResult { TaskId = "task-1", ObjType = 1 },
    };
}
