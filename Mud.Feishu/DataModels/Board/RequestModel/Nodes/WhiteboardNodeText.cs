// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Board;

/// <summary>
/// <para>文字</para>
/// </summary>
public class WhiteboardNodeText
{
    /// <summary>
    /// <para>文字内容</para>
    /// <para>必填：否</para>
    /// <para>示例值：文字内容</para>
    /// <para>最大长度：1024</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// <para>文字字重</para>
    /// <para>必填：否</para>
    /// <para>示例值：regular</para>
    /// <para>可选值：<list type="bullet">
    /// <item>regular：常规</item>
    /// <item>bold：加粗</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("font_weight")]
    public string? FontWeight { get; set; }

    /// <summary>
    /// <para>文字大小，单位 px，默认为 14 px</para>
    /// <para>必填：否</para>
    /// <para>示例值：14</para>
    /// </summary>
    [JsonPropertyName("font_size")]
    public int? FontSize { get; set; }

    /// <summary>
    /// <para>水平对齐</para>
    /// <para>必填：否</para>
    /// <para>示例值：center</para>
    /// <para>可选值：<list type="bullet">
    /// <item>left：向左对齐</item>
    /// <item>center：居中对齐</item>
    /// <item>right：向右对齐</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("horizontal_align")]
    public string? HorizontalAlign { get; set; }

    /// <summary>
    /// <para>垂直对齐</para>
    /// <para>必填：否</para>
    /// <para>示例值：mid</para>
    /// <para>可选值：<list type="bullet">
    /// <item>top：顶部对齐</item>
    /// <item>mid：垂直居中</item>
    /// <item>bottom：底部对齐</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("vertical_align")]
    public string? VerticalAlign { get; set; }

    /// <summary>
    /// <para>文字颜色，16 进制 rgb 值</para>
    /// <para>必填：否</para>
    /// <para>示例值：#6db5a3</para>
    /// <para>最大长度：10</para>
    /// <para>最小长度：7</para>
    /// </summary>
    [JsonPropertyName("text_color")]
    public string? TextColor { get; set; }

    /// <summary>
    /// <para>文字背景色，16 进制 rgb 值</para>
    /// <para>必填：否</para>
    /// <para>示例值：#6db5a3</para>
    /// <para>最大长度：10</para>
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
    /// <para>示例值：true</para>
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

    /// <summary>
    /// <para>文字旋转角度，单位度</para>
    /// <para>必填：否</para>
    /// <para>示例值：90</para>
    /// <para>最大值：270</para>
    /// <para>最小值：0</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：文字旋转角度0度</item>
    /// <item>90：文字旋转角度90度</item>
    /// <item>180：文字旋转角度180度</item>
    /// <item>270：文字旋转角度270度</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("angle")]
    public int? Angle { get; set; }

    /// <summary>
    /// <para>文字颜色主题配色编码值</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// <para>最大值：100</para>
    /// <para>最小值：-2</para>
    /// </summary>
    [JsonPropertyName("theme_text_color_code")]
    public int? ThemeTextColorCode { get; set; }

    /// <summary>
    /// <para>文字背景颜色主题配色编码值</para>
    /// <para>必填：否</para>
    /// <para>示例值：-1</para>
    /// <para>最大值：100</para>
    /// <para>最小值：-2</para>
    /// </summary>
    [JsonPropertyName("theme_text_background_color_code")]
    public int? ThemeTextBackgroundColorCode { get; set; }

    /// <summary>
    /// <para>富文本（富文本有值时候会覆盖上面的text信息）</para>
    /// <para>如果整段文本只有一个样式，不推荐使用富文本</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("rich_text")]
    public WhiteboardNodeRichText? RichText { get; set; }

    /// <summary>
    /// <para>文字颜色类型，0=系统颜色，1=自定义颜色</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：系统颜色</item>
    /// <item>1：自定义颜色</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("text_color_type")]
    public int? TextColorType { get; set; }

    /// <summary>
    /// <para>文字背景颜色类型，0=系统颜色，1=自定义颜色</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：系统颜色</item>
    /// <item>1：自定义颜色</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("text_background_color_type")]
    public int? TextBackgroundColorType { get; set; }
}