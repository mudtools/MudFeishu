// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── Lingo · 企业词典 ───────────────────────────

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
[AgentTool("lingo.list_repos",
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
[AgentTool("lingo.list_classifications",
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
[AgentTool("lingo.list_entities",
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
[AgentTool("lingo.get_entity",
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
[AgentTool("lingo.search_entities",
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
[AgentTool("lingo.match_entities",
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

// ─────────────────── 写面（免审词条 CRUD + 草稿申请） ───────────────────
//
// 与 ChatGroup 写面的**关键差别**：本域 SDK XML 注释里 **scope 字面量齐全**
//   · baike:entity:exempt_review —— 创建 / 更新免审词条
//   · baike:entity:exempt_delete —— 免审删除词条
//   · baike:entity（或 exempt_review，任一即可）—— 草稿创建 / 更新
// 故权限对照表本域区段**不标 ⚠️**（TrustParty 同款口径）。
//
// ⚠️ **刻意不做**的两处（理由写进 Guidance，不做静默缺口）：
//    · lingo.upload_file：入参是**本地绝对文件路径**（[FilePath]），模型侧无法可靠构造
//      （与 vc 域"有意不策展 apply_reserve"同款纪律）；
//    · 请求体的嵌套字段 related_meta（分类/图片/关联）/ outer_info（外部系统关联）/
//      i18n_descs（国际化释义）：扁平参数无法无损表达，需先引入结构化入参形态。

/// <summary>
/// 工具接口：lingo.create_entity（映射 <c>IFeishuTenantV1LingoEntity.CreateEntityAsync</c>）。
/// </summary>
/// <remarks>
/// <b>免审直写</b>：经此接口创建的词条**不经过词典管理员审核**，直接写入词库——
/// SDK 注释原文"调用时应当慎重操作"。若希望走审核流程，请用 <c>lingo.create_draft</c>。
/// </remarks>
[AgentTool("lingo.create_entity",
    Description = "创建词典词条（⚠️ 免审直写：不经过词典管理员审核，直接写入词库——要审核流程请改用 lingo.create_draft）。写操作：默认不启用，需宿主授权（IToolExecutionAuthorizer），需 baike:entity:exempt_review。建议先 dry_run 预演。",
    RequiredScopes = ["baike:entity:exempt_review"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1LingoEntity) + "." + nameof(IFeishuTenantV1LingoEntity.CreateEntityAsync))]
public interface IFeishuTenantLingoCreateEntityTool
{
    /// <summary>创建词条。</summary>
    /// <returns>白名单投影后的 JSON 回执（entity_id + main_key + created），超长截断并标记 truncated。</returns>
    Task<string> CreateEntityAsync(
        [ToolParameter("main_key", "词条名（必填，最多 1 个）", Required = true)] string main_key,
        [ToolParameter("repo_id", "词库 ID（可选，来自 lingo.list_repos；不传则写入默认词库）")] string? repo_id = null,
        [ToolParameter("aliases", "别名列表（可选，最多 10 个）")] string[]? aliases = null,
        [ToolParameter("description", "纯文本释义（可选，1~5000 字符；与 rich_text 至少填一个）")] string? description = null,
        [ToolParameter("rich_text", "富文本释义（可选，≤5000 字符；填了可覆盖 description）")] string? rich_text = null,
        [ToolParameter("allow_highlight", "是否允许智能高亮（可选，默认 true）")] bool? allow_highlight = null,
        [ToolParameter("allow_search", "是否允许被搜索命中（可选，默认 true）")] bool? allow_search = null,
        [ToolParameter("user_id_type", "user_id 类型（可选：open_id / user_id / union_id）")] string? user_id_type = null,
        [ToolParameter("dry_run", "仅预演不下发（可选，默认 false）：返回将要创建的词条名与别名数，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：lingo.update_entity（映射 <c>UpdateEntityAsync</c>）。
/// </summary>
/// <remarks>
/// <b>覆盖式语义</b>（与互通规则同款）：请求体整份提交，<b>未传的释义/别名会被清空</b>。
/// <b><c>main_key</c> 必填</b>（平台请求体口径：<c>main_keys</c> 是必填项）——只想改释义时
/// 也要把原词条名一起带上：先 <c>lingo.get_entity</c> 读出原值、合并后再整份提交。
/// </remarks>
[AgentTool("lingo.update_entity",
    Description = "更新词典词条（⚠️ 免审直写，且为**覆盖式**：未传的释义/别名会被清空——只改一项请先 lingo.get_entity 读出原值再整份提交；main_key 必填，与平台口径一致）。写操作：默认不启用，需宿主授权，需 baike:entity:exempt_review。建议先 dry_run 预演。",
    RequiredScopes = ["baike:entity:exempt_review"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1LingoEntity) + "." + nameof(IFeishuTenantV1LingoEntity.UpdateEntityAsync))]
public interface IFeishuTenantLingoUpdateEntityTool
{
    /// <summary>更新词条。</summary>
    /// <returns>白名单投影后的 JSON 回执（entity_id + updated_fields + updated），超长截断并标记 truncated。</returns>
    Task<string> UpdateEntityAsync(
        [ToolParameter("entity_id", "词条 ID（必填，来自 lingo.search_entities / lingo.get_entity）", Required = true)] string entity_id,
        [ToolParameter("main_key", "词条名（必填，与平台口径一致——只想改释义也要带上原词条名）", Required = true)] string main_key,
        [ToolParameter("aliases", "新别名列表（可选；覆盖式，不传则清空原有别名）")] string[]? aliases = null,
        [ToolParameter("description", "新纯文本释义（可选，1~5000 字符；与 rich_text 至少填一个）")] string? description = null,
        [ToolParameter("rich_text", "新富文本释义（可选，≤5000 字符）")] string? rich_text = null,
        [ToolParameter("allow_highlight", "是否允许智能高亮（可选）")] bool? allow_highlight = null,
        [ToolParameter("allow_search", "是否允许被搜索命中（可选）")] bool? allow_search = null,
        [ToolParameter("user_id_type", "user_id 类型（可选：open_id / user_id / union_id）")] string? user_id_type = null,
        [ToolParameter("dry_run", "仅预演不下发（可选，默认 false）：返回 entity_id 与将要修改的字段数，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：lingo.delete_entity（映射 <c>DeleteEntityAsync</c>）。
/// </summary>
/// <remarks>
/// <b>high-risk-write</b>（命中危险词 <c>delete</c>）：删除后词条**直接从词库移除**，
/// 外部系统里引用该 <c>entity_id</c> 的地方会全部失效，且平台不提供"恢复已删词条"入口。
/// </remarks>
[AgentTool("lingo.delete_entity",
    Description = "删除词典词条（⚠️ 不可逆：词条直接从词库移除，引用该 entity_id 的外部系统会全部失效——务必先 dry_run 预演并确认 entity_id 无误）。写操作：默认不启用，需宿主授权，需 baike:entity:exempt_delete。",
    RequiredScopes = ["baike:entity:exempt_delete"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1LingoEntity) + "." + nameof(IFeishuTenantV1LingoEntity.DeleteEntityAsync))]
public interface IFeishuTenantLingoDeleteEntityTool
{
    /// <summary>删除词条。</summary>
    /// <returns>白名单投影后的 JSON 回执（entity_id + deleted），超长截断并标记 truncated。</returns>
    Task<string> DeleteEntityAsync(
        [ToolParameter("entity_id", "要删除的词条 ID（必填，来自 lingo.search_entities / lingo.get_entity）", Required = true)] string entity_id,
        [ToolParameter("dry_run", "仅预演不下发（可选，默认 false）：返回将要删除的词条 ID，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：lingo.create_draft（映射 <c>IFeishuTenantV1LingoDraft.CreateDraftAsync</c>）。
/// </summary>
/// <remarks>
/// 与 <c>create_entity</c> 的核心差别：草稿**需经词典管理员审核通过后**才写入词库。
/// 这是"没有免审权限"或"改动需要留痕"时的正确路径。
/// </remarks>
[AgentTool("lingo.create_draft",
    Description = "发起创建词条的**草稿申请**（需经词典管理员审核通过后才写入词库）——没有免审权限或需要留痕时的正确路径。写操作：默认不启用，需宿主授权，需 baike:entity 或 baike:entity:exempt_review（任一即可）。",
    RequiredScopes = ["baike:entity", "baike:entity:exempt_review"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1LingoDraft) + "." + nameof(IFeishuTenantV1LingoDraft.CreateDraftAsync))]
public interface IFeishuTenantLingoCreateDraftTool
{
    /// <summary>发起创建草稿。</summary>
    /// <returns>白名单投影后的 JSON 回执（draft_id + entity_id + submitted），超长截断并标记 truncated。</returns>
    Task<string> CreateDraftAsync(
        [ToolParameter("main_key", "词条名（必填，最多 1 个）", Required = true)] string main_key,
        [ToolParameter("repo_id", "词库 ID（可选，来自 lingo.list_repos）")] string? repo_id = null,
        [ToolParameter("aliases", "别名列表（可选，最多 10 个）")] string[]? aliases = null,
        [ToolParameter("description", "纯文本释义（可选，1~5000 字符；与 rich_text 至少填一个）")] string? description = null,
        [ToolParameter("rich_text", "富文本释义（可选，≤5000 字符）")] string? rich_text = null,
        [ToolParameter("allow_highlight", "是否允许智能高亮（可选，默认 true）")] bool? allow_highlight = null,
        [ToolParameter("allow_search", "是否允许被搜索命中（可选，默认 true）")] bool? allow_search = null,
        [ToolParameter("user_id_type", "user_id 类型（可选：open_id / user_id / union_id）")] string? user_id_type = null,
        [ToolParameter("dry_run", "仅预演不下发（可选，默认 false）：返回将要提交的词条名与别名数，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：lingo.update_draft（映射 <c>UpdateDraftAsync</c>）。
/// </summary>
/// <remarks>
/// <b>已审批的草稿无法编辑</b>（SDK 注释原文）；同样按<b>覆盖式</b>语义提交，
/// 且 <c>main_key</c> 必填（平台请求体口径）。
/// </remarks>
[AgentTool("lingo.update_draft",
    Description = "更新词条草稿（⚠️ 已审批的草稿无法编辑；且为覆盖式提交，未传的释义/别名会被清空；main_key 必填）。写操作：默认不启用，需宿主授权，需 baike:entity 或 baike:entity:exempt_review（任一即可）。",
    RequiredScopes = ["baike:entity", "baike:entity:exempt_review"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1LingoDraft) + "." + nameof(IFeishuTenantV1LingoDraft.UpdateDraftAsync))]
public interface IFeishuTenantLingoUpdateDraftTool
{
    /// <summary>更新草稿。</summary>
    /// <returns>白名单投影后的 JSON 回执（draft_id + entity_id + updated），超长截断并标记 truncated。</returns>
    Task<string> UpdateDraftAsync(
        [ToolParameter("draft_id", "草稿 ID（必填）", Required = true)] string draft_id,
        [ToolParameter("main_key", "词条名（必填，与平台口径一致）", Required = true)] string main_key,
        [ToolParameter("aliases", "新别名列表（可选；覆盖式，不传则清空原有别名）")] string[]? aliases = null,
        [ToolParameter("description", "新纯文本释义（可选，1~5000 字符；与 rich_text 至少填一个）")] string? description = null,
        [ToolParameter("rich_text", "新富文本释义（可选，≤5000 字符）")] string? rich_text = null,
        [ToolParameter("allow_highlight", "是否允许智能高亮（可选）")] bool? allow_highlight = null,
        [ToolParameter("allow_search", "是否允许被搜索命中（可选）")] bool? allow_search = null,
        [ToolParameter("user_id_type", "user_id 类型（可选：open_id / user_id / union_id）")] string? user_id_type = null,
        [ToolParameter("dry_run", "仅预演不下发（可选，默认 false）：返回 draft_id 与将要修改的字段数，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}
