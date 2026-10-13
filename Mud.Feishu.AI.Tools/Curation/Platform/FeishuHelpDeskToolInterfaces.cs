// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── Helpdesk · 飞书服务台（B1 第三个补域） ───────────────────────────

/// <summary>
/// 工具接口：helpdesk.search_faqs（映射 <c>IFeishuTenantV1HelpDeskFaq.SearchFaqAsync</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>本域的主力工具</b>：服务台 FAQ 是"客服/员工自助问答"的知识源。回答
/// "某某问题有没有现成答复"时优先用它，而不是通用检索。
/// </para>
/// <para>
/// <b>与 <c>knowledge.search</c> / <c>wiki.*</c> 的边界（易混点）</b>：三者都叫"知识/文档检索"，
/// 但数据源与权限完全不同——<c>knowledge.search</c> 是 Aily 数据知识切片、
/// <c>wiki.*</c> 是云文档知识空间、本工具是<b>服务台内置 FAQ</b>（工单场景的答复口径）。
/// 选错工具会拿到"看起来像答案但不是服务台口径"的内容。
/// </para>
/// </remarks>
[AgentTool("helpdesk.search_faqs",
    Description = "按关键词检索飞书服务台 FAQ（返回 faq_id/问题/答案摘要/分类/标签）——回答「这个问题客户服务台有没有现成答复」的首选；与 knowledge.search（Aily 知识）、wiki.*（云文档知识库）数据源不同，别混用。只读，需 helpdesk:faq:readonly。",
    RequiredScopes = ["helpdesk:faq:readonly"],
    Source = nameof(IFeishuTenantV1HelpDeskFaq) + "." + nameof(IFeishuTenantV1HelpDeskFaq.SearchFaqAsync))]
public interface IFeishuTenantHelpdeskSearchFaqsTool
{
    /// <summary>检索服务台 FAQ。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token/total：faq_id/question/answer 摘要/categories/tags），超长截断并标记 truncated。</returns>
    Task<string> SearchFaqsAsync(
        [ToolParameter("query", "检索关键词（必填，如「发票」「退款」；短语比整句命中率高）", Required = true)] string query,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：helpdesk.list_faqs（映射 <c>IFeishuTenantV1HelpDeskFaq.GetFaqListAsync</c>）。
/// </summary>
/// <remarks>
/// 与 <c>search_faqs</c> 的分工：本工具是**浏览**（可按分类 / 状态过滤，或用 <c>search</c> 做粗过滤），
/// 适合"这个服务台有哪些常见问题"；按关键词精确找答复用 <c>search_faqs</c>。
/// </remarks>
[AgentTool("helpdesk.list_faqs",
    Description = "分页列出服务台 FAQ（可选按 category_id / status 过滤，或用 search 做粗过滤）——用于浏览「这个服务台有哪些常见问题」；按关键词找具体答复请用 helpdesk.search_faqs。只读，需 helpdesk:faq:readonly。",
    RequiredScopes = ["helpdesk:faq:readonly"],
    Source = nameof(IFeishuTenantV1HelpDeskFaq) + "." + nameof(IFeishuTenantV1HelpDeskFaq.GetFaqListAsync))]
public interface IFeishuTenantHelpdeskListFaqsTool
{
    /// <summary>列出 FAQ。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token/total：faq_id/question/answer 摘要/分类/标签），超长截断并标记 truncated。</returns>
    Task<string> ListFaqsAsync(
        [ToolParameter("category_id", "FAQ 分类 ID（可选，来自 helpdesk.list_categories）")] string? category_id = null,
        [ToolParameter("status", "FAQ 状态过滤（可选：0=草稿 / 1=已发布 / 2=已删除 / 3=审核中；一般只关心 1）")] string? status = null,
        [ToolParameter("search", "问题关键字粗过滤（可选；精确检索请用 search_faqs）")] string? search = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：helpdesk.get_faq（映射 <c>IFeishuTenantV1HelpDeskFaq.GetFaqAsync</c>）。
/// </summary>
/// <remarks>
/// <b>不返回 FAQ 的创建人 / 更新人</b>（出站净化）：那属员工身份信息，
/// 而"这条问答写了什么"才是本工具的用途。
/// </remarks>
[AgentTool("helpdesk.get_faq",
    Description = "按 faq_id 查询单条服务台 FAQ 的完整内容（问题/答案全文/富文本片段/分类/标签/生效与过期时间）。⚠️ 不返回创建人与更新人（出站净化）。只读，需 helpdesk:faq:readonly。",
    RequiredScopes = ["helpdesk:faq:readonly"],
    Source = nameof(IFeishuTenantV1HelpDeskFaq) + "." + nameof(IFeishuTenantV1HelpDeskFaq.GetFaqAsync))]
public interface IFeishuTenantHelpdeskGetFaqTool
{
    /// <summary>查询单条 FAQ。</summary>
    /// <returns>白名单投影后的 JSON 文本（found + faq：question/answer/content/richtext/categories/tags/时间），超长截断并标记 truncated。</returns>
    Task<string> GetFaqAsync(
        [ToolParameter("faq_id", "FAQ ID（必填，来自 helpdesk.search_faqs / helpdesk.list_faqs）", Required = true)] string faq_id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：helpdesk.list_categories（映射 <c>IFeishuTenantV1HelpDeskCategory.GetCategoryListAsync</c>）。
/// </summary>
/// <remarks>
/// 分类是 FAQ 的目录树（<c>parent_id</c> + <c>children</c>）——先拿分类 ID 再按类浏览 FAQ，
/// 比直接全量翻 FAQ 更省往返。
/// </remarks>
[AgentTool("helpdesk.list_categories",
    Description = "列出服务台 FAQ 分类目录树（category_id/name/parent_id/children）——用于回答「服务台都有哪些分类」或拿 category_id 收窄 helpdesk.list_faqs。只读，需 helpdesk:faq:readonly。",
    RequiredScopes = ["helpdesk:faq:readonly"],
    Source = nameof(IFeishuTenantV1HelpDeskCategory) + "." + nameof(IFeishuTenantV1HelpDeskCategory.GetCategoryListAsync))]
public interface IFeishuTenantHelpdeskListCategoriesTool
{
    /// <summary>列出 FAQ 分类。</summary>
    /// <returns>白名单投影后的 JSON 文本（items：category_id/name/parent_id/children 树），超长截断并标记 truncated。</returns>
    Task<string> ListCategoriesAsync(
        [ToolParameter("lang", "分类名语言（可选，如 zh_cn / en_us；省略则按服务台默认语言）")] string? lang = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：helpdesk.get_ticket（映射 <c>IFeishuTenantV1HelpDeskTicket.GetTicketAsync</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>本工具只回答"工单处于什么状态、由谁在处理"</b>：状态 / 类型 / 渠道 / 评分 / 是否解决 /
/// 时间线 / 标签 / 客户与客服的<b>标识与姓名</b>。
/// </para>
/// <para>
/// <b>有意不返回</b>：备注正文（<c>comments.content</c>）、工单消息对话、
/// 客户自定义字段值、以及客户 / 客服的<b>邮箱 · 部门 · 城市 · 国家</b>。
/// 这些属于"工单内容与客户画像"，按仓库的 PII 纪律另立批次评审后交付
/// （半开比不开更危险：模型会以为"这就是全部工单信息"）。
/// </para>
/// </remarks>
[AgentTool("helpdesk.get_ticket",
    Description = "按 ticket_id 查询工单的状态与流转信息（状态/类型/渠道/评分/是否解决/创建·更新·关闭时间/标签/客户与客服的 user_id 与姓名/处理时长）。⚠️ 不返回备注正文、对话消息与客户联系方式（工单内容属敏感面，另行评审）。只读，需 helpdesk:ticket:readonly。",
    RequiredScopes = ["helpdesk:ticket:readonly"],
    Source = nameof(IFeishuTenantV1HelpDeskTicket) + "." + nameof(IFeishuTenantV1HelpDeskTicket.GetTicketAsync))]
public interface IFeishuTenantHelpdeskGetTicketTool
{
    /// <summary>查询工单状态。</summary>
    /// <returns>白名单投影后的 JSON 文本（found + ticket：状态/时间线/标签/客户与客服标识/时长），超长截断并标记 truncated。</returns>
    Task<string> GetTicketAsync(
        [ToolParameter("ticket_id", "工单 ID（必填，形如 694...；来自服务台或事件回调）", Required = true)] string ticket_id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：helpdesk.list_agent_skills（映射 <c>IFeishuTenantV1HelpDeskAgent.GetAgentSkillListAsync</c>）。
/// </summary>
/// <remarks>
/// 客服技能（技能组）是服务台的<b>路由配置</b>：回答"这类问题该转给哪个技能组"、
/// "服务台配了哪些技能"。纯配置数据，不含任何个人信息。
/// </remarks>
[AgentTool("helpdesk.list_agent_skills",
    Description = "列出服务台的客服技能（技能组）配置（skill_id/name/is_default）——回答「服务台配了哪些技能组」「这类问题该转给哪一组」。只读，需 helpdesk:agent:readonly。",
    RequiredScopes = ["helpdesk:agent:readonly"],
    Source = nameof(IFeishuTenantV1HelpDeskAgent) + "." + nameof(IFeishuTenantV1HelpDeskAgent.GetAgentSkillListAsync))]
public interface IFeishuTenantHelpdeskListAgentSkillsTool
{
    /// <summary>列出客服技能。</summary>
    /// <returns>白名单投影后的 JSON 文本（items：skill_id/name/is_default），超长截断并标记 truncated。</returns>
    Task<string> ListAgentSkillsAsync(CancellationToken cancellationToken = default);
}
