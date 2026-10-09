// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// <summary>
// R5/WP5 邮件域工具接口集：mail.list_messages / mail.get_message（读侧 tenant）+
// mail.send_message（发信 user-only——首个触发 T5-0 身份放行的工具）。
// </summary>
// <remarks>
// <para>
// 邮件域是首个「默认装配即触发 fail-fast」的域：mail.send_message 落点为
// <c>IFeishuUserV1MailDraft</c>（仅 _User 形态），宿主须在 AllowedIdentities 放行 user。
// 读侧 mail.list_messages / mail.get_message 取 _Tenant 形态（<c>IFeishuTenantV1MailMessage</c>）。
// </para>
// <para>
// SDK 落点已在 T0-5 逐方法核实（见 R5 文档）。
// </para>
// </remarks>

// ─────────────────────────── Mail 只读（2 个，tenant） ───────────────────────────

/// <summary>工具接口：mail.list_messages（映射 <c>IFeishuTenantV1MailMessage.GetUserMailboxMessagePageListAsync</c>）。</summary>
[FeishuTool("mail.list_messages",
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
[FeishuTool("mail.get_message",
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

/// <summary>工具接口：mail.send_message（映射 <c>IFeishuUserV1MailDraft.SendUserMailboxDraftAsync</c>，两步操作：先创建草稿再发送）。</summary>
[FeishuTool("mail.send_message",
    Description = "以用户身份发送邮件（两步操作：先创建草稿再发送，模型无需感知中间态）。to/cc/bcc 为邮箱地址数组，subject 为主题，body 为纯文本正文。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 mail:mailbox。用户身份工具——宿主须在 AllowedIdentities 放行 user。",
    RequiredScopes = ["mail:mailbox"],
    IsWrite = true,
    Source = nameof(IFeishuUserV1MailDraft) + "." + nameof(IFeishuUserV1MailDraft.SendUserMailboxDraftAsync))]
public interface IFeishuUserMailSendMessageTool
{
    /// <summary>发送邮件。</summary>
    /// <returns>白名单投影后的 JSON 文本（message_id/thread_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> SendMessageAsync(
        [ToolParameter("user_mailbox_id", "发件人邮箱地址（如 user@example.com）", Required = true)] string user_mailbox_id,
        [ToolParameter("to", "收件人邮箱地址数组（如 [\"recipient@example.com\"]）", Required = true)] string[] to,
        [ToolParameter("subject", "邮件主题", Required = true)] string subject,
        [ToolParameter("body", "邮件正文（纯文本）", Required = true)] string body,
        [ToolParameter("cc", "抄送邮箱地址数组（可选）")] string[]? cc = null,
        [ToolParameter("bcc", "密送邮箱地址数组（可选）")] string[]? bcc = null,
        [ToolParameter("dry_run", "仅预演不发送（可选，默认 false）：返回将要下发的请求摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Contact 部门轴补齐（2 个只读，tenant） ───────────────────────────

/// <summary>工具接口：contact.list_departments（映射 <c>IFeishuTenantV3Departments.GetDepartmentsByParentIdAsync</c>）。</summary>
[FeishuTool("contact.list_departments",
    Description = "列出指定部门下的子部门（返回 department_id/name/parent_department_id）。department_id 为 0 时列出根部门。只读，需 contact:department.base:readonly。",
    RequiredScopes = ["contact:department.base:readonly"],
    Source = nameof(IFeishuTenantV3Departments) + "." + nameof(IFeishuTenantV3Departments.GetDepartmentsByParentIdAsync))]
public interface IFeishuTenantContactListDepartmentsTool
{
    /// <summary>列出子部门（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token），超长截断并标记 truncated。</returns>
    Task<string> ListDepartmentsAsync(
        [ToolParameter("department_id", "部门 ID（0=根部门，或来自上一次结果的 department_id）", Required = true)] string department_id,
        [ToolParameter("fetch_child", "是否递归获取全部子部门（可选，默认 false）")] bool? fetch_child = null,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：contact.list_department_members（映射 <c>IFeishuTenantV1Employees.QueryEmployeePageListAsync</c>，按 department_id 过滤）。</summary>
[FeishuTool("contact.list_department_members",
    Description = "按部门 ID 列出该部门下的员工（返回 open_id/name/employee_id）。department_id 来自 contact.list_departments。只读，需 contact:user.base:readonly。",
    RequiredScopes = ["contact:user.base:readonly"],
    Source = nameof(IFeishuTenantV1Employees) + "." + nameof(IFeishuTenantV1Employees.QueryEmployeePageListAsync))]
public interface IFeishuTenantContactListDepartmentMembersTool
{
    /// <summary>按部门列出员工（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token），超长截断并标记 truncated。</returns>
    Task<string> ListDepartmentMembersAsync(
        [ToolParameter("department_id", "部门 ID（来自 contact.list_departments）", Required = true)] string department_id,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Approval 实例详情（1 个只读，user-only，从 WP4 移入） ───────────────────────────

/// <summary>工具接口：approval.get_instance（映射 <c>IFeishuUserV4ApprovalInstance.GetInstanceDetailAsync</c>）。</summary>
[FeishuTool("approval.get_instance",
    Description = "获取审批实例详情（含表单/状态/审批流程节点）。instance_code 来自 approval.list_pending_tasks 或审批事件。只读，需 approval:approval:readonly。用户身份工具——宿主须在 AllowedIdentities 放行 user。",
    RequiredScopes = ["approval:approval:readonly"],
    Source = nameof(IFeishuUserV4ApprovalInstance) + "." + nameof(IFeishuUserV4ApprovalInstance.GetInstanceDetailAsync))]
public interface IFeishuUserApprovalGetInstanceTool
{
    /// <summary>获取审批实例详情。</summary>
    /// <returns>白名单投影后的 JSON 文本（instance_code/status/form/auditors），超长截断并标记 truncated。</returns>
    Task<string> GetInstanceAsync(
        [ToolParameter("instance_code", "审批实例 Code（来自 approval.list_pending_tasks 的 instance_code）", Required = true)] string instance_code,
        [ToolParameter("locale", "语言（可选，如 zh-CN/en-US，默认 zh-CN）")] string? locale = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Mail 增强（R5 / F-11） ───────────────────────────

/// <summary>工具接口：mail.search（映射 <c>IFeishuUserV1MailMessage.SearchUserMailboxMessageAsync</c>）。</summary>
/// <remarks>
/// R5 / F-11：<b>关键字搜索只在 user 身份接口上</b>（<c>IFeishuV1MailMessage_User.cs:76</c>），
/// tenant 侧的 mail.list_messages 只有 folder/label/未读过滤 ⇒ 此前模型无法按关键词搜邮件。
/// </remarks>
[FeishuTool("mail.search",
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
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：mail.list_labels（映射 <c>IFeishuTenantV1MailLabel.GetUserMailboxLabelListAsync</c>）。</summary>
[FeishuTool("mail.list_labels",
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
[FeishuTool("mail.get_thread",
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
[FeishuTool("mail.mark_read",
    Description = "把邮件标记为已读（或未读）——Agent 读完邮件后应回写，避免用户邮箱堆积未读。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 mail:mailbox。",
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