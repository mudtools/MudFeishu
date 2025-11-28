// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroupNotice;

/// <summary>
/// 批量更新群公告块的内容请求体
/// </summary>
public class BlocksBatchUpdateRequest
{
    /// <summary>
    /// <para>批量更新 Block</para>
    /// <para>必填：否</para>
    /// <para>最大长度：200</para>
    /// </summary>
    [JsonPropertyName("requests")]
    public UpdateBlockInfo[]? Requests { get; set; }

}


/// <summary>
/// <para>批量更新 Block</para>
/// </summary>
public class UpdateBlockInfo
{
    /// <summary>
    /// <para>更新文本元素请求</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("update_text_elements")]
    public UpdateTextElementsInfo? UpdateTextElements { get; set; }


    /// <summary>
    /// <para>更新文本样式请求</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("update_text_style")]
    public UpdateTextStyleInfo? UpdateTextStyle { get; set; }




    /// <summary>
    /// <para>更新表格属性请求</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("update_table_property")]
    public UpdateTablePropertyInfo? UpdateTableProperty { get; set; }


    /// <summary>
    /// <para>表格插入新行请求</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("insert_table_row")]
    public InsertTableRowInfo? InsertTableRow { get; set; }


    /// <summary>
    /// <para>表格插入新列请求</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("insert_table_column")]
    public InsertTableColumnInfo? InsertTableColumn { get; set; }


    /// <summary>
    /// <para>表格批量删除行请求</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("delete_table_rows")]
    public DeleteTableRowsInfo? DeleteTableRows { get; set; }


    /// <summary>
    /// <para>表格批量删除列请求</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("delete_table_columns")]
    public DeleteTableColumnsInfo? DeleteTableColumns { get; set; }


    /// <summary>
    /// <para>表格合并单元格请求</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("merge_table_cells")]
    public MergeTableCellsInfo? MergeTableCells { get; set; }



    /// <summary>
    /// <para>表格取消单元格合并状态请求</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("unmerge_table_cells")]
    public UnmergeTableCellsInfo? UnmergeTableCells { get; set; }



    /// <summary>
    /// <para>分栏插入新的分栏列请求</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("insert_grid_column")]
    public InsertGridColumnInfo? InsertGridColumn { get; set; }


    /// <summary>
    /// <para>分栏删除列请求</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("delete_grid_column")]
    public DeleteGridColumnInfo? DeleteGridColumn { get; set; }


    /// <summary>
    /// <para>更新分栏列宽比例请求</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("update_grid_column_width_ratio")]
    public UpdateGridColumnWidthRatioInfo? UpdateGridColumnWidthRatio { get; set; }



    /// <summary>
    /// <para>替换图片请求</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("replace_image")]
    public ReplaceImageInfo? ReplaceImage { get; set; }


    /// <summary>
    /// <para>替换附件请求</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("replace_file")]
    public ReplaceFileInfo? ReplaceFile { get; set; }


    /// <summary>
    /// <para>Block 唯一标识</para>
    /// <para>必填：否</para>
    /// <para>示例值：doxcnSS4ouQkQEouGSUkTg9NJPe</para>
    /// </summary>
    [JsonPropertyName("block_id")]
    public string? BlockId { get; set; }

    /// <summary>
    /// <para>更新文本元素及样式请求</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("update_text")]
    public UpdateTextInfo? UpdateText { get; set; }

    /// <summary>
    /// <para>更新任务 Block 请求</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("update_task")]
    public UpdateTaskInfo? UpdateTask { get; set; }

}


/// <summary>
/// <para>更新文本元素请求</para>
/// </summary>
public class UpdateTextElementsInfo
{
    /// <summary>
    /// <para>更新的文本元素列表，单次更新中 reminder 上限 30 个，mention_doc 上限 50 个，mention_user 上限 100 个</para>
    /// <para>必填：是</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("elements")]
    public TextElement[] Elements { get; set; } = Array.Empty<TextElement>();
}

/// <summary>
/// <para>更新文本样式请求</para>
/// </summary>
public class UpdateTextStyleInfo
{
    /// <summary>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("style")]
    public TextStyle Style { get; set; } = new();

    /// <summary>
    /// <para>应更新的字段，必须至少指定一个字段。例如，要调整 Block 对齐方式，请设置 fields 为 [1]。</para>
    /// <para>必填：是</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：修改 Block 的对齐方式</item>
    /// <item>2：修改 todo block 的完成状态</item>
    /// <item>3：修改 block 的折叠状态</item>
    /// <item>4：修改代码块的语言类型</item>
    /// <item>5：修改代码块的折叠状态</item>
    /// <item>6：块背景色（rgb|rgba 格式）</item>
    /// <item>7：首段缩进级别</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("fields")]
    public int[] Fields { get; set; } = Array.Empty<int>();
}

/// <summary>
/// <para>更新表格属性请求</para>
/// </summary>
public class UpdateTablePropertyInfo
{
    /// <summary>
    /// <para>表格列宽</para>
    /// <para>必填：否</para>
    /// <para>示例值：100</para>
    /// <para>最小值：50</para>
    /// </summary>
    [JsonPropertyName("column_width")]
    public int? ColumnWidth { get; set; }

    /// <summary>
    /// <para>需要修改列宽的表格列的索引（修改表格列宽时必填）</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("column_index")]
    public int? ColumnIndex { get; set; }

    /// <summary>
    /// <para>设置首行为标题行</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("header_row")]
    public bool? HeaderRow { get; set; }

    /// <summary>
    /// <para>设置首列为标题列</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("header_column")]
    public bool? HeaderColumn { get; set; }
}

/// <summary>
/// <para>表格插入新行请求</para>
/// </summary>
public class InsertTableRowInfo
{
    /// <summary>
    /// <para>插入的行在表格中的索引。（-1表示在表格末尾插入一行）</para>
    /// <para>必填：是</para>
    /// <para>示例值：-1</para>
    /// <para>最小值：-1</para>
    /// </summary>
    [JsonPropertyName("row_index")]
    public int RowIndex { get; set; }
}

/// <summary>
/// <para>表格插入新列请求</para>
/// </summary>
public class InsertTableColumnInfo
{
    /// <summary>
    /// <para>插入的列在表格中的索引。（-1表示在表格末尾插入一列）</para>
    /// <para>必填：是</para>
    /// <para>示例值：-1</para>
    /// <para>最小值：-1</para>
    /// </summary>
    [JsonPropertyName("column_index")]
    public int ColumnIndex { get; set; }
}

/// <summary>
/// <para>表格批量删除行请求</para>
/// </summary>
public class DeleteTableRowsInfo
{
    /// <summary>
    /// <para>行开始索引（区间左闭右开）</para>
    /// <para>必填：是</para>
    /// <para>示例值：0</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("row_start_index")]
    public int RowStartIndex { get; set; }

    /// <summary>
    /// <para>行结束索引（区间左闭右开）</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// <para>最小值：1</para>
    /// </summary>
    [JsonPropertyName("row_end_index")]
    public int RowEndIndex { get; set; }
}

/// <summary>
/// <para>表格批量删除列请求</para>
/// </summary>
public class DeleteTableColumnsInfo
{
    /// <summary>
    /// <para>列开始索引（区间左闭右开）</para>
    /// <para>必填：是</para>
    /// <para>示例值：0</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("column_start_index")]
    public int ColumnStartIndex { get; set; }

    /// <summary>
    /// <para>列结束索引（区间左闭右开）</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// <para>最小值：1</para>
    /// </summary>
    [JsonPropertyName("column_end_index")]
    public int ColumnEndIndex { get; set; }
}
/// <summary>
/// <para>表格合并单元格请求</para>
/// </summary>
public class MergeTableCellsInfo
{
    /// <summary>
    /// <para>行起始索引（区间左闭右开）</para>
    /// <para>必填：是</para>
    /// <para>示例值：0</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("row_start_index")]
    public int RowStartIndex { get; set; }

    /// <summary>
    /// <para>行结束索引（区间左闭右开）</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// <para>最小值：1</para>
    /// </summary>
    [JsonPropertyName("row_end_index")]
    public int RowEndIndex { get; set; }

    /// <summary>
    /// <para>列起始索引（区间左闭右开）</para>
    /// <para>必填：是</para>
    /// <para>示例值：0</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("column_start_index")]
    public int ColumnStartIndex { get; set; }

    /// <summary>
    /// <para>列结束索引（区间左闭右开）</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// <para>最小值：1</para>
    /// </summary>
    [JsonPropertyName("column_end_index")]
    public int ColumnEndIndex { get; set; }
}

/// <summary>
/// <para>表格取消单元格合并状态请求</para>
/// </summary>
public class UnmergeTableCellsInfo
{
    /// <summary>
    /// <para>table 行索引</para>
    /// <para>必填：是</para>
    /// <para>示例值：0</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("row_index")]
    public int RowIndex { get; set; }

    /// <summary>
    /// <para>table 列索引</para>
    /// <para>必填：是</para>
    /// <para>示例值：0</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("column_index")]
    public int ColumnIndex { get; set; }
}

/// <summary>
/// <para>分栏删除列请求</para>
/// </summary>
public class DeleteGridColumnInfo
{
    /// <summary>
    /// <para>删除列索引，从 0 开始，如 0 表示删除第一列（-1表示删除最后一列）</para>
    /// <para>必填：是</para>
    /// <para>示例值：0</para>
    /// <para>最小值：-1</para>
    /// </summary>
    [JsonPropertyName("column_index")]
    public int ColumnIndex { get; set; }
}

/// <summary>
/// <para>更新分栏列宽比例请求</para>
/// </summary>
public class UpdateGridColumnWidthRatioInfo
{
    /// <summary>
    /// <para>更新列宽比例时，需要传入所有列宽占比</para>
    /// <para>必填：是</para>
    /// <para>示例值：50</para>
    /// <para>最大长度：99</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("width_ratios")]
    public int[] WidthRatios { get; set; } = Array.Empty<int>();
}

/// <summary>
/// <para>分栏插入新的分栏列请求</para>
/// </summary>
public class InsertGridColumnInfo
{
    /// <summary>
    /// <para>插入列索引，从 1 开始，如 1 表示在第一列后插入，注意不允许传 0（-1表示在最后一列后插入）</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// <para>最小值：-1</para>
    /// </summary>
    [JsonPropertyName("column_index")]
    public int ColumnIndex { get; set; }
}

/// <summary>
/// <para>替换图片请求</para>
/// </summary>
public class ReplaceImageInfo
{
    /// <summary>
    /// <para>图片 token</para>
    /// <para>必填：是</para>
    /// <para>示例值：boxbcVA91JtFgNhaCgy6s6wK4he</para>
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// <para>图片宽度，单位 px</para>
    /// <para>必填：否</para>
    /// <para>示例值：100</para>
    /// </summary>
    [JsonPropertyName("width")]
    public int? Width { get; set; }

    /// <summary>
    /// <para>图片高度，单位 px</para>
    /// <para>必填：否</para>
    /// <para>示例值：100</para>
    /// </summary>
    [JsonPropertyName("height")]
    public int? Height { get; set; }

    /// <summary>
    /// <para>对齐方式</para>
    /// <para>必填：否</para>
    /// <para>示例值：2</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：居左排版</item>
    /// <item>2：居中排版</item>
    /// <item>3：居右排版</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("align")]
    public int? Align { get; set; }
}

/// <summary>
/// <para>替换附件请求</para>
/// </summary>
public class ReplaceFileInfo
{
    /// <summary>
    /// <para>附件 token</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// <para>更新任务 Block 请求</para>
/// </summary>
public class UpdateTaskInfo
{
    /// <summary>
    /// <para>任务 ID。该字段仅在首次更新 Task Block 时生效，更新成功后，后续请求中将忽略该字段。</para>
    /// <para>必填：否</para>
    /// <para>示例值：ba5040f4-8116-4042-ab3c-254e5cfe3ce7</para>
    /// </summary>
    [JsonPropertyName("task_id")]
    public string? TaskId { get; set; }

    /// <summary>
    /// <para>折叠状态，字段为空时不更新折叠状态</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("folded")]
    public bool? Folded { get; set; }
}

/// <summary>
/// <para>更新文本元素及样式请求</para>
/// </summary>
public class UpdateTextInfo
{
    /// <summary>
    /// <para>更新的文本元素列表，单次更新中 reminder 上限 30 个，mention_doc 上限 50 个，mention_user 上限 100 个</para>
    /// <para>必填：是</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("elements")]
    public TextElement[] Elements { get; set; } = Array.Empty<TextElement>();

    /// <summary>
    /// <para>更新的文本样式</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("style")]
    public TextStyle Style { get; set; } = new();

    /// <summary>
    /// <para>文本样式中应更新的字段，必须至少指定一个字段。例如，要调整 Block 对齐方式，请设置 fields 为 [1]。</para>
    /// <para>必填：是</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：修改 Block 的对齐方式</item>
    /// <item>2：修改 todo block 的完成状态</item>
    /// <item>3：修改 block 的折叠状态</item>
    /// <item>4：修改代码块的语言类型</item>
    /// <item>5：修改代码块的折叠状态</item>
    /// <item>6：块背景色（rgb|rgba 格式）</item>
    /// <item>7：首段缩进级别</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("fields")]
    public int[] Fields { get; set; } = Array.Empty<int>();
}
