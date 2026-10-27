// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.Slides;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// 幻灯片写执行器（<c>slides.create_presentation</c> / <c>slides.add_slide</c> /
/// <c>slides.replace_slide</c> / <c>slides.delete_slide</c> / <c>slides.revert_history</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>写面共同的三道前置</b>：① <c>dry_run</c> 预演；② 参数合法性校验<b>先于</b>预演判定（预演的价值
/// 在于"能提前发现的问题都提前发现"）；③ 模型自己写的 SML XML 与 <c>parts</c> 数组在本地解析到位——
/// 平台的 XML 错误码（3350001 一类）对模型没有修复信息量，本地判据才有。
/// </para>
/// <para>
/// <b>回滚与删页属不可恢复面</b>：本类不做"确认式二次调用"（授权由 <c>IToolExecutionAuthorizer</c>
/// 与 <c>WriteAllowList</c> 负责），但把"只能靠历史版本补救"这一事实写进结果，让模型如实转达。
/// </para>
/// </remarks>
internal sealed class SlidesWriteTools(
    Mud.Feishu.IFeishuTenantV1SlidesPresentation? presentationClient = null,
    Mud.Feishu.IFeishuTenantV1SlidesSlide? slideClient = null,
    Mud.Feishu.IFeishuTenantV1SlidesHistory? historyClient = null)
{
    /// <summary>单次替换的动作数上限（平台约束：parts 1~200 项）。</summary>
    private const int MaxPartsPerRequest = 200;

    /// <summary>替换动作闭集（平台规范值；CLI 的 replace/insert 别名在工具面不接受，避免两套口径）。</summary>
    private static readonly string[] ReplaceActions = ["block_replace", "block_insert"];

    private readonly Mud.Feishu.IFeishuTenantV1SlidesPresentation? _presentationClient = presentationClient;
    private readonly Mud.Feishu.IFeishuTenantV1SlidesSlide? _slideClient = slideClient;
    private readonly Mud.Feishu.IFeishuTenantV1SlidesHistory? _historyClient = historyClient;

    /// <summary>slides.create_presentation：以整份 SML XML 新建演示文稿。</summary>
    [ToolHandler(typeof(IFeishuTenantSlidesCreatePresentationTool))]
    public Task<ToolResult> CreatePresentationAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SlidesCreatePresentation);
        return executor.RunAsync(async () =>
        {
            var args = SlidesCreatePresentationArgs.Unpack(arguments);

            var content = args.Content.Trim();
            if (content.Length == 0)
            {
                throw new ArgumentException("content 不能为空——新建演示文稿须给出整份 SML 2.0 XML");
            }

            // 根元素判据：平台只认 <presentation> 整份文档，把单页 <slide> 传进来是最常见的误用形态。
            RequireRoot(content, "presentation", "content");

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/slides_ai/v1/xml_presentations", null,
                    ("content", content.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await Require(
                    executor.ToolName, _presentationClient, "IFeishuTenantV1SlidesPresentation")
                .CreateXmlPresentationAsync(
                    new CreateXmlPresentationRequest { XmlPresentation = new XmlPresentation { Content = content } },
                    cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["created"] = true,
                ["xml_presentation_id"] = data.XmlPresentationId,
                ["url"] = data.Url,
                ["revision_id"] = data.RevisionId,
                ["slide_ids"] = new JsonArray([.. (data.SlideIds ?? []).Select(static id => (JsonNode?)id)]),
                ["issues"] = data.Issues,
                ["note"] = IssueNote(data.Issues),
            });
        });
    }

    /// <summary>slides.add_slide：插入一页（before_slide_id 留省=末尾）。</summary>
    [ToolHandler(typeof(IFeishuTenantSlidesAddSlideTool))]
    public Task<ToolResult> AddSlideAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SlidesAddSlide);
        return executor.RunAsync(async () =>
        {
            var args = SlidesAddSlideArgs.Unpack(arguments);

            var content = args.Content.Trim();
            if (content.Length == 0)
            {
                throw new ArgumentException("content 不能为空——新增页面须给出单页 SML XML 片段");
            }

            // 只判良构：平台对新增页面的粒度容忍（<slide> 整页 / 内层片段均可），本地不替它收紧。
            ParseXml(content, "content");

            // ⚠️ 空串与"未给出"在平台侧语义不同（前者是"插到未知页面之前"），故归一为 null。
            var beforeSlideId = string.IsNullOrWhiteSpace(args.BeforeSlideId) ? null : args.BeforeSlideId!.Trim();

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST",
                    "/open-apis/slides_ai/v1/xml_presentations/{xml_presentation_id}/slide",
                    ToolDryRun.IdempotencyNote(args.IdempotencyKey),
                    ("xml_presentation_id", args.XmlPresentationId.Length), ("content", content.Length),
                    ("before_slide_id", beforeSlideId?.Length ?? 0)));
            }

            var outcome = FeishuApiResultReader.Read(await Require(
                    executor.ToolName, _slideClient, "IFeishuTenantV1SlidesSlide")
                .CreateSlideAsync(
                    args.XmlPresentationId,
                    new CreateSlideRequest { Slide = new Slide { Content = content }, BeforeSlideId = beforeSlideId },
                    args.RevisionId,
                    args.IdempotencyKey,
                    cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["added"] = true,
                ["slide_id"] = data.SlideId,
                ["revision_id"] = data.RevisionId,
                ["inserted_before"] = beforeSlideId,
                ["issues"] = data.Issues,
                ["note"] = IssueNote(data.Issues),
            });
        });
    }

    /// <summary>slides.replace_slide：整页重建或元素级局部改（共用平台同一端点）。</summary>
    [ToolHandler(typeof(IFeishuTenantSlidesReplaceSlideTool))]
    public Task<ToolResult> ReplaceSlideAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SlidesReplaceSlide);
        return executor.RunAsync(async () =>
        {
            var args = SlidesReplaceSlideArgs.Unpack(arguments);

            var parts = ParseReplaceParts(args.Parts);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST",
                    "/open-apis/slides_ai/v1/xml_presentations/{xml_presentation_id}/slide/replace",
                    ToolDryRun.IdempotencyNote(args.IdempotencyKey),
                    ("xml_presentation_id", args.XmlPresentationId.Length), ("slide_id", args.SlideId.Length),
                    ("parts", parts.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await Require(
                    executor.ToolName, _slideClient, "IFeishuTenantV1SlidesSlide")
                .ReplaceSlideAsync(
                    args.XmlPresentationId,
                    args.SlideId.Trim(),
                    new ReplaceSlideRequest
                    {
                        Parts = parts,
                        Comment = string.IsNullOrWhiteSpace(args.Comment) ? null : args.Comment.Trim(),
                        IdempotencyKey = string.IsNullOrWhiteSpace(args.IdempotencyKey) ? null : args.IdempotencyKey.Trim(),
                    },
                    args.RevisionId,
                    cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["replaced"] = data.FailedPartIndex is null or < 0,
                ["revision_id"] = data.RevisionId,
                ["part_count"] = parts.Length,
                ["failed_part_index"] = data.FailedPartIndex,
                ["failed_reason"] = data.FailedReason,
                ["note"] = data.FailedPartIndex >= 0
                    ? $"第 {data.FailedPartIndex} 项动作失败（{data.FailedReason}），其余可能已生效——请用 slides.get_slide 复核当前页面内容后再决定是否重试。"
                    : (string?)null,
            });
        });
    }

    /// <summary>slides.delete_slide：删除页面（不可恢复，只能靠历史版本回滚补救）。</summary>
    [ToolHandler(typeof(IFeishuTenantSlidesDeleteSlideTool))]
    public Task<ToolResult> DeleteSlideAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SlidesDeleteSlide);
        return executor.RunAsync(async () =>
        {
            var args = SlidesDeleteSlideArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "DELETE",
                    "/open-apis/slides_ai/v1/xml_presentations/{xml_presentation_id}/slide",
                    ToolDryRun.IdempotencyNote(args.IdempotencyKey) + "删除页面不可恢复：平台无回收站接口。",
                    ("xml_presentation_id", args.XmlPresentationId.Length), ("slide_id", args.SlideId.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await Require(
                    executor.ToolName, _slideClient, "IFeishuTenantV1SlidesSlide")
                .DeleteSlideAsync(
                    args.XmlPresentationId,
                    args.SlideId.Trim(),
                    args.RevisionId,
                    args.IdempotencyKey,
                    cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["deleted"] = true,
                ["xml_presentation_id"] = args.XmlPresentationId,
                ["slide_id"] = args.SlideId.Trim(),
                ["revision_id"] = data.RevisionId,
                ["note"] = "页面已删除且不可单独恢复；如需找回，用 slides.list_histories + slides.revert_history 回滚到删除前的历史版本（会连带抹掉其后的其他编辑）。",
            });
        });
    }

    /// <summary>slides.revert_history：整副回滚到历史版本（异步任务，须再用状态工具确认）。</summary>
    [ToolHandler(typeof(IFeishuTenantSlidesRevertHistoryTool))]
    public Task<ToolResult> RevertHistoryAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SlidesRevertHistory);
        return executor.RunAsync(async () =>
        {
            var args = SlidesRevertHistoryArgs.Unpack(arguments);

            // 平台把 history_version_id 定义为正整数字符串；最常见的误用是填 revision_id。
            var historyVersionId = args.HistoryVersionId.Trim();
            var isPositiveInteger = historyVersionId.Length > 0
                && historyVersionId.All(static c => c >= '0' && c <= '9')
                && historyVersionId.Any(static c => c != '0');
            if (!isPositiveInteger)
            {
                throw new ArgumentException(
                    $"history_version_id 须是正整数字符串（取自 slides.list_histories 的 history_version_id 字段），实际: {args.HistoryVersionId}");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST",
                    "/open-apis/slides_ai/v1/xml_presentations/{xml_presentation_id}/history/revert",
                    "整副回滚：回滚点之后的所有编辑（含他人改动）都会消失。",
                    ("xml_presentation_id", args.XmlPresentationId.Length), ("history_version_id", historyVersionId.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await Require(
                    executor.ToolName, _historyClient, "IFeishuTenantV1SlidesHistory")
                .RevertPresentationHistoryAsync(
                    args.XmlPresentationId,
                    new RevertPresentationHistoryRequest { HistoryVersionId = historyVersionId },
                    cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["submitted"] = true,
                ["task_id"] = data.TaskId,
                ["status"] = data.Status,
                ["poll_after_ms"] = data.PollAfterMs,
                ["history_version_id"] = data.HistoryVersionId,
                ["note"] = "回滚任务已提交（尚未确认落地）——请用 slides.get_revert_status 按 poll_after_ms 轮询 task_id 的结果。",
            });
        });
    }

    // ────────── 本地校验 ──────────

    /// <summary>parts JSON 数组字符串 → 强类型动作数组（闭集 action + 逐动作必填字段，全部本地拒绝）。</summary>
    /// <remarks>
    /// 解析走 <c>FeishuJsonAot</c>（AOT-3）：泛型 <c>JsonSerializer.Deserialize&lt;T&gt;(string, options)</c>
    /// 带 IL2026/IL3050 注解；<see cref="SlideReplacePart"/> 已登记在 <c>SlidesJsonContext</c>。
    /// </remarks>
    internal static SlideReplacePart[] ParseReplaceParts(string partsJson)
    {
        SlideReplacePart[]? parts;
        try
        {
            parts = FeishuJsonAot.Deserialize<SlideReplacePart[]>(partsJson, FeishuJsonDefaults.DeserializerOptions);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException(
                $"parts 不是合法 JSON 数组: {ex.Message}——形如 {{\"action\":\"block_replace\",\"block_id\":\"bab\",\"replacement\":\"<shape …>…</shape>\"}}");
        }

        if (parts is not { Length: > 0 })
        {
            throw new ArgumentException(
                $"parts 须为非空 JSON 数组（每项形如 {{\"action\":\"block_replace\",\"block_id\":\"bab\",\"replacement\":\"<shape/>\"}}），最多 {MaxPartsPerRequest} 项");
        }

        if (parts.Length > MaxPartsPerRequest)
        {
            throw new ArgumentException(
                $"parts 单次最多 {MaxPartsPerRequest} 项，实际 {parts.Length.ToString(CultureInfo.InvariantCulture)} 项；请分批调用");
        }

        for (var index = 0; index < parts.Length; index++)
        {
            ValidatePart(parts[index], index);
        }

        return parts;
    }

    /// <summary>单个替换动作的字段闭集校验（错误文本带项号，模型才知道改哪一项）。</summary>
    private static void ValidatePart(SlideReplacePart? part, int index)
    {
        if (part is null)
        {
            throw new ArgumentException($"parts[{index}] 为 null——每项须是 JSON 对象");
        }

        var action = part.Action?.Trim();
        if (string.IsNullOrEmpty(action))
        {
            throw new ArgumentException($"parts[{index}] 缺少 action（取 {string.Join(" 或 ", ReplaceActions)}）");
        }

        if (!ReplaceActions.Contains(action, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"parts[{index}].action 仅支持 {string.Join("/", ReplaceActions)}，实际: {part.Action}"
                + "（工具面不接受 replace/insert 简写，避免与平台规范值两套口径）");
        }

        if (action == ReplaceActions[0])
        {
            if (string.IsNullOrWhiteSpace(part.BlockId))
            {
                throw new ArgumentException(
                    $"parts[{index}] 为 block_replace 但缺 block_id——整页重建填页面 ID 本身，元素级改填页内 3 位 short element ID（须先用 slides.get_slide 读到）");
            }

            if (string.IsNullOrWhiteSpace(part.Replacement))
            {
                throw new ArgumentException($"parts[{index}] 为 block_replace 但缺 replacement（替换后的 XML 片段）");
            }

            ParseXml(part.Replacement!.Trim(), $"parts[{index}].replacement");
            return;
        }

        if (string.IsNullOrWhiteSpace(part.Insertion))
        {
            throw new ArgumentException($"parts[{index}] 为 block_insert 但缺 insertion（要插入的 XML 片段）");
        }

        if (!string.IsNullOrWhiteSpace(part.BlockId) || !string.IsNullOrWhiteSpace(part.Replacement))
        {
            throw new ArgumentException(
                $"parts[{index}] 为 block_insert 却填了 block_id/replacement——插入用 insertion 与可选的 insert_before_block_id（留省=追加到末尾）");
        }

        ParseXml(part.Insertion!.Trim(), $"parts[{index}].insertion");
    }

    /// <summary>
    /// XML 良构校验（平台对非良构 SML 只回数字错误码，本地给出行列号才有修复价值）。
    /// </summary>
    /// <remarks>
    /// <c>XDocument.Parse</c> 默认 <c>DtdProcessing=Prohibit</c>：带 DOCTYPE 的输入直接判失败，
    /// 本校验因此不构成外部实体入口。
    /// </remarks>
    private static XDocument ParseXml(string xml, string parameterName)
    {
        try
        {
            return XDocument.Parse(xml);
        }
        catch (XmlException ex)
        {
            throw new ArgumentException($"{parameterName} 不是良构 XML（{ex.Message}）——请修正后重试，本次未下发");
        }
    }

    /// <summary>良构校验 + 根元素判据（<c>slides.create_presentation</c> 只收整份 presentation 文档）。</summary>
    private static void RequireRoot(string xml, string expectedRoot, string parameterName)
    {
        var root = ParseXml(xml, parameterName).Root;
        if (!string.Equals(root?.Name.LocalName, expectedRoot, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"{parameterName} 的根元素须是 <{expectedRoot}>（SML 2.0 命名空间内），实际 <{root?.Name.LocalName}>"
                + "——只含单页时请改用 slides.add_slide");
        }
    }

    /// <summary>平台 XML 告警的可读转达（issues 是字符串，非空即代表"已建成但内容有偏差"）。</summary>
    private static string? IssueNote(string? issues)
        => string.IsNullOrWhiteSpace(issues)
            ? null
            : $"文稿/页面已写入，但平台提示 XML 存在偏差：{issues}——请用 slides.get_slide 复核实际渲染结果。";

    /// <summary>取客户端；缺席时给出<b>可执行</b>提示（宿主该启用什么），而不是空引用。</summary>
    private static TClient Require<TClient>(string toolName, TClient? client, string contractName)
        where TClient : class
        => client
            ?? throw new ArgumentException(
                $"{toolName} 需要 {contractName}——宿主须启用幻灯片（Slides）API；"
                + "未启用时本工具不在工具列表中（软缺席，不影响其它域工具）");
}
