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
using Mud.Feishu.AI.Events;
using Mud.Feishu.AI.Tools.Events;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.Messages;
using Mud.Feishu.EventCallback;
using Mud.Feishu.EventCallback.Bitable;

namespace Mud.Feishu.AI.Tools.Tests.Events;

/// <summary>
/// R7 / C2：多维表格记录变更事件处理器（事件 → 会话 → 模型 → 回复），
/// 以及"事件事实 → 上下文装配器 → prompt"的<b>端到端</b>链路。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么要端到端断言（而不是只测装配器）</b>：装配器与处理器之间靠"事实键约定 + EventKey"耦合，
/// 任何一侧写错（键名不一致、EventKey 常量不一致）都<b>不会编译报错</b>——表现为"片段永远为空"，
/// 也就是这条链路静默失效。故本类让模型输入成为断言对象：片段必须真的出现在用户消息里。
/// </para>
/// </remarks>
public class BitableRecordChangedConversationalEventHandlerTests
{
    private const string OperatorOpenId = "ou_operator";
    private const string UnchangedFieldValue = "原值-不该出现在prompt里";

    /// <summary>事件类型必须精确路由（不像审批/任务处理器那样"通吃"）。</summary>
    [Fact]
    public void SupportedEventType_ShouldBeBitableRecordChanged()
    {
        var (handler, _, _, _) = CreateHandler();

        handler.SupportedEventType.Should().Be(
            FeishuEventTypes.BitableRecordChanged,
            "记录变更是高频事件：必须按事件类型精确路由，不能让其它事件进入本处理器");
    }

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

        await handler.HandleAsync(BitableEvent("evt-1"), default);

        var userText = UserTextOf(captured);
        userText.Should().Contain("tblABC", "记录变更通知文本必须进入用户消息（否则模型从未被调用）");
        userText.Should().Contain("bascnTbl123");

        messageClient.Verify(
            c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.ReceiveId == OperatorOpenId),
                "open_id",
                It.IsAny<CancellationToken>()),
            Times.Once,
            "回复必须真实下发到操作人 open_id（记录变更是行级事件，无 chat_id）");
    }

    /// <summary>
    /// <b>端到端</b>：注册 <see cref="BitableRecordContextAssembler"/> 后，
    /// 变化字段的前后值必须出现在模型输入里，而<b>未变字段不得出现</b>。
    /// </summary>
    [Fact]
    public async Task HandleAsync_ShouldAssembleChangedFieldDiff_WithoutUnchangedFields()
    {
        var (handler, messageClient, chatClient, _) = CreateHandler(withContextAssembler: true);
        messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<MessageDataResult> { Code = 0, Data = new MessageDataResult { MessageId = "om_ok" } });

        var captured = new List<IEnumerable<ChatMessage>>();
        chatClient
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>((messages, _, _) => captured.Add(messages))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "模型回答")));

        await handler.HandleAsync(BitableEvent("evt-1"), default);

        var userText = UserTextOf(captured);
        userText.Should().Contain("修改记录 rec1", "平台枚举码必须翻成中文后进 prompt");
        userText.Should().Contain("新增记录 rec2");
        userText.Should().Contain("fldA", "变化的字段必须出现");
        userText.Should().Contain("新值");
        userText.Should().Contain(ContextBudgets.UntrustedHeader, "表格字段值属不可信数据，必须带标注");
        userText.Should().NotContain(
            UnchangedFieldValue,
            "未变字段绝不得进 prompt（before/after 带的是整行，全量灌入会淹没真正的变更信号）");
    }

    /// <summary>未注册消息客户端时应在调用模型之前短路（回复通道不可用 ⇒ 调模型纯属白花）。</summary>
    [Fact]
    public async Task HandleAsync_ShouldSkipModel_WhenMessageClientIsMissing()
    {
        var (handler, _, chatClient, _) = CreateHandler(withMessageClient: false);

        await handler.HandleAsync(BitableEvent("evt-1"), default);

        chatClient.Verify(
            c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "可投递性在调用模型前即可判定，必须在模型调用前短路");
    }

    /// <summary>解析不出操作人（三个 ID 全空）⇒ 无收件人，同样应在模型调用前短路。</summary>
    [Fact]
    public async Task HandleAsync_ShouldSkipModel_WhenOperatorIsMissing()
    {
        var (handler, _, chatClient, _) = CreateHandler();
        var eventData = BitableEvent("evt-1");
        ((BitableRecordChangedResult)eventData.Event!).OperatorId = null;

        await handler.HandleAsync(eventData, default);

        chatClient.Verify(
            c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "无收件人时必须在模型调用前短路");
    }

    /// <summary>平台返回错误码时不得静默成功——必须上抛以触发幂等回滚 + 重投递。</summary>
    [Fact]
    public async Task HandleAsync_ShouldRollback_WhenPlatformRejectsDelivery()
    {
        var (handler, messageClient, _, deduplicator) = CreateHandler();
        messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<MessageDataResult> { Code = 230002, Msg = "目标不可达" });

        var act = async () => await handler.HandleAsync(BitableEvent("evt-1"), default);

        await act.Should().ThrowAsync<InvalidOperationException>("平台拒绝时必须上抛（否则幂等标记落 Completed，用户永远收不到答复）");
        deduplicator.Verify(
            d => d.RollbackProcessingAsync(BusinessKey("evt-1"), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "回滚必须用带命名空间的真实业务键（用裸 EventId 断言会恒不命中而掩盖缺陷）");
    }

    /// <summary>
    /// R3-1/R3-2：有 appKey 却未注入作用域工厂属装配缺陷 —— 必须 fail-closed，
    /// 禁止回退默认应用身份把回复发给别的租户。
    /// </summary>
    [Fact]
    public async Task HandleAsync_ShouldFailClosed_WithoutSending_WhenScopeFactoryMissing()
    {
        var (handler, messageClient, _, _) = CreateHandler(appKey: "app-a");
        messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<MessageDataResult> { Code = 0, Data = new MessageDataResult { MessageId = "om_ok" } });

        var act = async () => await handler.HandleAsync(BitableEvent("evt-1"), default);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*未注入*");
        messageClient.Verify(
            c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "拒绝对外可见的形式是「一次下发都没有」");
    }

    // ────────── 辅助 ──────────

    private static string UserTextOf(List<IEnumerable<ChatMessage>> captured)
        => string.Join(
            "\n",
            captured.SelectMany(static batch => batch)
                .Where(static m => m.Role == ChatRole.User)
                .Select(static m => m.Text));

    private static (
        BitableRecordChangedConversationalEventHandler Handler,
        Mock<Mud.Feishu.IFeishuTenantV1Message> MessageClient,
        Mock<IChatClient> ChatClient,
        Mock<IFeishuEventDeduplicator> Deduplicator) CreateHandler(
        bool withMessageClient = true,
        bool withContextAssembler = false,
        string replyText = "模型回答",
        string? appKey = null,
        IFeishuAppContextScopeFactory? scopeFactory = null)
    {
        var chatClient = new Mock<IChatClient>();
        chatClient
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, replyText)));

        var agent = new FeishuAgent(chatClient.Object, new FeishuAgentOptions { Instructions = "x" });
        var deduplicator = CreateDeduplicator();
        var messageMock = new Mock<Mud.Feishu.IFeishuTenantV1Message>();

        var appKeyAccessor = new Mock<IAppKeyAccessor>();
        appKeyAccessor.Setup(a => a.CurrentAppKey).Returns(appKey);

        var handler = new BitableRecordChangedConversationalEventHandler(
            agent,
            deduplicator.Object,
            withMessageClient ? messageMock.Object : null,
            logger: NullLogger.Instance,
            contextAssemblers: withContextAssembler ? [new BitableRecordContextAssembler()] : null,
            appKeyAccessor: appKey is null ? null : appKeyAccessor.Object,
            appContextScopeFactory: scopeFactory);

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

    private static string BusinessKey(string eventId) => $"feishu.agent.bitable_record:{eventId}";

    /// <summary>
    /// 事件载荷：一条"修改"（含一个未变字段）+ 一条"新增"。
    /// </summary>
    private static EventData BitableEvent(string eventId) => new()
    {
        EventId = eventId,
        EventType = FeishuEventTypes.BitableRecordChanged,
        Event = new BitableRecordChangedResult
        {
            FileType = "bitable",
            FileToken = "bascnTbl123",
            TableId = "tblABC",
            Revision = 12,
            OperatorId = new UserIdInfo { OpenId = OperatorOpenId },
            ActionList =
            [
                new BitableTableRecordAction
                {
                    RecordId = "rec1",
                    Action = "record_edited",
                    BeforeValue =
                    [
                        new BitableTableRecordActionField { FieldId = "fldA", FieldValue = "旧值" },
                        new BitableTableRecordActionField { FieldId = "fldB", FieldValue = UnchangedFieldValue },
                    ],
                    AfterValue =
                    [
                        new BitableTableRecordActionField { FieldId = "fldA", FieldValue = "新值" },
                        new BitableTableRecordActionField { FieldId = "fldB", FieldValue = UnchangedFieldValue },
                    ],
                },
                new BitableTableRecordAction
                {
                    RecordId = "rec2",
                    Action = "record_added",
                    AfterValue =
                    [
                        new BitableTableRecordActionField { FieldId = "fldC", FieldValue = "新增字段值" },
                    ],
                },
            ],
        },
    };
}
