// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── Lingo · 企业词典（B1 首个补域） ───────────────────────────

/// <summary>
/// 工具接口：lingo.list_repos（映射 <c>IFeishuTenantV1LingoRepo.GetRepoListAsync</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么它必须是第一步</b>：<c>repo_id</c> 是词典域的"作用域句柄"——后面所有
/// 分类 / 词条 / 搜索都可按 <c>repo_id</c> 收窄。模型不知道租户有哪些词库，猜一个必然失败，
/// 所以进入本域的**第一跳**就是列词库。
/// </para>
/// <para>
/// <b>为什么不暴露 <c>user_id_type</c></b>：词典条目回流里的 <c>creator</c>/<c>updater</c> 属于
/// 员工身份信息（字段权限 <c>contact:user.employee_id:readonly</c>）。本域投影**一律不带出**
/// 创建人 / 更新人，故也不需要该参数——少一个参数，少一类 PII 面（出站净化纪律）。
/// </para>
/// </remarks>
[FeishuTool("lingo.list_repos",
    Description = "列出企业词典的全部词库（返回 repo_id/name）——进入词典域的第一步：拿到 repo_id 后才能列分类、列词条或收窄搜索范围。只读，需 baike:entity:readonly。",
    RequiredScopes = ["baike:entity:readonly"],
    Source = nameof(IFeishuTenantV1LingoRepo) + "." + nameof(IFeishuTenantV1LingoRepo.GetRepoListAsync))]
public interface IFeishuTenantLingoListReposTool
{
    /// <summary>列出词库。</summary>
    /// <returns>白名单投影后的 JSON 文本（items：repo_id/name；空结果给"未配置"提示），超长截断并标记 truncated。</returns>
    Task<string> ListReposAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：lingo.list_classifications（映射 <c>IFeishuTenantV1LingoClassification.GetClassificationListAsync</c>）。
/// </summary>
/// <remarks>
/// 分类是词条的"栏目"（如"业务术语"/"技术缩写"）。它只提供**目录结构**，
/// 不返回词条本体——要词条用 <c>lingo.list_entities</c> / <c>lingo.search_entities</c>。
/// </remarks>
[FeishuTool("lingo.list_classifications",
    Description = "列出企业词典的分类（栏目）目录，可按 repo_id 收窄；返回 classification_id/name/father_id，用 father_id 可还原层级。只读，需 baike:entity:readonly。",
    RequiredScopes = ["baike:entity:readonly"],
    Source = nameof(IFeishuTenantV1LingoClassification) + "." + nameof(IFeishuTenantV1LingoClassification.GetClassificationListAsync))]
public interface IFeishuTenantLingoListClassificationsTool
{
    /// <summary>列出分类。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token：classification_id/name/father_id），超长截断并标记 truncated。</returns>
    Task<string> ListClassificationsAsync(
        [ToolParameter("repo_id", "词库 ID（可选，来自 lingo.list_repos；省略则返回全部可见词库的分类）")] string? repo_id = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：lingo.list_entities（映射 <c>IFeishuTenantV1LingoEntity.GetEntityListAsync</c>）。
/// </summary>
/// <remarks>
/// <b>与 <c>search_entities</c> 的分工</b>：本工具是**无关键字**的浏览（按词库/来源列举），
/// 适合"这个租户都有哪些术语"；要按关键词找词条用 <c>lingo.search_entities</c>（更准）。
/// </remarks>
[FeishuTool("lingo.list_entities",
    Description = "分页列出企业词典词条（返回 entity_id/main_keys/aliases/description）——用于浏览「这个租户有哪些术语」，不按关键词过滤。按关键词查词条请用 lingo.search_entities。只读，需 baike:entity:readonly。",
    RequiredScopes = ["baike:entity:readonly"],
    Source = nameof(IFeishuTenantV1LingoEntity) + "." + nameof(IFeishuTenantV1LingoEntity.GetEntityListAsync))]
public interface IFeishuTenantLingoListEntitiesTool
{
    /// <summary>列出词条。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token：entity_id/main_keys/aliases/description），超长截断并标记 truncated。</returns>
    Task<string> ListEntitiesAsync(
        [ToolParameter("repo_id", "词库 ID（可选，来自 lingo.list_repos）")] string? repo_id = null,
        [ToolParameter("provider", "词条来源（可选，字符串：省略则官方库与租户自建库都返回；取值见官方文档）")] string? provider = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：lingo.get_entity（映射 <c>IFeishuTenantV1LingoEntity.GetEntityAsync</c>）。
/// </summary>
/// <remarks>
/// 二义性处理：<c>entity_id</c> 与 <c>outer_id</c> 是**两条入口**（平台侧二选一）。
/// 只给 <c>entity_id</c> 时 <c>outer_id</c> 会被忽略；同传两者以 <c>entity_id</c> 为准。
/// </remarks>
[FeishuTool("lingo.get_entity",
    Description = "按 entity_id 查询单个词条的完整信息（主词/别名/描述/富文本/点赞统计/多语言描述）——拿到 main_keys 与 description 后可回答「这个词是什么意思」。只读，需 baike:entity:readonly。",
    RequiredScopes = ["baike:entity:readonly"],
    Source = nameof(IFeishuTenantV1LingoEntity) + "." + nameof(IFeishuTenantV1LingoEntity.GetEntityAsync))]
public interface IFeishuTenantLingoGetEntityTool
{
    /// <summary>查询单个词条。</summary>
    /// <returns>白名单投影后的 JSON 文本（found + entity：主词/别名/描述/富文本/统计/多语言），超长截断并标记 truncated。</returns>
    Task<string> GetEntityAsync(
        [ToolParameter("entity_id", "词条 ID（必填，来自 lingo.list_entities / lingo.search_entities 返回的 entity_id）", Required = true)] string entity_id,
        [ToolParameter("provider", "词条来源（可选，字符串；取值见官方文档）")] string? provider = null,
        [ToolParameter("outer_id", "外部系统 ID（可选；仅当用外部 ID 定位词条时使用，与 entity_id 二选一）")] string? outer_id = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：lingo.search_entities（映射 <c>IFeishuTenantV1LingoEntity.SearchEntityAsync</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>本域的主力工具</b>：用户问"我们内部说的 XX 是什么意思"时走它（关键字 → 词条），
/// 比 <c>list_entities</c> 精确得多。
/// </para>
/// <para>
/// <b>为什么保留 <c>creators</c> 但只当"输入过滤"</b>：它是 open_id 列表，用于"只看某人建的词条"；
/// 属于<b>入参</b>而非出站内容，故不违反出站净化纪律（出站仍不带 creator/updater 字段）。
/// </para>
/// </remarks>
[FeishuTool("lingo.search_entities",
    Description = "按关键词搜索企业词典词条（返回 entity_id/main_keys/aliases/description）——回答「我们内部说的 XX 是什么」的首选工具；可用 repo_id 收窄词库、creators 只看指定创建人建的词条。只读，需 baike:entity:readonly。",
    RequiredScopes = ["baike:entity:readonly"],
    Source = nameof(IFeishuTenantV1LingoEntity) + "." + nameof(IFeishuTenantV1LingoEntity.SearchEntityAsync))]
public interface IFeishuTenantLingoSearchEntitiesTool
{
    /// <summary>搜索词条。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token：entity_id/main_keys/aliases/description），超长截断并标记 truncated。</returns>
    Task<string> SearchEntitiesAsync(
        [ToolParameter("query", "搜索关键词（必填，如「OKR」「报销」；过长的整句可能搜不到，建议用短语）", Required = true)] string query,
        [ToolParameter("repo_id", "词库 ID（可选，来自 lingo.list_repos）")] string? repo_id = null,
        [ToolParameter("creators", "只看指定创建人建的词条（可选，open_id 数组）")] string[]? creators = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：lingo.match_entities（映射 <c>IFeishuTenantV1LingoEntity.MatchEntityAsync</c>）。
/// </summary>
/// <remarks>
/// <b>与 <c>search_entities</c> 的差别（易混点）</b>：本工具做的是"**精确词形匹配**"——
/// 给一个词（或短语），判断它是否命中某个词条，返回命中的 <c>entity_id</c> 与来源类型；
/// 它<b>不返回词条正文</b>。所以"这个词是不是我们的术语"用它，"这个词什么意思"用
/// <c>search_entities</c> + <c>get_entity</c>。
/// </remarks>
[FeishuTool("lingo.match_entities",
    Description = "把一段词/短语与企业词典做精确匹配（返回命中的 entity_id/type）——用于判断「这个词是不是我们的专有术语」，不返回词条正文；要词条含义请改用 lingo.search_entities + lingo.get_entity。只读，需 baike:entity:readonly。",
    RequiredScopes = ["baike:entity:readonly"],
    Source = nameof(IFeishuTenantV1LingoEntity) + "." + nameof(IFeishuTenantV1LingoEntity.MatchEntityAsync))]
public interface IFeishuTenantLingoMatchEntitiesTool
{
    /// <summary>精确匹配词条。</summary>
    /// <returns>白名单投影后的 JSON 文本（matches：entity_id/type；未命中给"没命中"说明），超长截断并标记 truncated。</returns>
    Task<string> MatchEntitiesAsync(
        [ToolParameter("word", "待匹配的词或短语（必填，如「季度经营会」；整句长文本命中率低）", Required = true)] string word,
        [ToolParameter("repo_id", "词库 ID（可选，来自 lingo.list_repos）")] string? repo_id = null,
        CancellationToken cancellationToken = default);
}
