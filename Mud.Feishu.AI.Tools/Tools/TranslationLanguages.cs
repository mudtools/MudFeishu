// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Tools;

/// <summary>
/// 机器翻译（<c>translation/v1</c>）的<b>语种取值表与口径归一化</b>（R7 / C3）。
/// </summary>
/// <remarks>
/// <para>
/// <b>真相源</b>：飞书开放平台《Translate text》文档的"支持语言"清单（16 项）。
/// 该清单在文档里<b>大小写不一致</b>（示例用 <c>zh</c>/<c>en</c>，清单里又有 <c>Zh-Hant</c>），
/// 故本表统一登记为小写口径，并在匹配时<b>大小写不敏感</b>、转发时归一化为本表口径——
/// 模型写 <c>En</c> 或 <c>en</c> 都能命中，不会出现"写法差一个字母就 400"的静默失败。
/// </para>
/// <para>
/// <b>为什么不用 <c>[ToolParameter(EnumType = …)]</c> 闭集</b>：闭集渲染的是<b>常量名</b>
/// （<c>BlockTypes</c> 的先例），而这里的模型可见值必须是平台口径的语言代码本身；
/// 用常量名会把模型引向一套与平台不一致的写法（<c>ZhHant</c> vs <c>zh-Hant</c>）。
/// 故此处以"执行器闭集校验 + 合法值清单回填"实现同等能力（非法值 → <c>invalid_args</c>）。
/// </para>
/// </remarks>
internal static class TranslationLanguages
{
    /// <summary>平台支持的语言代码（小写口径，唯一真相源）。</summary>
    public static readonly string[] All =
    [
        "zh", "zh-Hant", "en", "ja", "ru", "de", "fr", "it", "pl", "th", "hi", "id", "es", "pt", "ko", "vi",
    ];

    /// <summary>合法值清单（错误文案用；模型据此自我纠正）。</summary>
    public static string LegalValues => string.Join(" / ", All);

    /// <summary>
    /// 把模型给出的语言值归一化为平台口径（<b>大小写不敏感</b>；忽略首尾空白）。
    /// </summary>
    /// <param name="raw">模型给出的语言代码。</param>
    /// <param name="code">归一化后的语言代码（未命中时为 <see cref="string.Empty"/>）。</param>
    /// <returns>是否命中闭集。</returns>
    public static bool TryNormalize(string? raw, out string code)
    {
        code = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var trimmed = raw.Trim();
        foreach (var candidate in All)
        {
            if (string.Equals(candidate, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                code = candidate;
                return true;
            }
        }

        return false;
    }

    /// <summary>闭集校验：不命中时抛出<b>附合法值清单</b>的参数错误（F-8 的 suggestions 等价物）。</summary>
    /// <param name="name">参数名（<c>source_language</c> / <c>target_language</c>）。</param>
    /// <param name="raw">模型给出的原始值。</param>
    /// <returns>归一化后的平台口径语言代码。</returns>
    /// <exception cref="ArgumentException">值不在闭集内。</exception>
    public static string Require(string name, string raw)
        => TryNormalize(raw, out var code)
            ? code
            : throw new ArgumentException(
                $"参数 {name} 的值 '{raw}' 不在允许取值内。合法取值：{LegalValues}");

    /// <summary>
    /// ISO 639-1 语言代码 → 可读语言名（<c>ai.detect_language</c> 用；模型友好）。
    /// </summary>
    /// <remarks>
    /// 识别接口支持 100 多种语言，本表只覆盖<b>常见</b>代码；未登记的代码原样返回代码本身
    /// （<b>不臆造</b>名称），模型据代码自行判断。
    /// </remarks>
    public static string? GetDisplayName(string? isoCode)
    {
        if (string.IsNullOrWhiteSpace(isoCode))
        {
            return null;
        }

        return DisplayNames.TryGetValue(isoCode.Trim(), out var name) ? name : null;
    }

    /// <summary>常见语种代码 → 中文名（字典序自证见 TranslationLanguagesTests）。</summary>
    private static readonly Dictionary<string, string> DisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["zh"] = "中文",
        ["zh-Hant"] = "繁体中文",
        ["en"] = "英语",
        ["ja"] = "日语",
        ["ko"] = "朝鲜语",
        ["ru"] = "俄语",
        ["de"] = "德语",
        ["fr"] = "法语",
        ["it"] = "意大利语",
        ["es"] = "西班牙语",
        ["pt"] = "葡萄牙语",
        ["pl"] = "波兰语",
        ["th"] = "泰语",
        ["hi"] = "印地语",
        ["id"] = "印尼语",
        ["vi"] = "越南语",
        ["ar"] = "阿拉伯语",
        ["tr"] = "土耳其语",
        ["nl"] = "荷兰语",
        ["sv"] = "瑞典语",
        ["ms"] = "马来语",
        ["he"] = "希伯来语",
        ["uk"] = "乌克兰语",
    };
}
