// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Slides;

/// <summary>
/// 创建演示文稿响应体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Slides")]
public class CreateXmlPresentationResult
{
    /// <summary>
    /// <para>演示文稿唯一标识符（后续所有页面、历史接口都用该值，而非 XML 内的 presentation_id）</para>
    /// <para>必填：否</para>
    /// <para>示例值：zTqAwsEb4clrjOLd3drAcNZabcef</para>
    /// </summary>
    [JsonPropertyName("xml_presentation_id")]
    public string? XmlPresentationId { get; set; }

    /// <summary>
    /// <para>页面 id 列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("slide_ids")]
    public string[]? SlideIds { get; set; }

    /// <summary>
    /// <para>版本号 id</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("revision_id")]
    public long? RevisionId { get; set; }

    /// <summary>
    /// <para>幻灯片地址</para>
    /// <para>必填：否</para>
    /// <para>示例值：xxx/slides/xxx</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>XML 语法问题（内容被接受但被服务端改写时返回，非空即说明产物与入参不完全一致）</para>
    /// <para>必填：否</para>
    /// <para>示例值：XML sanitize issues: </para>
    /// </summary>
    [JsonPropertyName("issues")]
    public string? Issues { get; set; }
}
