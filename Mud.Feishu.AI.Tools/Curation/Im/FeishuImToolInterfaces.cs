// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── IM（5 个：2 只读 + 1 只读会话治理 + 1 写会话治理 + 1 只读搜索） ───────────────────────────

/// <summary>工具接口：im.get_history_messages（映射 <c>IFeishuTenantV1Message.GetHistoryMessageAsync</c>）。</summary>
[AgentTool("im.get_history_messages",
    Description = "读取群聊的历史消息（chat_id 可由事件上下文获得），按创建时间倒序返回最近消息预览。只读，需 im:message:readonly。",
    RequiredScopes = ["im:message:readonly"],
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.GetHistoryMessageAsync))]
public interface IFeishuTenantImHistoryTool
{
    /// <summary>读取历史消息（分页，按创建时间倒序）。</summary>
    /// <returns>白名单投影后的 JSON 文本（message_id/create_time/sender_id/message_type/content 预览），超长截断并标记 truncated。</returns>
    Task<string> GetHistoryMessageAsync(
        [ToolParameter("chat_id", "群聊 ID（形如 ocXxx）", Required = true)] string chat_id,
        [ToolParameter("start_time", "起始时间（可选，RFC3339，如 2026-09-27T00:00:00+08:00）")] string? start_time = null,
        [ToolParameter("end_time", "结束时间（可选，RFC3339）")] string? end_time = null,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：im.get_message_content（映射 <c>IFeishuTenantV1Message.GetContentListByMessageIdAsync</c>）。</summary>
[AgentTool("im.get_message_content",
    Description = "按 message_id 回查单条消息的完整内容——与 im.get_history_messages 组成两步链（历史消息列表 → 指定消息内容）。只读，需 im:message:readonly。",
    RequiredScopes = ["im:message:readonly"],
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.GetContentListByMessageIdAsync))]
public interface IFeishuTenantImMessageContentTool
{
    /// <summary>单条消息内容回查。</summary>
    /// <returns>白名单投影后的 JSON 文本（message_id/msg_type/body/mentions），超长截断并标记 truncated。</returns>
    Task<string> GetContentAsync(
        [ToolParameter("message_id", "消息 ID（形如 omXxx，来自 im.get_history_messages 或事件上下文）", Required = true)] string message_id,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：im.list_chat_members（映射 <c>IFeishuTenantV1ChatGroupMember.GetMemberPageListByIdAsync</c>）。</summary>
[AgentTool("im.list_chat_members",
    Description = "分页列出群聊成员（member_id/name/tenant_key）——'这个群里有哪些人'的多步流程地基。chat_id 可由事件上下文获得。只读，需 im:chat:readonly。",
    RequiredScopes = ["im:chat:readonly"],
    Source = nameof(IFeishuTenantV1ChatGroupMember) + "." + nameof(IFeishuTenantV1ChatGroupMember.GetMemberPageListByIdAsync))]
public interface IFeishuTenantImListChatMembersTool
{
    /// <summary>列出群成员（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token/member_total），超长截断并标记 truncated。</returns>
    Task<string> GetMemberPageListByIdAsync(
        [ToolParameter("chat_id", "群聊 ID（形如 ocXxx）", Required = true)] string chat_id,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：im.reply_message（映射 <c>IFeishuTenantV1Message.ReplyMessageAsync</c>）。</summary>
[AgentTool("im.reply_message",
    Description = "回复指定消息，形成话题串避免刷屏。message_id 来自 im.get_history_messages 或事件上下文；content 为 JSON 字符串（msg_type=text 时如 {\"text\":\"回复内容\"}）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:message。",
    RequiredScopes = ["im:message"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.ReplyMessageAsync))]
public interface IFeishuTenantImReplyMessageTool
{
    /// <summary>回复消息。</summary>
    /// <returns>白名单投影后的 JSON 文本（message_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> ReplyMessageAsync(
        [ToolParameter("message_id", "待回复的消息 ID（形如 omXxx）", Required = true)] string message_id,
        [ToolParameter("msg_type", "消息类型（text/post/image/file/audio/media/sticker/interactive/share_chat/share_user）", Required = true)] string msg_type,
        [ToolParameter("content", "消息内容 JSON 字符串（msg_type=text 时如 {\"text\":\"回复内容\"}）", Required = true)] string content,
        [ToolParameter("reply_in_thread", "是否以话题形式回复（可选）。留空时：若当前会话处于话题中则自动为 true，否则 false。仅在需要脱离话题、直接回主会话时才显式传 false。")] bool? reply_in_thread = null, [ToolParameter("idempotency_key", "幂等键（可选）：相同 uuid 在 1 小时内至多成功回复一条。省略时不保证幂等。")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不回复（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：im.search_messages（映射 <c>IFeishuTenantV1Message.SearchMessageAsync</c>）。</summary>
[AgentTool("im.search_messages",
    Description = "按关键词搜索可见会话中的消息——支持按会话/发送者/时间过滤。返回消息 ID 与命中片段预览，可用 im.get_message_content 回查完整内容。只读，需 im:message:readonly。",
    RequiredScopes = ["im:message:readonly"],
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.SearchMessageAsync))]
public interface IFeishuTenantImSearchMessagesTool
{
    /// <summary>搜索消息（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/total/has_more/page_token），超长截断并标记 truncated。</returns>
    Task<string> SearchMessageAsync(
        [ToolParameter("query", "搜索关键词（≤50 字符）", Required = true)] string query,

        [ToolParameter("chat_ids", "限定会话 ID 列表（可选，字符串数组）")] string[]? chat_ids = null,
        [ToolParameter("from_ids", "限定发送者 ID 列表（可选，字符串数组）")] string[]? from_ids = null,
        [ToolParameter("chat_type", "会话类型过滤（可选：p2p=单聊 / group=群聊）")] string? chat_type = null,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

// ────────── R5 / F-3：IM 域补齐（thread / 群管理 / 撤回转发 / 已读） ──────────
// ⚠️ SDK 方法名缺陷说明（U-19：本轮不改 SDK，只在工具名与注释中用正确语义）：
//   · forward_message 落到 SDK 的 ReceiveMessageAsync（实为 POST /messages/{id}/forward，不是"接收消息"）
//   · 已读用户落到 GetMessageReadUsesAsync（"Uses" 应为 "Users"）
//   · 群信息落到 GetChatGroupInoByIdAsync（"Ino" 应为 "Info"）

/// <summary>im.get_thread_messages：读取话题（thread）内的消息。</summary>
[AgentTool("im.get_thread_messages",
    Description = "读取某个话题（thread）内的消息——群话题场景下用 thread_id 取代 chat_id 读话题内容。thread_id 可由事件上下文获得。只读，需 im:message:readonly。",
    RequiredScopes = ["im:message:readonly"],
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.GetHistoryMessageAsync))]
    public interface IFeishuTenantImGetThreadMessagesTool
{
    /// <summary>读取话题消息（分页，按创建时间倒序）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token），超长截断并标记 truncated。</returns>
    Task<string> GetThreadMessagesAsync(
        [ToolParameter("thread_id", "话题 ID（形如 omt_xxx，来自事件上下文或 im.reply_message 返回）", Required = true)] string thread_id,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>im.revoke_message：撤回自己发出的消息。</summary>
[AgentTool("im.revoke_message", IsWrite = true,
    Description = "撤回一条自己发出的消息（发错内容时纠正）。只能撤回本 Bot 发送的消息；message_id 来自事件上下文或 im.get_history_messages。high-risk-write：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:message。建议先 dry_run 预演确认目标消息。",
    RequiredScopes = ["im:message"],
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.RevokeMessageAsync))]
    public interface IFeishuTenantImRevokeMessageTool
{
    /// <summary>撤回消息。</summary>
    /// <returns>结构化文本（ok=true 表示已受理）。</returns>
    Task<string> RevokeMessageAsync(
        [ToolParameter("message_id", "待撤回的消息 ID（形如 omXxx）", Required = true)] string message_id,
        [ToolParameter("dry_run", "仅预演不执行（可选，默认 false）：返回将要下发的 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>im.forward_message：转发单条消息到指定会话。</summary>
[AgentTool("im.forward_message", IsWrite = true,
    Description = "把一条消息转发给用户或群。receive_id 来自 im.search_user / 事件上下文。⚠️ 底层 SDK 方法名为 ReceiveMessageAsync 但语义是转发（POST /messages/{id}/forward），不是接收消息。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:message:send_as_bot。",
    RequiredScopes = ["im:message:send_as_bot"],
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.ReceiveMessageAsync))]
public interface IFeishuTenantImForwardMessageTool
{
    /// <summary>转发消息。</summary>
    /// <returns>结构化文本（转发结果 message_id）。</returns>
    Task<string> ForwardMessageAsync(
        [ToolParameter("message_id", "待转发的消息 ID（形如 omXxx）", Required = true)] string message_id,
        [ToolParameter("receive_id", "接收方 ID（open_id / user_id / union_id 之一）", Required = true)] string receive_id,
        [ToolParameter("receive_id_type", "接收方 ID 类型（可选：open_id / user_id / union_id，默认 open_id）")] string? receive_id_type = null,
        [ToolParameter("idempotency_key", "幂等键（可选）：相同键的重复请求不会重复转发；建议由调用方给出稳定值，不要用随机数")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不执行（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>im.forward_thread：转发整个话题。</summary>
[AgentTool("im.forward_thread", IsWrite = true,
    Description = "把整个话题（thread）转发给用户或群——一次性把讨论上下文带过去。thread_id 来自事件上下文。⚠️ 底层 SDK 方法名为 ReceiveThreadsAsync 但语义是转发话题。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:message:send_as_bot。",
    RequiredScopes = ["im:message:send_as_bot"],
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.ReceiveThreadsAsync))]
    public interface IFeishuTenantImForwardThreadTool
{
    /// <summary>转发话题。</summary>
    /// <returns>结构化文本（转发结果 thread_id）。</returns>
    Task<string> ForwardThreadAsync(
        [ToolParameter("thread_id", "待转发的话题 ID（形如 omt_xxx）", Required = true)] string thread_id,
        [ToolParameter("receive_id", "接收方 ID（open_id / user_id / union_id 之一）", Required = true)] string receive_id,
        [ToolParameter("receive_id_type", "接收方 ID 类型（可选：open_id / user_id / union_id，默认 open_id）")] string? receive_id_type = null,
        [ToolParameter("idempotency_key", "幂等键（可选）：相同键的重复请求不会重复转发")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不执行（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>im.get_message_read_users：查询消息已读用户。</summary>
[AgentTool("im.get_message_read_users",
    Description = "查询某条消息已被哪些人读到（user_id + 读取时间）。⚠️ 底层 SDK 方法名拼写为 GetMessageReadUsesAsync（Uses 应为 Users），此处按正确语义命名。只读，需 im:message:readonly。",
    RequiredScopes = ["im:message:readonly"],
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.GetMessageReadUsesAsync))]
    public interface IFeishuTenantImGetMessageReadUsersTool
{
    /// <summary>查询已读用户（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token）。</returns>
    Task<string> GetMessageReadUsersAsync(
        [ToolParameter("message_id", "目标消息 ID（形如 omXxx）", Required = true)] string message_id,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>im.get_chat：读取群基础信息。</summary>
[AgentTool("im.get_chat",
    Description = "读取群基础信息（群名/描述/成员数/群主）。chat_id 可由事件上下文获得。⚠️ 底层 SDK 方法名拼写为 GetChatGroupInoByIdAsync（Ino 应为 Info），此处按正确语义命名。只读，需 im:chat:readonly。",
    RequiredScopes = ["im:chat:readonly"],
    Source = nameof(IFeishuTenantV1ChatGroup) + "." + nameof(IFeishuTenantV1ChatGroup.GetChatGroupInoByIdAsync))]
    public interface IFeishuTenantImGetChatTool
{
    /// <summary>读取群信息。</summary>
    /// <returns>白名单投影后的 JSON 文本（chat_id/name/description/user_count/owner_id）。</returns>
    Task<string> GetChatAsync(
        [ToolParameter("chat_id", "群 ID（形如 ocXxx）", Required = true)] string chat_id,
        CancellationToken cancellationToken = default);
}

/// <summary>im.search_chats：按关键词搜索群。</summary>
[AgentTool("im.search_chats",
    Description = "按关键词搜索群聊（返回 chat_id/name/描述）——用户只记得群名片段时的入口。只读，需 im:chat:readonly。",
    RequiredScopes = ["im:chat:readonly"],
    Source = nameof(IFeishuTenantV1ChatGroup) + "." + nameof(IFeishuTenantV1ChatGroup.GetChatGroupPageListByKeywordAsync))]
    public interface IFeishuTenantImSearchChatsTool
{
    /// <summary>搜索群（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token）。</returns>
    Task<string> SearchChatsAsync(
        [ToolParameter("query", "搜索关键词（群名片段）", Required = true)] string query,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}
