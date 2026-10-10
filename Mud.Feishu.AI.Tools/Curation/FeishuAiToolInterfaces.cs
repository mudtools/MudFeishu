// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── AI 多模态 · 文本面（R7 / C3） ───────────────────────────

/// <summary>
/// 工具接口：ai.translate_text（映射 <c>IFeishuTenantV1AITranslation.TranslateTextAsync</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>R7 / C3（DP-C3-1）</b>：多模态能力里<b>唯一</b>能进工具面的形态是"纯文本入参"。
/// OCR（base64 图片）/ 文档识别（<c>[FormContent]</c> 本地文件）/ STT（base64 音频）三者
/// 的入参在进程内库中无法由模型表达（模型没有文件系统、也不该把 MB 级 base64 塞进参数），
/// 故<b>不策展</b>，改由宿主侧通道承担（见 <c>IFeishuBinaryArtifactSource</c> 与 Guidance/ai.md）。
/// </para>
/// <para>
/// <b>为什么语言代码不用 <c>EnumType</c> 闭集</b>：平台文档里同一语言值的<b>大小写并不一致</b>
/// （示例用 <c>zh</c>/<c>en</c>，清单里又有 <c>Zh-Hant</c>），闭集若按常量名渲染会把模型引向
/// 一套与平台不完全一致的写法；故参数保持 <c>string</c>，由执行器做<b>大小写不敏感</b>的闭集校验并
/// 归一化为平台口径（非法值 → <c>invalid_args</c> + 合法值清单，与 <c>EnumType</c> 路径同等的
/// 可自我纠正能力）。
/// </para>
/// </remarks>
[FeishuTool("ai.translate_text",
    Description = "把文本翻译成目标语言（飞书机器翻译）。text 上限 1000 字符（超限本地拒绝，不会下发）；source_language / target_language 用语言代码（zh / zh-Hant / en / ja / ru / de / fr / it / pl / th / hi / id / es / pt / ko / vi，大小写不敏感），不确定源语言时先用 ai.detect_language 识别。可选 glossary 传本次生效的术语表（JSON 数组字符串，形如 [{\"from\":\"飞书\",\"to\":\"Lark\"}]，最多 128 项）。只读，需 translation:text。",
    RequiredScopes = ["translation:text"],
    Source = nameof(IFeishuTenantV1AITranslation) + "." + nameof(IFeishuTenantV1AITranslation.TranslateTextAsync))]
public interface IFeishuTenantAiTranslateTextTool
{
    /// <summary>翻译文本。</summary>
    /// <returns>白名单投影后的 JSON 文本（text / source_language / target_language）。</returns>
    Task<string> TranslateTextAsync(
        [ToolParameter("text", "待翻译文本（上限 1000 字符）", Required = true)] string text,
        [ToolParameter("source_language", "源语言代码（如 zh / en / ja；大小写不敏感）", Required = true)] string source_language,
        [ToolParameter("target_language", "目标语言代码（如 en / zh-Hant；大小写不敏感）", Required = true)] string target_language,
        [ToolParameter("glossary", "可选术语表（JSON 数组字符串，形如 [{\"from\":\"飞书\",\"to\":\"Lark\"}]，最多 128 项）")] string? glossary = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：ai.detect_language（映射 <c>IFeishuTenantV1AITranslation.DetectTextAsync</c>）。
/// </summary>
/// <remarks>
/// R7 / C3：返回 ISO 639-1 语言代码，并映射为可读语言名（模型友好）——
/// "这段内容是什么语言"是多语言工作流（翻译、检索、审阅）的分支首环。
/// 平台文档未声明 <c>text</c> 的字符上限，故本地只拦"空文本"（不臆造上限）。
/// </remarks>
[FeishuTool("ai.detect_language",
    Description = "识别一段文本的语种（飞书机器翻译语种识别），返回 ISO 639-1 语言代码与可读语言名（如 en / 英语）。用于在翻译、检索、审阅前先确定内容语言。只读，需 translation:text。",
    RequiredScopes = ["translation:text"],
    Source = nameof(IFeishuTenantV1AITranslation) + "." + nameof(IFeishuTenantV1AITranslation.DetectTextAsync))]
public interface IFeishuTenantAiDetectLanguageTool
{
    /// <summary>识别文本语种。</summary>
    /// <returns>白名单投影后的 JSON 文本（language / language_name）。</returns>
    Task<string> DetectTextAsync(
        [ToolParameter("text", "待识别语种的文本", Required = true)] string text,
        CancellationToken cancellationToken = default);
}
