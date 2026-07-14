// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;

/// <summary>字体相关样式</summary>
[HttpJsonSerializable(SerializerClassName = "Spreadsheets")]
public class CellFont
{
    /// <summary>
    /// <para>是否加粗。默认值 false。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("bold")]
    public bool? Bold { get; set; }

    /// <summary>
    /// <para>是否斜体。默认值 false。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("italic")]
    public bool? Italic { get; set; }

    /// <summary>
    /// <para>字体大小，如 10pt/1.5。其中 10pt 表示字号，取值范围为 [9,36]pt。1.5 为行距，固定为 1.5px。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("fontSize")]
    public string? FontSize { get; set; }

    /// <summary>
    /// <para>是否清除字体格式，默认为 false。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("clean")]
    public bool? Clean { get; set; }
}
