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
/// <b>为什么逐字稿要截断</b>：<c>Transcript</c> 是完整逐字稿，长度可达数万字；
/// 原样回填会挤爆上下文。故按消息预览长度截断并标记 <c>transcript_truncated</c>，
/// 让模型知道"还有更多"（对齐 B-3 的截断口径纪律）。
/// </para>
/// </remarks>
[FeishuTool("minutes.get_artifacts",
    Description = "按 minute_token 获取妙记的智能产物：AI 总结、章节摘要、待办事项、关键词——'总结这周会议'这类请求的首选入口（无需自行归纳全文）。逐字稿会截断以适应上下文。只读，需 minutes:minutes:readonly。",
    RequiredScopes = ["minutes:minutes:readonly"],
    Source = nameof(IFeishuTenantV1MinutesMinute) + "." + nameof(IFeishuTenantV1MinutesMinute.GetMinuteArtifactsAsync))]
public interface IFeishuTenantMinutesGetArtifactsTool
{
    /// <summary>获取妙记智能产物。</summary>
    /// <returns>白名单投影后的 JSON 文本（summary / chapters / todos / keywords / transcript 截断）。</returns>
    Task<string> GetMinuteArtifactsAsync(
        [ToolParameter("minute_token", "妙记 token（形如 obcnXxx）", Required = true)] string minute_token,
        CancellationToken cancellationToken = default);
}