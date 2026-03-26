// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;


/// <summary>
/// <para>移动源位置信息</para>
/// </summary>
public class MoveRangeDimension
{
    /// <summary>
    /// <para>移动的维度。可选值：</para>
    /// <para>- `ROWS`：行</para>
    /// <para>- `COLUMNS`：列</para>
    /// <para>必填：否</para>
    /// <para>示例值：ROWS</para>
    /// </summary>
    [JsonPropertyName("major_dimension")]
    public string? MajorDimension { get; set; }

    /// <summary>
    /// <para>要移动的行或列的起始位置。从 0 开始计数。若 `startIndex` 为 3，则从第 4 行或列开始移动。包含第 4 行或列。</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("start_index")]
    public int? StartIndex { get; set; }

    /// <summary>
    /// <para>要移动的行或列结束的位置。从 0 开始计数。若 `endIndex` 为 7，则要移动的范围至第 8 行或列结束。包含第 8 行或列。</para>
    /// <para>示例：当 `majorDimension`为 `ROWS`、 `startIndex` 为 3、`endIndex ` 为 7 时，则移动第 4、5、6、7、8 行，共 5 行。</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("end_index")]
    public int? EndIndex { get; set; }
}