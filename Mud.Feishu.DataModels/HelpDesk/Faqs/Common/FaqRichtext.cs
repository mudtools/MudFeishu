// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.HelpDesk;

/// <summary>
/// <para>知识库FAQ富文本片段</para>
/// <para>支持 text / hyperlink / img / line break 四种类型，不同类型的字段不同（换行即 content 为空串的 text）。</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "HelpDesk")]
public class FaqRichtext
{
    /// <summary>
    /// <para>类型。可选值：text、hyperlink、img、line break</para>
    /// <para>必填：否</para>
    /// <para>**示例值**："text"</para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>文本内容（type = text / line break 时使用，赋值 "" 表示换行）</para>
    /// <para>必填：否</para>
    /// <para>**示例值**："Hi, please visit xxx."</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// <para>文本内容（type = hyperlink 时使用）</para>
    /// <para>必填：否</para>
    /// <para>**示例值**："ByteDance"</para>
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// <para>链接（type = hyperlink 时使用）</para>
    /// <para>必填：否</para>
    /// <para>**示例值**："https://bytedance.com"</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>图片源（type = img 时使用）</para>
    /// <para>必填：否</para>
    /// <para>**示例值**："https://link.to.image/image.jpg"</para>
    /// </summary>
    [JsonPropertyName("src")]
    public string? Src { get; set; }

    /// <summary>
    /// <para>图片名（type = img 时使用）</para>
    /// <para>必填：否</para>
    /// <para>**示例值**："abcd"</para>
    /// </summary>
    [JsonPropertyName("key")]
    public string? Key { get; set; }
}
