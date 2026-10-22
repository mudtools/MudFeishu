// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── Slides 读取面（7 个） ───────────────────────────

/// <summary>
/// 工具接口：slides.get_presentation（映射 <c>IFeishuTenantV1SlidesPresentation.GetXmlPresentationAsync</c>）。
/// </summary>
/// <remarks>
/// 整份 XML 可达数百 KB，故与逐字稿同口径<b>分窗口</b>返回（游标续读），而不是截断。
/// 定位单个页面请用 slides.list_slides / slides.get_slide，不要滑动本工具读完整副。
/// </remarks>
[AgentTool("slides.get_presentation",
    Description = "按 xml_presentation_id 获取 XML 演示文稿的整份 SML 2.0 内容（分窗口返回：content_offset/content_limit，默认 1 万字符，回填 content_total_length 与 has_more/next_offset 可续读）。revision_id=-1 表示最新版；remove_attr_id=true 会剥掉 XML 的 id 属性（只适合阅读，元素级替换需要 id，届时不要剥）。只读，需 slides:presentation:read。",
    RequiredScopes = ["slides:presentation:read"],
    Source = nameof(IFeishuTenantV1SlidesPresentation) + "." + nameof(IFeishuTenantV1SlidesPresentation.GetXmlPresentationAsync))]
public interface IFeishuTenantSlidesGetPresentationTool
{
    /// <summary>获取演示文稿 XML（窗口化）。</summary>
    /// <returns>白名单投影后的 JSON 文本（content 窗口 + 续读游标）。</returns>
    Task<string> GetPresentationAsync(
        [ToolParameter("xml_presentation_id", "演示文稿 token（形如 S7Yws…，来自幻灯片 URL 的 /slides/ 段）", Required = true)] string xml_presentation_id,
        [ToolParameter("revision_id", "版本号（可选，-1 或缺省=最新版）")] int? revision_id = null,
        [ToolParameter("remove_attr_id", "是否剥掉 XML 的 id 属性（可选，默认 false；剥掉后无法再做元素级替换）")] bool? remove_attr_id = null,
        [ToolParameter("content_offset", "内容分窗口偏移（可选，默认 0；续读时取上一轮的 next_offset）")] int? content_offset = null,
        [ToolParameter("content_limit", "内容分窗口上限字符数（可选，默认 10000，范围 1~50000）")] int? content_limit = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：slides.list_slides（同样映射 <c>GetXmlPresentationAsync</c>，本地解析页面清单）。
/// </summary>
/// <remarks>
/// 平台<b>没有</b>「列页面」端点，而编辑必须先拿到 slide_id——故在此把 XML 解析成清单，
/// 让模型不必把整份 XML 拉进上下文。「与 get_presentation 共用一次 SDK 调用定义」是有意的：
/// 两条口径必须来自同一份内容，否则清单里的 id 可能与实际可编辑内容不同版。
/// </remarks>
[AgentTool("slides.list_slides",
    Description = "列出 XML 演示文稿的页面清单（序号、slide_id、页数与正文文本预览）——编辑某一页前的第一步。平台无「列页面」接口，本工具在本地解析整份 XML 得到清单（不返回 XML 本身）。remove_attr_id=true 或平台剥属性时 slide_id 可能为空，此时改用 slide_number 定位。只读，需 slides:presentation:read。",
    RequiredScopes = ["slides:presentation:read"],
    Source = nameof(IFeishuTenantV1SlidesPresentation) + "." + nameof(IFeishuTenantV1SlidesPresentation.GetXmlPresentationAsync))]
public interface IFeishuTenantSlidesListSlidesTool
{
    /// <summary>列出页面清单。</summary>
    /// <returns>白名单投影后的 JSON 文本（slide_count/slides[]）。</returns>
    Task<string> ListSlidesAsync(
        [ToolParameter("xml_presentation_id", "演示文稿 token", Required = true)] string xml_presentation_id,
        [ToolParameter("revision_id", "版本号（可选，-1 或缺省=最新版）")] int? revision_id = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：slides.get_slide（映射 <c>IFeishuTenantV1SlidesSlide.GetSlideAsync</c>）。</summary>
/// <remarks>
/// 单页 XML 才是要进上下文的编辑单元；slide_id 与 slide_number <b>二选一必填</b>（本地拒绝，不下发）。
/// </remarks>
[AgentTool("slides.get_slide",
    Description = "获取演示文稿中某一页的 SML 2.0 XML（改这一页前先读它）。slide_id 与 slide_number 二选一必填（slide_number 从 1 起，page 1 = 第一页）；两者同传或都不传都会在本地被拒绝，不消耗下游。单页 XML 通常几 KB，直接进上下文；整副文稿请用 slides.get_presentation。只读，需 slides:presentation:read。",
    RequiredScopes = ["slides:presentation:read"],
    Source = nameof(IFeishuTenantV1SlidesSlide) + "." + nameof(IFeishuTenantV1SlidesSlide.GetSlideAsync))]
public interface IFeishuTenantSlidesGetSlideTool
{
    /// <summary>获取单页 XML。</summary>
    /// <returns>白名单投影后的 JSON 文本（slide_id/content/revision_id）。</returns>
    Task<string> GetSlideAsync(
        [ToolParameter("xml_presentation_id", "演示文稿 token", Required = true)] string xml_presentation_id,
        [ToolParameter("slide_id", "页面 ID（与 slide_number 二选一；取自 slides.list_slides）")] string? slide_id = null,
        [ToolParameter("slide_number", "页码（从 1 起，与 slide_id 二选一）")] int? slide_number = null,
        [ToolParameter("revision_id", "版本号（可选，-1 或缺省=最新版）")] int? revision_id = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：slides.list_histories（映射 <c>IFeishuTenantV1SlidesHistory.ListPresentationHistoriesAsync</c>）。</summary>
[AgentTool("slides.list_histories",
    Description = "分页列出 XML 演示文稿的历史版本（revision_id、编辑时间、类型、名称、编辑者与可回滚的 history_version_id）。用于「改坏了想回滚」前先看清有哪些版本。只读，需 slides:presentation:read。",
    RequiredScopes = ["slides:presentation:read"],
    Source = nameof(IFeishuTenantV1SlidesHistory) + "." + nameof(IFeishuTenantV1SlidesHistory.ListPresentationHistoriesAsync))]
public interface IFeishuTenantSlidesListHistoriesTool
{
    /// <summary>列出历史版本（分页）。</summary>
    /// <returns>分页信封 JSON 文本（entries/has_more/page_token）。</returns>
    Task<string> ListHistoriesAsync(
        [ToolParameter("xml_presentation_id", "演示文稿 token", Required = true)] string xml_presentation_id,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：slides.get_revert_status（映射 <c>IFeishuTenantV1SlidesHistory.GetHistoryRevertStatusAsync</c>）。</summary>
/// <remarks>
/// 回滚是异步任务，<c>slides.revert_history</c> 成功只代表已提交——没有本工具，模型无从知道回滚是否落地。
/// </remarks>
[AgentTool("slides.get_revert_status",
    Description = "查询历史版本回滚任务的执行状态（status：done/partial_failed/running/failed，失败时给出 failed_block_tokens）。slides.revert_history 是异步任务，必须用它确认结果；按返回的 poll_after_ms 间隔轮询，不要连续空轮。只读，需 slides:presentation:read。",
    RequiredScopes = ["slides:presentation:read"],
    Source = nameof(IFeishuTenantV1SlidesHistory) + "." + nameof(IFeishuTenantV1SlidesHistory.GetHistoryRevertStatusAsync))]
public interface IFeishuTenantSlidesGetRevertStatusTool
{
    /// <summary>获取回滚任务状态。</summary>
    /// <returns>白名单投影后的 JSON 文本（status/failed_block_tokens/history_version_id）。</returns>
    Task<string> GetRevertStatusAsync(
        [ToolParameter("xml_presentation_id", "演示文稿 token", Required = true)] string xml_presentation_id,
        [ToolParameter("task_id", "回滚任务 ID（来自 slides.revert_history）", Required = true)] string task_id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：slides.list_slide_images（映射 <c>IFeishuTenantV1SlidesSlideImage.ListSlideImagesAsync</c>）。
/// </summary>
/// <remarks>
/// <b>二进制防线</b>：平台回 Base64 图片，直接回填会击穿 SSE 帧并挤爆窗口，故走受控下载出口落盘。
/// </remarks>
[AgentTool("slides.list_slide_images",
    Description = "把演示文稿的既有页面渲染为服务端截图并交宿主落盘，结果只回 local_path/size/content_type——图片字节不进上下文（no-bytes-in-context）。选择器二选一：slide_ids（来自 slides.list_slides）或 page_start/page_end 连续页码（从 1 起），单次最多 10 页。要看「这页讲了什么」用 slides.get_slide（文本更省窗口），本工具用于交付图片。只读，需 slides:presentation:screenshot。",
    RequiredScopes = ["slides:presentation:screenshot"],
    Source = nameof(IFeishuTenantV1SlidesSlideImage) + "." + nameof(IFeishuTenantV1SlidesSlideImage.ListSlideImagesAsync))]
public interface IFeishuTenantSlidesListSlideImagesTool
{
    /// <summary>导出页面截图（落盘，只回路径）。</summary>
    /// <returns>白名单投影后的 JSON 文本（files[]/count）。</returns>
    Task<string> ListSlideImagesAsync(
        [ToolParameter("xml_presentation_id", "演示文稿 token", Required = true)] string xml_presentation_id,
        [ToolParameter("slide_ids", "页面 ID 数组（与 page_start/page_end 二选一，最多 10 个）")] string[]? slide_ids = null,
        [ToolParameter("page_start", "起始页码（从 1 起，与 slide_ids 二选一；配合 page_end 取连续页）")] int? page_start = null,
        [ToolParameter("page_end", "结束页码（含，须 ≥ page_start；缺省等于 page_start）")] int? page_end = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：slides.render_slide_image（映射 <c>IFeishuTenantV1SlidesSlideImage.RenderSlideImageAsync</c>）。
/// </summary>
/// <remarks>
/// 渲染入站内容是模型自己写的 XML，<b>不落库、不产生可回滚版本</b>——这是它与
/// <c>slides.list_slide_images</c> 的选型分界。
/// </remarks>
[AgentTool("slides.render_slide_image",
    Description = "把一段 SML 2.0 页面 XML 渲染成截图并交宿主落盘（只回 local_path/size/content_type，字节不进上下文）。不写入任何演示文稿、也不产生历史版本——适合「先看效果再决定要不要落稿」。content 是单页 <slide> 片段；要导出已有页面请用 slides.list_slide_images。只读，需 slides:presentation:screenshot。",
    RequiredScopes = ["slides:presentation:screenshot"],
    Source = nameof(IFeishuTenantV1SlidesSlideImage) + "." + nameof(IFeishuTenantV1SlidesSlideImage.RenderSlideImageAsync))]
public interface IFeishuTenantSlidesRenderSlideImageTool
{
    /// <summary>渲染页面 XML 为图片（落盘，只回路径）。</summary>
    /// <returns>白名单投影后的 JSON 文本（local_path/size/content_type）。</returns>
    Task<string> RenderSlideImageAsync(
        [ToolParameter("content", "单页 SML XML 片段（<slide …>…</slide>）", Required = true)] string content,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Slides 写入面（5 个） ───────────────────────────

/// <summary>
/// 工具接口：slides.create_presentation（映射 <c>IFeishuTenantV1SlidesPresentation.CreateXmlPresentationAsync</c>）。
/// </summary>
/// <remarks>
/// 标题不是独立字段而是 XML 的一部分——平台只有 <c>xml_presentation.content</c>，
/// 故本工具不接受 title 参数（避免「传了 title 但没进稿」的静默丢内容）。
/// </remarks>
[AgentTool("slides.create_presentation",
    Description = "以整份 SML 2.0 XML 新建一个演示文稿，返回新文稿 token、链接、版本号与页面 ID 列表。content 是 <presentation xmlns=\"http://www.larkoffice.com/sml/2.0\" width=\"960\" height=\"540\">…</presentation> 整份文档；平台不单独收标题，标题须写进首页 XML。返回的 issues 是 XML 语法告警（已建成但内容有偏差，须如实转达）。⚠️ 配图需先经云盘素材上传取得 file_token 再写进 <img src>，本工具不搬运字节。写操作：可用 dry_run=true 预演；需 slides:presentation:create 与 slides:presentation:write_only。",
    RequiredScopes = ["slides:presentation:create", "slides:presentation:write_only"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1SlidesPresentation) + "." + nameof(IFeishuTenantV1SlidesPresentation.CreateXmlPresentationAsync))]
public interface IFeishuTenantSlidesCreatePresentationTool
{
    /// <summary>创建演示文稿。</summary>
    /// <returns>白名单投影后的 JSON 文本（xml_presentation_id/url/revision_id/slide_ids/issues）。</returns>
    Task<string> CreatePresentationAsync(
        [ToolParameter("content", "整份演示文稿的 SML 2.0 XML", Required = true)] string content,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回将要下发的 method/path 与字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：slides.add_slide（映射 <c>IFeishuTenantV1SlidesSlide.CreateSlideAsync</c>）。</summary>
[AgentTool("slides.add_slide",
    Description = "在已有演示文稿中插入一页 SML XML，返回新页 slide_id 与版本号。默认追加到末尾；before_slide_id 指定插到某一页之前（留空=末尾，⚠️ 传空字符串会被判为「未知页面」而不是「末尾」）。同内容多页请逐页调用，本工具不接页面数组。返回的 issues 是 XML 语法告警，须如实转达。写操作：可用 dry_run=true 预演；需 slides:presentation:update 与 slides:presentation:write_only。",
    RequiredScopes = ["slides:presentation:update", "slides:presentation:write_only"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1SlidesSlide) + "." + nameof(IFeishuTenantV1SlidesSlide.CreateSlideAsync))]
public interface IFeishuTenantSlidesAddSlideTool
{
    /// <summary>新增页面。</summary>
    /// <returns>白名单投影后的 JSON 文本（slide_id/revision_id/issues）。</returns>
    Task<string> AddSlideAsync(
        [ToolParameter("xml_presentation_id", "演示文稿 token", Required = true)] string xml_presentation_id,
        [ToolParameter("content", "单页 SML XML 片段（<slide …>…</slide>）", Required = true)] string content,
        [ToolParameter("before_slide_id", "插入到该页面之前（可选；留省=末尾，不要传空字符串）")] string? before_slide_id = null,
        [ToolParameter("revision_id", "基于的版本号（可选，-1 或缺省=最新版）")] int? revision_id = null,
        [ToolParameter("idempotency_key", "幂等键（可选）：相同键至多插入一次；建议由调用方给出稳定值，不要用随机数")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回将要下发的 method/path 与字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：slides.replace_slide（映射 <c>IFeishuTenantV1SlidesSlide.ReplaceSlideAsync</c>）。
/// </summary>
/// <remarks>
/// <c>parts</c> 是二维结构，模型写嵌套 JSON 数组时最容易出半成品；沿用工具面既有约定
/// （bitable.record_batch_create 的 records）——<b>JSON 数组字符串</b> + 下发前本地解析。
/// 整页重建与元素级局部改**共用同一端点**（整页 = block_id 填页面 ID、replacement 给整份 slide 元素），
/// 拆成两枚工具只会让两侧口径漂移，也逼模型先判断"我该调哪一枚"。
/// </remarks>
[AgentTool("slides.replace_slide",
    Description = "替换演示文稿页面的内容（parts 为 JSON 数组字符串，1~200 项，每项 action 取 block_replace 或 block_insert）。整页重建：block_replace + block_id 填页面 ID 本身、replacement 给整份 <slide> XML（⚠️ 新 XML 里没写的内容会从页面上消失）。元素级局部改：block_id 填页内 3 位 short element ID（须先用 slides.get_slide 读到，remove_attr_id=true 的 XML 没有 id 就无法替换），形如 {\"action\":\"block_replace\",\"block_id\":\"bab\",\"replacement\":\"<shape …>…</shape>\"}；插入用 {\"action\":\"block_insert\",\"insertion\":\"<shape …/>\",\"insert_before_block_id\":\"baa\"}，insert_before_block_id 留省=追加到末尾。失败回 failed_part_index 与 failed_reason，须如实转达。写操作：可用 dry_run=true 预演；需 slides:presentation:update 与 slides:presentation:write_only。",
    RequiredScopes = ["slides:presentation:update", "slides:presentation:write_only"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1SlidesSlide) + "." + nameof(IFeishuTenantV1SlidesSlide.ReplaceSlideAsync))]
public interface IFeishuTenantSlidesReplaceSlideTool
{
    /// <summary>替换页面内容（整页或元素级）。</summary>
    /// <returns>白名单投影后的 JSON 文本（revision_id/failed_part_index/failed_reason）。</returns>
    Task<string> ReplaceSlideAsync(
        [ToolParameter("xml_presentation_id", "演示文稿 token", Required = true)] string xml_presentation_id,
        [ToolParameter("slide_id", "目标页面 ID", Required = true)] string slide_id,
        [ToolParameter("parts", "替换动作数组的 JSON 字符串（1~200 项），每项形如 {\"action\":\"block_replace\",\"block_id\":\"bab\",\"replacement\":\"<shape/>\"} 或 {\"action\":\"block_insert\",\"insertion\":\"<shape/>\",\"insert_before_block_id\":\"baa\"}", Required = true)] string parts,
        [ToolParameter("comment", "本次修改的说明（可选，随历史版本记录）")] string? comment = null,
        [ToolParameter("revision_id", "基于的版本号（可选，-1 或缺省=最新版）")] int? revision_id = null,
        [ToolParameter("idempotency_key", "幂等键（可选）：相同键至多生效一次；建议由调用方给出稳定值，不要用随机数")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回将要下发的 method/path 与字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：slides.delete_slide（映射 <c>IFeishuTenantV1SlidesSlide.DeleteSlideAsync</c>）。
/// </summary>
/// <remarks>
/// 页面删除不可逆（平台无回收站接口），故属<b>高危写</b>；删前必须先用 slides.list_slides
/// 确认目标页，不得凭页码直接删。
/// </remarks>
[AgentTool("slides.delete_slide",
    Description = "删除演示文稿中的指定页面（⚠️ 不可恢复：平台不提供恢复被删页面的接口，误删只能靠 slides.revert_history 回到历史版本）。slide_id 必填且必须来自 slides.list_slides 的实际结果，不要凭页码猜测。写操作（高危）：可用 dry_run=true 预演；需 slides:presentation:update 与 slides:presentation:write_only。",
    RequiredScopes = ["slides:presentation:update", "slides:presentation:write_only"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1SlidesSlide) + "." + nameof(IFeishuTenantV1SlidesSlide.DeleteSlideAsync))]
public interface IFeishuTenantSlidesDeleteSlideTool
{
    /// <summary>删除页面。</summary>
    /// <returns>白名单投影后的 JSON 文本（revision_id）。</returns>
    Task<string> DeleteSlideAsync(
        [ToolParameter("xml_presentation_id", "演示文稿 token", Required = true)] string xml_presentation_id,
        [ToolParameter("slide_id", "要删除的页面 ID（必填，取自 slides.list_slides）", Required = true)] string slide_id,
        [ToolParameter("revision_id", "基于的版本号（可选，-1 或缺省=最新版）")] int? revision_id = null,
        [ToolParameter("idempotency_key", "幂等键（可选）：相同键至多删除一次；建议由调用方给出稳定值，不要用随机数")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回将要下发的 method/path 与字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：slides.revert_history（映射 <c>IFeishuTenantV1SlidesHistory.RevertPresentationHistoryAsync</c>）。
/// </summary>
/// <remarks>
/// 异步任务且<b>整副回滚</b>（会把用户后续编辑一起抹掉），故必须配 slides.get_revert_status 读结果。
/// </remarks>
[AgentTool("slides.revert_history",
    Description = "把整副演示文稿回滚到指定历史版本（⚠️ 异步任务 + 整副覆盖：回滚点之后的所有编辑都会消失，包括其他人刚改的内容）。history_version_id 必须来自 slides.list_histories（正整数字符串，不是 revision_id）。调用成功只代表任务已提交，须用 slides.get_revert_status 按 poll_after_ms 轮询确认。写操作：可用 dry_run=true 预演；需 slides:presentation:update 与 slides:presentation:write_only。",
    RequiredScopes = ["slides:presentation:update", "slides:presentation:write_only"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1SlidesHistory) + "." + nameof(IFeishuTenantV1SlidesHistory.RevertPresentationHistoryAsync))]
public interface IFeishuTenantSlidesRevertHistoryTool
{
    /// <summary>回滚到历史版本（异步任务）。</summary>
    /// <returns>白名单投影后的 JSON 文本（task_id/status/poll_after_ms/history_version_id）。</returns>
    Task<string> RevertHistoryAsync(
        [ToolParameter("xml_presentation_id", "演示文稿 token", Required = true)] string xml_presentation_id,
        [ToolParameter("history_version_id", "历史版本 ID（正整数字符串，来自 slides.list_histories）", Required = true)] string history_version_id,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回将要下发的 method/path 与字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}
