// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── Board 画板（R7 / A4 新域） ───────────────────────────

/// <summary>
/// 工具接口：board.get_theme（映射 <c>IFeishuTenantV1Board.GetWhiteboardThemeAsync</c>）。
/// </summary>
/// <remarks>
/// R7 / A4：获取画板主题（默认配色）。画板 whiteboard_id 来自文档块——
/// <c>docx.get_document_blocks</c> 中 <c>block_type=43</c> 的 block.token 即画板 ID。
/// </remarks>
[FeishuTool("board.get_theme",
    Description = "获取画板主题信息（默认配色方案）。whiteboard_id 来自文档块：docx.get_document_blocks 中 block_type=43 的 block.token 即画板 ID。只读，需 board:whiteboard:readonly。",
    RequiredScopes = ["board:whiteboard:readonly"],
    Source = nameof(IFeishuTenantV1Board) + "." + nameof(IFeishuTenantV1Board.GetWhiteboardThemeAsync))]
public interface IFeishuTenantBoardGetThemeTool
{
    /// <summary>获取画板主题。</summary>
    /// <returns>白名单投影后的 JSON 文本（theme_id/theme_name/colors）。</returns>
    Task<string> GetWhiteboardThemeAsync(
        [ToolParameter("whiteboard_id", "画板 ID（来自文档 block_type=43 的 block.token）", Required = true)] string whiteboard_id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：board.update_theme（映射 <c>IFeishuTenantV1Board.UpdateWhiteboardThemeAsync</c>）。
/// </summary>
/// <remarks>
/// R7 / A4：更新画板主题（幂等）。
/// </remarks>
[FeishuTool("board.update_theme",
    Description = "更新画板主题（配色方案）。幂等操作。需 board:whiteboard 权限。",
    RequiredScopes = ["board:whiteboard"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1Board) + "." + nameof(IFeishuTenantV1Board.UpdateWhiteboardThemeAsync))]
public interface IFeishuTenantBoardUpdateThemeTool
{
    /// <summary>更新画板主题。</summary>
    /// <returns>操作结果 JSON 文本。</returns>
    Task<string> UpdateWhiteboardThemeAsync(
        [ToolParameter("whiteboard_id", "画板 ID（来自文档 block_type=43 的 block.token）", Required = true)] string whiteboard_id,
        [ToolParameter("theme_id", "目标主题 ID", Required = true)] string theme_id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：board.render_dsl（映射 <c>IFeishuTenantV1Board.CreatePlantumlWhiteboardNodeAsync</c>）。
/// </summary>
/// <remarks>
/// R7 / A4：<b>本域杀手工具</b>——PlantUML/Mermaid 源码 → 画板节点。
/// 模型无需懂画板节点结构，只需输出 DSL 文本。
/// </remarks>
[FeishuTool("board.render_dsl",
    Description = "将 PlantUML 或 Mermaid 源码解析为画板节点（协同编辑）。模型只需输出 DSL 文本，无需了解画板节点结构。需 board:whiteboard 权限。",
    RequiredScopes = ["board:whiteboard"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1Board) + "." + nameof(IFeishuTenantV1Board.CreatePlantumlWhiteboardNodeAsync))]
public interface IFeishuTenantBoardRenderDslTool
{
    /// <summary>解析画板语法（PlantUML/Mermaid → 画板节点）。</summary>
    /// <returns>操作结果 JSON 文本。</returns>
    Task<string> CreatePlantumlWhiteboardNodeAsync(
        [ToolParameter("whiteboard_id", "画板 ID（来自文档 block_type=43 的 block.token）", Required = true)] string whiteboard_id,
        [ToolParameter("dsl_type", "DSL 类型：plantuml 或 mermaid", Required = true)] string dsl_type,
        [ToolParameter("content", "DSL 源码内容（PlantUML 或 Mermaid 语法）", Required = true)] string content,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：board.create_nodes（映射 <c>IFeishuTenantV1Board.CreateWhiteboardNodeAsync</c>）。
/// </summary>
/// <remarks>
/// R7 / A4：创建画板节点。底层支持 <c>client_token</c> 幂等 → 本工具暴露 <c>idempotency_key</c>。
/// </remarks>
[FeishuTool("board.create_nodes",
    Description = "在画板中创建节点（支持批量创建、父子关系）。支持幂等：传入 idempotency_key 可安全重试。需 board:whiteboard 权限。",
    RequiredScopes = ["board:whiteboard"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1Board) + "." + nameof(IFeishuTenantV1Board.CreateWhiteboardNodeAsync))]
public interface IFeishuTenantBoardCreateNodesTool
{
    /// <summary>创建画板节点。</summary>
    /// <returns>白名单投影后的 JSON 文本（nodes 列表）。</returns>
    Task<string> CreateWhiteboardNodeAsync(
        [ToolParameter("whiteboard_id", "画板 ID（来自文档 block_type=43 的 block.token）", Required = true)] string whiteboard_id,
        [ToolParameter("nodes_json", "节点数组 JSON（每个节点含 type/parent_id/props 等）", Required = true)] string nodes_json,
        [ToolParameter("idempotency_key", "幂等键（可选，传入后相同键的重复请求不会创建重复节点）")] string? idempotency_key = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：board.list_nodes（映射 <c>IFeishuTenantV1Board.GetWhiteboardNodesAsync</c>）。
/// </summary>
/// <remarks>
/// R7 / A4：返回画板内所有节点，可通过 parent_id/children 组装成画板内容。
/// </remarks>
[FeishuTool("board.list_nodes",
    Description = "获取画板内所有节点（返回节点数组，可通过 parent_id/children 组装画板结构）。只读，需 board:whiteboard:readonly。",
    RequiredScopes = ["board:whiteboard:readonly"],
    Source = nameof(IFeishuTenantV1Board) + "." + nameof(IFeishuTenantV1Board.GetWhiteboardNodesAsync))]
public interface IFeishuTenantBoardListNodesTool
{
    /// <summary>获取画板所有节点。</summary>
    /// <returns>白名单投影后的 JSON 文本（nodes 数组含 parent_id/children 关系）。</returns>
    Task<string> GetWhiteboardNodesAsync(
        [ToolParameter("whiteboard_id", "画板 ID（来自文档 block_type=43 的 block.token）", Required = true)] string whiteboard_id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：board.delete_nodes（映射 <c>IFeishuTenantV1Board.BatchDeleteWhiteboardNodeAsync</c>）。
/// </summary>
/// <remarks>
/// R7 / A4：批量删除画板节点。<b>子节点会被递归删除</b>——描述中声明后果。
/// </remarks>
[FeishuTool("board.delete_nodes",
    Description = "批量删除画板节点。注意：子节点会被递归删除，操作不可逆。需 board:whiteboard 权限。",
    RequiredScopes = ["board:whiteboard"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1Board) + "." + nameof(IFeishuTenantV1Board.BatchDeleteWhiteboardNodeAsync))]
public interface IFeishuTenantBoardDeleteNodesTool
{
    /// <summary>批量删除画板节点。</summary>
    /// <returns>操作结果 JSON 文本。</returns>
    Task<string> BatchDeleteWhiteboardNodeAsync(
        [ToolParameter("whiteboard_id", "画板 ID（来自文档 block_type=43 的 block.token）", Required = true)] string whiteboard_id,
        [ToolParameter("node_ids", "要删除的节点 ID 数组", Required = true)] string[] node_ids,
        CancellationToken cancellationToken = default);
}
