// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.HelpDesk;

/// <summary>
/// <para>修改知识库FAQ的FAQ内容</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "HelpDesk")]
public class FaqUpdateInfo
{
    /// <summary>
    /// <para>知识库分类 ID</para>
    /// <para>必填：否</para>
    /// <para>**示例值**："6836004780707807251"</para>
    /// </summary>
    [JsonPropertyName("category_id")]
    public string? CategoryId { get; set; }

    /// <summary>
    /// <para>问题</para>
    /// <para>必填：是</para>
    /// <para>**示例值**："Question"</para>
    /// </summary>
    [JsonPropertyName("question")]
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// <para>答案</para>
    /// <para>必填：否</para>
    /// <para>**示例值**："Answer"</para>
    /// </summary>
    [JsonPropertyName("answer")]
    public string? Answer { get; set; }

    /// <summary>
    /// <para>富文本答案与答案二者填其一。Json Array 形式，富文本结构参见
    /// <see href="https://open.feishu.cn/document/ukTMukTMukTM/uITM0YjLyEDN24iMxQjN">富文本</see>。</para>
    /// <para>必填：否</para>
    /// <para>**示例值**：{"content":"Obtain medical insurance question","type":"text"}</para>
    /// </summary>
    [JsonPropertyName("answer_richtext")]
    public FaqRichtext[]? AnswerRichtext { get; set; }

    /// <summary>
    /// <para>内容</para>
    /// <para>必填：否</para>
    /// <para>**示例值**："Obtain medical insurance question"</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// <para>类型。可选值：text、hyperlink、img、line break</para>
    /// <para>必填：否</para>
    /// <para>**示例值**："text"</para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>相似问题</para>
    /// <para>必填：否</para>
    /// <para>**示例值**：["Similar question"]</para>
    /// </summary>
    [JsonPropertyName("tags")]
    public string[]? Tags { get; set; }
}
