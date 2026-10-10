// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Tools;
using Mud.Feishu.DataModels.AI;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// AI 文本面工具执行器（R7 / C3：<c>ai.translate_text</c> / <c>ai.detect_language</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>软依赖</b>：翻译客户端可空注入——宿主未启用 AI 翻译能力时，调用得到"须启用
/// <c>IFeishuTenantV1AITranslation</c>"的<b>可执行</b>结构化错误，不影响其它域工具。
/// </para>
/// <para>
/// <b>参数校验前置</b>：<c>text</c> 超 1000 字符（平台上限）、语言代码不在闭集、术语表非数组/超 128 项，
/// 一律在<b>本地</b>拒绝并附合法值清单（F-8 的 suggestions 等价物）——不让平台返回
/// <c>1140101 invalid param</c> 这种对模型毫无指向性的错误。
/// </para>
/// <para>
/// <b>二进制防线（A10 / DP-C3-1）</b>：OCR（base64 图片）、文档识别（<c>[FormContent]</c> 本地文件）、
/// STT（base64 音频）<b>不策展</b>——它们的入参在进程内库中无法由模型表达，
/// 正确形态是宿主侧转换（宿主把 URL/本地文件直接交给 SDK 的 <c>[FormContent]</c>/base64 参数，
/// 再把得到的<b>文本</b>放进模型上下文；见 Guidance/ai.md）。
/// </para>
/// </remarks>
internal sealed class TranslationTools(
    IOptions<FeishuAgentOptions> options,
    Mud.Feishu.IFeishuTenantV1AITranslation? translationClient = null)
{
    /// <summary>平台对 <c>translate</c> 的 <c>text</c> 上限（官方文档：1,000 字符）。（detect 未声明上限，故不拦。）</summary>
    private const int MaxTranslateTextLength = 1000;

    /// <summary>平台对 <c>glossary</c> 的上限（官方文档：128 个术语）。</summary>
    private const int MaxGlossaryTerms = 128;

    private readonly Mud.Feishu.IFeishuTenantV1AITranslation? _translationClient = translationClient;
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;

    /// <summary>ai.translate_text：把文本翻译成目标语言（机器翻译）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantAiTranslateTextTool))]
    public Task<FeishuToolResult> TranslateTextAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.AiTranslateText, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = AiTranslateTextArgs.Unpack(arguments);

            var text = RequireTranslateText(args.Text);
            var sourceLanguage = TranslationLanguages.Require("source_language", args.SourceLanguage);
            var targetLanguage = TranslationLanguages.Require("target_language", args.TargetLanguage);
            var glossary = ParseGlossary(args.Glossary);

            var request = new TranslateTextRequest
            {
                Text = text,
                SourceLanguage = sourceLanguage,
                TargetLanguage = targetLanguage,
                Glossaies = glossary,
            };

            var outcome = FeishuApiResultReader.Read(await Require(executor.ToolName)
                .TranslateTextAsync(request, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, data => new JsonObject
            {
                ["text"] = data.Text,
                ["source_language"] = sourceLanguage,
                ["target_language"] = targetLanguage,

                // 术语表生效条数：模型据此确认"术语表真的被带上了"（0 = 未使用）。
                ["glossary_applied"] = glossary?.Length ?? 0,
            });
        });
    }

    /// <summary>ai.detect_language：识别文本语种（ISO 639-1 + 可读语言名）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantAiDetectLanguageTool))]
    public Task<FeishuToolResult> DetectTextAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.AiDetectLanguage, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = AiDetectLanguageArgs.Unpack(arguments);

            // 平台文档未声明 detect 的 text 上限 ⇒ 本地只拦空文本（不臆造上限）。
            var text = args.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("缺少必填参数 text（待识别语种的文本不能为空）");
            }

            var outcome = FeishuApiResultReader.Read(await Require(executor.ToolName)
                .DetectTextAsync(new DetectTextRequest { Text = text }, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, data => new JsonObject
            {
                ["language"] = data.Language,

                // 语言名映射让模型不必自带"en=英语"的对照表；未登记的代码原样返回 null（不臆造名称）。
                ["language_name"] = TranslationLanguages.GetDisplayName(data.Language),
            });
        });
    }

    /// <summary>校验翻译文本：非空且不超平台上限（超限本地拒绝，避免平台 1140101）。</summary>
    private static string RequireTranslateText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("缺少必填参数 text（待翻译文本不能为空）");
        }

        if (text.Length > MaxTranslateTextLength)
        {
            throw new ArgumentException(
                $"参数 text 超长：{text.Length.ToString(CultureInfo.InvariantCulture)} 字符，平台上限 "
                + $"{MaxTranslateTextLength.ToString(CultureInfo.InvariantCulture)} 字符——请分段翻译后拼接");
        }

        return text;
    }

    /// <summary>校验并解析可选术语表（JSON 数组字符串：<c>[{"from":"飞书","to":"Lark"}]</c>）。</summary>
    private static TranslateTerm[]? ParseGlossary(string? glossaryJson)
    {
        if (string.IsNullOrWhiteSpace(glossaryJson))
        {
            return null;
        }

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(glossaryJson);
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new ArgumentException(
                $"参数 glossary 不是合法 JSON：{ex.Message}。期望形态：[{{\"from\":\"飞书\",\"to\":\"Lark\"}}]");
        }

        if (node is not JsonArray array)
        {
            throw new ArgumentException("参数 glossary 必须是 JSON 数组（每项形如 {\"from\":\"原文\",\"to\":\"译文\"}）");
        }

        if (array.Count == 0)
        {
            throw new ArgumentException("参数 glossary 不能为空数组（不需要术语表时请省略该参数）");
        }

        if (array.Count > MaxGlossaryTerms)
        {
            throw new ArgumentException(
                $"参数 glossary 最多 {MaxGlossaryTerms.ToString(CultureInfo.InvariantCulture)} 项，"
                + $"实际 {array.Count.ToString(CultureInfo.InvariantCulture)} 项");
        }

        var terms = new List<TranslateTerm>(array.Count);
        for (var i = 0; i < array.Count; i++)
        {
            if (array[i] is not JsonObject item)
            {
                throw new ArgumentException($"参数 glossary 第 {i + 1} 项必须是对象（形如 {{\"from\":\"原文\",\"to\":\"译文\"}}）");
            }

            var from = item["from"]?.GetValue<string>();
            var to = item["to"]?.GetValue<string>();

            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            {
                throw new ArgumentException($"参数 glossary 第 {i + 1} 项缺少 from/to（两者均为必填的术语原文与译文）");
            }

            terms.Add(new TranslateTerm { From = from!, To = to! });
        }

        return [.. terms];
    }

    /// <summary>取客户端；缺席时给出<b>可执行</b>提示（宿主该启用什么），而不是空引用。</summary>
    private Mud.Feishu.IFeishuTenantV1AITranslation Require(string toolName)
        => _translationClient
            ?? throw new ArgumentException(
                $"{toolName} 需要 IFeishuTenantV1AITranslation——宿主须启用飞书 AI 翻译能力（并在开发者后台开通 "
                + "translation:text 权限）；未启用时本工具不在工具列表中（软缺席，不影响其它域工具）");
}
