// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Board;


/// <summary>
/// <para>样式</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Board")]
public class WhiteboardNodeStyle
{
    /// <summary>
    /// <para>填充颜色，16 进制 rbg 值</para>
    /// <para>必填：否</para>
    /// <para>示例值：#6db5a3</para>
    /// <para>最大长度：10</para>
    /// <para>最小长度：7</para>
    /// </summary>
    [JsonPropertyName("fill_color")]
    public string? FillColor { get; set; }

    /// <summary>
    /// <para>填充透明度，百分比</para>
    /// <para>必填：否</para>
    /// <para>示例值：50</para>
    /// <para>最大值：100</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("fill_opacity")]
    public float? FillOpacity { get; set; }

    /// <summary>
    /// <para>边框样式</para>
    /// <para>必填：否</para>
    /// <para>示例值：solid</para>
    /// <para>可选值：<list type="bullet">
    /// <item>solid：实线</item>
    /// <item>none：无边框</item>
    /// <item>dash：虚线</item>
    /// <item>dot：点状虚线</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("border_style")]
    public string? BorderStyle { get; set; }

    /// <summary>
    /// <para>边框宽度</para>
    /// <para>必填：否</para>
    /// <para>示例值：narrow</para>
    /// <para>可选值：<list type="bullet">
    /// <item>extra_narrow：极细</item>
    /// <item>narrow：细</item>
    /// <item>medium：中</item>
    /// <item>bold：粗</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("border_width")]
    public string? BorderWidth { get; set; }

    /// <summary>
    /// <para>边框透明度，百分比</para>
    /// <para>必填：否</para>
    /// <para>示例值：50</para>
    /// <para>最大值：100</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("border_opacity")]
    public float? BorderOpacity { get; set; }

    /// <summary>
    /// <para>水平翻折</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("h_flip")]
    public bool? HFlip { get; set; }

    /// <summary>
    /// <para>垂直翻折</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("v_flip")]
    public bool? VFlip { get; set; }

    /// <summary>
    /// <para>边框颜色，16 进制 rgb 值</para>
    /// <para>必填：否</para>
    /// <para>示例值：#6db5a3</para>
    /// <para>最大长度：10</para>
    /// <para>最小长度：7</para>
    /// </summary>
    [JsonPropertyName("border_color")]
    public string? BorderColor { get; set; }

    /// <summary>
    /// <para>填充颜色主题配色编码值</para>
    /// <para>必填：否</para>
    /// <para>示例值：3</para>
    /// <para>最大值：100</para>
    /// <para>最小值：-2</para>
    /// </summary>
    [JsonPropertyName("theme_fill_color_code")]
    public int? ThemeFillColorCode { get; set; }

    /// <summary>
    /// <para>边框颜色主题配色编码值</para>
    /// <para>必填：否</para>
    /// <para>示例值：4</para>
    /// <para>最大值：100</para>
    /// <para>最小值：-2</para>
    /// </summary>
    [JsonPropertyName("theme_border_color_code")]
    public int? ThemeBorderColorCode { get; set; }

    /// <summary>
    /// <para>填充颜色类型：0=系统颜色，取theme_fill_color_code，1=自定义颜色，取fill_color</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：系统颜色</item>
    /// <item>1：自定义颜色</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("fill_color_type")]
    public int? FillColorType { get; set; }

    /// <summary>
    /// <para>边框颜色类型：0=系统颜色，取theme_border_color_code，1=自定义颜色，取border_color</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：系统颜色</item>
    /// <item>1：自定义颜色</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("border_color_type")]
    public int? BorderColorType { get; set; }

}
