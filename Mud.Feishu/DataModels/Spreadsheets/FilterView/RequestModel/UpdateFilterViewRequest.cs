// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;

/// <summary>
/// <para>更新筛选视图请求体</para>
/// </summary>
public class UpdateFilterViewRequest
{
    /// <summary>
    /// <para>筛选视图名称。长度不得超过 100 个字符，且在工作表内必须唯一。</para>
    /// <para>必填：否</para>
    /// <para>示例值：筛选视图 1</para>
    /// </summary>
    [JsonPropertyName("filter_view_name")]
    public string? FilterViewName { get; set; }

    /// <summary>
    /// <para>筛选视图的筛选范围。</para>
    /// <para>- sheetId：填写实际的工作表 ID，表示将筛选应用于整表</para>
    /// <para>- sheetId!1:2 ：填写工作表 ID 和行数区间，表示将筛选应用于整行</para>
    /// <para>- sheetId!A:B ：填写工作表 ID 和列的区间，表示将筛选应用于整列</para>
    /// <para>- sheetId!A1:B2 ：填写工作表 ID 和单元格区间，表示将筛选应用于单元格选定的区域中</para>
    /// <para>- sheetId!A1:C ：填写工作表 ID、起始单元格和结束列，表示省略结束行，使用表格的最后行作为结束行</para>
    /// <para>必填：否</para>
    /// <para>示例值：8fe9d6!C1:H14</para>
    /// </summary>
    [JsonPropertyName("range")]
    public string? Range { get; set; }
}