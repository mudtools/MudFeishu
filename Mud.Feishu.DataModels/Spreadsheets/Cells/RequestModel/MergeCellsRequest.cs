// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;

/// <summary>
/// 合并单元格请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Spreadsheets")]
public class MergeCellsRequest
{
    /// <summary>
    /// <para>要合并的单元格的范围，格式为 `&lt;sheetId&gt;!&lt;开始位置&gt;:&lt;结束位置&gt;`。其中：</para>
    /// <para>- `sheetId` 为工作表 ID。</para>
    /// <para>- `&lt;开始位置&gt;:&lt;结束位置&gt;` 为工作表中单元格的范围，数字表示行索引，字母表示列索引。如 `A2:B2` 表示该工作表第 2 行的 A 列到 B 列。`range`支持四种写法，详情参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uATMzUjLwEzM14CMxMTN/overview"> 电子表格概述</see>]。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("range")]
    public string Range { get; set; } = string.Empty;

    /// <summary>
    /// <para>指定合并单元格的方式。可选值：</para>
    /// <para>- MERGE_ALL：合并所有单元格，即将选定区域内的所有单元格合并成一个单元格</para>
    /// <para>- MERGE_ROWS：按行合并，即在选定的区域内，将同一行相邻的单元格合并成一个单元格</para>
    /// <para>- MERGE_COLUMNS：按列合并，即在选定的区域内，将同一列中相邻的单元格合并成一个单元格</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("mergeType")]
    public string MergeType { get; set; } = string.Empty;
}
