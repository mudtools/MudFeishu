// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;


/// <summary>
/// <para>单元格属性，仅当 `resource_type` 为 `sheet` 即工作表类型为电子表格时返回。</para>
/// </summary>
public class SheetGridProperties
{
    /// <summary>
    /// <para>冻结的行数量</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("frozen_row_count")]
    public int? FrozenRowCount { get; set; }

    /// <summary>
    /// <para>冻结的列数量</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("frozen_column_count")]
    public int? FrozenColumnCount { get; set; }

    /// <summary>
    /// <para>工作表的行数</para>
    /// <para>必填：否</para>
    /// <para>示例值：200</para>
    /// </summary>
    [JsonPropertyName("row_count")]
    public int? RowCount { get; set; }

    /// <summary>
    /// <para>工作表的列数量</para>
    /// <para>必填：否</para>
    /// <para>示例值：20</para>
    /// </summary>
    [JsonPropertyName("column_count")]
    public int? ColumnCount { get; set; }
}