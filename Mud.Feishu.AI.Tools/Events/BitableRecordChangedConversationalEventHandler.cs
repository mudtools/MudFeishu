// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Globalization;
using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Conversations;
using Mud.Feishu.Abstractions.EventHandlers;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.AI.Channels;
using Mud.Feishu.AI.Conversations;
using Mud.Feishu.AI.Events;
using Mud.Feishu.EventCallback;
using Mud.Feishu.EventCallback.Bitable;

namespace Mud.Feishu.AI.Tools.Events;

/// <summary>
/// 多维表格记录变更会话事件处理器（R7 / C2 配套的生产者）：
/// <c>drive.file.bitable_record_changed_v1</c> → 会话 → 模型 → 回复（"这张表刚变了什么"，Agent 据此总结/追问/联动）。
/// </summary>
/// <remarks>
/// <para>
/// <b>与审批/任务事件处理器的差异（事件类型精确匹配）</b>：本类<b>覆写</b>
/// <see cref="SupportedEventType"/> 为 <see cref="FeishuEventTypes.BitableRecordChanged"/>——
/// 事件分派器按事件类型路由，其它事件不会进入本处理器（既有的审批/任务处理器是"通吃"式，
/// 靠投递判定短路兜底；本类是新增能力，直接走精确路由，更省一次无效反序列化）。
/// </para>
/// <para>
/// <b>平台事实 → 结构化事实（<see cref="FeishuEventFact"/>）</b>：只登记事件载荷里真实存在的字段
/// （表格 token / 数据表 / 版本号 / 操作人 / 每条行操作 / <b>仅变化字段</b>的前后值）。
/// 表名、字段名<b>不在</b>事件载荷里（需另调 <c>bitable.list_tables</c> / <c>bitable.list_fields</c>）——
/// 不臆造这些事实（模型会以为事件自带）。
/// </para>
/// <para>
/// <b>事件频率与开通方式（重要）</b>：记录变更可以是<b>高频</b>事件（批量导入/脚本写入会让一次订阅
/// 变成连续多轮模型调用）。故本处理器是<b>可选装配</b>：宿主不注册即完全不生效；注册即"每条订阅表变更
/// 都跑一轮模型"。平台本身只对<b>被订阅</b>的多维表格投递事件，宿主应据此收窄订阅面。
/// </para>
/// <para>
/// <b>幂等业务键</b>：自带命名空间 <c>feishu.agent.bitable_record:{EventId}</c>（D 系契约：禁止裸 EventId）。
/// </para>
/// </remarks>
public sealed class BitableRecordChangedConversationalEventHandler(
    FeishuAgent agent,
    IFeishuEventDeduplicator businessDeduplicator,
    Mud.Feishu.IFeishuTenantV1Message? messageClient,
    ILogger? logger = null,
    IReadOnlyList<IContextAssembler>? contextAssemblers = null,
    IFeishuToolContextAccessor? toolContextAccessor = null,
    IMessageChannel? messageChannel = null,
    IConversationGate? conversationGate = null,
    IAppKeyAccessor? appKeyAccessor = null,
    IFeishuToolApprovalChannel? approvalChannel = null,
    IFeishuAppContextScopeFactory? appContextScopeFactory = null,
    IFeishuPendingApprovalStore? pendingApprovalStore = null)
    : ConversationalFeishuEventHandler<BitableRecordChangedResult>(
        agent, businessDeduplicator, logger, contextAssemblers, toolContextAccessor, messageChannel, conversationGate, appKeyAccessor,
        approvalChannel, appContextScopeFactory: appContextScopeFactory, pendingApprovalStore: pendingApprovalStore)
{
    /// <summary>解析不出操作人时的会话主体占位值（只用于会话分桶，<b>不是</b>可投递接收方）。</summary>
    private const string UnknownSubject = "unknown_operator";

    /// <summary>单次事件最多登记的行操作条数（防一次批量导入灌爆 prompt 预算）。</summary>
    internal const int MaxActions = 5;

    /// <summary>单条行操作最多登记的变化字段数。</summary>
    internal const int MaxDiffsPerAction = 6;

    private readonly Mud.Feishu.IFeishuTenantV1Message? _messageClient = messageClient;

    /// <inheritdoc />
    /// <remarks>
    /// 精确路由：只接收多维表格记录变更事件（见类注释的差异说明）。
    /// </remarks>
    public override string SupportedEventType => FeishuEventTypes.BitableRecordChanged;

    /// <inheritdoc />
    protected override Task<ConversationRequest> BuildRequestAsync(BitableRecordChangedResult eventData, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        // 记录变更面向操作人（open_id），单聊维度。
        var operatorId = OperatorOf(eventData);
        var subjectId = string.IsNullOrEmpty(operatorId) ? UnknownSubject : operatorId!;

        var request = new ConversationRequest(
            AppKey: CurrentAppKey ?? string.Empty,
            Scope: ConversationScope.P2P(),
            SubjectId: subjectId,
            SenderId: operatorId ?? string.Empty,
            MessageId: string.Empty,
            MentionedText: BuildNotification(eventData),

            // R7 / C2：事件维度（供 BitableRecordContextAssembler 装配"变化字段 diff"片段）。
            // 宿主把 BitableRecordContextAssembler 放进 contextAssemblers 即启用；
            // 不传时本字段无消费者，行为 = 纯文本通知（与既有事件处理器一致）。
            EventKey: FeishuEventKeys.BitableRecordChanged,
            EventFacts: BuildFacts(eventData));

        return Task.FromResult(request);
    }

    /// <summary>
    /// 记录变更事件 → 有序结构化事实。
    /// </summary>
    /// <remarks>
    /// 键约定见 <see cref="BitableRecordContextAssembler"/>（单一源）：标量键 + <c>action.n</c> + <c>diff.n.m</c>。
    /// </remarks>
    private static List<FeishuEventFact> BuildFacts(BitableRecordChangedResult eventData)
    {
        var facts = new List<FeishuEventFact>(capacity: 8);

        AddFact(facts, BitableRecordContextAssembler.FileTokenKey, eventData.FileToken);
        AddFact(facts, BitableRecordContextAssembler.TableIdKey, eventData.TableId);
        AddFact(
            facts,
            BitableRecordContextAssembler.RevisionKey,
            eventData.Revision?.ToString(CultureInfo.InvariantCulture));
        AddFact(facts, BitableRecordContextAssembler.OperatorKey, OperatorOf(eventData));

        var actions = eventData.ActionList;
        if (actions is null || actions.Length == 0)
        {
            return facts;
        }

        var actionIndex = 0;
        foreach (var action in actions)
        {
            if (actionIndex >= MaxActions)
            {
                facts.Add(new FeishuEventFact(
                    BitableRecordContextAssembler.ActionPrefix + "truncated",
                    $"仅登记前 {MaxActions} 条变更（本次共 {actions.Length} 条）"));
                break;
            }

            facts.Add(new FeishuEventFact(
                BitableRecordContextAssembler.ActionPrefix + actionIndex.ToString(CultureInfo.InvariantCulture),
                $"{(string.IsNullOrEmpty(action.Action) ? "unknown" : action.Action)} {action.RecordId}".TrimEnd()));

            AddDiffFacts(facts, actionIndex, action);
            actionIndex++;
        }

        return facts;
    }

    /// <summary>
    /// 登记<b>仅变化字段</b>的前后值（同名未变字段不登记——防"灌全量"）。
    /// </summary>
    private static void AddDiffFacts(List<FeishuEventFact> facts, int actionIndex, BitableTableRecordAction action)
    {
        var before = IndexFields(action.BeforeValue);
        var after = IndexFields(action.AfterValue);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var diffIndex = 0;

        // 先按 after 的顺序（模型更容易对应"现在的值"），再补 before 里被删掉的字段。
        foreach (var fieldId in after.Keys.Concat(before.Keys))
        {
            if (!seen.Add(fieldId))
            {
                continue;
            }

            var oldValue = before.TryGetValue(fieldId, out var oldRaw) ? oldRaw : null;
            var newValue = after.TryGetValue(fieldId, out var newRaw) ? newRaw : null;

            if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
            {
                // 变更事件里 before/after 都会带上整行字段，只有真正变化的才值得进 prompt。
                continue;
            }

            if (diffIndex >= MaxDiffsPerAction)
            {
                facts.Add(new FeishuEventFact(
                    $"{BitableRecordContextAssembler.DiffPrefix}{actionIndex}.more",
                    $"仅登记前 {MaxDiffsPerAction} 个变化字段"));
                break;
            }

            facts.Add(new FeishuEventFact(
                $"{BitableRecordContextAssembler.DiffPrefix}{actionIndex}.{diffIndex.ToString(CultureInfo.InvariantCulture)}",
                $"{fieldId}: {RenderValue(oldValue)} → {RenderValue(newValue)}"));

            diffIndex++;
        }
    }

    /// <summary>字段 ID → 字段值（同 ID 重复出现时按序拼接，不静默丢值）。</summary>
    private static Dictionary<string, string?> IndexFields(BitableTableRecordActionField[]? fields)
    {
        var index = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (fields is null)
        {
            return index;
        }

        foreach (var field in fields)
        {
            if (string.IsNullOrEmpty(field.FieldId))
            {
                continue;
            }

            index[field.FieldId!] = index.TryGetValue(field.FieldId!, out var existing) && !string.IsNullOrEmpty(existing)
                ? existing + "," + field.FieldValue
                : field.FieldValue;
        }

        return index;
    }

    private static string RenderValue(string? value)
        => string.IsNullOrEmpty(value) ? BitableRecordContextAssembler.EmptyValueToken : value!;

    private static void AddFact(List<FeishuEventFact> facts, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            facts.Add(new FeishuEventFact(key, value));
        }
    }

    private static string? OperatorOf(BitableRecordChangedResult eventData)
    {
        var operatorId = eventData.OperatorId;
        if (operatorId is null)
        {
            return null;
        }

        return !string.IsNullOrEmpty(operatorId.OpenId) ? operatorId.OpenId
            : !string.IsNullOrEmpty(operatorId.UserId) ? operatorId.UserId
            : operatorId.UnionId;
    }

    /// <inheritdoc />
    protected override async Task ReplyAsync(ConversationRequest request, string responseText, CancellationToken cancellationToken)
    {
        if (_messageClient is null)
        {
            // 正常路径已由 TryFindDeliveryBlockAsync 在调用模型前拦截；此处是补发路径（outbox replay）的兜底。
            throw new InvalidOperationException(
                $"多维表格记录变更回复失败：IFeishuTenantV1Message 未注册（appKey: {request.AppKey}, subject: {request.SubjectId}）");
        }

        if (string.IsNullOrEmpty(request.SubjectId))
        {
            throw new InvalidOperationException("多维表格记录变更回复失败：会话主体为空，无可投递接收方");
        }

        // 回复前必须切到事件的租户上下文（否则生成的客户端退回默认应用身份 ⇒ 跨租户错发，TMA2-20）。
        using var appScope = BeginAppScope(request.AppKey);

        // 记录变更是<b>行级</b>事件，没有 chat_id：以操作人 open_id 发单聊。
        var outcome = FeishuApiResultReader.Read(await _messageClient
            .SendMessageAsync(
                new Mud.Feishu.DataModels.Messages.SendMessageRequest
                {
                    ReceiveId = request.SubjectId,
                    MsgType = "text",
                    Content = new JsonObject { ["text"] = responseText }.ToJsonString(),
                },
                "open_id",
                cancellationToken)
            .ConfigureAwait(false));

        if (!outcome.Ok)
        {
            // 平台拒绝（限流/权限/目标不存在）不得静默成功 ⇒ 上抛触发幂等回滚 + 重投递。
            throw new InvalidOperationException(
                $"多维表格记录变更回复失败（receiver: {request.SubjectId}）: {outcome.ErrorText ?? "返回空数据"}");
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// 投递目标来自事件本身（操作人 open_id）⇒"能不能送出去"在调用模型前即可判定，
    /// 不可投递时短路，省下一次完整模型调用（与审批/任务事件处理器同一处置）。
    /// </remarks>
    protected override Task<string?> TryFindDeliveryBlockAsync(ConversationRequest request, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        if (_messageClient is null)
        {
            return Task.FromResult<string?>(
                "未注册 IFeishuTenantV1Message，多维表格记录变更回复通道不可用——已跳过本轮模型调用（注册消息客户端后重试）");
        }

        var subject = request.SubjectId;
        return Task.FromResult<string?>(string.IsNullOrEmpty(subject) || string.Equals(subject, UnknownSubject, StringComparison.Ordinal)
            ? "多维表格记录变更事件解析不出操作人（open_id/user_id/union_id 均为空）——已跳过本轮模型调用"
            : null);
    }

    /// <summary>
    /// 构造记录变更通知文本（注入为 <c>MentionedText</c>，模型据此决定是否调用工具取详情）。
    /// </summary>
    private static string BuildNotification(BitableRecordChangedResult eventData)
    {
        var count = eventData.ActionList?.Length ?? 0;
        var actions = count == 0
            ? "（本次未携带行变更明细）"
            : $"（含 {count} 条行变更）";

        return $"多维表格记录变更通知：表格 {eventData.FileToken} 的数据表 {eventData.TableId} 发生记录变更{actions}"
            + (eventData.Revision is null ? "" : $"（版本号：{eventData.Revision}）")
            + "。你可以使用 bitable.get_records_by_ids 获取变更记录详情，或使用 bitable.query_records 检索该表记录。";
    }

    /// <inheritdoc />
    protected override string? GetBusinessKey(EventData eventData)
        => string.IsNullOrEmpty(eventData.EventId)
            ? null
            : $"feishu.agent.bitable_record:{eventData.EventId}";
}
