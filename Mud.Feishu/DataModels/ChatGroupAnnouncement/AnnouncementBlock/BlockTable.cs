// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroupNotice;

/// <summary>
/// <para>表格 Block</para>
/// </summary>
public class BlockTable
{
    /// <summary>
    /// <para>单元格数组，数组元素为 Table Cell Block 的 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("cells")]
    public string[]? Cells { get; set; }

    /// <summary>
    /// <para>表格属性</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("property")]
    public TableProperty Property { get; set; } = new();

    /// <summary>
    /// <para>表格属性</para>
    /// </summary>
    public class TableProperty
    {
        /// <summary>
        /// <para>行数</para>
        /// <para>必填：是</para>
        /// <para>示例值：1</para>
        /// <para>最小值：1</para>
        /// </summary>
        [JsonPropertyName("row_size")]
        public int RowSize { get; set; }

        /// <summary>
        /// <para>列数</para>
        /// <para>必填：是</para>
        /// <para>示例值：1</para>
        /// <para>最小值：1</para>
        /// </summary>
        [JsonPropertyName("column_size")]
        public int ColumnSize { get; set; }

        /// <summary>
        /// <para>列宽，单位px</para>
        /// <para>必填：否</para>
        /// <para>示例值：100</para>
        /// <para>默认值：100</para>
        /// </summary>
        [JsonPropertyName("column_width")]
        public int[]? ColumnWidth { get; set; }

        /// <summary>
        /// <para>单元格合并信息</para>
        /// <para>必填：否</para>
        /// </summary>
        [JsonPropertyName("merge_info")]
        public TableMergeInfo[]? MergeInfos { get; set; }

        /// <summary>
        /// <para>单元格合并信息</para>
        /// </summary>
        public class TableMergeInfo
        {
            /// <summary>
            /// <para>从当前行索引起被合并的连续行数</para>
            /// <para>必填：否</para>
            /// <para>示例值：2</para>
            /// <para>最小值：1</para>
            /// </summary>
            [JsonPropertyName("row_span")]
            public int? RowSpan { get; set; }

            /// <summary>
            /// <para>从当前列索引起被合并的连续列数</para>
            /// <para>必填：否</para>
            /// <para>示例值：2</para>
            /// <para>最小值：1</para>
            /// </summary>
            [JsonPropertyName("col_span")]
            public int? ColSpan { get; set; }
        }

        /// <summary>
        /// <para>设置首行为标题行</para>
        /// <para>必填：否</para>
        /// <para>示例值：false</para>
        /// <para>默认值：false</para>
        /// </summary>
        [JsonPropertyName("header_row")]
        public bool? HeaderRow { get; set; }

        /// <summary>
        /// <para>设置首列为标题列</para>
        /// <para>必填：否</para>
        /// <para>示例值：false</para>
        /// <para>默认值：false</para>
        /// </summary>
        [JsonPropertyName("header_column")]
        public bool? HeaderColumn { get; set; }
    }
}
