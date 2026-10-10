// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Tools;
using Mud.Feishu.DataModels.ChatGroupMember;
using Mud.Feishu.DataModels.Messages;
using System.Text.Json.Nodes;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// IM 工具执行器（<c>im.get_history_messages</c> / <c>im.get_message_content</c>）：
/// RFC3339 → 秒级时间戳转换、<c>container_id_type=chat</c> 与 <c>sort_type=ByCreateTimeDesc</c>
/// 绑定层注入（§3.3.3）；消息内容回查依赖 P1D-1a 路由修复（AI-FD-D12）。
/// </summary>
/// <remarks>执行骨架（catch/回填/截断）由 <see cref="ToolExecutor"/> 承担（WP3）；本类只保留参数校验与投影语义。</remarks>
internal sealed class ImTools(
    IFeishuTenantV1Message messageClient,
    IFeishuTenantV1ChatGroupMember chatMemberClient,
    IOptions<FeishuAgentOptions> options,

    // R5 / F-3：im.get_chat / im.search_chats 落到 ChatGroup 客户端。
    // ⚠️ 刻意做成**可选**：ChatGroup 客户端缺席时（宿主未注册群管理服务），
    // 只应让这两个工具不出现，而不是让整个 im 域工具连坐消失——
    // 这正是 ToolHandlerBinding 里 ToolDependencyKind.SoftService 的语义。
    IFeishuTenantV1ChatGroup? chatGroupClient = null,

    // R5 / F-4：新增依赖。刻意**放在末尾且为可选**——它属"软缺席"语义
    // （未注册时执行器仍须能构造），且追加在末尾可让既有构造点保持源码兼容。
    IFeishuToolContextAccessor? toolContextAccessor = null)
{
    private const string ContainerIdTypeChat = "chat";

    /// <summary>R5 / F-3：话题容器类型（<c>im.get_thread_messages</c> 用它取代 chat）。</summary>
    private const string ContainerIdTypeThread = "thread";

    private const string SortTypeByCreateTimeDesc = "ByCreateTimeDesc";

    /// <summary>转发类工具的 <c>receive_id_type</c> 缺省值（与 SDK 的 <c>Consts.User_Id_Type</c> 一致）。</summary>
    private const string DefaultReceiveIdType = "open_id";

    /// <summary>允许的 <c>receive_id_type</c> 白名单（非法值提前拒绝，便于模型自我纠正）。</summary>
    private static readonly string[] AllowedReceiveIdTypes = ["open_id", "user_id", "union_id"];

    private readonly IFeishuTenantV1Message _messageClient = messageClient
        ?? throw new ArgumentNullException(nameof(messageClient));
    private readonly IFeishuTenantV1ChatGroup? _chatGroupClient = chatGroupClient;
    private readonly IFeishuTenantV1ChatGroupMember _chatMemberClient = chatMemberClient
        ?? throw new ArgumentNullException(nameof(chatMemberClient));
    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;
    private readonly IFeishuToolContextAccessor? _toolContextAccessor = toolContextAccessor;

    /// <summary>
    /// 解析 <c>reply_in_thread</c>：<b>模型显式传参优先，其次才是"当前处于话题中"的自动推断</b>。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么需要自动推断（R5 / F-4）</b>：原实现是 <c>args.ReplyInThread ?? false</c>，
    /// 意味着模型不显式传参就拿不到话题串⇒ 在真实群话题场景里回复会**掉回主会话**。
    /// 而"当前是否处于话题中"是<b>会话固有事实</b>，不该让模型猜或显式表达。
    /// </para>
    /// <para>
    /// <b>优先级为什么是"显式 &gt; 自动"</b>：显式传参是模型的<b>明确指令</b>（如话题中要求
    /// "不接话题、直接回主会话"）。若让自动推断覆盖显式值，等于让系统擅自推翻用户/模型的意图。
    /// </para>
    /// <para>
    /// <b>为什么允许 accessor 为 null</b>：该依赖是<b>可选</b>（<c>IFeishuToolContextAccessor</c>
    /// 属 <c>SoftService</c> 语义，未注册时ImTools 仍须能构造并注册，见 ToolHandlerBinding 的依赖分类）。
    /// 缺上下文 ⇒ 退化为原行为 <c>args.ReplyInThread ?? false</c>。
    /// </para>
    /// </remarks>
    private bool ResolveReplyInThread(bool? modelSupplied)
    {
        if (modelSupplied.HasValue)
        {
            return modelSupplied.Value;
        }

        return _toolContextAccessor?.Current?.ThreadId is not null;
    }

    /// <summary>im.get_history_messages：读取历史消息（白名单 message_id/create_time/sender_id/message_type/content 预览）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantImHistoryTool))]
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
    [FeishuToolHandler(typeof(IFeishuTenantImMessageContentTool))]
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
    // ────────── R5 / F-3：IM 域补齐（thread / 撤回 / 转发 / 已读 / 群管理） ──────────

    /// <summary>im.get_thread_messages：读取话题内消息（container_id_type 固定 thread）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantImGetThreadMessagesTool))]
    public Task<FeishuToolResult> GetThreadMessagesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ImGetThreadMessages, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = ImGetThreadMessagesArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _messageClient
                .GetHistoryMessageAsync(
                    ContainerIdTypeThread,
                    args.ThreadId,
                    start_time: null,
                    end_time: null,
                    sort_type: SortTypeByCreateTimeDesc,
                    page_size: PageSizes.History,
                    page_token: args.PageToken,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectHistory);
        });
    }

    /// <summary>im.revoke_message：撤回本 Bot 发出的消息（写面：DELETE = high-risk-write）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantImRevokeMessageTool))]
    public Task<FeishuToolResult> RevokeMessageAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ImRevokeMessage, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = ImRevokeMessageArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "DELETE", $"/open-apis/im/v1/messages/{args.MessageId}",
                    ToolDryRun.IdempotencyNote(null),
                    ("message_id", args.MessageId.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _messageClient
                .RevokeMessageAsync(args.MessageId, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, _ => new JsonObject { ["ok"] = true });
        });
    }

    /// <summary>im.forward_message：转发单条消息（SDK 方法名 ReceiveMessageAsync，语义为转发）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantImForwardMessageTool))]
    public Task<FeishuToolResult> ForwardMessageAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ImForwardMessage, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = ImForwardMessageArgs.Unpack(arguments);
            var receiveIdType = ResolveReceiveIdType(args.ReceiveIdType);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", $"/open-apis/im/v1/messages/{args.MessageId}/forward",
                    ToolDryRun.IdempotencyNote(args.IdempotencyKey),
                    ("message_id", args.MessageId.Length), ("receive_id", args.ReceiveId.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _messageClient
                .ReceiveMessageAsync(
                    args.MessageId,
                    new ReceiveMessageRequest { ReceiveId = args.ReceiveId },
                    receiveIdType,
                    args.IdempotencyKey,
                    cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["message_id"] = data?.MessageId,
            });
        });
    }

    /// <summary>im.forward_thread：转发整个话题（SDK 方法名 ReceiveThreadsAsync，语义为转发话题）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantImForwardThreadTool))]
    public Task<FeishuToolResult> ForwardThreadAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ImForwardThread, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = ImForwardThreadArgs.Unpack(arguments);
            var receiveIdType = ResolveReceiveIdType(args.ReceiveIdType);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", $"/open-apis/im/v1/threads/{args.ThreadId}/forward",
                    ToolDryRun.IdempotencyNote(args.IdempotencyKey),
                    ("thread_id", args.ThreadId.Length), ("receive_id", args.ReceiveId.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _messageClient
                .ReceiveThreadsAsync(
                    args.ThreadId,
                    new ReceiveMessageRequest { ReceiveId = args.ReceiveId },
                    receiveIdType,
                    args.IdempotencyKey,
                    cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["thread_id"] = data?.ThreadId,
            });
        });
    }

    /// <summary>im.get_message_read_users：查询已读用户（SDK 方法名 GetMessageReadUsesAsync）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantImGetMessageReadUsersTool))]
    public Task<FeishuToolResult> GetMessageReadUsersAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ImGetMessageReadUsers, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = ImGetMessageReadUsersArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _messageClient
                .GetMessageReadUsesAsync(
                    args.MessageId,
                    PageSizes.History,
                    args.PageToken,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectReadUsers);
        });
    }

    /// <summary>im.get_chat：读取群基础信息（SDK 方法名 GetChatGroupInoByIdAsync）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantImGetChatTool))]
    public Task<FeishuToolResult> GetChatAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ImGetChat, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = ImGetChatArgs.Unpack(arguments);
            var client = RequireChatGroupClient();

            var outcome = FeishuApiResultReader.Read(await client
                .GetChatGroupInoByIdAsync(args.ChatId, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, data => new JsonObject
            {
                // chat_id 由入参回显：SDK 的 GetChatGroupInfoResult 不含该字段（继承自 ChatGroupBase）。
                ["chat_id"] = args.ChatId,
                ["name"] = data?.Name,
                ["description"] = data?.Description,
                ["user_count"] = data?.UserCount,
                ["owner_id"] = data?.OwnerId,
                ["owner_id_type"] = data?.OwnerIdType,
            });
        });
    }

    /// <summary>im.search_chats：按关键词搜索群聊。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantImSearchChatsTool))]
    public Task<FeishuToolResult> SearchChatsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ImSearchChats, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = ImSearchChatsArgs.Unpack(arguments);
            var client = RequireChatGroupClient();

            var outcome = FeishuApiResultReader.Read(await client
                .GetChatGroupPageListByKeywordAsync(
                    args.Query,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectChats);
        });
    }

    /// <summary>取 ChatGroup 客户端；缺席时给模型可执行的错误（而非 NRE）。</summary>
    private IFeishuTenantV1ChatGroup RequireChatGroupClient()
        => _chatGroupClient ?? throw new InvalidOperationException(
            "im.get_chat / im.search_chats 需要宿主注册 IFeishuTenantV1ChatGroup；"
            + "当前未注册（该依赖为软缺席，缺席时本就不应出现这两个工具）");

    /// <summary>
    /// 解析 <c>receive_id_type</c>（转发类工具）：空 ⇒ <c>open_id</c>；非空但不在白名单 ⇒ <b>抛错</b>。
    /// </summary>
    /// <remarks>
    /// <b>为什么对非空值严格白名单而对空值宽容默认</b>：空值有明确的默认语义（平台侧也是 open_id），
    /// 而非法值会被平台拒绝并返回难以理解的错误；提前拒绝能让模型立刻改对。
    /// 名单与 <c>ContactTools.DefaultUserIdType</c> 同一口径（open_id / user_id / union_id）。
    /// </remarks>
    private static string ResolveReceiveIdType(string? modelSupplied)
    {
        if (string.IsNullOrWhiteSpace(modelSupplied))
        {
            return DefaultReceiveIdType;
        }

        var value = modelSupplied.Trim();
        if (Array.IndexOf(AllowedReceiveIdTypes, value) < 0)
        {
            throw new ArgumentException(
                $"receive_id_type 只能是 {string.Join(" / ", AllowedReceiveIdTypes)}，收到 '{value}'");
        }

        return value;
    }

    private static JsonObject ProjectReadUsers(ApiPageListResult<ReadMessageUser> data)
    {
        // R-3：信封形态单源（无翻页契约的列表信封）。
        var envelope = ToolResultJsons.ItemsEnvelope();
        foreach (var user in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["user_id"] = user.UserId,
                ["user_id_type"] = user.UserIdType,
                ["read_time"] = user.Timestamp,
            });
        }

        return envelope;
    }

    private static JsonObject ProjectChats(
        ApiPageListResult<DataModels.ChatGroup.ChatItemInfo> data)
    {
        // R-3：信封形态单源（无翻页契约的列表信封）。
        var envelope = ToolResultJsons.ItemsEnvelope();
        foreach (var chat in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["chat_id"] = chat.ChatId,
                ["name"] = chat.Name,
                ["description"] = chat.Description,
            });
        }

        return envelope;
    }

    private static JsonObject ProjectHistory(ApiPageListResult<HistoryMessageData> data)
    {
        // R-3：信封形态单源（ToolResultJsons.PageEnvelope）。
        var envelope = ToolResultJsons.PageEnvelope(data.HasMore, data.PageToken);

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
        // R-3：信封形态单源（无翻页契约的列表信封）。
        var envelope = ToolResultJsons.ItemsEnvelope();
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
    [FeishuToolHandler(typeof(IFeishuTenantImListChatMembersTool))]
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
    [FeishuToolHandler(typeof(IFeishuTenantImReplyMessageTool))]
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
                        ReplyInThread = ResolveReplyInThread(args.ReplyInThread),
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
    [FeishuToolHandler(typeof(IFeishuTenantImSearchMessagesTool))]
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
        // R-3：信封形态单源（ToolResultJsons.PageEnvelope）+ 本域附加标量（member_total）。
        var envelope = ToolResultJsons.PageEnvelope(data.HasMore, data.PageToken);
        envelope["member_total"] = data.MemberTotal;

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
        // R-3：信封形态单源（ToolResultJsons.PageEnvelope）+ 本域附加标量（total）。
        var envelope = ToolResultJsons.PageEnvelope(data.HasMore, data.PageToken);
        envelope["total"] = data.Total;

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
