// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;
using Mud.Feishu.AI.Conversations;
using Mud.Feishu.DataModels.Messages;

namespace Mud.Feishu.AI.Events;

/// <summary>
/// 群聊历史上下文装配器（T2 / D2）：群聊场景下自动拉取被 @ 之前的 N 条消息注入 prompt，
/// 使模型获得上下文以做指代消解——不依赖模型自觉调工具。
/// </summary>
/// <remarks>
/// <para>
/// <b>数据源</b>：直调 SDK <c>IFeishuTenantV1Message.GetHistoryMessageAsync</c>——
/// <b>不经工具面</b>：装配器是系统侧动作，不应出现在模型可见工具清单里、也不消耗工具预算/审计面。
/// </para>
/// <para>
/// <b>触发条件</b>（A15 零差异）：<c>request.Scope.IsGroup &amp;&amp; request.ChatId 非空 &amp;&amp; request.MessageId 非空</c>。
/// 单聊走会话历史已足够；无 chat_id 直接返回 null。
/// </para>
/// <para>
/// <b>去重</b>：排除 <c>MessageId</c>（本轮）与 <c>ParentId</c>（由 QuoteMessage 装配器负责）。
/// </para>
/// <para>
/// <b>过滤</b>：跳过 <c>sender_type == "app"</c>（Bot 自身消息）与纯系统/媒体消息（无可读文本）。
/// </para>
/// <para>
/// <b>安全</b>：只输出发送者 <b>ID</b>（不取昵称原文）、文本经长度截断；输出带 <c>UntrustedHeader</c> 标注。
/// </para>
/// <para>
/// <b>失败降级</b>：拉取失败/无权限 ⇒ 记 Information 日志 + 返回 null（<b>不阻断对话</b>）。
/// </para>
/// <para>
/// <b>单租户作用域</b>：经 <see cref="IFeishuAppContextScopeFactory.BeginScope"/> 切租户（R3-1 同款纪律，防跨租户读）。
/// </para>
/// </remarks>
public sealed class GroupHistoryContextAssembler : IContextAssembler
{
    /// <summary>默认装配顺序（引用消息之后、知识之前）。</summary>
    /// <remarks>
    /// 顺序定稿（M2）：SenderInfo=10 → MediaMessage=15 → QuoteMessage=20 → <b>GroupHistory=30</b> → Knowledge=100。
    /// 群上文在引用之后：引用是本条消息的父，比群上文更贴近本轮。
    /// </remarks>
    public const int DefaultOrder = 30;

    private const string ContainerIdTypeChat = "chat";
    private const string SortTypeByCreateTimeDesc = "ByCreateTimeDesc";

    private readonly Mud.Feishu.IFeishuTenantV1Message _messageClient;
    private readonly IFeishuAppContextScopeFactory _scopeFactory;
    private readonly ILogger? _logger;

    /// <summary>
    /// 初始化 <see cref="GroupHistoryContextAssembler"/>。
    /// </summary>
    /// <param name="messageClient">消息客户端（Tenant 身份）。</param>
    /// <param name="scopeFactory">租户上下文作用域工厂。</param>
    /// <param name="logger">日志（可空）。</param>
    public GroupHistoryContextAssembler(
        Mud.Feishu.IFeishuTenantV1Message messageClient,
        IFeishuAppContextScopeFactory scopeFactory,
        ILogger<GroupHistoryContextAssembler>? logger = null)
    {
        _messageClient = messageClient ?? throw new ArgumentNullException(nameof(messageClient));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger;
    }

    /// <inheritdoc />
    public int Order => DefaultOrder;

    /// <inheritdoc />
    public async Task<string?> AssembleAsync(ConversationRequest request, CancellationToken cancellationToken = default)
    {
        // A15 零差异：非群聊 / 无 chatId / 无 messageId 直接返回 null。
        if (request is null || !request.Scope.IsGroup || string.IsNullOrEmpty(request.ChatId) || string.IsNullOrEmpty(request.MessageId))
        {
            return null;
        }

        try
        {
            using var scope = _scopeFactory.BeginScope(request.AppKey);

            var outcome = FeishuApiResultReader.Read(await _messageClient
                .GetHistoryMessageAsync(
                    ContainerIdTypeChat,
                    request.ChatId!,
                    sort_type: SortTypeByCreateTimeDesc,
                    page_size: ContextBudgets.GroupHistoryMessageCount,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            if (!outcome.Ok || outcome.Data?.Items is null || outcome.Data.Items.Count == 0)
            {
                _logger?.LogInformation("群聊历史拉取未返回数据（chatId: {ChatId}）: {Error}——跳过装配",
                    request.ChatId, outcome.ErrorText);
                return null;
            }

            var messages = outcome.Data.Items
                .Where(m => ShouldInclude(m, request))
                .Take(ContextBudgets.GroupHistoryMessageCount)
                .ToList();

            if (messages.Count == 0)
            {
                return null;
            }

            return BuildContextText(messages);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogInformation(ex, "群聊历史装配失败（chatId: {ChatId}）——跳过", request.ChatId);
            return null;
        }
    }

    /// <summary>
    /// 过滤条件：排除本轮消息、引用父消息、Bot 自身消息、无可读文本的消息。
    /// </summary>
    private static bool ShouldInclude(HistoryMessageData message, ConversationRequest request)
    {
        // 去重：排除本轮消息。
        if (string.Equals(message.MessageId, request.MessageId, StringComparison.Ordinal))
        {
            return false;
        }

        // 去重：排除引用父消息（由 QuoteMessageContextAssembler 负责）。
        if (!string.IsNullOrEmpty(request.ParentId)
            && string.Equals(message.MessageId, request.ParentId, StringComparison.Ordinal))
        {
            return false;
        }

        // 过滤 Bot 自身消息。
        if (string.Equals(message.Sender?.SenderType, "app", StringComparison.Ordinal))
        {
            return false;
        }

        // 过滤无可读文本的消息。
        var content = ExtractTextContent(message);
        return !string.IsNullOrWhiteSpace(content);
    }

    /// <summary>
    /// 从消息体提取纯文本预览（text 类型取 text 字段；其余取原文）。
    /// </summary>
    private static string ExtractTextContent(HistoryMessageData message)
    {
        var content = message.Body?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        // text 类型 content 形如 {"text":"…"}，尽量还原纯文本。
        try
        {
            if (JsonNode.Parse(content!) is JsonObject obj
                && obj["text"] is JsonValue value
                && value.TryGetValue<string>(out var text))
            {
                return text;
            }
        }
        catch (JsonException)
        {
            // 有意静默（守卫白名单）：这是"消息体不是 text JSON"的正常分支（纯函数，无 logger 面），
            // 降级输出确定为原文；为非 JSON 的历史消息记日志只会产生噪声。
            // 非法 JSON 按原文展示。
        }

        return content!;
    }

    /// <summary>
    /// 构造带 untrusted 标注的群聊上文片段（三级闸：条数 / 单条长度 / 总长度）。
    /// </summary>
    private static string BuildContextText(IReadOnlyList<HistoryMessageData> messages)
    {
        var sb = new StringBuilder();
        _ = sb.AppendLine(ContextBudgets.UntrustedHeader);
        _ = sb.AppendLine("[群聊上文]");

        var totalLength = 0;
        var included = 0;

        foreach (var msg in messages)
        {
            if (included >= ContextBudgets.GroupHistoryMessageCount)
            {
                break;
            }

            var text = ExtractTextContent(msg);
            if (text.Length > ContextBudgets.GroupHistoryPerMessageLength)
            {
                text = text.Substring(0, ContextBudgets.GroupHistoryPerMessageLength) + ContextBudgets.TruncationMarker;
            }

            var senderId = msg.Sender?.Id ?? "unknown";
            var time = msg.CreateTime ?? "";

            var line = $"[{time}] {senderId}: {text}";

            if (totalLength + line.Length > ContextBudgets.GroupHistoryTotalLength)
            {
                _ = sb.AppendLine(ContextBudgets.TruncationMarker);
                break;
            }

            _ = sb.AppendLine(line);
            totalLength += line.Length;
            included++;
        }

        return sb.ToString();
    }
}
