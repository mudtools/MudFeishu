// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Interfaces;

namespace Mud.Feishu.AI.Tools.Curation;

// <summary>
// Drive 域只读工具接口（AI-FD-D12 P1D-1b 批次 A：云空间导航入口，与 search.doc_wiki 互补——浏览 vs 搜索）。
// </summary>

/// <summary>工具接口：drive.list_folder_files（映射 <c>IFeishuTenantV1DriveFolder.GetFilesPageListAsync</c>）。</summary>
[FeishuTool("drive.list_folder_files",
    Description = "列出云空间文件夹内的文件与子文件夹（token/name/type/url）；folder_token 缺省列根目录。与 search.doc_wiki 互补（浏览 vs 搜索）。只读，需 drive:drive:readonly。",
    RequiredScopes = ["drive:drive:readonly"],
    Source = nameof(IFeishuTenantV1DriveFolder) + "." + nameof(IFeishuTenantV1DriveFolder.GetFilesPageListAsync))]
public interface IFeishuTenantDriveFolderFilesTool
{
    /// <summary>列出文件夹内容（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token），超长截断并标记 truncated。</returns>
    Task<string> GetFilesPageListAsync(
        [ToolParameter("folder_token", "文件夹 token（可选；缺省列出云空间根目录）")] string? folder_token = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：drive.get_file_metas（映射 <c>IFeishuTenantV1DriveFiles.BatchQueryMetasAsync</c>）。</summary>
[FeishuTool("drive.get_file_metas",
    Description = "按 token 批量查询文件元信息（类型/标题/链接/所有者，最多 200 个）；支撑后续读文档链（docx.get_raw_content 等）。只读，需 drive:drive:readonly。",
    RequiredScopes = ["drive:drive:readonly"],
    Source = nameof(IFeishuTenantV1DriveFiles) + "." + nameof(IFeishuTenantV1DriveFiles.BatchQueryMetasAsync))]
public interface IFeishuTenantDriveFileMetasTool
{
    /// <summary>元信息批量查询。</summary>
    /// <returns>白名单投影后的 JSON 文本（metas/failed_lists），超长截断并标记 truncated。</returns>
    Task<string> BatchQueryMetasAsync(
        [ToolParameter("tokens", "文件/文档 token 数组（最多 200 个）", Required = true)] string[] tokens,
        [ToolParameter("types", "与 tokens 等长的类型数组（可选：doc/docx/sheet/bitable/mindnote/file/wiki/folder/synced_block；缺省按 file 处理）")] string[]? types = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Drive 协作面：评论（4 个） ───────────────────────────

/// <summary>工具接口：drive.list_comments（映射 <c>IFeishuV1DriveComments.GetCommentsPageListAsync</c>）。</summary>
[FeishuTool("drive.list_comments",
    Description = "列出云文档的所有评论（comment_id/user_id/is_solved/reply_count/created_at）。先获取评论列表，再用 drive.reply_comment 或 drive.resolve_comment 操作。只读，需 drive:drive:readonly。",
    RequiredScopes = ["drive:drive:readonly"],
    Source = nameof(IFeishuTenantV1DriveComments) + "." + nameof(IFeishuV1DriveComments.GetCommentsPageListAsync))]
public interface IFeishuTenantDriveListCommentsTool
{
    /// <summary>列出评论（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token），超长截断并标记 truncated。</returns>
    Task<string> GetCommentsPageListAsync(
        [ToolParameter("file_token", "文件 token（形如 doxcnXxx）", Required = true)] string file_token,
        [ToolParameter("file_type", "文件类型（doc/docx/sheet/file/slides）", Required = true)] string file_type,
        [ToolParameter("is_solved", "是否只返回已解决的评论（可选，默认 false）")] bool? is_solved = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：drive.add_comment（映射 <c>IFeishuV1DriveComments.CreateFileCommentAsync</c>）。</summary>
[FeishuTool("drive.add_comment",
    Description = "向云文档添加一条全文评论（不支持局部评论）。参数为 file_token + file_type + text，请求体由绑定层组装（模型零 JSON 串）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 drive:drive。",
    RequiredScopes = ["drive:drive"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1DriveComments) + "." + nameof(IFeishuV1DriveComments.CreateFileCommentAsync))]
public interface IFeishuTenantDriveAddCommentTool
{
    /// <summary>添加评论。</summary>
    /// <returns>白名单投影后的 JSON 文本（comment_id），超长截断并标记 truncated。</returns>
    Task<string> CreateFileCommentAsync(
        [ToolParameter("file_token", "文件 token（形如 doxcnXxx）", Required = true)] string file_token,
        [ToolParameter("file_type", "文件类型（doc/docx/sheet/file/slides）", Required = true)] string file_type,
        [ToolParameter("text", "评论文本内容（不能为空）", Required = true)] string text,
        [ToolParameter("dry_run", "仅预演不评论（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：drive.reply_comment（映射 <c>IFeishuV1DriveComments.CreateFileCommentReplyAsync</c>）。</summary>
[FeishuTool("drive.reply_comment",
    Description = "回复指定评论（需 comment_id + text）。空文本会在下发前被拒绝。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 drive:drive。",
    RequiredScopes = ["drive:drive"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1DriveComments) + "." + nameof(IFeishuV1DriveComments.CreateFileCommentReplyAsync))]
public interface IFeishuTenantDriveReplyCommentTool
{
    /// <summary>回复评论。</summary>
    /// <returns>白名单投影后的 JSON 文本（reply_id），超长截断并标记 truncated。</returns>
    Task<string> CreateFileCommentReplyAsync(
        [ToolParameter("file_token", "文件 token（形如 doxcnXxx）", Required = true)] string file_token,
        [ToolParameter("comment_id", "评论 ID（来自 drive.list_comments）", Required = true)] string comment_id,
        [ToolParameter("file_type", "文件类型（doc/docx/sheet/file/slides）", Required = true)] string file_type,
        [ToolParameter("text", "回复文本内容（不能为空）", Required = true)] string text,
        [ToolParameter("dry_run", "仅预演不回复（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：drive.resolve_comment（映射 <c>IFeishuV1DriveComments.PatchFileCommentAsync</c>）。</summary>
[FeishuTool("drive.resolve_comment",
    Description = "解决或恢复评论（is_solved 三态），不是删除——天然幂等。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 drive:drive。",
    RequiredScopes = ["drive:drive"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1DriveComments) + "." + nameof(IFeishuV1DriveComments.PatchFileCommentAsync))]
public interface IFeishuTenantDriveResolveCommentTool
{
    /// <summary>解决/恢复评论。</summary>
    /// <returns>操作结果 JSON 文本。</returns>
    Task<string> PatchFileCommentAsync(
        [ToolParameter("file_token", "文件 token（形如 doxcnXxx）", Required = true)] string file_token,
        [ToolParameter("comment_id", "评论 ID（来自 drive.list_comments）", Required = true)] string comment_id,
        [ToolParameter("file_type", "文件类型（doc/docx/sheet/file/slides）", Required = true)] string file_type,
        [ToolParameter("is_solved", "是否解决（true=解决，false=恢复，必填）")] bool? is_solved,
        [ToolParameter("dry_run", "仅预演不操作（可选，默认 false）：返回将要下发的 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Drive 协作面：权限（6 个） ───────────────────────────

/// <summary>工具接口：drive.get_permission_public（映射 <c>IFeishuV1DrivePermissions.GetPermissionPublicAsync</c>）。</summary>
[FeishuTool("drive.get_permission_public",
    Description = "获取云文档的公开链接权限设置（link_share_entity/external_access 等），用于分享前确认当前文档的可见范围。⚠️ 该接口因涉及权限面，被风险分级器标记为写面——须经授权门禁。需 drive:drive:readonly。",
    RequiredScopes = ["drive:drive:readonly"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1DrivePermissions) + "." + nameof(IFeishuV1DrivePermissions.GetPermissionPublicAsync))]
public interface IFeishuTenantDriveGetPermissionPublicTool
{
    /// <summary>获取公开权限设置。</summary>
    /// <returns>白名单投影后的 JSON 文本（link_share_entity/external_access/...）。</returns>
    Task<string> GetPermissionPublicAsync(
        [ToolParameter("token", "云文档 token", Required = true)] string token,
        [ToolParameter("type", "云文档类型（doc/docx/sheet/file/wiki/bitable/folder/mindnote/slides）", Required = true)] string type,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：drive.update_permission_public（映射 <c>IFeishuV1DrivePermissions.UpdatePermissionPublicAsync</c>）。</summary>
[FeishuTool("drive.update_permission_public",
    Description = "更新云文档的公开链接权限设置。⚠️ 此操作可能使文档对组织外可见——请先 drive.get_permission_public 确认当前设置，并优先使用 dry_run 预演。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 drive:drive。",
    RequiredScopes = ["drive:drive"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1DrivePermissions) + "." + nameof(IFeishuV1DrivePermissions.UpdatePermissionPublicAsync))]
public interface IFeishuTenantDriveUpdatePermissionPublicTool
{
    /// <summary>更新公开权限设置。</summary>
    /// <returns>更新后的权限设置 JSON 文本。</returns>
    Task<string> UpdatePermissionPublicAsync(
        [ToolParameter("token", "云文档 token", Required = true)] string token,
        [ToolParameter("type", "云文档类型（doc/docx/sheet/file/wiki/bitable/folder/mindnote/slides）", Required = true)] string type,
        [ToolParameter("external_access", "外部访问开关（可选：open/closed）")] string? external_access = null,
        [ToolParameter("security_entity", "安全实体（可选：open/closed/anyone_can_view）")] string? security_entity = null,
        [ToolParameter("comment_entity", "评论权限实体（可选：open/closed/only_follower）")] string? comment_entity = null,
        [ToolParameter("share_entity", "分享权限实体（可选：open/closed/anyone）")] string? share_entity = null,
        [ToolParameter("link_share_entity", "链接分享实体（可选：open/closed/anyone_readable/anyone_editable）")] string? link_share_entity = null,
        [ToolParameter("dry_run", "仅预演不更新（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：drive.grant_permission（映射 <c>IFeishuV1DrivePermissions.CreatePermissionMemberAsync</c>）。</summary>
[FeishuTool("drive.grant_permission",
    Description = "为指定云文档添加单个协作者权限（member_type 为 openid/email/userid/chatid/departmentid）。⚠️ 此操作可扩大数据可达范围。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 drive:drive。",
    RequiredScopes = ["drive:drive"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1DrivePermissions) + "." + nameof(IFeishuV1DrivePermissions.CreatePermissionMemberAsync))]
public interface IFeishuTenantDriveGrantPermissionTool
{
    /// <summary>添加协作者权限。</summary>
    /// <returns>操作结果 JSON 文本（member_id/perm）。</returns>
    Task<string> CreatePermissionMemberAsync(
        [ToolParameter("token", "云文档 token", Required = true)] string token,
        [ToolParameter("type", "云文档类型（doc/docx/sheet/file/wiki/bitable/folder/mindnote/slides）", Required = true)] string type,
        [ToolParameter("member_type", "协作者类型（openid/email/userid/chatid/departmentid）", Required = true)] string member_type,
        [ToolParameter("member_id", "协作者 ID", Required = true)] string member_id,
        [ToolParameter("perm", "权限档位（view/edit/full_access）", Required = true)] string perm,
        [ToolParameter("dry_run", "仅预演不授权（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：drive.update_permission_member（映射 <c>IFeishuV1DrivePermissions.UpdatePermissionMemberAsync</c>）。</summary>
[FeishuTool("drive.update_permission_member",
    Description = "更新指定协作者的权限档位（view/edit/full_access），幂等操作。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 drive:drive。",
    RequiredScopes = ["drive:drive"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1DrivePermissions) + "." + nameof(IFeishuV1DrivePermissions.UpdatePermissionMemberAsync))]
public interface IFeishuTenantDriveUpdatePermissionMemberTool
{
    /// <summary>更新协作者权限。</summary>
    /// <returns>操作结果 JSON 文本。</returns>
    Task<string> UpdatePermissionMemberAsync(
        [ToolParameter("token", "云文档 token", Required = true)] string token,
        [ToolParameter("type", "云文档类型（doc/docx/sheet/file/wiki/bitable/folder/mindnote/slides）", Required = true)] string type,
        [ToolParameter("member_id", "协作者 ID", Required = true)] string member_id,
        [ToolParameter("perm", "新权限档位（view/edit/full_access）", Required = true)] string perm,
        [ToolParameter("dry_run", "仅预演不更新（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：drive.remove_permission（映射 <c>IFeishuV1DrivePermissions.DeletePermissionMemberAsync</c>）。</summary>
[FeishuTool("drive.remove_permission",
    Description = "删除指定协作者的权限（删除天然幂等）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 drive:drive。",
    RequiredScopes = ["drive:drive"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1DrivePermissions) + "." + nameof(IFeishuV1DrivePermissions.DeletePermissionMemberAsync))]
public interface IFeishuTenantDriveRemovePermissionTool
{
    /// <summary>删除协作者权限。</summary>
    /// <returns>操作结果 JSON 文本。</returns>
    Task<string> DeletePermissionMemberAsync(
        [ToolParameter("token", "云文档 token", Required = true)] string token,
        [ToolParameter("type", "云文档类型（doc/docx/sheet/file/wiki/bitable/folder/mindnote/slides）", Required = true)] string type,
        [ToolParameter("member_id", "协作者 ID", Required = true)] string member_id,
        [ToolParameter("dry_run", "仅预演不删除（可选，默认 false）：返回将要下发的 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：drive.transfer_owner（映射 <c>IFeishuV1DrivePermissions.TransferOwnerPermissionMemberAsync</c>）。</summary>
[FeishuTool("drive.transfer_owner",
    Description = "转移云文档所有者（不可逆！转移后原所有者降为编辑者）。必须过授权器。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 drive:drive。建议先 dry_run 预演确认。",
    RequiredScopes = ["drive:drive"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1DrivePermissions) + "." + nameof(IFeishuV1DrivePermissions.TransferOwnerPermissionMemberAsync))]
public interface IFeishuTenantDriveTransferOwnerTool
{
    /// <summary>转移所有者。</summary>
    /// <returns>操作结果 JSON 文本。</returns>
    Task<string> TransferOwnerPermissionMemberAsync(
        [ToolParameter("token", "云文档 token", Required = true)] string token,
        [ToolParameter("type", "云文档类型（doc/docx/sheet/file/wiki/bitable/folder/mindnote/slides）", Required = true)] string type,
        [ToolParameter("member_id", "新所有者的协作者 ID", Required = true)] string member_id,
        [ToolParameter("dry_run", "仅预演不转移（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}
