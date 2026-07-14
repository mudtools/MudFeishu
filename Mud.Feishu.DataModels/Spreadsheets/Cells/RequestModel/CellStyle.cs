// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;

/// <summary>单元格样式</summary>
[HttpJsonSerializable(SerializerClassName = "Spreadsheets")]
public class CellStyle
{
    /// <summary>
    /// <para>字体相关样式</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("font")]
    public CellFont? Font { get; set; }

    /// <summary>
    /// <para>文本的其它样式，可选值：</para>
    /// <para>- 0：默认样式，不加下划线和删除线</para>
    /// <para>- 1：下划线</para>
    /// <para>- 2：删除线</para>
    /// <para>- 3： 下划线和删除线</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("textDecoration")]
    public int? TextDecoration { get; set; }

    /// <summary>
    /// <para>数字格式，详见[电子表格支持的数字格式类型](https://open.feishu.cn/document/ukTMukTMukTM/uMjM2UjLzIjN14yMyYTN)。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("formatter")]
    public string? Formatter { get; set; }

    /// <summary>
    /// <para>水平对齐方式。可选值：</para>
    /// <para>- 0：左对齐</para>
    /// <para>- 1：中对齐</para>
    /// <para>- 2：右对齐</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("hAlign")]
    public int? HAlign { get; set; }

    /// <summary>
    /// <para>垂直对齐方式。可选值：</para>
    /// <para>- 0：上对齐</para>
    /// <para>- 1：中对齐</para>
    /// <para>- 2：下对齐</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("vAlign")]
    public int? VAlign { get; set; }

    /// <summary>
    /// <para>字体颜色，用十六进制颜色代码表示。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("foreColor")]
    public string? ForeColor { get; set; }

    /// <summary>
    /// <para>背景颜色，用十六进制颜色代码表示。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("backColor")]
    public string? BackColor { get; set; }

    /// <summary>
    /// <para>边框类型，可选值：</para>
    /// <para>- FULL_BORDER：全边框，即四周都有边框</para>
    /// <para>- OUTER_BORDER：外边框，只有外侧有边框</para>
    /// <para>- INNER_BORDER：内边框，只有内部有边框</para>
    /// <para>- NO_BORDER：无边框，即没有任何边框</para>
    /// <para>- LEFT_BORDER：左边框，只有左侧有边框</para>
    /// <para>- RIGHT_BORDER：右边框，只有右侧有边框</para>
    /// <para>- TOP_BORDER：上边框，只有顶部有边框</para>
    /// <para>- BOTTOM_BORDER：下边框，只有底部有边框</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("borderType")]
    public string? BorderType { get; set; }

    /// <summary>
    /// <para>边框颜色，用十六进制颜色代码表示。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("borderColor")]
    public string? BorderColor { get; set; }

    /// <summary>
    /// <para>是否清除所有格式。默认值为 false。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("clean")]
    public bool? Clean { get; set; }
}
