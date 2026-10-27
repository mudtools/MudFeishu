// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Slides;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// 幻灯片截图执行器（<c>slides.list_slide_images</c> / <c>slides.render_slide_image</c>）：
/// 平台回 Base64 图片，解码后交宿主落盘，结果只回路径。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么单独成类而不并进 <c>SlidesReadTools</c></b>：本类需要<b>非可空</b>的落盘器
/// （<see cref="IFeishuAttachmentStager"/>）——与 <c>MinutesTranscriptTools</c> 同款理由：
/// 落盘器一旦降级为可空，就会造出"拿到图但没法交付"的中间态，即静默丢数据。
/// </para>
/// <para>
/// <b>Base64 是二进制通道</b>：一页截图的 Base64 文本可达数百 KB，直接回填会击穿上下文与 SSE 帧
/// （A10 二进制防线）。故解码点放在执行器里、<b>不放在契约面</b>，工具参数面只接受文本选择器。
/// </para>
/// </remarks>
internal sealed class SlidesImageTools(
    Mud.Feishu.IFeishuTenantV1SlidesSlideImage? imageClient,
    IFeishuAttachmentStager stager)
{
    /// <summary>单次请求的页面数上限（平台约束：slide_ids / slide_numbers 各最多 10 项）。</summary>
    private const int MaxPagesPerRequest = 10;

    private readonly Mud.Feishu.IFeishuTenantV1SlidesSlideImage? _imageClient = imageClient;
    private readonly IFeishuAttachmentStager _stager = stager
        ?? throw new ArgumentNullException(nameof(stager));

    /// <summary>slides.list_slide_images：导出既有页面的服务端截图（逐页落盘，只回路径）。</summary>
    [ToolHandler(typeof(IFeishuTenantSlidesListSlideImagesTool))]
    public Task<ToolResult> ListSlideImagesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SlidesListSlideImages);
        return executor.RunAsync(async () =>
        {
            var args = SlidesListSlideImagesArgs.Unpack(arguments);

            // F-8：选择器（互斥 + 数量上限）在任何下游调用之前本地校验完，模型看得到修复指引。
            var request = BuildImageRequest(args.SlideIds, args.PageStart, args.PageEnd);

            var outcome = FeishuApiResultReader.Read(await Require(executor.ToolName, _imageClient)
                .ListSlideImagesAsync(args.XmlPresentationId, request, cancellationToken)
                .ConfigureAwait(false));
            if (executor.FailIfError(outcome) is { } failure)
            {
                return failure;
            }

            var images = outcome.Data!.SlideImages ?? [];
            if (images.Length == 0)
            {
                return ToolResult.FromText(new JsonObject
                {
                    ["found"] = false,
                    ["count"] = 0,
                    ["xml_presentation_id"] = args.XmlPresentationId,
                    ["message"] = "平台未返回任何页面截图——请确认选择器命中的页面存在（先用 slides.list_slides 核对页码与 slide_id）。",
                }.ToJsonString());
            }

            var files = new JsonArray();
            foreach (var image in images)
            {
                files.AddNode(await StageImageAsync(
                    image,
                    $"slide-{args.XmlPresentationId}-{SelectorSuffix(image)}",
                    cancellationToken).ConfigureAwait(false));
            }

            return ToolResult.FromText(new JsonObject
            {
                ["found"] = true,
                ["xml_presentation_id"] = args.XmlPresentationId,
                ["count"] = files.Count,
                ["total_count"] = outcome.Data.TotalCount,
                ["revision"] = outcome.Data.Revision,
                ["files"] = files,
            }.ToJsonString());
        });
    }

    /// <summary>slides.render_slide_image：渲染入站 XML 为截图（不落库、不产生版本，只回路径）。</summary>
    [ToolHandler(typeof(IFeishuTenantSlidesRenderSlideImageTool))]
    public Task<ToolResult> RenderSlideImageAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SlidesRenderSlideImage);
        return executor.RunAsync(async () =>
        {
            var args = SlidesRenderSlideImageArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await Require(executor.ToolName, _imageClient)
                .RenderSlideImageAsync(new RenderSlideImageRequest { Content = args.Content }, cancellationToken)
                .ConfigureAwait(false));
            if (executor.FailIfError(outcome) is { } failure)
            {
                return failure;
            }

            var image = outcome.Data!.SlideImage;
            if (image is null)
            {
                return ToolResult.FromText(new JsonObject
                {
                    ["found"] = false,
                    ["message"] = "平台未返回渲染结果——请确认 content 是单页 <slide> 片段（不是整份 <presentation>）且 XML 语法可解析。",
                }.ToJsonString());
            }

            return ToolResult.FromText(new JsonObject
            {
                ["found"] = true,
                ["note"] = "本次渲染未写入任何演示文稿、也不产生历史版本；要落稿请改用 slides.create_presentation 或 slides.add_slide。",
                ["file"] = await StageImageAsync(image, "slide-render", cancellationToken).ConfigureAwait(false),
            }.ToJsonString());
        });
    }

    // ────────── 本地校验与落盘 ──────────

    /// <summary>把「slide_ids 或 page_start/page_end」两形态选择器组装成平台请求体。</summary>
    /// <remarks>
    /// 平台对两个数组字段是「二选一」语义，同传时行为未定义；本方法在本地就把这条约束变成
    /// 一次可执行的报错，而不是让模型去猜飞书最终渲染了哪几页。
    /// </remarks>
    internal static ListSlideImagesRequest BuildImageRequest(string[]? slideIds, int? pageStart, int? pageEnd)
    {
        var ids = (slideIds ?? [])
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id.Trim())
            .ToArray();
        var hasRange = pageStart is not null || pageEnd is not null;

        if (ids.Length > 0 && hasRange)
        {
            throw new ArgumentException("slide_ids 与 page_start/page_end 须二选一（同传无法确定要导出哪几页）");
        }

        if (ids.Length > 0)
        {
            if (ids.Length > MaxPagesPerRequest)
            {
                throw new ArgumentException(
                    $"slide_ids 单次最多 {MaxPagesPerRequest} 个页面，实际 {ids.Length.ToString(CultureInfo.InvariantCulture)} 个；请分批调用");
            }

            return new ListSlideImagesRequest { SlideIds = ids };
        }

        if (!hasRange)
        {
            throw new ArgumentException("须给出页面选择器：slide_ids（来自 slides.list_slides）或 page_start/page_end（页码从 1 起）");
        }

        var start = pageStart ?? pageEnd ?? 0;
        var end = pageEnd ?? start;
        if (start < 1)
        {
            throw new ArgumentException($"page_start 须从 1 起（页码是 1-based），实际 {start.ToString(CultureInfo.InvariantCulture)}");
        }

        if (end < start)
        {
            throw new ArgumentException(
                $"page_end 须 ≥ page_start，实际 {start.ToString(CultureInfo.InvariantCulture)}~{end.ToString(CultureInfo.InvariantCulture)}");
        }

        if (end - start + 1 > MaxPagesPerRequest)
        {
            throw new ArgumentException(
                $"单次最多导出 {MaxPagesPerRequest} 页，请求区间 {start.ToString(CultureInfo.InvariantCulture)}~{end.ToString(CultureInfo.InvariantCulture)}"
                + $"（{(end - start + 1).ToString(CultureInfo.InvariantCulture)} 页）；请分批调用");
        }

        var numbers = new int[end - start + 1];
        for (var index = 0; index < numbers.Length; index++)
        {
            numbers[index] = start + index;
        }

        return new ListSlideImagesRequest { SlideNumbers = numbers };
    }

    /// <summary>
    /// 单张截图的落盘：Base64 解码 → 错误体防线 → 交宿主落盘 → 只投影路径与元数据。
    /// </summary>
    /// <remarks>
    /// <b>错误体防线不是多余的</b>：平台在部分失败场景会把 JSON 错误信封塞进 <c>data</c> 字段，
    /// 解码后即得一份内容为错误信的假 <c>.png</c>（B-2 的落盘污染形态），故落盘前一律自检。
    /// </remarks>
    private async Task<JsonObject> StageImageAsync(SlideImage image, string namePrefix, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(image.Data))
        {
            throw new ArgumentException(
                $"平台对页面 {SelectorSuffix(image)} 返回了空图片内容——请确认该页存在，且当前身份有截图权限（slides:presentation:screenshot）");
        }

        byte[] content;
        try
        {
            content = Convert.FromBase64String(image.Data!.Trim());
        }
        catch (FormatException ex)
        {
            throw new ArgumentException(
                $"平台返回的图片内容不是合法 Base64（{ex.Message}）——本次不落盘，请重试或改用 slides.get_slide 读该页文本");
        }

        if (content.Length == 0)
        {
            throw new ArgumentException("平台返回的图片内容为空字节——本次不落盘，请改用 slides.get_slide 读该页文本");
        }

        if (DownloadedContentGuard.ShouldRejectForJsonErrorBody(content, expectedContentType: null, out var reason))
        {
            throw new ArgumentException($"截图内容被错误体防线拒绝：{reason}");
        }

        var (extension, mimeType) = ImageFormat(image.Format);
        var staged = await _stager
            .StageAsync(new AttachmentSource(null, content, $"{namePrefix}.{extension}"), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new ArgumentException("附件落盘被宿主拒绝（宿主安全域：大小上限 / 落盘策略）——图片未交付");

        return new JsonObject
        {
            ["slide_id"] = image.SlideId,
            ["slide_number"] = image.SlideNumber,
            ["local_path"] = staged.LocalPath,
            ["size"] = staged.Size,
            ["content_type"] = staged.ContentType ?? mimeType,
        };
    }

    /// <summary>平台 <c>format</c> 取值 → 扩展名与 MIME（1=png，2=jpg）。</summary>
    private static (string Extension, string MimeType) ImageFormat(int? format) => format switch
    {
        2 => ("jpg", "image/jpeg"),
        _ => ("png", "image/png"),
    };

    /// <summary>文件名用的页面标识（页码优先，缺席退回页面 ID）。</summary>
    private static string SelectorSuffix(SlideImage image)
        => image.SlideNumber?.ToString(CultureInfo.InvariantCulture) ?? image.SlideId ?? "unknown";

    /// <summary>取客户端；缺席时给出<b>可执行</b>提示（宿主该启用什么），而不是空引用。</summary>
    private static Mud.Feishu.IFeishuTenantV1SlidesSlideImage Require(string toolName, Mud.Feishu.IFeishuTenantV1SlidesSlideImage? client)
        => client
            ?? throw new ArgumentException(
                $"{toolName} 需要 IFeishuTenantV1SlidesSlideImage——宿主须启用幻灯片（Slides）API；"
                + "未启用时本工具不在工具列表中（软缺席，不影响其它域工具）");
}
