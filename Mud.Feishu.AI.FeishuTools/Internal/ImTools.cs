// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.FeishuTools.Tools;
using Mud.Feishu.DataModels.ChatGroupMember;
using Mud.Feishu.DataModels.Messages;
using System.Text.Json.Nodes;

namespace Mud.Feishu.AI.FeishuTools.Internal;

/// <summary>
/// IM 工具执行器（<c>im.get_history_messages</c> / <c>im.get_message_content</c>）：
/// RFC3339 → 秒级时间戳转换、<c>container_id_type=chat</c> 与 <c>sort_type=ByCreateTimeDesc</c>
/// 绑定层注入（§3.3.3）；消息内容回查依赖 P1D-1a 路由修复（AI-FD-D12）。
/// </summary>
/// <remarks>执行骨架（catch/回填/截断）由 <see cref="ToolExecutor"/> 承担（WP3）；本类只保留参数校验与投影语义。</remarks>
internal sealed class ImTools(
    Mud.Feishu.IFeishuTenantV1Message messageClient,
    Mud.Feishu.IFeishuTenantV1ChatGroupMember chatMemberClient,
    IOptions<FeishuAgentOptions> options)
{
    private const string ContainerIdTypeChat = "chat";
    private const string SortTypeByCreateTimeDesc = "ByCreateTimeDesc";

    private readonly Mud.Feishu.IFeishuTenantV1Message _messageClient = messageClient
        ?? throw new ArgumentNullException(nameof(messageClient));
    private readonly Mud.Feishu.IFeishuTenantV1ChatGroupMember _chatMemberClient = chatMemberClient
        ?? throw new ArgumentNullException(nameof(chatMemberClient));
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;

    /// <summary>im.get_history_messages：读取历史消息（白名单 message_id/create_time/sender_id/message_type/content 预览）。</summary>
    [FeishuToolHandler(typeof(IFeishuImHistoryTool))]
    public Task<FeishuToolResult> GetHistoryAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ImGetHistoryMessages, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = ImGetHistoryMessagesArgs.Unpack(arguments);

            var startTime = ToUnixSeconds(args.StartTime, "start_time");
            var endTime = ToUnixSeconds(args.EndTime, "end_time");

            var outcome = FeishuApiResultReader.Read(await _messageClient
                .GetHistoryMessageAsync(
                    ContainerIdTypeChat,
                    args.ChatId,
                    startTime,
                    endTime,
                    SortTypeByCreateTimeDesc,
                    PageSizes.History,
                    args.PageToken,
                    cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectHistory);
        });
    }

    /// <summary>im.get_message_content：单条消息内容回查（白名单 message_id/msg_type/body/mentions）。</summary>
    [FeishuToolHandler(typeof(IFeishuImMessageContentTool))]
    public Task<FeishuToolResult> GetContentAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ImGetMessageContent, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = ImGetMessageContentArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _messageClient
                .GetContentListByMessageIdAsync(args.MessageId, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectContent);
        });
    }

    /// <summary>get_history_messages 投影：items（含 content 预览截断）+ 翻页契约。</summary>
    private static JsonObject ProjectHistory(ApiPageListResult<HistoryMessageData> data)
    {
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

        return envelope;
    }

    /// <summary>get_message_content 投影：items（message_id/msg_type/body/mentions）。</summary>
    private static JsonObject ProjectContent(ApiListResult<MessageContentData> data)
    {
        var envelope = new JsonObject { ["items"] = new JsonArray() };
        foreach (var message in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["message_id"] = message.MessageId,
                ["msg_type"] = message.MsgType,

                // R5 / B-3：与 ProjectHistory 的 content 保持同一口径（此前 body 直接回填未预截断，
                // 同一工具面内content 与 body 两个同类字段截断规则不一致）。
                ["body"] = ToolResultText.Truncate(message.Body?.Content, PageSizes.MessagePreviewLength),
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

        return envelope;
    }

    /// <summary>im.list_chat_members：分页列出群成员（白名单 member_id/name/tenant_key）。</summary>
    [FeishuToolHandler(typeof(IFeishuImListChatMembersTool))]
    public Task<FeishuToolResult> ListChatMembersAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ImListChatMembers, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = ImListChatMembersArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _chatMemberClient
                .GetMemberPageListByIdAsync(
                    args.ChatId,
                    page_size: PageSizes.History,
                    page_token: args.PageToken,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectChatMembers);
        });
    }

    /// <summary>im.reply_message：回复指定消息（<c>dry_run=true</c> 时只预演）。</summary>
    /// <remarks>幂等键（uuid）：相同 uuid 在 1 小时内至多成功回复一条。</remarks>
    [FeishuToolHandler(typeof(IFeishuImReplyMessageTool))]
    public Task<FeishuToolResult> ReplyMessageAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ImReplyMessage);
        return executor.RunAsync(async () =>
        {
            var args = ImReplyMessageArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", $"/open-apis/im/v1/messages/{args.MessageId}/reply",
                    ToolDryRun.IdempotencyNote(args.IdempotencyKey),
                    ("message_id", args.MessageId.Length), ("msg_type", args.MsgType.Length), ("content", args.Content.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _messageClient
                .ReplyMessageAsync(
                    args.MessageId,
                    new ReplyMessageRequest
                    {
                        Content = args.Content,
                        MsgType = args.MsgType,
                        ReplyInThread = args.ReplyInThread ?? false,
                        Uuid = args.IdempotencyKey,
                    },
                    cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["message_id"] = data.MessageId,
            });
        });
    }

    /// <summary>im.search_messages：按关键词搜索消息（白名单 id/display_info/meta_data）。</summary>
    [FeishuToolHandler(typeof(IFeishuImSearchMessagesTool))]
    public Task<FeishuToolResult> SearchMessagesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ImSearchMessages, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = ImSearchMessagesArgs.Unpack(arguments);

            var searchRequest = new SearchMessageRequest
            {
                Query = args.Query,
                Filter = BuildSearchFilter(args),
            };

            var outcome = FeishuApiResultReader.Read(await _messageClient
                .SearchMessageAsync(
                    searchRequest,
                    page_size: PageSizes.Search,
                    page_token: args.PageToken,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectSearchResults);
        });
    }

    /// <summary>list_chat_members 投影：items（member_id/name/tenant_key）+ 翻页契约。</summary>
    private static JsonObject ProjectChatMembers(GetMemberPageListResult data)
    {
        var envelope = new JsonObject
        {
            ["items"] = new JsonArray(),
            ["has_more"] = data.HasMore,
            ["member_total"] = data.MemberTotal,
        };
        if (!string.IsNullOrEmpty(data.PageToken))
        {
            envelope["page_token"] = data.PageToken;
        }

        foreach (var member in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["member_id"] = member.MemberId,
                ["name"] = member.Name,
                ["tenant_key"] = member.TenantKey,
            });
        }

        return envelope;
    }

    /// <summary>search_messages 投影：items（id/display_info + meta_data 白名单）+ 翻页契约。</summary>
    private static JsonObject ProjectSearchResults(SearchMessageResult data)
    {
        var envelope = new JsonObject
        {
            ["items"] = new JsonArray(),
            ["total"] = data.Total,
            ["has_more"] = data.HasMore,
        };
        if (!string.IsNullOrEmpty(data.PageToken))
        {
            envelope["page_token"] = data.PageToken;
        }

        foreach (var item in data.Items ?? [])
        {
            var meta = item.MetaData is null ? null : new JsonObject
            {
                ["message_id"] = item.MetaData.MessageId,
                ["type"] = item.MetaData.Type,
                ["create_time"] = item.MetaData.CreateTime,
                ["chat_id"] = item.MetaData.ChatId,
                ["from_id"] = item.MetaData.FromId,
            };
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["id"] = item.Id,
                ["display_info"] = ToolResultText.Truncate(item.DisplayInfo ?? string.Empty, PageSizes.MessagePreviewLength),
                ["meta_data"] = meta,
            });
        }

        return envelope;
    }

    /// <summary>从工具参数构造搜索过滤器（绑定层组装，模型只见标量/标量数组）。</summary>
    private static MessageSearchFilter? BuildSearchFilter(ImSearchMessagesArgs args)
    {
        var hasChatIds = args.ChatIds is { Length: > 0 };
        var hasFromIds = args.FromIds is { Length: > 0 };
        var hasChatType = !string.IsNullOrWhiteSpace(args.ChatType);

        if (!hasChatIds && !hasFromIds && !hasChatType)
        {
            return null;
        }

        return new MessageSearchFilter
        {
            ChatIds = hasChatIds ? args.ChatIds : null,
            FromIds = hasFromIds ? args.FromIds : null,
            ChatType = hasChatType ? args.ChatType : null,
        };
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
