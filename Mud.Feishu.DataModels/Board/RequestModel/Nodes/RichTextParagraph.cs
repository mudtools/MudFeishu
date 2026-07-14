// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Board;

/// <summary>
/// <para>段落列表</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Board")]
public class RichTextParagraph
{
    /// <summary>
    /// <para>段落类别</para>
    /// <para>必填：是</para>
    /// <para>示例值：0</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：普通段落</item>
    /// <item>1：无序列表</item>
    /// <item>2：有序列表</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("paragraph_type")]
    public int ParagraphType { get; set; }

    /// <summary>
    /// <para>段落的元素列表</para>
    /// <para>必填：否</para>
    /// <para>最大长度：1000</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("elements")]
    public RichTextElement[]? Elements { get; set; }


    /// <summary>
    /// <para>缩进（单位：字符）</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// <para>最大值：100</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("indent")]
    public int? Indent { get; set; }

    /// <summary>
    /// <para>有序列表开始序号(第一个有序列表的序号为list_begin_index+1)</para>
    /// <para>例如：list_begin_index = 0， 则第一个有序列表的序号为1</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// <para>最大值：10000</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("list_begin_index")]
    public int? ListBeginIndex { get; set; }

    /// <summary>
    /// <para>引用</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("quote")]
    public bool? Quote { get; set; }
}
