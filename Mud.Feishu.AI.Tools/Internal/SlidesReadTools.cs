// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Xml;
using System.Xml.Linq;

using Mud.Feishu.DataModels.Slides;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// 幻灯片只读执行器（<c>slides.get_presentation</c> / <c>slides.list_slides</c> /
/// <c>slides.get_slide</c> / <c>slides.list_histories</c> / <c>slides.get_revert_status</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>客户端可空（软缺席）</b>：宿主未启用幻灯片 API 时本执行器仍须能构造，由执行期给出可执行错误
/// ——硬依赖会让整域工具静默从注册表消失（<c>MinutesReadTools</c> 踩过的坑）。
/// </para>
/// <para>
/// <b>整副 XML 分窗口、单页 XML 不截断</b>：演示文稿正文可达数百 KB（窗口化 + 续读游标），
/// 而"改一页"的输入必须是**完整且未被截断**的页 XML——截断过的片段回填给平台会静默丢内容。
/// </para>
/// </remarks>
internal sealed class SlidesReadTools(
    Mud.Feishu.IFeishuTenantV1SlidesPresentation? presentationClient = null,
    Mud.Feishu.IFeishuTenantV1SlidesSlide? slideClient = null,
    Mud.Feishu.IFeishuTenantV1SlidesHistory? historyClient = null,

    // 带默认值是刻意的：既有构造点（测试直接 new）不会被破坏；生产路径由生成器注入非空实例。
    IOptions<FeishuAgentOptions> options = null!)
{
    /// <summary>页面清单里正文预览的截断长度（只为"认出这一页"，不是读正文的通道）。</summary>
    private const int SlidePreviewLength = 120;

    /// <summary>历史版本单页条数（平台上限；页大小属运维参数，不对模型开放——DP-B1-0）。</summary>
    private const int PageSize = 20;

    private readonly Mud.Feishu.IFeishuTenantV1SlidesPresentation? _presentationClient = presentationClient;
    private readonly Mud.Feishu.IFeishuTenantV1SlidesSlide? _slideClient = slideClient;
    private readonly Mud.Feishu.IFeishuTenantV1SlidesHistory? _historyClient = historyClient;

    private readonly int _maxAutoFetchItems = ToolExecutor.Require(options).MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = ToolExecutor.Require(options).MaxAutoFetchPages;

    /// <summary>slides.get_presentation：整副 SML XML（分窗口返回）。</summary>
    [ToolHandler(typeof(IFeishuTenantSlidesGetPresentationTool))]
    public Task<ToolResult> GetPresentationAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SlidesGetPresentation);
        return executor.RunAsync(async () =>
        {
            var args = SlidesGetPresentationArgs.Unpack(arguments);

            // 窗口校验前置：越界在本地拒绝，不消耗一次下游调用。
            ToolContentWindow.Validate(args.ContentOffset, args.ContentLimit, "content_offset", "content_limit");

            var outcome = FeishuApiResultReader.Read(await Require(
                    executor.ToolName, _presentationClient, "IFeishuTenantV1SlidesPresentation")
                .GetXmlPresentationAsync(args.XmlPresentationId, args.RevisionId, args.RemoveAttrId, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data =>
            {
                var presentation = data.XmlPresentation;
                if (string.IsNullOrEmpty(presentation?.Content))
                {
                    return NotFound(args.XmlPresentationId, "该演示文稿没有可读内容，请确认 token 是否正确、当前身份是否有权限。");
                }

                var window = ToolContentWindow.Compute(
                    presentation!.Content, args.ContentOffset, args.ContentLimit, "content_offset", "content_limit");

                // 载荷键用 xml 而非 content：这是**回填编辑的原文**，不是消息正文预览——
                // 截断会静默丢内容（窗口已按 content_offset/content_limit 限定上限）。
                return new JsonObject
                {
                    ["found"] = true,
                    ["xml_presentation_id"] = presentation.PresentationId ?? args.XmlPresentationId,
                    ["revision_id"] = presentation.RevisionId,
                    ["remove_attr_id"] = args.RemoveAttrId == true,
                    ["xml"] = window.Text,
                    ["xml_total_length"] = window.TotalLength,
                    ["xml_offset"] = window.Offset,
                    ["xml_limit"] = window.Limit,
                    ["has_more"] = window.HasMore,
                    ["next_offset"] = window.NextOffset,
                    ["offset_out_of_range"] = window.OffsetOutOfRange,
                };
            });
        });
    }

    /// <summary>slides.list_slides：本地解析整副 XML 得到页面清单（平台无"列页面"端点）。</summary>
    [ToolHandler(typeof(IFeishuTenantSlidesListSlidesTool))]
    public Task<ToolResult> ListSlidesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SlidesListSlides);
        return executor.RunAsync(async () =>
        {
            var args = SlidesListSlidesArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await Require(
                    executor.ToolName, _presentationClient, "IFeishuTenantV1SlidesPresentation")
                .GetXmlPresentationAsync(args.XmlPresentationId, args.RevisionId, remove_attr_id: false, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data => ProjectInventory(data.XmlPresentation, args.XmlPresentationId));
        });
    }

    /// <summary>slides.get_slide：单页 XML（不截断——回填编辑需要完整页）。</summary>
    [ToolHandler(typeof(IFeishuTenantSlidesGetSlideTool))]
    public Task<ToolResult> GetSlideAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SlidesGetSlide);
        return executor.RunAsync(async () =>
        {
            var args = SlidesGetSlideArgs.Unpack(arguments);

            var hasSlideId = !string.IsNullOrWhiteSpace(args.SlideId);
            var hasSlideNumber = args.SlideNumber is > 0;
            if (hasSlideId == hasSlideNumber)
            {
                throw new ArgumentException("slide_id 与 slide_number 须二选一（slide_number 从 1 起；同传或都不传都无法定位页面）");
            }

            var outcome = FeishuApiResultReader.Read(await Require(
                    executor.ToolName, _slideClient, "IFeishuTenantV1SlidesSlide")
                .GetSlideAsync(
                    args.XmlPresentationId,
                    hasSlideId ? args.SlideId!.Trim() : null,
                    args.RevisionId,
                    hasSlideNumber ? args.SlideNumber : null,
                    cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data =>
            {
                var slide = data.Slide;
                if (string.IsNullOrEmpty(slide?.Content))
                {
                    return NotFound(args.XmlPresentationId, "未找到该页面，请先用 slides.list_slides 取实际 slide_id 或页码。");
                }

                return new JsonObject
                {
                    ["found"] = true,
                    ["xml_presentation_id"] = args.XmlPresentationId,
                    ["slide_id"] = slide!.SlideId,
                    ["revision_id"] = data.RevisionId,
                    ["xml"] = slide.Content,
                    ["xml_length"] = slide.Content.Length,
                };
            });
        });
    }

    /// <summary>slides.list_histories：历史版本分页（聚合与单页共用同一份投影）。</summary>
    [ToolHandler(typeof(IFeishuTenantSlidesListHistoriesTool))]
    public Task<ToolResult> ListHistoriesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SlidesListHistories);
        return executor.RunAsync(async () =>
        {
            var args = SlidesListHistoriesArgs.Unpack(arguments);

            async Task<ToolApiOutcome<ListPresentationHistoriesResult>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await Require(
                        executor.ToolName, _historyClient, "IFeishuTenantV1SlidesHistory")
                    .ListPresentationHistoriesAsync(args.XmlPresentationId, PageSize, token, ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync,
                ProjectHistoryPage,
                static data => (data.HasMore, data.PageToken),
                args.FetchAll,
                args.MaxItems,
                args.PageToken,
                _maxAutoFetchItems,
                _maxAutoFetchPages,
                cancellationToken,
                untruncated: true).ConfigureAwait(false);
        });
    }

    /// <summary>slides.get_revert_status：回滚任务状态（回滚是异步的，成功提交不等于已落地）。</summary>
    [ToolHandler(typeof(IFeishuTenantSlidesGetRevertStatusTool))]
    public Task<ToolResult> GetRevertStatusAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SlidesGetRevertStatus);
        return executor.RunAsync(async () =>
        {
            var args = SlidesGetRevertStatusArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await Require(
                    executor.ToolName, _historyClient, "IFeishuTenantV1SlidesHistory")
                .GetHistoryRevertStatusAsync(args.XmlPresentationId, args.TaskId, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["xml_presentation_id"] = args.XmlPresentationId,
                ["task_id"] = args.TaskId,
                ["status"] = data.Status,
                ["history_version_id"] = data.HistoryVersionId,
                ["failed_block_tokens"] = new JsonArray([.. (data.FailedBlockTokens ?? []).Select(static token => (JsonNode?)token)]),
                ["note"] = data.Status switch
                {
                    "running" => "回滚仍在进行，请按 slides.revert_history 返回的 poll_after_ms 稍后重试。",
                    "partial_failed" => "部分块回滚失败（见 failed_block_tokens），文稿已不再是目标版本。",
                    _ => (string?)null,
                },
            });
        });
    }

    // ────────── 本地投影 ──────────

    /// <summary>
    /// 把整副 XML 解析成页面清单（序号 / slide_id / 正文预览）。
    /// </summary>
    /// <remarks>
    /// 按 <c>LocalName</c> 匹配 <c>slide</c>：SML 2.0 的根元素带默认命名空间
    /// （<c>xmlns="http://www.larkoffice.com/sml/2.0"</c>），按全名匹配会一个也找不到。
    /// </remarks>
    private static JsonObject ProjectInventory(XmlPresentation? presentation, string requestedToken)
    {
        var content = presentation?.Content;
        if (string.IsNullOrEmpty(content))
        {
            return NotFound(requestedToken, "该演示文稿没有可读内容，请确认 token 是否正确、当前身份是否有权限。");
        }

        XDocument document;
        try
        {
            document = XDocument.Parse(content);
        }
        catch (XmlException ex)
        {
            throw new ArgumentException(
                $"演示文稿 XML 无法解析（{ex.Message}）——请改用 slides.get_presentation 直接读原文核对");
        }

        var slides = new JsonArray();
        var number = 0;
        foreach (var element in document.Descendants().Where(static node => node.Name.LocalName == "slide"))
        {
            number++;
            slides.AddNode(new JsonObject
            {
                ["slide_number"] = number,
                ["slide_id"] = element.Attribute("id")?.Value,
                ["text_preview"] = ToolResultText.Truncate(CollapseWhitespace(element.Value), SlidePreviewLength),
            });
        }

        return new JsonObject
        {
            ["found"] = true,
            ["xml_presentation_id"] = presentation!.PresentationId ?? requestedToken,
            ["revision_id"] = presentation.RevisionId,
            ["slide_count"] = number,
            ["slides"] = slides,
        };
    }

    /// <summary>历史版本单页投影（信封形态单源 <c>ToolResultJsons.PageEnvelope</c>）。</summary>
    private static JsonObject ProjectHistoryPage(ListPresentationHistoriesResult data)
    {
        var entries = new JsonArray();
        foreach (var entry in data.Entries ?? [])
        {
            entries.AddNode(new JsonObject
            {
                ["history_version_id"] = entry.HistoryVersionId,
                ["revision_id"] = entry.RevisionId,
                ["edit_time"] = entry.EditTime,
                ["type"] = entry.Type,
                ["name"] = entry.Name,
                ["description"] = ToolResultText.Truncate(entry.Description, PageSizes.MessagePreviewLength),
                ["editor_ids"] = new JsonArray([.. (entry.EditorIds ?? []).Select(static id => (JsonNode?)id)]),
            });
        }

        var envelope = ToolResultJsons.PageEnvelope(data.HasMore, data.PageToken);
        envelope["entries"] = entries;
        return envelope;
    }

    /// <summary>空白折叠（XML 缩进与换行不进预览，否则 120 字符全是空格）。</summary>
    private static string CollapseWhitespace(string? value)
        => string.Join(" ", (value ?? string.Empty).Split((char[])[' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));

    /// <summary>空结果哨兵（F-5）：显式"没找到"，而不是返回空对象让模型猜。</summary>
    private static JsonObject NotFound(string requestedToken, string message) => new()
    {
        ["found"] = false,
        ["xml_presentation_id"] = requestedToken,
        ["message"] = message,
    };

    /// <summary>取客户端；缺席时给出<b>可执行</b>提示（宿主该启用什么），而不是空引用。</summary>
    private static TClient Require<TClient>(string toolName, TClient? client, string contractName)
        where TClient : class
        => client
            ?? throw new ArgumentException(
                $"{toolName} 需要 {contractName}——宿主须启用幻灯片（Slides）API；"
                + "未启用时本工具不在工具列表中（软缺席，不影响其它域工具）");
}
