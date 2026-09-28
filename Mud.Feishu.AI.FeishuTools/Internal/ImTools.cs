// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.FeishuTools.Internal;

/// <summary>
/// IM 工具执行器（<c>im.get_history_messages</c> / <c>im.get_message_content</c>）：
/// RFC3339 → 秒级时间戳转换、<c>container_id_type=chat</c> 与 <c>sort_type=ByCreateTimeDesc</c>
/// 绑定层注入（§3.3.3）；消息内容回查依赖 P1D-1a 路由修复（AI-FD-D12）。
/// </summary>
internal sealed class ImTools(Mud.Feishu.IFeishuTenantV1Message messageClient, IOptions<FeishuAgentOptions> options)
{
    private const string ContainerIdTypeChat = "chat";
    private const string SortTypeByCreateTimeDesc = "ByCreateTimeDesc";

    private readonly Mud.Feishu.IFeishuTenantV1Message _messageClient = messageClient
        ?? throw new ArgumentNullException(nameof(messageClient));
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;

    /// <summary>im.get_history_messages：读取历史消息（白名单 message_id/create_time/sender_id/message_type/content 预览）。</summary>
    public async Task<FeishuToolResult> GetHistoryAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        try
        {
            var chatId = ToolArgs.RequireString(arguments, "chat_id");
            var startTime = ToUnixSeconds(ToolArgs.OptionalString(arguments, "start_time"), "start_time");
            var endTime = ToUnixSeconds(ToolArgs.OptionalString(arguments, "end_time"), "end_time");
            var pageToken = ToolArgs.OptionalString(arguments, "page_token");

            var outcome = FeishuApiResultReader.Read(await _messageClient
                .GetHistoryMessageAsync(
                    ContainerIdTypeChat,
                    chatId,
                    startTime,
                    endTime,
                    SortTypeByCreateTimeDesc,
                    PageSizes.History,
                    pageToken,
                    cancellationToken)
                .ConfigureAwait(false));
            if (!outcome.Ok)
            {
                return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.ImGetHistoryMessages, outcome.Code, outcome.ErrorText!));
            }

            var data = outcome.Data!;
            var envelope = new JsonObject
            {
                ["items"] = new JsonArray(),
                ["has_more"] = data.HasMore,
            };
            if (!string.IsNullOrEmpty(data.PageToken))
            {
                envelope["page_token"] = data.PageToken;
            }

            foreach (var message in data.Items ?? [])
            {
                var content = message.Body?.Content ?? string.Empty;
                envelope["items"]!.AsArray().AddNode(new JsonObject
                {
                    ["message_id"] = message.MessageId,
                    ["create_time"] = message.CreateTime,
                    ["sender_id"] = message.Sender?.Id,
                    ["message_type"] = message.MsgType,
                    ["content"] = ToolResultText.Truncate(content, PageSizes.MessagePreviewLength),
                });
            }

            return FeishuToolResult.FromText(ToolResultText.TruncateJson(envelope.ToJsonString(), _maxResultLength));
        }
        catch (ArgumentException ex)
        {
            return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.ImGetHistoryMessages, ex.Message));
        }
    }

    /// <summary>im.get_message_content：单条消息内容回查（白名单 message_id/msg_type/body/mentions）。</summary>
    public async Task<FeishuToolResult> GetContentAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        try
        {
            var messageId = ToolArgs.RequireString(arguments, "message_id");

            var outcome = FeishuApiResultReader.Read(await _messageClient
                .GetContentListByMessageIdAsync(messageId, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            if (!outcome.Ok)
            {
                return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.ImGetMessageContent, outcome.Code, outcome.ErrorText!));
            }

            var envelope = new JsonObject { ["items"] = new JsonArray() };
            foreach (var message in outcome.Data!.Items ?? [])
            {
                envelope["items"]!.AsArray().AddNode(new JsonObject
                {
                    ["message_id"] = message.MessageId,
                    ["msg_type"] = message.MsgType,
                    ["body"] = message.Body?.Content,
                    ["mentions"] = message.Mentions is { Count: > 0 }
                        ? new JsonArray([.. message.Mentions.Select(m => (JsonNode?)new JsonObject
                            {
                                ["key"] = m.Key,
                                ["id"] = m.Id,
                                ["name"] = m.Name,
                            }).ToArray()])
                        : null,
                });
            }

            return FeishuToolResult.FromText(ToolResultText.TruncateJson(envelope.ToJsonString(), _maxResultLength));
        }
        catch (ArgumentException ex)
        {
            return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.ImGetMessageContent, ex.Message));
        }
    }

    /// <summary>RFC3339 → 秒级时间戳字符串（绑定层转换；解析失败抛 <see cref="ArgumentException"/> 结构化回填）。</summary>
    /// <remarks>须为带时区（<c>Z</c> 或 <c>±HH:mm</c>）的 RFC3339 格式——宽松解析会把无时区值按本地时区折算，产生静默偏移。</remarks>
    private static readonly string[] Rfc3339Formats =
    [
        "yyyy-MM-dd'T'HH:mm:ssK",
        "yyyy-MM-dd'T'HH:mm:ss.fK",
        "yyyy-MM-dd'T'HH:mm:ss.ffK",
        "yyyy-MM-dd'T'HH:mm:ss.fffK",
    ];

    private static string? ToUnixSeconds(string? rfc3339, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(rfc3339))
        {
            return null;
        }

        // netstandard2.0 的 BCL 不带 IsNullOrWhiteSpace 的 NotNullWhen 注解，需显式局部化。
        var trimmedInput = rfc3339!.Trim();
        if (!DateTimeOffset.TryParseExact(
                trimmedInput,
                Rfc3339Formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var parsed))
        {
            throw new ArgumentException($"{parameterName} 需为 RFC3339 格式（如 2026-09-27T00:00:00+08:00 或 2026-09-27T00:00:00Z），实际: {trimmedInput}");
        }

        return parsed.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
    }
}
