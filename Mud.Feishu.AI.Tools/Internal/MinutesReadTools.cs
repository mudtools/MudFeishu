// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

using Mud.Feishu.AI.Tools.Tools;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.Minutes;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// Minutes 只读工具执行器（R5 / F-11：<c>minutes.get</c> / <c>minutes.get_artifacts</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么客户端是可空（软依赖）</b>：<c>FeishuToolDomainRegistrars</c> 的契约是
/// "客户端缺席 → 执行器缺席 → 注册器不注册"。若把 minutes 客户端声明为<b>硬依赖</b>，
/// 宿主未启用妙记 API 时会连带让本执行器整体缺席。声明为可空后：未启用时这两个工具
/// 软缺席（执行期给可执行错误），已启用时正常在位。
/// </para>
/// <para>
/// <b>S-13 教训</b>：首版实现用的是硬依赖，结果目录里<b>静默少了这两个工具</b>，
/// 而 MUDFT022/025 均为 Error 且构建干净 —— 说明"绑定生成"与"注册器装配"是两段，
/// 后者失败会被<b>软缺席机制静默吞掉</b>。改软依赖后问题消失。
/// </para>
/// </remarks>
internal sealed class MinutesReadTools(Mud.Feishu.IFeishuTenantV1MinutesMinute? minutesClient = null)
{
    /// <summary>逐字稿窗口的默认长度（字符）。</summary>
    /// <remarks>
    /// R7/A3：逐字稿可达数万字，<b>一次性返回</b>会挤爆上下文。默认窗口 1 万字符，
    /// 模型据 <c>has_more</c>/<c>next_offset</c> 自行续读（取代原先"截断 + 布尔标记"的口径——
    /// 那个口径下模型<b>永远拿不到</b>后半段）。
    /// </remarks>
    private const int DefaultTranscriptWindow = 10000;

    /// <summary>逐字稿窗口的硬上限：再大就失去"分窗口"的意义（一次灌爆上下文）。</summary>
    private const int MaxTranscriptWindow = 50000;

    private readonly Mud.Feishu.IFeishuTenantV1MinutesMinute? _minutesClient = minutesClient;

    [FeishuToolHandler(typeof(IFeishuTenantMinutesGetTool))]
    public Task<FeishuToolResult> GetMinuteAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MinutesGet);
        return executor.RunAsync(async () =>
        {
            var args = MinutesGetArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await Require(executor.ToolName)
                .GetMinuteAsync(args.MinuteToken, cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data =>
            {
                var minute = data.Minute;
                if (minute is null)
                {
                    // 空结果哨兵（F-5）：显式"没找到"，而不是返回空对象让模型猜。
                    return new JsonObject
                    {
                        ["found"] = false,
                        ["minute_token"] = args.MinuteToken,
                        ["message"] = "未找到该妙记，请确认 minute_token 是否正确、以及当前身份是否有权限访问。",
                    };
                }

                return new JsonObject
                {
                    ["found"] = true,
                    ["minute_token"] = minute.Token ?? args.MinuteToken,
                    ["title"] = minute.Title,
                    ["url"] = minute.Url,
                    ["duration"] = minute.Duration,
                    ["create_time"] = minute.CreateTime,
                    ["owner_id"] = minute.OwnerId,
                };
            });
        });
    }

    [FeishuToolHandler(typeof(IFeishuTenantMinutesGetArtifactsTool))]
    public Task<FeishuToolResult> GetMinuteArtifactsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MinutesGetArtifacts);
        return executor.RunAsync(async () =>
        {
            var args = MinutesGetArtifactsArgs.Unpack(arguments);

            // 参数校验前置（F-8 纪律）：窗口参数越界在**本地**拒绝，
            // 不消耗一次下游调用、也不让模型等着看飞书的错误码。
            ValidateTranscriptWindow(args.TranscriptOffset, args.TranscriptLimit);

            var outcome = FeishuApiResultReader.Read(await Require(executor.ToolName)
                .GetMinuteArtifactsAsync(args.MinuteToken, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data =>
            {
                var chapters = new JsonArray();
                foreach (var chapter in data.MinuteChapters ?? [])
                {
                    // AddNode（而非 Add）：见 ToolResultText.AddNode——泛型 Add<T>(T) 的裁剪/AOT 注解会红。
                    chapters.AddNode(new JsonObject
                    {
                        ["title"] = chapter.Title,
                        ["start_ms"] = chapter.StartMs,
                        ["stop_ms"] = chapter.StopMs,
                        ["summary"] = chapter.SummaryContent,
                    });
                }

                var todos = new JsonArray();
                foreach (var todo in data.MinuteTodos ?? [])
                {
                    todos.AddNode(new JsonObject
                    {
                        // 与 body/content 同口径预截断（B-3 纪律：回填的文本一律不等长放任）。
                        ["content"] = ToolResultText.Truncate(todo.Content, PageSizes.MessagePreviewLength),
                        ["is_done"] = todo.IsDone,
                    });
                }

                // 逐字稿按窗口返回（R7/A3 改造）：模型用 transcript_offset/transcript_limit 分段读全量，
                // 而不是拿到"截断标记 + 永远读不到的后半段"。
                var window = ComputeTranscriptWindow(data.Transcript, args.TranscriptOffset, args.TranscriptLimit);

                return new JsonObject
                {
                    ["minute_token"] = args.MinuteToken,
                    ["summary"] = data.Summary,
                    ["keywords"] = new JsonArray([.. (data.Keywords ?? []).Select(static k => (JsonNode?)k)]),
                    ["chapters"] = chapters,
                    ["todos"] = todos,

                    ["transcript"] = window.Text,
                    ["transcript_total_length"] = window.TotalLength,
                    ["transcript_offset"] = window.Offset,
                    ["transcript_limit"] = window.Limit,
                    ["has_more"] = window.HasMore,

                    // 续读游标：has_more=false 时为 null（模型不必猜"下一段从哪开始"）。
                    ["next_offset"] = window.NextOffset,

                    // 模型传了超过总长的 offset：如实告知（返回空串 + 该标记），
                    // 而不是让它以为"这段逐字稿就是空的"。
                    ["offset_out_of_range"] = window.OffsetOutOfRange,
                };
            });
        });
    }

    /// <summary>
    /// 校验逐字稿窗口参数（<b>下游调用之前</b>执行：非法参数不消耗限频配额）。
    /// </summary>
    /// <param name="offset">起始偏移（可选，默认 0；负数拒绝）。</param>
    /// <param name="limit">窗口长度（可选，默认 <see cref="DefaultTranscriptWindow"/>；超出 1~<see cref="MaxTranscriptWindow"/> 拒绝）。</param>
    /// <exception cref="ArgumentException">参数越界（执行器骨架转为 <c>validation/invalid_args</c> 结构化错误）。</exception>
    internal static void ValidateTranscriptWindow(int? offset, int? limit)
    {
        if ((offset ?? 0) < 0)
        {
            throw new ArgumentException("参数 transcript_offset 不能为负数（它表示从第几个字符开始读）");
        }

        var windowLength = limit ?? DefaultTranscriptWindow;
        if (windowLength < 1 || windowLength > MaxTranscriptWindow)
        {
            throw new ArgumentException(
                $"参数 transcript_limit 必须在 1~{MaxTranscriptWindow.ToString(CultureInfo.InvariantCulture)} 之间，"
                + $"实际 {windowLength.ToString(CultureInfo.InvariantCulture)}");
        }
    }

    /// <summary>
    /// 计算逐字稿窗口（<b>纯函数</b>，便于独立单测；无 IO、无反射、AOT 友好）。
    /// </summary>
    /// <param name="transcript">完整逐字稿（可为 null ⇒ 视为空）。</param>
    /// <param name="offset">起始偏移（可选，默认 0；负数拒绝）。</param>
    /// <param name="limit">窗口长度（可选，默认 <see cref="DefaultTranscriptWindow"/>；超出 1~<see cref="MaxTranscriptWindow"/> 拒绝）。</param>
    /// <exception cref="ArgumentException">参数越界（执行器骨架会转为 <c>validation/invalid_args</c> 结构化错误）。</exception>
    internal static TranscriptWindow ComputeTranscriptWindow(string? transcript, int? offset, int? limit)
    {
        ValidateTranscriptWindow(offset, limit);

        var text = transcript ?? string.Empty;
        var start = offset ?? 0;
        var windowLength = limit ?? DefaultTranscriptWindow;

        var total = text.Length;

        // 偏移超过总长 ⇒ 空窗口（不抛错：模型可能按上一轮的 next_offset 恰好读到末尾）。
        var effectiveStart = start < total ? start : total;
        var length = Math.Min(windowLength, total - effectiveStart);
        var slice = length > 0 ? text.Substring(effectiveStart, length) : string.Empty;
        var hasMore = effectiveStart + length < total;

        return new TranscriptWindow(
            Text: slice,
            TotalLength: total,
            Offset: effectiveStart,
            Limit: windowLength,
            HasMore: hasMore,
            NextOffset: hasMore ? effectiveStart + length : null,
            OffsetOutOfRange: start > total);
    }

    /// <summary>取客户端；缺席时给出<b>可执行</b>提示（宿主该启用什么），而不是空引用。</summary>
    private Mud.Feishu.IFeishuTenantV1MinutesMinute Require(string toolName)
        => _minutesClient
            ?? throw new ArgumentException(
                $"{toolName} 需要 IFeishuTenantV1MinutesMinute——宿主须启用妙记（Minutes）API；"
                + "未启用时本工具不在工具列表中（软缺席，不影响其它域工具）");

    // ─────────────────────────── Minutes 深化（R7 / A3） ───────────────────────────

    [FeishuToolHandler(typeof(IFeishuTenantMinutesSearchTool))]
    public Task<FeishuToolResult> SearchMinutesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MinutesSearch);
        return executor.RunAsync(async () =>
        {
            var args = MinutesSearchArgs.Unpack(arguments);

            // 构造搜索请求体：SearchMinutesRequest { Query, Filter, Sorter }。
            var request = new SearchMinutesRequest
            {
                Query = args.Query,
            };

            // 过滤条件（至少提供一个：query / owner_ids / participant_ids / create_time）。
            var filter = new SearchMinutesFilter();
            var hasFilter = false;

            if (args.OwnerIds is { Length: > 0 } ownerIds)
            {
                filter.OwnerIds = [.. ownerIds];
                hasFilter = true;
            }

            if (args.ParticipantIds is { Length: > 0 } participantIds)
            {
                filter.ParticipantIds = [.. participantIds];
                hasFilter = true;
            }

            // create_time_start / create_time_end 是 ISO 8601 字符串，
            // SearchMinutesFilter.CreateTime 是 MinutesTimeRange（也是 ISO 8601 字符串）。
            if (!string.IsNullOrEmpty(args.CreateTimeStart) || !string.IsNullOrEmpty(args.CreateTimeEnd))
            {
                filter.CreateTime = new MinutesTimeRange
                {
                    StartTime = args.CreateTimeStart,
                    EndTime = args.CreateTimeEnd,
                };
                hasFilter = true;
            }

            if (hasFilter)
            {
                request.Filter = filter;
            }

            var outcome = FeishuApiResultReader.Read(await Require(executor.ToolName)
                .SearchMinutesAsync(request, page_token: args.PageToken, cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data =>
            {
                var items = new JsonArray();
                foreach (var item in data.Items ?? [])
                {
                    items.AddNode(new JsonObject
                    {
                        ["minute_token"] = item.Token,
                        ["display_info"] = ToolResultText.Truncate(item.DisplayInfo, PageSizes.MessagePreviewLength),
                        ["description"] = item.MetaData?.Description,
                        ["app_link"] = item.MetaData?.AppLink,
                    });
                }

                // R-3：信封形态单源（ToolResultJsons.PageEnvelope）。⚠️ 与旧写法的**有意差异**：
                // 旧写法无条件写出 page_token（空值时是 ""/null）——模型会当成可续游标照抄回传，
                // 平台按"从头再来"处理（重复拉第一页）；统一出口只在非空时写出。
                var envelope = ToolResultJsons.PageEnvelope(data.HasMore, data.PageToken);
                envelope["items"] = items;
                envelope["total"] = data.Total;
                envelope["notice"] = data.Notice;
                return envelope;
            });
        });
    }

    [FeishuToolHandler(typeof(IFeishuTenantMinutesGetStatisticsTool))]
    public Task<FeishuToolResult> GetMinuteStatisticsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MinutesGetStatistics);
        return executor.RunAsync(async () =>
        {
            var args = MinutesGetStatisticsArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await Require(executor.ToolName)
                .GetMinuteStatisticsAsync(args.MinuteToken, cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data =>
            {
                var stats = data.Statistics;
                if (stats is null)
                {
                    return new JsonObject
                    {
                        ["found"] = false,
                        ["minute_token"] = args.MinuteToken,
                        ["message"] = "未找到统计数据。",
                    };
                }

                var visitList = new JsonArray();
                foreach (var view in stats.UserViewList ?? [])
                {
                    visitList.AddNode(new JsonObject
                    {
                        ["user_id"] = view.UserId,
                        ["view_time"] = view.ViewTime,
                    });
                }

                return new JsonObject
                {
                    ["found"] = true,
                    ["minute_token"] = args.MinuteToken,
                    ["uv"] = stats.UserViewCount,
                    ["pv"] = stats.PageViewCount,
                    ["visit_list"] = visitList,
                };
            });
        });
    }

    [FeishuToolHandler(typeof(IFeishuTenantMinutesGetMediaTool))]
    public Task<FeishuToolResult> GetMinuteMediaAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MinutesGetMedia);
        return executor.RunAsync(async () =>
        {
            var args = MinutesGetMediaArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await Require(executor.ToolName)
                .GetMinuteMediaAsync(args.MinuteToken, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data =>
            {
                // A10 二进制防线：只返回下载 URL（文本），绝不返回字节流。
                return new JsonObject
                {
                    ["minute_token"] = args.MinuteToken,
                    ["download_url"] = data.DownloadUrl,
                    ["expire_note"] = "下载链接有效期 1 天，请及时下载。",
                };
            });
        });
    }
}

/// <summary>逐字稿窗口切片结果（<see cref="MinutesReadTools.ComputeTranscriptWindow"/> 的产物）。</summary>
/// <param name="Text">窗口内的逐字稿文本。</param>
/// <param name="TotalLength">逐字稿总长度（模型据此判断"还剩多少"）。</param>
/// <param name="Offset">实际生效的起始偏移（超过总长时收敛为总长）。</param>
/// <param name="Limit">实际生效的窗口长度。</param>
/// <param name="HasMore">是否还有后续内容（<c>next_offset</c> 非空当且仅当它为 true）。</param>
/// <param name="NextOffset">续读游标；无后续时为 null。</param>
/// <param name="OffsetOutOfRange">调用方传入的 offset 是否超过逐字稿总长（如实回填，避免"空串"被误读为"没有逐字稿"）。</param>
internal readonly record struct TranscriptWindow(
    string Text,
    int TotalLength,
    int Offset,
    int Limit,
    bool HasMore,
    int? NextOffset,
    bool OffsetOutOfRange);