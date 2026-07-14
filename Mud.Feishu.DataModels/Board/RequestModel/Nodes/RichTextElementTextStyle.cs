// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Board;


/// <summary>
/// <para>文字样式</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Board")]
public class RichTextElementTextStyle
{
    /// <summary>
    /// <para>文字字重(可选值有：regular：常规, bold：加粗)</para>
    /// <para>必填：否</para>
    /// <para>示例值：bold</para>
    /// <para>最大长度：10</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("font_weight")]
    public string? FontWeight { get; set; }

    /// <summary>
    /// <para>文字大小，单位 px，默认为 14 px</para>
    /// <para>必填：否</para>
    /// <para>示例值：14</para>
    /// <para>最大值：1000</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("font_size")]
    public int? FontSize { get; set; }

    /// <summary>
    /// <para>文字颜色，16 进制 rgb 值</para>
    /// <para>必填：否</para>
    /// <para>示例值：#000000</para>
    /// <para>最大长度：7</para>
    /// <para>最小长度：7</para>
    /// </summary>
    [JsonPropertyName("text_color")]
    public string? TextColor { get; set; }

    /// <summary>
    /// <para>文字背景色，16 进制 rgb 值</para>
    /// <para>必填：否</para>
    /// <para>示例值：#000000</para>
    /// <para>最大长度：7</para>
    /// <para>最小长度：7</para>
    /// </summary>
    [JsonPropertyName("text_background_color")]
    public string? TextBackgroundColor { get; set; }

    /// <summary>
    /// <para>是否存在删除线</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("line_through")]
    public bool? LineThrough { get; set; }

    /// <summary>
    /// <para>是否存在下划线</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("underline")]
    public bool? Underline { get; set; }

    /// <summary>
    /// <para>是否斜体</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("italic")]
    public bool? Italic { get; set; }
}

