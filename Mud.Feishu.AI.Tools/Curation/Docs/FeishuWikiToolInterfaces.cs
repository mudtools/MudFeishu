// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── Wiki（2 个） ───────────────────────────

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
