// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;

/// <summary>行列的维度信息</summary>
public class SheetRangeDimension
{
    /// <summary>
    /// <para>电子表格工作表的 ID。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("sheetId")]
    public string SheetId { get; set; } = string.Empty;

    /// <summary>
    /// <para>要更新的维度。可选值：</para>
    /// <para>- `ROWS`：行</para>
    /// <para>- `COLUMNS`：列</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("majorDimension")]
    public string MajorDimension { get; set; } = string.Empty;

    /// <summary>
    /// <para>插入的行或列的起始位置。从 0 开始计数。若 `startIndex` 为 3，则从第 4 行或列开始插入空行或列。包含第 4 行或列。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("startIndex")]
    public int StartIndex { get; set; }

    /// <summary>
    /// <para>插入的行或列结束的位置。从 0 开始计数。若 `endIndex` 为 7，则从第 8 行结束插入行。第 8 行不再插入空行。</para>
    /// <para>示例：当 `majorDimension`为 `ROWS`、 `startIndex` 为 3、`endIndex ` 为 7 时，则在第 4、5、6、7 行插入空白行，共插入 4 行。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("endIndex")]
    public int EndIndex { get; set; }
}