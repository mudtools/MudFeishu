// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── Wiki 节点面（5 个） ───────────────────────────

/// <summary>工具接口：wiki.get_node（映射 <c>IFeishuTenantV2WikiNodes.GetNodeSpaceInfoAsync</c>）。</summary>
[AgentTool("wiki.get_node",
    Description = "解析知识库节点信息（node_token/title/obj_type/obj_token）；obj_token 可传给 docx.get_raw_content 读取正文。只读，需 wiki:wiki:readonly。",
    RequiredScopes = ["wiki:wiki:readonly"],
    Source = nameof(IFeishuTenantV2WikiNodes) + "." + nameof(IFeishuTenantV2WikiNodes.GetNodeSpaceInfoAsync))]
public interface IFeishuTenantWikiGetNodeTool
{
    /// <summary>获取节点信息（单对象）。</summary>
    /// <returns>白名单投影后的 JSON 文本（node 对象）。</returns>
    Task<string> GetNodeSpaceInfoAsync(
        [ToolParameter("token", "知识库节点 token（node_token，形如 wikcnXxx）", Required = true)] string token,
        [ToolParameter("obj_type", "对象类型（可选，默认 wiki）")] string? obj_type = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：wiki.list_nodes（映射 <c>IFeishuTenantV2WikiNodes.GetSpaceNodesPageListAsync</c>）。</summary>
[AgentTool("wiki.list_nodes",
    Description = "列出知识空间（或某父节点下）的子节点列表；node_token 可传给 wiki.get_node 解析详情。只读，需 wiki:wiki:readonly。",
    RequiredScopes = ["wiki:wiki:readonly"],
    Source = nameof(IFeishuTenantV2WikiNodes) + "." + nameof(IFeishuTenantV2WikiNodes.GetSpaceNodesPageListAsync))]
public interface IFeishuTenantWikiListNodesTool
{
    /// <summary>列出子节点（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token），超长截断并标记 truncated。</returns>
    Task<string> GetSpaceNodesPageListAsync(
        [ToolParameter("space_id", "知识空间 ID（形如 7xxx）", Required = true)] string space_id,
        [ToolParameter("parent_node_token", "父节点 token（可选；缺省列出空间顶层节点）")] string? parent_node_token = null,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Wiki 写 ───────────────────────────

/// <summary>
/// 工具接口：wiki.create_node（映射 <c>IFeishuTenantV2WikiNodes.CreateSpaceNodeAsync</c>）。
/// </summary>
/// <remarks>
/// wiki 域<b>必须有写面</b>（get_node / list_nodes 只读，否则模型能看知识库但不能改）。
/// </remarks>
[AgentTool("wiki.create_node",
    Description = "在知识空间下创建节点（新建一篇 wiki 文档/普通页面）。space_id 可由 wiki.list_nodes 的结果推断。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 wiki:wiki。",
    RequiredScopes = ["wiki:wiki"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2WikiNodes) + "." + nameof(IFeishuTenantV2WikiNodes.CreateSpaceNodeAsync))]
public interface IFeishuTenantWikiCreateNodeTool
{
    /// <summary>创建知识空间节点。</summary>
    /// <returns>白名单投影后的 JSON 文本（node_token/obj_token/title/obj_type）。</returns>
    Task<string> CreateNodeAsync(
        [ToolParameter("space_id", "知识空间 ID（形如 7xxx）", Required = true)] string space_id,
        [ToolParameter("title", "节点标题", Required = true)] string title,
        [ToolParameter("obj_type", "节点对象类型（可选：docx=文档 / sheet=表格 / mindnote=思维笔记 / bitable=多维表格 / file=文件，默认 docx）")] string? obj_type = null,
        [ToolParameter("parent_node_token", "父节点 token（可选；缺省创建到空间顶层）")] string? parent_node_token = null,
        [ToolParameter("node_type", "节点类型（可选：origin=普通节点 / shortcut=快捷方式，默认 origin）")] string? node_type = null,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：wiki.move_node（映射 <c>IFeishuTenantV2WikiNodes.MoveSpaceNodeAsync</c>）。
/// </summary>
[AgentTool("wiki.move_node",
    Description = "移动知识空间节点（改父节点或换空间）——用于知识库整理、归档。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 wiki:wiki。",
    RequiredScopes = ["wiki:wiki"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2WikiNodes) + "." + nameof(IFeishuTenantV2WikiNodes.MoveSpaceNodeAsync))]
public interface IFeishuTenantWikiMoveNodeTool
{
    /// <summary>移动节点。</summary>
    /// <returns>白名单投影后的 JSON 文本（node_token/parent_node_token/space_id）。</returns>
    Task<string> MoveNodeAsync(
        [ToolParameter("space_id", "节点当前所在空间 ID", Required = true)] string space_id,
        [ToolParameter("node_token", "要移动的节点 token", Required = true)] string node_token,
        [ToolParameter("target_parent_token", "目标父节点 token（可选；缺省移到目标空间顶层）")] string? target_parent_token = null,
        [ToolParameter("target_space_id", "目标空间 ID（可选；缺省在当前空间内移动）")] string? target_space_id = null,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：wiki.move_docs_to_space（映射 <c>IFeishuTenantV2WikiNodes.MoveDocsToWikiSpaceNodeAsync</c>）。
/// </summary>
/// <remarks>
/// "**把文档挪进知识库**"是高频诉求，而 SDK 的节点移动只对已在 wiki 中的节点有效；
/// 本工具走平台提供的<b>文档迁入</b>接口，是该场景的唯一正确入口。
/// </remarks>
[AgentTool("wiki.move_docs_to_space",
    Description = "把已有云文档（docx/sheet/bitable 等）迁移进知识空间，成为 wiki 节点——'把这份文档挪进知识库'的首选入口。⚠️ 平台以异步任务执行，返回 task_id。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 wiki:wiki。",
    RequiredScopes = ["wiki:wiki"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2WikiNodes) + "." + nameof(IFeishuTenantV2WikiNodes.MoveDocsToWikiSpaceNodeAsync))]
public interface IFeishuTenantWikiMoveDocsToSpaceTool
{
    /// <summary>把文档迁入知识空间（异步任务）。</summary>
    /// <returns>白名单投影后的 JSON 文本（wiki_token/task_id/applied）。</returns>
    Task<string> MoveDocsToSpaceAsync(
        [ToolParameter("space_id", "目标知识空间 ID", Required = true)] string space_id,
        [ToolParameter("obj_token", "要迁入的文档 token（形如 doxcnXxx）", Required = true)] string obj_token,
        [ToolParameter("parent_wiki_token", "目标父节点 token（可选；缺省放到空间顶层）")] string? parent_wiki_token = null,
        [ToolParameter("obj_type", "文档类型（可选：docx / sheet / bitable / mindnote / file，默认 docx）")] string? obj_type = null,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Wiki 空间与成员（知识空间层，节点层的上游） ───────────────────────────
//
// 策展口径：本组补齐"空间"这一层——原有节点类工具全部从 space_id 出发，而 space_id 此前只能靠
// 用户口述或别的域（search.doc_wiki）间接得到，链路是断的。成员面只做增/删/查三件事：
// **不策展 `wiki.update_space_setting`**（改空间级创建/安全/评论设置），因为它一次写三项互相独立
// 的开关，部分传参会把未传的开关按平台默认值覆盖，需要单独设计参数面后再入面。

/// <summary>工具接口：wiki.space_list（映射 <c>IFeishuTenantV2Wiki.GetSpacesPageListAsync</c>）。</summary>
/// <remarks>
/// <b>空列表 ≠ 租户没有知识库</b>：平台按调用身份可见性过滤，应用未被加入任何空间时同样返回空。
/// 这一点必须写进描述，否则模型会把"没权限"表述成"没有内容"。
/// </remarks>
[AgentTool("wiki.space_list",
    Description = "列出当前调用身份有权限访问的知识空间（space_id/name/space_type/visibility），space_id 可喂 wiki.list_nodes / wiki.member_list。⚠️ 结果按可见性过滤：返回空列表表示『该身份没有被加入任何空间』，不代表企业里没有知识库。只读，需 wiki:wiki:readonly。",
    RequiredScopes = ["wiki:wiki:readonly"],
    Source = nameof(IFeishuTenantV2Wiki) + "." + nameof(IFeishuTenantV2Wiki.GetSpacesPageListAsync))]
public interface IFeishuTenantWikiListSpacesTool
{
    /// <summary>列出有权限的知识空间（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token）。</returns>
    Task<string> ListSpacesAsync(
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：wiki.space_get（映射 <c>IFeishuTenantV2Wiki.GetSpaceInfoAsync</c>）。</summary>
/// <remarks>
/// `visibility=public` 的空间对租户内所有用户可见——**这类空间不支持再加普通成员，只能加管理员**，
/// 是 member_add 最常见的失败原因，故把判据放在这里而不是写工具的错误分支里。
/// </remarks>
[AgentTool("wiki.space_get",
    Description = "按 space_id 获取单个知识空间信息（name/description/space_type/visibility/open_sharing）。查'这个知识库对谁开放、是什么类型'用它；visibility=public 表示租户内全员可见，个人空间（space_type=person）不支持添加其他管理员。只读，需 wiki:wiki:readonly。",
    RequiredScopes = ["wiki:wiki:readonly"],
    Source = nameof(IFeishuTenantV2Wiki) + "." + nameof(IFeishuTenantV2Wiki.GetSpaceInfoAsync))]
public interface IFeishuTenantWikiGetSpaceTool
{
    /// <summary>获取知识空间信息。</summary>
    Task<string> GetSpaceAsync(
        [ToolParameter("space_id", "知识空间 ID（形如 7xxx，来自 wiki.space_list）", Required = true)] string space_id,
        [ToolParameter("lang", "空间名称展示语言（可选，仅对个人文档库生效：zh / en / ja / ko / ...，默认 en）")] string? lang = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：wiki.member_list（映射 <c>IFeishuTenantV2Wiki.GetSpaceMemberPageListAsync</c>）。</summary>
/// <remarks>
/// 本工具是 member_remove 的<b>前置定位器</b>：删除成员要同时给对 member_type 与 member_role，
/// 而这两个值只有列出来才知道；调用身份须为该空间管理员，否则平台返回权限错误。
/// </remarks>
[AgentTool("wiki.member_list",
    Description = "列出知识空间的成员与管理员（member_type/member_id/member_role）。wiki.member_remove 需要同时提供 member_type 与 member_role，删前先调本工具核对；调用身份须是该空间的管理员，否则平台报权限不足。只读，需 wiki:wiki:readonly。",
    RequiredScopes = ["wiki:wiki:readonly"],
    Source = nameof(IFeishuTenantV2Wiki) + "." + nameof(IFeishuTenantV2Wiki.GetSpaceMemberPageListAsync))]
public interface IFeishuTenantWikiListMembersTool
{
    /// <summary>列出空间成员（分页）。</summary>
    Task<string> ListMembersAsync(
        [ToolParameter("space_id", "知识空间 ID", Required = true)] string space_id,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：wiki.member_add（映射 <c>IFeishuTenantV2Wiki.CreateSpaceMemberAsync</c>）。
/// </summary>
/// <remarks>
/// 与 <c>drive.grant_permission</c> 同类：<b>授权动作会扩大数据可达范围</b>，一次调用即让某人看到
/// 整个空间。成员增删的 ID 类型闭集只在描述里给全，模型才不至于把 open_id 当 user_id 传。
/// </remarks>
[AgentTool("wiki.member_add",
    Description = "为知识空间添加成员或管理员（member_role：admin=管理员 / member=成员）。⚠️ 这是授权动作：一次调用即让该身份看到整个空间；公开空间（visibility=public）不支持加成员、只能加管理员；以应用身份调用时不能用 opendepartmentid。调用身份须为该空间管理员。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），建议先 dry_run 预演确认，需 wiki:wiki。",
    RequiredScopes = ["wiki:wiki"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2Wiki) + "." + nameof(IFeishuTenantV2Wiki.CreateSpaceMemberAsync))]
public interface IFeishuTenantWikiAddMemberTool
{
    /// <summary>添加空间成员/管理员。</summary>
    Task<string> AddMemberAsync(
        [ToolParameter("space_id", "知识空间 ID", Required = true)] string space_id,
        [ToolParameter("member_type", "成员身份类型（必填闭集：openid / unionid / userid / email / openchat / opendepartmentid；应用身份不支持 opendepartmentid）", Required = true)] string member_type,
        [ToolParameter("member_id", "成员 ID，类型由 member_type 决定（如 ou_xxx / 邮箱）", Required = true)] string member_id,
        [ToolParameter("member_role", "角色（必填：admin=管理员 / member=成员）", Required = true)] string member_role,
        [ToolParameter("need_notification", "添加后是否通知对方（可选，默认不通知）")] bool? need_notification = null,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：wiki.member_remove（映射 <c>IFeishuTenantV2Wiki.DeleteSpaceMemberAsync</c>）。
/// </summary>
/// <remarks>
/// 平台的删除按 <c>member_id + member_type + member_role</c> 三元组定位：角色填错即"删不到人"，
/// 而接口仍可能返回成功，所以把 member_list 写成硬前置（校验期拒绝闭集外的角色值），
/// 不指望下游报错来教育模型。
/// </remarks>
[AgentTool("wiki.member_remove",
    Description = "移除知识空间的成员或管理员。high-risk-write：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 wiki:wiki。建议先 dry_run 预演确认。平台按 member_id + member_type + member_role 三元组定位，角色填错会删不到人——先用 wiki.member_list 核对三项再动手。",
    RequiredScopes = ["wiki:wiki"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2Wiki) + "." + nameof(IFeishuTenantV2Wiki.DeleteSpaceMemberAsync))]
public interface IFeishuTenantWikiRemoveMemberTool
{
    /// <summary>移除空间成员/管理员。</summary>
    Task<string> RemoveMemberAsync(
        [ToolParameter("space_id", "知识空间 ID", Required = true)] string space_id,
        [ToolParameter("member_id", "要移除的成员 ID，类型由 member_type 决定", Required = true)] string member_id,
        [ToolParameter("member_type", "成员身份类型（必填闭集：openid / unionid / userid / email / openchat / opendepartmentid），须与 wiki.member_list 返回的一致", Required = true)] string member_type,
        [ToolParameter("member_role", "角色（必填：admin / member），须与该成员当前角色一致", Required = true)] string member_role,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：wiki.node_copy（映射 <c>IFeishuTenantV2WikiNodes.CopySpaceNodeAsync</c>）。
/// </summary>
/// <remarks>
/// 与 <c>wiki.move_node</c> 的分工是易错点：copy <b>保留原节点</b>并产出新 token，
/// move 改位置而 token 不变。模型常把"复制一份到别的空间"表述成 move，故描述里对照写明。
/// </remarks>
[AgentTool("wiki.node_copy",
    Description = "复制知识空间节点副本到指定位置（原节点保留，产出新的 node_token）——'把这个页面复制一份到另一个空间/目录'用本工具；要改位置且保留 token 用 wiki.move_node。target_space_id 与 target_parent_token **至少提供一个**（两者都不传平台会拒绝，不会复制在当前空间顶层）；副本的底层文档也会被一并复制（obj_token 与原来不同）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），建议先 dry_run 预演确认，需 wiki:wiki。",
    RequiredScopes = ["wiki:wiki"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2WikiNodes) + "." + nameof(IFeishuTenantV2WikiNodes.CopySpaceNodeAsync))]
public interface IFeishuTenantWikiCopyNodeTool
{
    /// <summary>复制节点副本。</summary>
    Task<string> CopyNodeAsync(
        [ToolParameter("space_id", "源节点所在知识空间 ID", Required = true)] string space_id,
        [ToolParameter("node_token", "要复制的源节点 token", Required = true)] string node_token,
        [ToolParameter("target_space_id", "目标知识空间 ID（可选；与 target_parent_token 至少提供一个）")] string? target_space_id = null,
        [ToolParameter("target_parent_token", "目标父节点 token（可选；与 target_space_id 至少提供一个）")] string? target_parent_token = null,
        [ToolParameter("title", "副本标题（可选；缺省沿用源节点标题）")] string? title = null,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── 空间创建（用户身份） ───────────────────────────

/// <summary>
/// 工具接口：wiki.space_create（映射 <c>IFeishuUserV2Wiki.CreateSpaceAsync</c>）。
/// </summary>
/// <remarks>
/// 创建知识空间在平台上<b>只有 user_access_token 形态</b>（租户态 <c>IFeishuTenantV2Wiki</c> 无此方法），
/// 故接口名必须以 <c>IFeishuUser</c> 开头——身份轴由接口名前缀派生，写错前缀会让工具被当成
/// 租户身份而在必将失败的调用上反复重试。空间创建者即空间管理员，后续
/// <c>wiki.member_add</c> 才有权限可依据。
/// </remarks>
[AgentTool("wiki.space_create",
    Description = "创建一个新的知识空间（返回 space_id，可直接作为 wiki.create_node / wiki.member_add 的上游输入）。⚠️ 仅支持用户身份调用：宿主须在 FeishuAgent:AllowedIdentities 放行 user，否则会因身份被拒而失败。创建者自动成为该空间的管理员。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），建议先 dry_run 预演确认，需 wiki:wiki。",
    RequiredScopes = ["wiki:wiki"],
    IsWrite = true,
    Source = nameof(IFeishuUserV2Wiki) + "." + nameof(IFeishuUserV2Wiki.CreateSpaceAsync))]
public interface IFeishuUserWikiCreateSpaceTool
{
    /// <summary>创建知识空间。</summary>
    /// <returns>白名单投影后的 JSON 文本（space_id/name/description/space_type/visibility/open_sharing）。</returns>
    Task<string> CreateSpaceAsync(
        [ToolParameter("name", "知识空间名称", Required = true)] string name,
        [ToolParameter("description", "知识空间描述（可选）")] string? description = null,
        [ToolParameter("open_sharing", "分享状态（可选闭集：open=允许分享 / closed=不允许分享）")] string? open_sharing = null,
        [ToolParameter("dry_run", "仅预演不创建（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}
