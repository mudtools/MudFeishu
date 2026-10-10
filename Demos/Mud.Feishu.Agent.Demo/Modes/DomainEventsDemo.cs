// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Conversations;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Channels;
using Mud.Feishu.AI.Events;
using Mud.Feishu.AI.Extensions;
using Mud.Feishu.AI.Tools;
using Mud.Feishu.AI.Tools.Events;
using Mud.Feishu.DataModels;
using Mud.Feishu.EventCallback;
using Mud.Feishu.EventCallback.Approval;
using Mud.Feishu.EventCallback.Bitable;
using Mud.Feishu.EventCallback.Task;

namespace Mud.Feishu.Agent.Demo;

/// <summary>
/// <b>R-13</b>：领域会话事件处理器 + 流式通道降级链的<b>真实调用点</b>演示。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要这个模式（缺陷背景）</b>：以下 5 个类此前<b>只有各自单测消费</b>——发布面承诺了
/// "宿主注册即生效"，却没有任何真实调用点，正确性完全靠测试维护：
/// <list type="bullet">
/// <item><see cref="ApprovalTaskConversationalEventHandler"/>（审批任务状态变更 → 会话 → 模型 → 回复）；</item>
/// <item><see cref="BitableRecordChangedConversationalEventHandler"/>（多维表格记录变更）；</item>
/// <item><see cref="TaskUpdatedConversationalEventHandler"/>（任务更新）；</item>
/// <item><see cref="ApprovalContextAssembler"/> / <see cref="BitableRecordContextAssembler"/>
/// （事件事实 → 结构化 prompt 片段，此前无消费者）；</item>
/// <item><see cref="CardStreamMessageChannel"/> + <see cref="StreamingChannelChain"/>
/// （卡片流 → 分片编辑的降级链，此前连 Demo 都没走）。</item>
/// </list>
/// </para>
/// <para>
/// <b>事件注入方式（不依赖外部平台通道）</b>：事件载荷由本模式<b>直接构造</b>并调用
/// <c>HandleAsync</c>——这正是 R-13 建议的 harness 形态（避免依赖 WebSocket/Webhook 通道与真实平台投递）。
/// 生产形态是"通道派发 + 应用键访问器提供键"，本模式用 <see cref="DemoAppKeyAccessor"/> 提供等价事实。
/// </para>
/// <para>
/// <b>两节演示</b>：① 三个领域处理器各走一轮真实事件（含上下文装配器端到端）；
/// ② 流式通道降级链走一轮真实流式回复，并对"增量落点次数"做断言（0 次即抛——链路没被走通）。
/// 未配置 <c>StreamChatId</c> 时跳过 ② 并打印提示（不静默降级成"看起来跑过了"）。
/// </para>
/// </remarks>
public static class DomainEventsDemo
{
    /// <summary>
    /// 运行领域事件演示（模式开关：<c>FeishuDomainEventsDemo:Enabled</c>；凭证读统一节 <c>FeishuDemo</c>）。
    /// </summary>
    /// <param name="configuration">配置来源。</param>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/> 为 <see langword="null"/>。</exception>
    public static async Task RunAsync(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var settings = DomainEventsDemoSettings.FromConfiguration(configuration);
        settings.Validate();

        var appKey = DemoAgentDefaults.DefaultAppKey;
        var feishuConfig = DemoAppConfig.CreateSingleApp(appKey, settings.AppId, settings.AppSecret);

        var services = new ServiceCollection()
            .AddDemoLogging(configuration)
            .AddFeishuApp(feishuConfig, "FeishuApps")
            .AddFeishuServices(builder => builder
                .AddMessageApi()
                // AddCardApi 注册 Cards 模块（含卡片流客户端 IFeishuTenantV2AppCardMessageStream）；
                // 未启用时降级链内只剩编辑通道（注册期软缺席，不是错误）。
                .AddCardApi()
                // 任务模块：任务事件处理器据它解析负责人（投递目标）。
                .AddTaskApi())
            .AddDemoAgent(settings.Model, "FeishuDomainEventsDemo", InstructionsText, options =>
            {
                options.Tools = [.. FeishuToolNames.ReadonlyAll];
                options.AllowedIdentities = ["tenant", "user"];
            })
            .AddFeishuTools()

            // ② 流式通道降级链：首选卡片流（CardStreamMessageChannel），Begin 失败自动降级分片编辑。
            .AddFeishuStreamingChannel();

        // 事件处理器的应用键事实（缺它时基类会因"有工具链 + 无 appKey"fail-closed 抛错）。
        services.AddSingleton<IAppKeyAccessor>(new DemoAppKeyAccessor(appKey));

        // 事件幂等：内置内存去重器（多实例生产应替换为 Redis 实现——AddFeishuRedisConversationGate 同源纪律）。
        services.TryAddSingleton<IFeishuEventDeduplicator>(sp =>
            new FeishuEventDeduplicator(sp.GetService<ILogger<FeishuEventDeduplicator>>()));

        // ③ 领域上下文装配器：D-2 记录的"无消费方"两个装配器在此获得真实消费者
        // （装配器与处理器靠"事实键 + EventKey"耦合，写错不会编译报错——故必须端到端跑一次）。
        services.TryAddSingleton<ApprovalContextAssembler>();
        services.TryAddSingleton<BitableRecordContextAssembler>();

        // ① 三个领域会话处理器：显式装配（把装配器真正注入处理器，链路可见）。
        services.TryAddSingleton(sp => new ApprovalTaskConversationalEventHandler(
            sp.GetRequiredService<FeishuAgent>(),
            sp.GetRequiredService<IFeishuEventDeduplicator>(),
            sp.GetService<Mud.Feishu.IFeishuTenantV1Message>(),
            sp.GetService<ILogger<ApprovalTaskConversationalEventHandler>>(),
            contextAssemblers: [sp.GetRequiredService<ApprovalContextAssembler>()],
            toolContextAccessor: sp.GetRequiredService<IFeishuToolContextAccessor>(),
            messageChannel: sp.GetService<IMessageChannel>(),
            appKeyAccessor: sp.GetRequiredService<IAppKeyAccessor>(),
            appContextScopeFactory: sp.GetRequiredService<IFeishuAppContextScopeFactory>()));

        services.TryAddSingleton(sp => new BitableRecordChangedConversationalEventHandler(
            sp.GetRequiredService<FeishuAgent>(),
            sp.GetRequiredService<IFeishuEventDeduplicator>(),
            sp.GetService<Mud.Feishu.IFeishuTenantV1Message>(),
            sp.GetService<ILogger<BitableRecordChangedConversationalEventHandler>>(),
            contextAssemblers: [sp.GetRequiredService<BitableRecordContextAssembler>()],
            toolContextAccessor: sp.GetRequiredService<IFeishuToolContextAccessor>(),
            messageChannel: sp.GetService<IMessageChannel>(),
            appKeyAccessor: sp.GetRequiredService<IAppKeyAccessor>(),
            appContextScopeFactory: sp.GetRequiredService<IFeishuAppContextScopeFactory>()));

        services.TryAddSingleton(sp => new TaskUpdatedConversationalEventHandler(
            sp.GetRequiredService<FeishuAgent>(),
            sp.GetRequiredService<IFeishuEventDeduplicator>(),
            sp.GetService<Mud.Feishu.IFeishuTenantV1Message>(),
            // 任务客户端用于解析负责人（投递目标）；缺席时处理器在调用模型前短路（不白跑模型）。
            taskClient: sp.GetService<Mud.Feishu.IFeishuTenantV2Task>(),
            scopeFactory: sp.GetRequiredService<IFeishuAppContextScopeFactory>(),
            logger: sp.GetService<ILogger<TaskUpdatedConversationalEventHandler>>(),
            toolContextAccessor: sp.GetRequiredService<IFeishuToolContextAccessor>(),
            messageChannel: sp.GetService<IMessageChannel>(),
            appKeyAccessor: sp.GetRequiredService<IAppKeyAccessor>()));

        using var provider = services.BuildServiceProvider();

        var channel = provider.GetService<IMessageChannel>();
        Console.WriteLine("领域事件演示：三个处理器已装配，事件载荷由本模式直接构造并派发。");
        Console.WriteLine(channel is not null
            ? $"流式通道链：{channel.GetType().Name}（卡片流客户端缺席时链内只剩编辑通道——软缺席，非错误）"
            : "流式通道链：未装配（未调用 AddFeishuStreamingChannel）");

        // ── ① 三个领域处理器各走一轮真实事件 ──
        await DispatchAsync(
            "审批任务（approval_task）",
            ct => provider.GetRequiredService<ApprovalTaskConversationalEventHandler>().HandleAsync(ApprovalEvent(), ct));

        await DispatchAsync(
            "多维表格记录变更（drive.file.bitable_record_changed）",
            ct => provider.GetRequiredService<BitableRecordChangedConversationalEventHandler>().HandleAsync(BitableEvent(), ct));

        await DispatchAsync(
            "任务更新（task.task.updated_v1）",
            ct => provider.GetRequiredService<TaskUpdatedConversationalEventHandler>().HandleAsync(TaskEvent(), ct));

        // ── ② 流式通道降级链走一轮真实流式回复 ──
        if (channel is null || string.IsNullOrEmpty(settings.StreamChatId))
        {
            Console.WriteLine(
                $"已跳过流式链路演示（{(channel is null ? "通道未注册" : "未配置 " + DomainEventsDemoSettings.SectionName + ":StreamChatId")}）");
            return;
        }

        var chatId = settings.StreamChatId!;
        var agent = provider.GetRequiredService<FeishuAgent>();
        var conversationKey = ConversationKeyBuilder.Build(appKey, ConversationScope.P2P(), DemoAgentDefaults.DemoUserId);
        var session = await agent.GetOrCreateSessionAsync(conversationKey);

        using var toolScope = provider.GetRequiredService<IFeishuToolContextAccessor>()
            .Begin(new FeishuToolContext(appKey, conversationKey));

        var messageId = await channel.BeginAsync(appKey, chatId);
        var increments = 0;
        await foreach (var update in agent.RunStreamingAsync(StreamingPrompt, session))
        {
            if (string.IsNullOrEmpty(update.Text))
            {
                continue;
            }

            await channel.WriteStreamAsync(appKey, chatId, messageId, update.Text);
            increments++;
        }

        await channel.FlushAsync(appKey, chatId, messageId);
        await agent.SaveSessionAsync(conversationKey, session);

        // 增量落点断言（"存在即可"不算通过）：零增量说明模型流没产出文本，或增量根本没写进链路。
        if (increments == 0)
        {
            throw new InvalidOperationException(
                "流式链路走完了但增量落点为 0——模型流未产出文本或 WriteStreamAsync 未被调用，"
                + "该形态下飞书侧只会看到一个空占位消息");
        }

        Console.WriteLine($"流式回复完成：{increments} 次增量落点（链：{channel.GetType().Name}，目标 chat：{chatId}）");
    }

    /// <summary>派发一个事件并打印结果（异常不吞：装配/投递缺陷必须当场可见）。</summary>
    /// <remarks>
    /// 两种终态都是<b>预期</b>的：① 可投递 ⇒ 调模型 + 下发回复；② 不可投递（如演示用的任务 id
    /// 在平台上不存在 ⇒ 解析不出负责人）⇒ 在<b>调用模型之前</b>短路（省下一次完整模型调用）。
    /// 该模式正是 R2-01 锁定的行为，故此处不把"跳过"渲染成失败。
    /// </remarks>
    private static async Task DispatchAsync(string label, Func<CancellationToken, Task> dispatch)
    {
        Console.WriteLine($"→ 派发 {label} …");
        await dispatch(CancellationToken.None);
        Console.WriteLine($"  {label} 已处理（可投递 ⇒ 回复已下发；不可投递 ⇒ 已在模型调用前短路）");
    }

    /// <summary>审批任务事件载荷（<c>PENDING</c>：操作人 open_id 非空 ⇒ 可投递到其单聊）。</summary>
    private static EventData ApprovalEvent() => new()
    {
        EventId = "evt-demo-approval",
        Event = new ApprovalTaskResult
        {
            OpenId = DemoOperatorOpenId,
            ApprovalCode = "approval_demo",
            InstanceCode = "inst-demo-1",
            TaskId = "task-demo-1",
            Status = "PENDING",
        },
    };

    /// <summary>多维表格记录变更事件载荷（含一条"已变更字段"事实，供装配器产片段）。</summary>
    private static EventData BitableEvent() => new()
    {
        EventId = "evt-demo-bitable",
        EventType = FeishuEventTypes.BitableRecordChanged,
        Event = new BitableRecordChangedResult
        {
            FileType = "bitable",
            FileToken = "bascnDemoAppToken",
            TableId = "tblDemo",
            Revision = 1,
            OperatorId = new UserIdInfo { OpenId = DemoOperatorOpenId },
            ActionList =
            [
                new BitableTableRecordAction
                {
                    RecordId = "rec-demo-1",
                    Action = "record_edited",
                    BeforeValue =
                    [
                        new BitableTableRecordActionField { FieldId = "fldStatus", FieldValue = "进行中" },
                    ],
                },
            ],
        },
    };

    /// <summary>任务更新事件载荷（负责人 open_id 供投递目标解析）。</summary>
    private static EventData TaskEvent() => new()
    {
        EventId = "evt-demo-task",
        Event = new TaskUpdatedResult { TaskId = "task-demo-2", ObjType = 1 },
    };

    /// <summary>演示操作人 open_id（不是真实用户；事件载荷里的投递目标）。</summary>
    private const string DemoOperatorOpenId = "ou_demo_operator";

    /// <summary>流式链路的提问（够短以降低演示成本，但足以产生多个增量分片）。</summary>
    private const string StreamingPrompt = "用三句话说明多维表格与电子表格各自适合什么场景。";

    /// <summary>本模式的系统提示词。</summary>
    private const string InstructionsText =
        "你是嵌入在 .NET 服务里的飞书助手。当前处于「领域事件演示」模式："
        + "你会收到由审批任务 / 多维表格记录变更 / 任务更新事件触发的提问，请用简洁中文回答，"
        + "并在需要时调用只读工具核实事实（例如 approval.list_pending_tasks、bitable.query_records、task.list_my_tasks）。";
}
