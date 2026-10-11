// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── Minutes（R5 / F-11） ───────────────────────────

/// <summary>
/// 工具接口：minutes.get（映射 <c>IFeishuTenantV1MinutesMinute.GetMinuteAsync</c>）。
/// </summary>
/// <remarks>
/// R5 / F-11：取出妙记的<b>元信息</b>（标题/时长/链接/创建时间）——模型拿到
/// <c>minute_token</c> 后先确认"是不是这场会议"，再决定是否取内容。
/// </remarks>
[FeishuTool("minutes.get",
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
/// R5 / F-11 —— <b>直接命中"总结这周会议"这一高频场景</b>：一次调用即拿到
/// 总结 / 章节 / 待办 / 关键词，模型无需再自行归纳全文。
/// </para>
/// <para>
/// <b>逐字稿为什么是"分窗口"而不是"截断"</b>（R7 / A3）：<c>Transcript</c> 是完整逐字稿，
/// 长度可达数万字，原样回填会挤爆上下文；但"截断 + 布尔标记"的口径有个死结——
/// 模型<b>永远拿不到</b>后半段（没有任何参数能把它取回来）。故改为窗口化：
/// <c>transcript_offset</c> + <c>transcript_limit</c> 分段读，并回填
/// <c>transcript_total_length</c> / <c>has_more</c> / <c>next_offset</c> 作为续读依据。
/// 总结 / 章节 / 待办不受影响（它们本来就很短）。
/// </para>
/// </remarks>
[FeishuTool("minutes.get_artifacts",
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

// ─────────────────────────── Minutes 深化（R7 / A3） ───────────────────────────

/// <summary>
/// 工具接口：minutes.search（映射 <c>IFeishuTenantV1MinutesMinute.SearchMinutesAsync</c>）。
/// </summary>
/// <remarks>
/// R7 / A3：<b>"找会议"入口</b>——当前模型只能靠 minute_token 逐个查，无从搜索。
/// 本工具按关键词 + 时间窗检索妙记列表，是"总结这周会议"链路的首环。
/// </remarks>
[FeishuTool("minutes.search",
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
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：minutes.get_statistics（映射 <c>IFeishuTenantV1MinutesMinute.GetMinuteStatisticsAsync</c>）。
/// </summary>
/// <remarks>
/// R7 / A3：与会人/发言时长等统计，用于"谁说了什么"类问题。
/// </remarks>
[FeishuTool("minutes.get_statistics",
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
/// R7 / A3：<b>只返回媒体下载 URL（文本），绝不返回字节</b>（A10 二进制防线）。
/// 工具描述限定"返回的是需宿主落盘的临时链接"。
/// </remarks>
[FeishuTool("minutes.get_media",
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