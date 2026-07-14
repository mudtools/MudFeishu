// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;


/// <summary>
/// <para>合并单元格的相关信息。没有合并单元格则不返回。</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Spreadsheets")]
public class SheetMergeRange
{
    /// <summary>
    /// <para>起始行，从 0 开始计数</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("start_row_index")]
    public int? StartRowIndex { get; set; }

    /// <summary>
    /// <para>结束行，从 0 开始计数</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("end_row_index")]
    public int? EndRowIndex { get; set; }

    /// <summary>
    /// <para>起始列，从 0 开始计数。</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("start_column_index")]
    public int? StartColumnIndex { get; set; }

    /// <summary>
    /// <para>结束列，从 0 开始计数。</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("end_column_index")]
    public int? EndColumnIndex { get; set; }
}
