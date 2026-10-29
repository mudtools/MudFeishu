// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── Aily · 智能伙伴数据知识 ───────────────────────────

// 命名口径：工具名前缀 aily ⇒ 常量 FeishuToolNames.Aily*、参数类 Aily*Args、
// DI 入口 AddFeishuAilyToolsCore（执行器类 AilyTools）；SDK 接口名
// IFeishuTenantV1AilyDataKnowledge（注意基接口是 IFeishuV1Aily*，无 Tenant/User 中缀）。
//
// ⚠️ 域边界（与 knowledge / lingo / wiki 的三方划界，全部有 SDK 注释依据）：
//   ① 本域是 Aily 数据知识的**配置清单面**（有哪些知识库、什么状态、挂了哪些标签）；
//   ② 「文档里写过什么」的**问答/检索**走 knowledge.search（同一数据域的 RAG-A 门面，
//      底层 AskDataKnowledgeAsync 返回 SSE 流，模型侧不可达，故只保留 provider 门面）；
//   ③ 「这个词是什么意思」走 lingo.*（人工维护的企业词典术语）；
//   ④ 「文档在哪、知识空间结构」走 wiki.* 与 search.doc_wiki。
//
// ⚠️ **刻意不做**（理由写进 Guidance，不做静默缺口）：
//    · ask_data_knowledge：返回 HttpResponseMessage SSE 流，模型侧无法消费裸流；
//    · upload_data_asset_file / create_agent_attachment：`[FormContent]` + `[FilePath]` 本地路径，模型侧不可达；
//    · create/delete_data_asset、agent/session/skill 三族的会话与技能写面：属"对话编排"面，
//      价值低于数据知识清单，且 delete_data_asset 命中危险词需 affordance——不在本面策展。

/// <summary>
/// 工具接口：aily.list_data_assets（映射 <c>IFeishuTenantV1AilyDataKnowledge.GetDataAssetPageListAsync</c>）。
/// </summary>
/// <remarks>
/// 回答"这个 Aily 应用配置了哪些数据知识库、各自什么状态"——数据知识的管理清单面。
/// </remarks>
[AgentTool("aily.list_data_assets",
    Description = "分页列出 Aily 应用的数据知识库（data_asset_id/多语言标题/描述/数据源类型/连接状态/标签）——回答「这个应用接了哪些知识库」「哪个库连接失败」。只读，需 aily:knowledge:readonly。",
    RequiredScopes = ["aily:knowledge:readonly"],
    Source = nameof(IFeishuTenantV1AilyDataKnowledge) + "." + nameof(IFeishuTenantV1AilyDataKnowledge.GetDataAssetPageListAsync))]
public interface IFeishuTenantAilyListDataAssetsTool
{
    /// <summary>列出数据知识库。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token），超长截断并标记 truncated。</returns>
    Task<string> ListDataAssetsAsync(
        [ToolParameter("app_id", "Aily 应用 ID（必填，形如 app_xxx）", Required = true)] string app_id,
        [ToolParameter("keyword", "按标题/描述过滤的关键词（可选）")] string? keyword = null,
        [ToolParameter("data_asset_ids", "只查这些数据知识库 ID（可选，数组）")] string[]? data_asset_ids = null,
        [ToolParameter("data_asset_tag_ids", "只查打了这些标签的知识库（可选，数组）")] string[]? data_asset_tag_ids = null,
        [ToolParameter("with_connect_status", "是否带连接状态明细（可选，默认 false）")] bool? with_connect_status = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：aily.get_data_asset（映射 <c>GetDataAssetAsync</c>）。
/// </summary>
[AgentTool("aily.get_data_asset",
    Description = "查询单个数据知识库详情（标题/描述/数据源类型/连接状态与失败原因/标签/条目数）——「这个知识库为什么连不上」。只读，需 aily:knowledge:readonly。",
    RequiredScopes = ["aily:knowledge:readonly"],
    Source = nameof(IFeishuTenantV1AilyDataKnowledge) + "." + nameof(IFeishuTenantV1AilyDataKnowledge.GetDataAssetAsync))]
public interface IFeishuTenantAilyGetDataAssetTool
{
    /// <summary>查询数据知识库详情。</summary>
    /// <returns>白名单投影后的 JSON 文本（found + data_asset 详情），超长截断并标记 truncated。</returns>
    Task<string> GetDataAssetAsync(
        [ToolParameter("app_id", "Aily 应用 ID（必填，形如 app_xxx）", Required = true)] string app_id,
        [ToolParameter("data_asset_id", "数据知识库 ID（必填，来自 aily.list_data_assets）", Required = true)] string data_asset_id,
        [ToolParameter("with_data_asset_item", "是否带条目明细（可选，默认 false）")] bool? with_data_asset_item = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：aily.list_data_asset_tags（映射 <c>GetDataAssetTagPageListAsync</c>）。
/// </summary>
[AgentTool("aily.list_data_asset_tags",
    Description = "分页列出数据知识库的标签（tag_id/name）——「知识库都打了哪些标签」「按标签筛库」。只读，需 aily:knowledge:readonly。",
    RequiredScopes = ["aily:knowledge:readonly"],
    Source = nameof(IFeishuTenantV1AilyDataKnowledge) + "." + nameof(IFeishuTenantV1AilyDataKnowledge.GetDataAssetTagPageListAsync))]
public interface IFeishuTenantAilyListDataAssetTagsTool
{
    /// <summary>列出数据知识库标签。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token），超长截断并标记 truncated。</returns>
    Task<string> ListDataAssetTagsAsync(
        [ToolParameter("app_id", "Aily 应用 ID（必填，形如 app_xxx）", Required = true)] string app_id,
        [ToolParameter("keyword", "按标签名过滤的关键词（可选）")] string? keyword = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}
