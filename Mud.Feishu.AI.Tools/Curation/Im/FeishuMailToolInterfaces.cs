// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// <summary>
// 邮件域工具接口集：mail.list_messages / mail.get_message（读侧 tenant）+
// mail.send（发信 user-only——首个触发身份放行的工具）。
// </summary>
// <remarks>
// <para>
// 邮件域是首个「默认装配即触发 fail-fast」的域：mail.send 落点为
// <c>IFeishuUserV1MailDraft</c>（仅 _User 形态），宿主须在 AllowedIdentities 放行 user。
// 读侧 mail.list_messages / mail.get_message 取 _Tenant 形态（<c>IFeishuTenantV1MailMessage</c>）。
// </para>
// </remarks>

// ─────────────────────────── Mail 只读（2 个，tenant） ───────────────────────────

/// <summary>工具接口：mail.list_messages（映射 <c>IFeishuTenantV1MailMessage.GetUserMailboxMessagePageListAsync</c>）。</summary>
[AgentTool("mail.list_messages",
    Description = "列出用户邮箱中的邮件（返回 message_id 列表，可传给 mail.get_message 获取详情）。user_mailbox_id 为用户邮箱地址。只读，需 mail:mailbox:readonly。",
    RequiredScopes = ["mail:mailbox:readonly"],
    Source = nameof(IFeishuTenantV1MailMessage) + "." + nameof(IFeishuTenantV1MailMessage.GetUserMailboxMessagePageListAsync))]
public interface IFeishuTenantMailListMessagesTool
{
    /// <summary>列出邮件（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token），超长截断并标记 truncated。</returns>
    Task<string> ListMessagesAsync(
        [ToolParameter("user_mailbox_id", "用户邮箱地址（如 user@example.com）", Required = true)] string user_mailbox_id,
        [ToolParameter("folder_id", "文件夹 ID（可选，如 INBOX/SENT/DRAFT）")] string? folder_id = null,
        [ToolParameter("only_unread", "是否只返回未读邮件（可选，默认 false）")] bool? only_unread = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：mail.get_message（映射 <c>IFeishuTenantV1MailMessage.GetUserMailboxMessageAsync</c>）。</summary>
[AgentTool("mail.get_message",
    Description = "获取邮件详情（主题/收发件人/正文预览）。message_id 来自 mail.list_messages。只读，需 mail:mailbox:readonly。",
    RequiredScopes = ["mail:mailbox:readonly"],
    Source = nameof(IFeishuTenantV1MailMessage) + "." + nameof(IFeishuTenantV1MailMessage.GetUserMailboxMessageAsync))]
public interface IFeishuTenantMailGetMessageTool
{
    /// <summary>获取邮件详情。</summary>
    /// <returns>白名单投影后的 JSON 文本（subject/from/to/cc/body_preview/message_id），超长截断并标记 truncated。</returns>
    Task<string> GetMessageAsync(
        [ToolParameter("user_mailbox_id", "用户邮箱地址（如 user@example.com）", Required = true)] string user_mailbox_id,
        [ToolParameter("message_id", "邮件 ID（来自 mail.list_messages）", Required = true)] string message_id,
        [ToolParameter("format", "返回格式（可选：full=全文 / plain_text_full=纯文本 / metadata=元数据，默认 metadata）")] string? format = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Mail 写（1 个，user-only） ───────────────────────────

/// <summary>工具接口：mail.send（映射 <c>IFeishuUserV1MailDraft.SendUserMailboxDraftAsync</c>；无 draft_id 时内部先建草稿再发送）。</summary>
/// <remarks>
/// 两种用法合一：传 <c>draft_id</c> 发已存草稿；不传则按 to/subject/body 现写现发
/// （工具层组装 EML 并在同一次调用内建草稿 → 发送，模型不感知中间态）。
/// 二者互斥——既给草稿 ID 又给正文时，"最终发出的是哪份内容"不可判定。
/// </remarks>
[AgentTool("mail.send",
    Description = "以用户身份发送邮件。两种用法：① 传 draft_id 发送一封已存草稿（可定时：send_time 为 RFC3339，须至少晚于当前时间 5 分钟）；② 不传 draft_id 则现写现发（to/subject/body 必填，内部先建草稿再发送，模型无需感知中间态）。两种用法不要混用：有 draft_id 时不再接受 to/subject/body/cc/bcc。写操作：需 mail:mailbox。用户身份工具——宿主须在 AllowedIdentities 放行 user。",
    RequiredScopes = ["mail:mailbox"],
    IsWrite = true,
    Source = nameof(IFeishuUserV1MailDraft) + "." + nameof(IFeishuUserV1MailDraft.SendUserMailboxDraftAsync),

    // 两种用法至少要有一个入口：草稿 ID 或收件人。
    AnyOf = ["draft_id|to"])]
public interface IFeishuUserMailSendTool
{
    /// <summary>发送邮件（草稿或现写现发）。</summary>
    /// <returns>白名单投影后的 JSON 文本（message_id/thread_id；现写现发时另附 draft_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> SendAsync(
        [ToolParameter("user_mailbox_id", "发件人邮箱地址（如 user@example.com）", Required = true)] string user_mailbox_id,
        [ToolParameter("draft_id", "已存草稿 ID（可选）：给出即发送该草稿，此时不再接受 to/subject/body/cc/bcc")] string? draft_id = null,
        [ToolParameter("to", "收件人邮箱地址数组（可选；不传 draft_id 时必填，如 [\"recipient@example.com\"]）")] string[]? to = null,
        [ToolParameter("subject", "邮件主题（不传 draft_id 时必填）")] string? subject = null,
        [ToolParameter("body", "邮件正文纯文本（不传 draft_id 时必填）")] string? body = null,
        [ToolParameter("cc", "抄送邮箱地址数组（可选，仅现写现发）")] string[]? cc = null,
        [ToolParameter("bcc", "密送邮箱地址数组（可选，仅现写现发）")] string[]? bcc = null,
        [ToolParameter("send_time", "定时发送时间（RFC3339，如 2026-10-20T09:00:00+08:00；可选，不传立即发送；须至少晚于当前时间 5 分钟）")] string? send_time = null,
        [ToolParameter("dry_run", "仅预演不发送（可选，默认 false）：返回将要下发的请求摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Mail 增强 ───────────────────────────

/// <summary>工具接口：mail.search（映射 <c>IFeishuUserV1MailMessage.SearchUserMailboxMessageAsync</c>）。</summary>
/// <remarks>
/// <b>关键字搜索只在 user 身份接口上</b>（<c>IFeishuV1MailMessage_User.cs:76</c>），
/// tenant 侧的 mail.list_messages 只有 folder/label/未读过滤 ⇒ 关键词搜邮件必须走 user 身份。
/// </remarks>
[AgentTool("mail.search",
    Description = "按关键字搜索邮箱邮件（如'找张三发的关于合同的邮件'）。user 身份接口，支持分页。只读，需 mail:mailbox:readonly。",
    RequiredScopes = ["mail:mailbox:readonly"],
    Source = nameof(IFeishuUserV1MailMessage) + "." + nameof(IFeishuUserV1MailMessage.SearchUserMailboxMessageAsync))]
public interface IFeishuUserMailSearchTool
{
    /// <summary>搜索邮件。</summary>
    /// <returns>白名单投影后的 JSON 文本（items + 分页信息），超长截断并标记 truncated。</returns>
    Task<string> SearchMailAsync(
        [ToolParameter("user_mailbox_id", "用户邮箱 ID（形如 xxx@xxx）", Required = true)] string user_mailbox_id,
        [ToolParameter("query", "搜索关键字", Required = true)] string query,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：mail.list_labels（映射 <c>IFeishuTenantV1MailLabel.GetUserMailboxLabelListAsync</c>）。</summary>
[AgentTool("mail.list_labels",
    Description = "列出邮箱的邮件标签（label）——用于查清可用的 label_id，再传给 mail.list_messages 做标签过滤。只读，需 mail:mailbox:readonly。",
    RequiredScopes = ["mail:mailbox:readonly"],
    Source = nameof(IFeishuTenantV1MailLabel) + "." + nameof(IFeishuTenantV1MailLabel.GetUserMailboxLabelListAsync))]
public interface IFeishuTenantMailListLabelsTool
{
    /// <summary>列出标签。</summary>
    /// <returns>白名单投影后的 JSON 文本（label_id/name）。</returns>
    Task<string> ListLabelsAsync(
        [ToolParameter("user_mailbox_id", "用户邮箱 ID（形如 xxx@xxx）", Required = true)] string user_mailbox_id,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：mail.get_thread（映射 <c>IFeishuTenantV1MailThread.GetUserMailboxThreadAsync</c>）。</summary>
[AgentTool("mail.get_thread",
    Description = "取一封邮件的会话线程（同一主题下的往来邮件）——回复/追问类问题的完整上下文来源。只读，需 mail:mailbox:readonly。",
    RequiredScopes = ["mail:mailbox:readonly"],
    Source = nameof(IFeishuTenantV1MailThread) + "." + nameof(IFeishuTenantV1MailThread.GetUserMailboxThreadAsync))]
public interface IFeishuTenantMailGetThreadTool
{
    /// <summary>获取会话线程。</summary>
    /// <returns>白名单投影后的 JSON 文本（thread_id/body_preview/messages）。</returns>
    Task<string> GetThreadAsync(
        [ToolParameter("user_mailbox_id", "用户邮箱 ID（形如 xxx@xxx）", Required = true)] string user_mailbox_id,
        [ToolParameter("thread_id", "会话线程 ID（形如 txxx）", Required = true)] string thread_id,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：mail.mark_read（映射 <c>IFeishuTenantV1MailMessage.ModifyUserMailboxMessageAsync</c>）。</summary>
/// <remarks>
/// <b>为何需要它</b>：模型读完邮件后若不回写已读，用户邮箱会堆积大量"未读"，
/// 与人工处理结果不一致（Agent 读过的邮件在用户眼里仍是未读）。
/// </remarks>
[AgentTool("mail.mark_read",
    Description = "把邮件标记为已读（或未读）——Agent 读完邮件后应回写，避免用户邮箱堆积未读。写操作：需 mail:mailbox。",
    RequiredScopes = ["mail:mailbox"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1MailMessage) + "." + nameof(IFeishuTenantV1MailMessage.ModifyUserMailboxMessageAsync))]
public interface IFeishuTenantMailMarkReadTool
{
    /// <summary>标记已读状态。</summary>
    /// <returns>结构化文本（message_id/read）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> MarkReadAsync(
        [ToolParameter("user_mailbox_id", "用户邮箱 ID（形如 xxx@xxx）", Required = true)] string user_mailbox_id,
        [ToolParameter("message_id", "邮件 ID（形如 xxx）", Required = true)] string message_id,
        [ToolParameter("read", "目标状态（可选，默认 true=true=已读；传 false 标记未读）")] bool? read = null,
        [ToolParameter("dry_run", "仅预演不修改（可选，默认 false）：返回将要下发的 method/path 与字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Mail 成环（15 个） ───────────────────────────

// ── 草稿（4 个，全部 user 身份：草稿接口仅 _User 形态） ──

/// <summary>工具接口：mail.create_draft（映射 <c>IFeishuUserV1MailDraft.CreateUserMailboxDraftAsync</c>）。</summary>
/// <remarks>正文经工具层组装为 RFC 5822 EML 并 base64url 编码（与 mail.send 同源），模型不感知。</remarks>
[AgentTool("mail.create_draft",
    Description = "在用户邮箱中创建一封草稿（不发送，可先检查再经 mail.send 传 draft_id 发出）。to/cc/bcc 为邮箱地址数组，subject 为主题，body 为纯文本正文。写操作：需 mail:mailbox。用户身份工具——宿主须在 AllowedIdentities 放行 user。",
    RequiredScopes = ["mail:mailbox"],
    IsWrite = true,
    Source = nameof(IFeishuUserV1MailDraft) + "." + nameof(IFeishuUserV1MailDraft.CreateUserMailboxDraftAsync))]
public interface IFeishuUserMailCreateDraftTool
{
    /// <summary>创建草稿。</summary>
    /// <returns>白名单投影后的 JSON 文本（draft_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> CreateDraftAsync(
        [ToolParameter("user_mailbox_id", "发件人邮箱地址（如 user@example.com）", Required = true)] string user_mailbox_id,
        [ToolParameter("to", "收件人邮箱地址数组（如 [\"recipient@example.com\"]）", Required = true)] string[] to,
        [ToolParameter("subject", "邮件主题", Required = true)] string subject,
        [ToolParameter("body", "邮件正文（纯文本）", Required = true)] string body,
        [ToolParameter("cc", "抄送邮箱地址数组（可选）")] string[]? cc = null,
        [ToolParameter("bcc", "密送邮箱地址数组（可选）")] string[]? bcc = null,
        [ToolParameter("dry_run", "仅预演不创建（可选，默认 false）：返回将要下发的 method/path 与字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：mail.update_draft（映射 <c>IFeishuUserV1MailDraft.UpdateUserMailboxDraftAsync</c>）。</summary>
/// <remarks>整段替换语义（平台原文："将整体替换草稿原有内容"）——必须给出完整字段集，PUT 天然幂等。</remarks>
[AgentTool("mail.update_draft",
    Description = "更新一封已有草稿的内容（**整段替换**：直接给全 to/subject/body 等完整内容，平台会用新内容整体替换旧事实）。draft_id 来自 mail.create_draft 或邮件列表。写操作：需 mail:mailbox。用户身份工具——宿主须在 AllowedIdentities 放行 user。",
    RequiredScopes = ["mail:mailbox"],
    IsWrite = true,
    Source = nameof(IFeishuUserV1MailDraft) + "." + nameof(IFeishuUserV1MailDraft.UpdateUserMailboxDraftAsync))]
public interface IFeishuUserMailUpdateDraftTool
{
    /// <summary>更新草稿。</summary>
    /// <returns>白名单投影后的 JSON 文本（draft_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> UpdateDraftAsync(
        [ToolParameter("user_mailbox_id", "发件人邮箱地址（如 user@example.com）", Required = true)] string user_mailbox_id,
        [ToolParameter("draft_id", "草稿 ID（来自 mail.create_draft 的返回）", Required = true)] string draft_id,
        [ToolParameter("to", "收件人邮箱地址数组（如 [\"recipient@example.com\"]）", Required = true)] string[] to,
        [ToolParameter("subject", "邮件主题", Required = true)] string subject,
        [ToolParameter("body", "邮件正文（纯文本）", Required = true)] string body,
        [ToolParameter("cc", "抄送邮箱地址数组（可选）")] string[]? cc = null,
        [ToolParameter("bcc", "密送邮箱地址数组（可选）")] string[]? bcc = null,
        [ToolParameter("dry_run", "仅预演不更新（可选，默认 false）：返回将要下发的 method/path 与字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：mail.delete_draft（映射 <c>IFeishuUserV1MailDraft.DeleteUserMailboxDraftAsync</c>）。</summary>
[AgentTool("mail.delete_draft",
    Description = "删除一封草稿。高危写（high-risk-write）：需 mail:mailbox。建议先 dry_run 预演确认。draft_id 来自 mail.create_draft 或邮件列表。用户身份工具——宿主须在 AllowedIdentities 放行 user。",
    RequiredScopes = ["mail:mailbox"],
    IsWrite = true,
    Source = nameof(IFeishuUserV1MailDraft) + "." + nameof(IFeishuUserV1MailDraft.DeleteUserMailboxDraftAsync))]
public interface IFeishuUserMailDeleteDraftTool
{
    /// <summary>删除草稿。</summary>
    /// <returns>白名单投影后的 JSON 文本（deleted=true）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> DeleteDraftAsync(
        [ToolParameter("user_mailbox_id", "发件人邮箱地址（如 user@example.com）", Required = true)] string user_mailbox_id,
        [ToolParameter("draft_id", "草稿 ID（来自 mail.create_draft 的返回）", Required = true)] string draft_id,
        [ToolParameter("dry_run", "仅预演不删除（可选，默认 false）：返回将要下发的 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

// ── 邮件处置 / 附件（2 个，tenant 身份） ──

/// <summary>工具接口：mail.trash_message（映射 <c>IFeishuTenantV1MailMessage.BatchTrashUserMailboxMessageAsync</c>）。</summary>
[AgentTool("mail.trash_message",
    Description = "批量把邮件移到已删除文件夹（**回收站语义，可在邮箱里找回**，不是永久删除）。message_ids 来自 mail.list_messages。写操作：需 mail:mailbox。",
    RequiredScopes = ["mail:mailbox"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1MailMessage) + "." + nameof(IFeishuTenantV1MailMessage.BatchTrashUserMailboxMessageAsync))]
public interface IFeishuTenantMailTrashMessageTool
{
    /// <summary>批量删除邮件到回收站。</summary>
    /// <returns>白名单投影后的 JSON 文本（trashed=true/count）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> TrashMessageAsync(
        [ToolParameter("user_mailbox_id", "用户邮箱地址（如 user@example.com）", Required = true)] string user_mailbox_id,
        [ToolParameter("message_ids", "邮件 ID 数组（如 [\"xxx\"]，来自 mail.list_messages）", Required = true)] string[] message_ids,
        [ToolParameter("dry_run", "仅预演不移除（可选，默认 false）：返回将要下发的 method/path 与字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：mail.get_attachment_url（映射 <c>IFeishuTenantV1MailMessage.GetMessageAttachmentDownloadUrlAsync</c>）。</summary>
/// <remarks>邮件附件下载须先取下载链接（链接有时效）——本工具是"看邮件附件"的读取闭环；下载本身由宿主按链接完成。</remarks>
[AgentTool("mail.get_attachment_url",
    Description = "获取邮件内附件的下载链接（有下载需求时先取链接）。message_id 来自 mail.list_messages；attachment_ids 来自邮件详情。只读，需 mail:mailbox:readonly。",
    RequiredScopes = ["mail:mailbox:readonly"],
    Source = nameof(IFeishuTenantV1MailMessage) + "." + nameof(IFeishuTenantV1MailMessage.GetMessageAttachmentDownloadUrlAsync))]
public interface IFeishuTenantMailGetAttachmentUrlTool
{
    /// <summary>获取附件下载链接。</summary>
    /// <returns>白名单投影后的 JSON 文本（downloads/failed_ids）。</returns>
    Task<string> GetAttachmentUrlAsync(
        [ToolParameter("user_mailbox_id", "用户邮箱地址（如 user@example.com）", Required = true)] string user_mailbox_id,
        [ToolParameter("message_id", "邮件 ID（来自 mail.list_messages）", Required = true)] string message_id,
        [ToolParameter("attachment_ids", "附件 ID 数组（来自邮件详情；一次最多 100 个）", Required = true)] string[] attachment_ids,
        CancellationToken cancellationToken = default);
}

// ── 规则（5 个，tenant 身份） ──
//
// 规则条件是平台代码表（左值 1-16 / 操作符 1-10 / 动作 1-13，见 DataModels XML 注释）。
// 模型侧不写代码，也不写 JSON——用管道分隔的简化式（与 CardDsl 同款纪律）：
//   条件：field|operator|value（如 subject|contains|发票）；旗标型字段可省略 operator/value（如 has_attachment）
//   动作：action（如 mark_read）；move_to_folder 须带目标文件夹 id（move_to_folder|283412371233）

/// <summary>工具接口：mail.rule_list（映射 <c>IFeishuTenantV1MailRule.GetMailboxRuleListAsync</c>）。</summary>
[AgentTool("mail.rule_list",
    Description = "列出邮箱的收件规则（含规则 id、名称、启用状态与条件/动作的简化式描述）——用于查清 rule_id 再执行 rule_update/rule_delete/rule_reorder。只读，需 mail:mailbox:readonly。",
    RequiredScopes = ["mail:mailbox:readonly"],
    Source = nameof(IFeishuTenantV1MailRule) + "." + nameof(IFeishuTenantV1MailRule.GetMailboxRuleListAsync))]
public interface IFeishuTenantMailRuleListTool
{
    /// <summary>列出收件规则。</summary>
    /// <returns>白名单投影后的 JSON 文本（rules：rule_id/name/is_enable/match_all/ignore_the_rest_of_rules/conditions/actions）。</returns>
    Task<string> RuleListAsync(
        [ToolParameter("user_mailbox_id", "用户邮箱地址（如 user@example.com）", Required = true)] string user_mailbox_id,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：mail.rule_save（新建映射 <c>IFeishuTenantV1MailRule.CreateUserMailboxRuleAsync</c>；覆盖映射 <c>UpdateUserMailboxRuleAsync</c>）。</summary>
/// <remarks>
/// <c>rule_id</c> 缺省 = 新建；给出 = 整条替换该规则。平台请求体即完整规则对象，
/// 更新路径下省略的可选字段将按默认值参与覆盖。
/// </remarks>
[AgentTool("mail.rule_save",
    Description = "新建或整条替换一条收件规则（自动分拣邮件）。不传 rule_id 即新建；传 rule_id 即整条替换该规则（建议先用 mail.rule_list 回填原内容）。⚠️ 条件与动作用管道分隔的简化式，**不要写 JSON**：condition 形如 'field|operator|value'（field：sender/recipient/cc/recipient_or_cc/subject/body/attachment_name/attachment_type/any_address/has_attachment/is_external/is_spam/not_spam/all；operator：contains/not_contains/starts_with/ends_with/is/is_not/contains_me/empty；旗标型 field 可省略 operator 与 value，如 has_attachment）；action 形如 'action'（archive/delete/mark_read/to_spam/not_spam/add_flag/no_notify）或 'move_to_folder|文件夹ID'。写操作：需 mail:mailbox。",
    RequiredScopes = ["mail:mailbox"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1MailRule) + "." + nameof(IFeishuTenantV1MailRule.CreateUserMailboxRuleAsync))]
public interface IFeishuTenantMailRuleSaveTool
{
    /// <summary>新建或整条替换收件规则。</summary>
    /// <returns>白名单投影后的 JSON 文本（rule_id；更新路径另附 updated=true）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> RuleSaveAsync(
        [ToolParameter("user_mailbox_id", "用户邮箱地址（如 user@example.com）", Required = true)] string user_mailbox_id,
        [ToolParameter("name", "规则名称（如「发票归档」）", Required = true)] string name,
        [ToolParameter("conditions", "条件数组：每项形如 'field|operator|value'（如 [\"subject|contains|发票\"]）；旗标型字段可省（如 \"has_attachment\"）", Required = true)] string[] conditions,
        [ToolParameter("actions", "动作数组：每项形如 'archive'/'mark_read' 或 'move_to_folder|文件夹ID'（如 [\"move_to_folder|283412371233\"]）", Required = true)] string[] actions,
        [ToolParameter("rule_id", "规则 ID（可选，传空=新建；来自 mail.rule_list：给出即整条替换该规则）")] string? rule_id = null,
        [ToolParameter("match_all", "条件组合方式（可选，默认 true=满足全部条件；false=满足任一条件）")] bool? match_all = null,
        [ToolParameter("is_enable", "是否启用该规则（可选，默认 true）")] bool? is_enable = null,
        [ToolParameter("ignore_the_rest_of_rules", "命中后是否跳过其余规则（可选，默认 false）")] bool? ignore_the_rest_of_rules = null,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回将要下发的 method/path 与字段摘要（含编译出的条件/动作代码），不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：mail.rule_delete（映射 <c>IFeishuTenantV1MailRule.DeleteUserMailboxRuleAsync</c>）。</summary>
[AgentTool("mail.rule_delete",
    Description = "删除一条收件规则。高危写（high-risk-write）：需 mail:mailbox。建议先用 mail.rule_list 确认对象并 dry_run 预演。rule_id 来自 mail.rule_list。",
    RequiredScopes = ["mail:mailbox"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1MailRule) + "." + nameof(IFeishuTenantV1MailRule.DeleteUserMailboxRuleAsync))]
public interface IFeishuTenantMailRuleDeleteTool
{
    /// <summary>删除收件规则。</summary>
    /// <returns>白名单投影后的 JSON 文本（deleted=true）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> RuleDeleteAsync(
        [ToolParameter("user_mailbox_id", "用户邮箱地址（如 user@example.com）", Required = true)] string user_mailbox_id,
        [ToolParameter("rule_id", "规则 ID（来自 mail.rule_list）", Required = true)] string rule_id,
        [ToolParameter("dry_run", "仅预演不删除（可选，默认 false）：返回将要下发的 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：mail.rule_reorder（映射 <c>IFeishuTenantV1MailRule.ReorderUserMailboxRuleAsync</c>）。</summary>
/// <remarks>规则按列表顺序自上而下匹配——顺序即优先级；本工具整表重排（给全量规则 id 序列）。</remarks>
[AgentTool("mail.rule_reorder",
    Description = "调整收件规则的匹配顺序（规则自上而下匹配，**顺序即优先级**；须给出全量规则 id 序列，按目标优先级排列）。rule_ids 来自 mail.rule_list。写操作：需 mail:mailbox。",
    RequiredScopes = ["mail:mailbox"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1MailRule) + "." + nameof(IFeishuTenantV1MailRule.ReorderUserMailboxRuleAsync))]
public interface IFeishuTenantMailRuleReorderTool
{
    /// <summary>重排收件规则顺序。</summary>
    /// <returns>白名单投影后的 JSON 文本（reordered=true/count）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> RuleReorderAsync(
        [ToolParameter("user_mailbox_id", "用户邮箱地址（如 user@example.com）", Required = true)] string user_mailbox_id,
        [ToolParameter("rule_ids", "全量规则 ID 数组，按目标优先级排列（如 [\"rule_b\",\"rule_a\"]；来自 mail.rule_list）", Required = true)] string[] rule_ids,
        [ToolParameter("dry_run", "仅预演不重排（可选，默认 false）：返回将要下发的 method/path 与字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

// ── 模板与签名（6 个：模板 5 个 tenant + 签名 1 个 user） ──

/// <summary>工具接口：mail.template_list（映射 <c>IFeishuTenantV1MailTemplate.GetMailTemplateListAsync</c>）。</summary>
[AgentTool("mail.template_list",
    Description = "列出邮箱的个人邮件模板（template_id/名称/创建时间）——用于查清 template_id 再取详情或更新。只读，需 mail:mailbox:readonly。",
    RequiredScopes = ["mail:mailbox:readonly"],
    Source = nameof(IFeishuTenantV1MailTemplate) + "." + nameof(IFeishuTenantV1MailTemplate.GetMailTemplateListAsync))]
public interface IFeishuTenantMailTemplateListTool
{
    /// <summary>列出邮件模板。</summary>
    /// <returns>白名单投影后的 JSON 文本（items：template_id/name/create_time）。</returns>
    Task<string> TemplateListAsync(
        [ToolParameter("user_mailbox_id", "用户邮箱地址（如 user@example.com）", Required = true)] string user_mailbox_id,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：mail.template_get（映射 <c>IFeishuTenantV1MailTemplate.GetMailTemplateAsync</c>）。</summary>
[AgentTool("mail.template_get",
    Description = "获取邮件模板的完整内容（名称/主题/正文/收件地址）——用于按模板起草邮件前读取模板正文做二次填充。template_id 来自 mail.template_list。只读，需 mail:mailbox:readonly。",
    RequiredScopes = ["mail:mailbox:readonly"],
    Source = nameof(IFeishuTenantV1MailTemplate) + "." + nameof(IFeishuTenantV1MailTemplate.GetMailTemplateAsync))]
public interface IFeishuTenantMailTemplateGetTool
{
    /// <summary>获取邮件模板详情。</summary>
    /// <returns>白名单投影后的 JSON 文本（found 哨兵 + template_id/name/subject/template_content/is_plain_text_mode/tos/ccs/bccs/attachments）。</returns>
    Task<string> TemplateGetAsync(
        [ToolParameter("user_mailbox_id", "用户邮箱地址（如 user@example.com）", Required = true)] string user_mailbox_id,
        [ToolParameter("template_id", "模板 ID（来自 mail.template_list）", Required = true)] string template_id,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：mail.template_save（新建映射 <c>IFeishuTenantV1MailTemplate.CreateMailTemplateAsync</c>；覆盖映射 <c>UpdateMailTemplateAsync</c>）。</summary>
/// <remarks>
/// <c>template_id</c> 缺省 = 新建；给出 = 整段替换该模板。平台请求体即完整模板对象，
/// 更新路径下省略的可选字段会以默认值/空参与整体覆盖，故建议先 template_get 回填再改。
/// </remarks>
[AgentTool("mail.template_save",
    Description = "新建或整段替换一份可复用的个人邮件模板。不传 template_id 即新建；传 template_id 即整段替换该模板（建议先用 mail.template_get 回填原内容，再改需要变的字段）。template_content 为模板正文（可含 HTML）；tos/ccs/bccs 为默认收件地址数组。写操作：需 mail:mailbox。",
    RequiredScopes = ["mail:mailbox"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1MailTemplate) + "." + nameof(IFeishuTenantV1MailTemplate.CreateMailTemplateAsync))]
public interface IFeishuTenantMailTemplateSaveTool
{
    /// <summary>新建或整段替换邮件模板。</summary>
    /// <returns>白名单投影后的 JSON 文本（template_id；更新路径另附 updated=true）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> TemplateSaveAsync(
        [ToolParameter("user_mailbox_id", "用户邮箱地址（如 user@example.com）", Required = true)] string user_mailbox_id,
        [ToolParameter("name", "模板名称（如「报价回复模板」）", Required = true)] string name,
        [ToolParameter("template_id", "模板 ID（可选，传空=新建；来自 mail.template_list：给出即整段替换该模板）")] string? template_id = null,
        [ToolParameter("subject", "模板邮件主题（可选）")] string? subject = null,
        [ToolParameter("template_content", "模板正文（可选，可含 HTML）")] string? template_content = null,
        [ToolParameter("is_plain_text_mode", "正文是否纯文本模式（可选，默认 false=HTML）")] bool? is_plain_text_mode = null,
        [ToolParameter("tos", "默认收件人邮箱地址数组（可选）")] string[]? tos = null,
        [ToolParameter("ccs", "默认抄送邮箱地址数组（可选）")] string[]? ccs = null,
        [ToolParameter("bccs", "默认密送邮箱地址数组（可选）")] string[]? bccs = null,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回将要下发的 method/path 与字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：mail.template_delete（映射 <c>IFeishuTenantV1MailTemplate.DeleteMailTemplateAsync</c>）。</summary>
[AgentTool("mail.template_delete",
    Description = "删除一份邮件模板（**删除后不可恢复**，模板将不再出现在列表中）。高危写（high-risk-write）：需 mail:mailbox。建议先用 mail.template_list 确认对象并 dry_run 预演。",
    RequiredScopes = ["mail:mailbox"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1MailTemplate) + "." + nameof(IFeishuTenantV1MailTemplate.DeleteMailTemplateAsync))]
public interface IFeishuTenantMailTemplateDeleteTool
{
    /// <summary>删除邮件模板。</summary>
    /// <returns>白名单投影后的 JSON 文本（deleted=true）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> TemplateDeleteAsync(
        [ToolParameter("user_mailbox_id", "用户邮箱地址（如 user@example.com）", Required = true)] string user_mailbox_id,
        [ToolParameter("template_id", "模板 ID（来自 mail.template_list）", Required = true)] string template_id,
        [ToolParameter("dry_run", "仅预演不删除（可选，默认 false）：返回将要下发的 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：mail.signature_list（映射 <c>IFeishuUserV1MailTemplate.GetUserMailboxSignaturesAsync</c>）。</summary>
/// <remarks>签名接口仅 user 形态（<c>IFeishuV1MailTemplate_User.cs</c>）——发信落款需要知道有哪些签名可用。</remarks>
[AgentTool("mail.signature_list",
    Description = "列出用户邮箱的邮件签名（signature_id/名称/摘要内容）与各邮箱地址的签名使用配置——写信时按签名落款前先查清可用签名。只读，需 mail:mailbox:readonly。用户身份工具——宿主须在 AllowedIdentities 放行 user。",
    RequiredScopes = ["mail:mailbox:readonly"],
    Source = nameof(IFeishuUserV1MailTemplate) + "." + nameof(IFeishuUserV1MailTemplate.GetUserMailboxSignaturesAsync))]
public interface IFeishuUserMailSignatureListTool
{
    /// <summary>列出邮件签名。</summary>
    /// <returns>白名单投影后的 JSON 文本（signatures/usages；签名正文截断并标记）。</returns>
    Task<string> SignatureListAsync(
        [ToolParameter("user_mailbox_id", "用户邮箱地址（如 user@example.com）", Required = true)] string user_mailbox_id,
        CancellationToken cancellationToken = default);
}
