// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.Minutes;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// Minutes 只读工具执行器（<c>minutes.get</c> / <c>minutes.get_artifacts</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么客户端是可空（软依赖）</b>：<c>FeishuToolDomainRegistrars</c> 的契约是
/// "客户端缺席 → 执行器缺席 → 注册器不注册"。若把 minutes 客户端声明为<b>硬依赖</b>，
/// 宿主未启用妙记 API 时会连带让本执行器整体缺席。声明为可空后：未启用时这两个工具
/// 软缺席（执行期给可执行错误），已启用时正常在位。
/// </para>
/// <para>
/// <b>教训</b>：首版实现用的是硬依赖，结果目录里<b>静默少了这两个工具</b>，
/// 而 MUDFT022/025 均为 Error 且构建干净 —— 说明"绑定生成"与"注册器装配"是两段，
/// 后者失败会被<b>软缺席机制静默吞掉</b>。改软依赖后问题消失。
/// </para>
/// </remarks>
internal sealed class MinutesReadTools(
    Mud.Feishu.IFeishuTenantV1MinutesMinute? minutesClient = null,

    // P-4（统一分页语义）：取分页预算需要宿主配置。**带默认值**是刻意的——
    // 既有构造点（测试直接 new、以及生成器按"声明顺序"传参）不会被破坏；
    // 生产路径由生成器注入非空实例，故 Require 的前置断言仍然成立。
    IOptions<FeishuAgentOptions> options = null!)
{
    private readonly Mud.Feishu.IFeishuTenantV1MinutesMinute? _minutesClient = minutesClient;

    /// <summary>P-4（统一分页语义）：<c>fetch_all</c> 的条目/页数预算（来自宿主配置）。</summary>
    private readonly int _maxAutoFetchItems = ToolExecutor.Require(options).MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = ToolExecutor.Require(options).MaxAutoFetchPages;

    [ToolHandler(typeof(IFeishuTenantMinutesGetTool))]
    public Task<ToolResult> GetMinuteAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
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

    [ToolHandler(typeof(IFeishuTenantMinutesGetArtifactsTool))]
    public Task<ToolResult> GetMinuteArtifactsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MinutesGetArtifacts);
        return executor.RunAsync(async () =>
        {
            var args = MinutesGetArtifactsArgs.Unpack(arguments);

            // 参数校验前置（F-8 纪律）：窗口参数越界在**本地**拒绝，
            // 不消耗一次下游调用、也不让模型等着看飞书的错误码。
            ToolContentWindow.Validate(args.TranscriptOffset, args.TranscriptLimit, "transcript_offset", "transcript_limit");

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
                var window = ToolContentWindow.Compute(
                    data.Transcript, args.TranscriptOffset, args.TranscriptLimit, "transcript_offset", "transcript_limit");

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

    /// <summary>取客户端；缺席时给出<b>可执行</b>提示（宿主该启用什么），而不是空引用。</summary>
    private Mud.Feishu.IFeishuTenantV1MinutesMinute Require(string toolName)
        => _minutesClient
            ?? throw new ArgumentException(
                $"{toolName} 需要 IFeishuTenantV1MinutesMinute——宿主须启用妙记（Minutes）API；"
                + "未启用时本工具不在工具列表中（软缺席，不影响其它域工具）");

    // ─────────────────────────── Minutes 深化（R7 / A3） ───────────────────────────

    [ToolHandler(typeof(IFeishuTenantMinutesSearchTool))]
    public Task<ToolResult> SearchMinutesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
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

            // P-4：统一分页出口——聚合路径与单页路径共用**同一次调用定义**与**同一份投影**。
            // SearchMinutesResult 是"自带 has_more/page_token 但不继承 ApiPageListResult"的历史 DTO，
            // 故走显式页状态重载（读它自己声明的字段）。
            async Task<ToolApiOutcome<SearchMinutesResult>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await Require(executor.ToolName)
                    .SearchMinutesAsync(request, page_token: token, cancellationToken: ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync,
                ProjectSearchPage,
                static data => (data.HasMore == true, data.PageToken),
                args.FetchAll,
                args.MaxItems,
                args.PageToken,
                _maxAutoFetchItems,
                _maxAutoFetchPages,
                cancellationToken,
                // 单页语义保持"不截断"（与改造前的 FromApiUntruncated 逐字节等价）。
                untruncated: true).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// <c>minutes.search</c> 的单页投影（信封形态单源 <c>ToolResultJsons.PageEnvelope</c>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// P-4：统一分页出口要求"聚合路径与单页路径<b>共用同一份投影</b>"，故把它从内联 lambda
    /// 提为静态方法——<b>内容与改造前逐字节一致</b>（含 <c>total</c>/<c>notice</c> 两个本域标量）。
    /// </para>
    /// <para>
    /// ⚠️ 与更早写法的**有意差异**（已在本次改造之前落地）：旧写法无条件写出 <c>page_token</c>
    /// （空值时为 ""/null），模型会把它当成可续游标照抄回传，平台按"从头再来"处理（重复拉第一页）；
    /// 统一信封只在非空时写出。
    /// </para>
    /// </remarks>
    private static JsonObject ProjectSearchPage(SearchMinutesResult data)
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

        var envelope = ToolResultJsons.PageEnvelope(data.HasMore, data.PageToken);
        envelope["items"] = items;
        envelope["total"] = data.Total;
        envelope["notice"] = data.Notice;
        return envelope;
    }

    [ToolHandler(typeof(IFeishuTenantMinutesGetStatisticsTool))]
    public Task<ToolResult> GetMinuteStatisticsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
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

    [ToolHandler(typeof(IFeishuTenantMinutesGetMediaTool))]
    public Task<ToolResult> GetMinuteMediaAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
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
