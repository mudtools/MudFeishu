// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Board;

/// <summary>
/// <para>单元格列表</para>
/// </summary>
public class TableCell
{
    /// <summary>
    /// <para>行下标，从 1 开始</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// <para>最大值：10000</para>
    /// <para>最小值：1</para>
    /// </summary>
    [JsonPropertyName("row_index")]
    public int RowIndex { get; set; }

    /// <summary>
    /// <para>列下标，从 1 开始</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// <para>最大值：10000</para>
    /// <para>最小值：1</para>
    /// </summary>
    [JsonPropertyName("col_index")]
    public int ColIndex { get; set; }

    /// <summary>
    /// <para>单元格合并信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("merge_info")]
    public TableCellMergeInfo? MergeInfo { get; set; }



    /// <summary>
    /// <para>单元格包含的子节点 id</para>
    /// <para>必填：否</para>
    /// <para>最大长度：3000</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("children")]
    public string[]? Children { get; set; }

    /// <summary>
    /// <para>单元格内文字</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("text")]
    public WhiteboardNodeText? Text { get; set; }

    /// <summary>
    /// <para>单元格样式，设置后会覆盖表格样式</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("style")]
    public WhiteboardNodeStyle? Style { get; set; }
}