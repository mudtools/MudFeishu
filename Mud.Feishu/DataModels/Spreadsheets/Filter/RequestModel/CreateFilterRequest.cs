// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;

/// <summary>
/// 创建筛选请求体
/// </summary>
public class CreateFilterRequest
{
    /// <summary>
    /// <para>设置筛选的应用范围。</para>
    /// <para>- `sheetId`：填写实际的工作表 ID，表示将筛选应用于整表</para>
    /// <para>- `sheetId!{开始行索引}:{结束行索引}` ：填写工作表 ID 和行数区间，表示将筛选应用于整行</para>
    /// <para>- `sheetId!{开始列索引}:{结束列索引}`：填写工作表 ID 和列的区间，表示将筛选应用于整列</para>
    /// <para>- `sheetId!{开始单元格}:{结束单元格}`：填写工作表 ID 和单元格区间，表示将筛选应用于单元格选定的区域中</para>
    /// <para>- `sheetId!{开始单元格}:{结束列索引}`：填写工作表 ID、起始单元格和结束列，表示省略结束行，使用表格的最后行作为结束行</para>
    /// <para>必填：是</para>
    /// <para>示例值：8fe9d6!A1:H14</para>
    /// </summary>
    [JsonPropertyName("range")]
    public string Range { get; set; } = string.Empty;

    /// <summary>
    /// <para>设置应用筛选条件的列。</para>
    /// <para>必填：是</para>
    /// <para>示例值：E</para>
    /// </summary>
    [JsonPropertyName("col")]
    public string Col { get; set; } = string.Empty;

    /// <summary>
    /// <para>设置筛选条件。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("condition")]
    public SheetFilterCondition Condition { get; set; } = new();

}