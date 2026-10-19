// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── Minutes ───────────────────────────

/// <summary>
/// 工具接口：minutes.get（映射 <c>IFeishuTenantV1MinutesMinute.GetMinuteAsync</c>）。
/// </summary>
/// <remarks>
/// 取出妙记的<b>元信息</b>（标题/时长/链接/创建时间）——模型拿到
/// <c>minute_token</c> 后先确认"是不是这场会议"，再决定是否取内容。
/// </remarks>
[AgentTool("minutes.get",
    Description = "按 minute_token 获取妙记的元信息（标题、时长、链接、创建时间、所有者）。拿到 token 后先用它确认是不是目标会议，再用 minutes.get_artifacts 取总结与待办。只读，需 minutes:minutes:readonly。",
    RequiredScopes = ["minutes:minutes:readonly"],
    Source = nameof(IFeishuTenantV1MinutesMinute) + "." + nameof(IFeishuTenantV1MinutesMinute.GetMinuteAsync))]
public interface IFeishuTenantMinutesGetTool
{
    /// <summary>获取妙记元信息。</summary>
    /// <returns>白名单投影后的 JSON 文本（token/title/url/duration/create_time/owner_id）。</returns>
    Task<string> GetMinuteAsync(
        [ToolParameter("minute_token", "妙记 token（形如 obcnXxx）", Required = true)] string minute_token,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：minutes.get_artifacts（映射 <c>IFeishuTenantV1MinutesMinute.GetMinuteArtifactsAsync</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>直接命中"总结这周会议"这一高频场景</b>：一次调用即拿到
/// 总结 / 章节 / 待办 / 关键词，模型无需再自行归纳全文。
/// </para>
/// <para>
/// <b>逐字稿为什么是"分窗口"而不是"截断"</b>：<c>Transcript</c> 是完整逐字稿，
/// 长度可达数万字，原样回填会挤爆上下文；但"截断 + 布尔标记"的口径有个死结——
/// 模型<b>永远拿不到</b>后半段（没有任何参数能把它取回来）。故改为窗口化：
/// <c>transcript_offset</c> + <c>transcript_limit</c> 分段读，并回填
/// <c>transcript_total_length</c> / <c>has_more</c> / <c>next_offset</c> 作为续读依据。
/// 总结 / 章节 / 待办不受影响（它们本来就很短）。
/// </para>
/// </remarks>
[AgentTool("minutes.get_artifacts",
    Description = "按 minute_token 获取妙记的智能产物：AI 总结、章节摘要、待办事项、关键词——'总结这周会议'这类请求的首选入口（无需自行归纳全文）。逐字稿按窗口返回（transcript_offset/transcript_limit，默认 1 万字符），并给出 transcript_total_length 与 has_more/next_offset，可据此续读后续片段。只读，需 minutes:minutes:readonly。",
    RequiredScopes = ["minutes:minutes:readonly"],
    Source = nameof(IFeishuTenantV1MinutesMinute) + "." + nameof(IFeishuTenantV1MinutesMinute.GetMinuteArtifactsAsync))]
public interface IFeishuTenantMinutesGetArtifactsTool
{
    /// <summary>获取妙记智能产物。</summary>
    /// <returns>白名单投影后的 JSON 文本（summary / chapters / todos / keywords / transcript 窗口 + 续读游标）。</returns>
    Task<string> GetMinuteArtifactsAsync(
        [ToolParameter("minute_token", "妙记 token（形如 obcnXxx）", Required = true)] string minute_token,
        [ToolParameter("transcript_offset", "逐字稿分窗口偏移（可选，默认 0；配合 transcript_limit 分段读取长逐字稿，续读时取上一轮的 next_offset）")] int? transcript_offset = null,
        [ToolParameter("transcript_limit", "逐字稿分窗口上限字符数（可选，默认 10000，范围 1~50000；返回 transcript_total_length + has_more 以便续读）")] int? transcript_limit = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Minutes 深化 ───────────────────────────

/// <summary>
/// 工具接口：minutes.search（映射 <c>IFeishuTenantV1MinutesMinute.SearchMinutesAsync</c>）。
/// </summary>
/// <remarks>
/// <b>"找会议"入口</b>——没有本工具时模型只能靠 minute_token 逐个查，无从搜索。
/// 本工具按关键词 + 时间窗检索妙记列表，是"总结这周会议"链路的首环。
/// </remarks>
[AgentTool("minutes.search",
    Description = "按关键词、所有者、参与者与创建时间搜索妙记列表（分页）。query 关键词 10~50 字符；至少提供一个过滤条件。搜索时间范围最大 1 个月。只读，需 minutes:minutes:readonly。",
    RequiredScopes = ["minutes:minutes:readonly"],
    Source = nameof(IFeishuTenantV1MinutesMinute) + "." + nameof(IFeishuTenantV1MinutesMinute.SearchMinutesAsync))]
public interface IFeishuTenantMinutesSearchTool
{
    /// <summary>搜索妙记。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/total/has_more/page_token）。</returns>
    Task<string> SearchMinutesAsync(
        [ToolParameter("query", "搜索关键词（10~50 字符）", Required = true)] string query,
        [ToolParameter("owner_ids", "所有者 ID 数组（可选，open_id 格式）")] string[]? owner_ids = null,
        [ToolParameter("participant_ids", "参会人 ID 数组（可选，open_id 格式）")] string[]? participant_ids = null,
        [ToolParameter("create_time_start", "创建时间起始（可选，ISO 8601 格式如 2026-03-21T16:15:30+08:00）")] string? create_time_start = null,
        [ToolParameter("create_time_end", "创建时间结束（可选，ISO 8601 格式如 2026-03-21T17:15:30+08:00）")] string? create_time_end = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：minutes.get_statistics（映射 <c>IFeishuTenantV1MinutesMinute.GetMinuteStatisticsAsync</c>）。
/// </summary>
/// <remarks>
/// 与会人/发言时长等统计，用于"谁说了什么"类问题。
/// </remarks>
[AgentTool("minutes.get_statistics",
    Description = "按 minute_token 获取妙记的访问统计数据（PV、UV、访问用户列表与访问时间）。用于了解妙记的访问情况。只读，需 minutes:minutes:readonly。",
    RequiredScopes = ["minutes:minutes:readonly"],
    Source = nameof(IFeishuTenantV1MinutesMinute) + "." + nameof(IFeishuTenantV1MinutesMinute.GetMinuteStatisticsAsync))]
public interface IFeishuTenantMinutesGetStatisticsTool
{
    /// <summary>获取妙记统计数据。</summary>
    /// <returns>白名单投影后的 JSON 文本（uv/pv/visit_list）。</returns>
    Task<string> GetMinuteStatisticsAsync(
        [ToolParameter("minute_token", "妙记 token（形如 obcnXxx）", Required = true)] string minute_token,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：minutes.get_media（映射 <c>IFeishuTenantV1MinutesMinute.GetMinuteMediaAsync</c>）。
/// </summary>
/// <remarks>
/// <b>只返回媒体下载 URL（文本），绝不返回字节</b>（二进制防线）。
/// 工具描述限定"返回的是需宿主落盘的临时链接"。
/// </remarks>
[AgentTool("minutes.get_media",
    Description = "按 minute_token 获取妙记音视频文件的下载链接（有效期 1 天）。返回的是需宿主落盘的临时 URL，不含二进制内容。只读，需 minutes:minutes:readonly。",
    RequiredScopes = ["minutes:minutes:readonly"],
    Source = nameof(IFeishuTenantV1MinutesMinute) + "." + nameof(IFeishuTenantV1MinutesMinute.GetMinuteMediaAsync))]
public interface IFeishuTenantMinutesGetMediaTool
{
    /// <summary>获取妙记媒体下载链接。</summary>
    /// <returns>白名单投影后的 JSON 文本（media_url/expire_time）。</returns>
    Task<string> GetMinuteMediaAsync(
        [ToolParameter("minute_token", "妙记 token（形如 obcnXxx）", Required = true)] string minute_token,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── 逐字稿落盘出口 ───────────────────────────

/// <summary>
/// 工具接口：minutes.get_transcript（映射 <c>IFeishuTenantV1MinutesMinute.GetMinuteTranscriptAsync</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>与 <c>minutes.get_artifacts</c> 的分工是选型要点</b>：artifacts 里的 <c>transcript</c> 是
/// AI 产物面附带的逐字稿（按窗口回填进上下文，适合"读一段"）；本工具是平台的<b>导出文件</b>
/// （完整、可选 srt 时间戳轨），<b>落宿主磁盘</b>而不是进上下文。
/// </para>
/// <para>
/// <b>二进制防线</b>：SDK 该方法返回 <c>byte[]</c>，直接回填会击穿 SSE 帧并挤爆窗口，
/// 故走与 <c>drive.download_file</c> 同一条受控下载出口（落盘器缺席 ⇒ 整域软缺席；
/// 平台错误体不得落盘）。
/// </para>
/// </remarks>
[AgentTool("minutes.get_transcript",
    Description = "按 minute_token 导出妙记的完整逐字稿文件（txt/srt）并交宿主落盘，结果只回 local_path/size/content_type——字节不进上下文（no-bytes-in-context）。要『会议讲了什么』先用 minutes.get_artifacts（AI 总结/章节/待办，直接进上下文）；只有需要逐字原文或按时间戳定位原话才用本工具。只读，需 minutes:minutes.transcript:export。",
    RequiredScopes = ["minutes:minutes.transcript:export"],
    Source = nameof(IFeishuTenantV1MinutesMinute) + "." + nameof(IFeishuTenantV1MinutesMinute.GetMinuteTranscriptAsync))]
public interface IFeishuTenantMinutesGetTranscriptTool
{
    /// <summary>导出逐字稿文件（落盘，只回路径）。</summary>
    /// <returns>白名单投影后的 JSON 文本（local_path/size/content_type）。</returns>
    Task<string> GetMinuteTranscriptAsync(
        [ToolParameter("minute_token", "妙记 token（形如 obcnXxx）", Required = true)] string minute_token,
        [ToolParameter("file_format", "导出格式（可选，闭集：txt=纯文本 / srt=带时间轴字幕，默认 txt）")] string? file_format = null,
        [ToolParameter("need_speaker", "是否包含说话人（可选，默认由平台决定）")] bool? need_speaker = null,
        [ToolParameter("need_timestamp", "是否包含时间戳（可选，默认由平台决定；srt 格式本身已带时间轴）")] bool? need_timestamp = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── 妙记生产面（user 身份，4 个） ───────────────────────────
//
// 策展口径：剪辑/导入/订阅三件事在 SDK 里<b>只有 User 形态</b>（`IFeishuUserV1MinutesMinute`），
// 因为它们的语义主语是"某个人的妙记"——用应用身份无法回答"剪谁的妙记、导入到谁的云盘"。
// 故本组工具全部标注用户身份，宿主未放行 user 时装配期即失败（不是运行期才报错）。

/// <summary>
/// 工具接口：minutes.clip（映射 <c>IFeishuUserV1MinutesMinute.ClipMinuteAsync</c>）。
/// </summary>
/// <remarks>
/// <b>time_ranges 用"每行 <c>起始|结束</c>"的文本 DSL 而不是嵌套对象数组</b>：模型产出
/// 二维结构时最容易写出半成品 JSON；一行一段、竖线分隔可以把格式错误压在最可读的位置，
/// 并在本地校验（段数、区间长度下限、RFC3339 带时区）后再下发。
/// </remarks>
[AgentTool("minutes.clip",
    Description = "从已完成的妙记按时间段切出一段新妙记，返回新妙记链接。time_ranges 每行一段、格式「起始|结束」的 ISO 8601 带时区时间（如 2026-03-21T16:15:30+08:00|2026-03-21T16:25:00+08:00），1~50 段、每段须大于 1000 毫秒，重叠或相邻区间平台自动合并。⚠️ 异步生成：调用成功只表示已提交，转写与音视频在后台产出。用户身份工具：宿主须在 FeishuAgent:AllowedIdentities 放行 user。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 minutes:minutes.clip:write。",
    RequiredScopes = ["minutes:minutes.clip:write"],
    IsWrite = true,
    Source = nameof(IFeishuUserV1MinutesMinute) + "." + nameof(IFeishuUserV1MinutesMinute.ClipMinuteAsync))]
public interface IFeishuUserMinutesClipTool
{
    /// <summary>创建妙记剪辑。</summary>
    /// <returns>白名单投影后的 JSON 文本（minute_url/requested_range_count）。</returns>
    Task<string> ClipMinuteAsync(
        [ToolParameter("minute_token", "源妙记 token（形如 obcnXxx）", Required = true)] string minute_token,
        [ToolParameter("time_ranges", "时间段数组，每项格式「起始|结束」（ISO 8601 带时区），1~50 段，每段 >1000 毫秒", Required = true)] string[] time_ranges,
        [ToolParameter("title", "剪辑标题（可选；留空由平台自动生成）")] string? title = null,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回将要下发的 method/path 与字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：minutes.upload（映射 <c>IFeishuUserV1MinutesMinute.UploadMinuteAsync</c>）。
/// </summary>
/// <remarks>
/// 入参是<b>已在云盘的</b>文件 token，不是字节——工具面不接受上传字节（no-bytes-in-context
/// 的同一纪律：模型手里只有 token/路径，二进制由宿主侧通道进入云盘）。
/// </remarks>
[AgentTool("minutes.upload",
    Description = "把云盘上的音视频文件导入生成妙记，返回新妙记链接。file_token 是云盘文件 token（需当前用户对该文件有下载权限）。支持音频 wav/mp3/m4a/aac/ogg/wma/amr、视频 avi/wmv/mov/mp4/m4v/mpeg/ogg/flv；时长 ≤6 小时、文件 ≤6GB。本工具只接受 file_token，不搬运字节。用户身份工具：宿主须在 FeishuAgent:AllowedIdentities 放行 user。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 minutes:minutes.upload:write。",
    RequiredScopes = ["minutes:minutes.upload:write"],
    IsWrite = true,
    Source = nameof(IFeishuUserV1MinutesMinute) + "." + nameof(IFeishuUserV1MinutesMinute.UploadMinuteAsync))]
public interface IFeishuUserMinutesUploadTool
{
    /// <summary>导入云盘文件生成妙记。</summary>
    /// <returns>白名单投影后的 JSON 文本（minute_url）。</returns>
    Task<string> UploadMinuteAsync(
        [ToolParameter("file_token", "云盘音视频文件 token", Required = true)] string file_token,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回将要下发的 method/path 与字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：minutes.subscribe（映射 <c>IFeishuUserV1MinutesMinute.SubscribeMinuteAsync</c>）。
/// </summary>
/// <remarks>
/// <b>订阅是"后续事件流向"的开关</b>：订阅后该用户的妙记生成事件会推给应用，
/// 属于副作用型写操作（虽不改动妙记内容本身），故仍标 <c>IsWrite</c> 并走写白名单。
/// 平台侧的订阅集合是幂等的（重复订阅不产生第二条订阅关系）。
/// </remarks>
[AgentTool("minutes.subscribe",
    Description = "订阅妙记变更事件（当前 event_type 闭集只有 minutes.minute.generated_v1 = 妙记生成事件），订阅后该用户新产生的妙记会以事件回调推给应用。用户身份工具：宿主须在 FeishuAgent:AllowedIdentities 放行 user，且事件通道已接入（WebSocket/Webhook）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 minutes:minutes.basic:read。",
    RequiredScopes = ["minutes:minutes.basic:read"],
    IsWrite = true,
    Source = nameof(IFeishuUserV1MinutesMinute) + "." + nameof(IFeishuUserV1MinutesMinute.SubscribeMinuteAsync))]
public interface IFeishuUserMinutesSubscribeTool
{
    /// <summary>订阅妙记变更事件。</summary>
    /// <returns>白名单投影后的 JSON 文本（subscribed/event_type）。</returns>
    Task<string> SubscribeMinuteAsync(
        [ToolParameter("event_type", "事件类型（可选，闭集：minutes.minute.generated_v1，默认即该值）")] string? event_type = null,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回将要下发的 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：minutes.unsubscribe（映射 <c>IFeishuUserV1MinutesMinute.UnsubscribeMinuteAsync</c>）。
/// </summary>
/// <remarks>
/// 与 subscribe 成对出现是"写面成环"纪律：只给订阅不给退订，用户无法收回事件授权。
/// </remarks>
[AgentTool("minutes.unsubscribe",
    Description = "取消订阅妙记变更事件（与 minutes.subscribe 成对；取消后该用户的妙记生成事件不再推给应用）。用户身份工具：宿主须在 FeishuAgent:AllowedIdentities 放行 user。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 minutes:minutes.basic:read。",
    RequiredScopes = ["minutes:minutes.basic:read"],
    IsWrite = true,
    Source = nameof(IFeishuUserV1MinutesMinute) + "." + nameof(IFeishuUserV1MinutesMinute.UnsubscribeMinuteAsync))]
public interface IFeishuUserMinutesUnsubscribeTool
{
    /// <summary>取消订阅妙记变更事件。</summary>
    /// <returns>白名单投影后的 JSON 文本（unsubscribed/event_type）。</returns>
    Task<string> UnsubscribeMinuteAsync(
        [ToolParameter("event_type", "事件类型（可选，闭集：minutes.minute.generated_v1，默认即该值）")] string? event_type = null,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回将要下发的 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}
