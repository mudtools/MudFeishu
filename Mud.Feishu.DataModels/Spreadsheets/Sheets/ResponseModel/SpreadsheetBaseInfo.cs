// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;

/// <summary>
/// 电子表格的基础信息
/// </summary>
public class SpreadsheetBaseInfo
{
    /// <summary>
    /// <para>电子表格标题</para>
    /// <para>必填：否</para>
    /// <para>示例值：Sales sheet</para>
    /// <para>最大长度：255</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>文件夹 token</para>
    /// <para>必填：否</para>
    /// <para>示例值：fldbcO1UuPz8VwnpPx5a92abcef</para>
    /// </summary>
    [JsonPropertyName("folder_token")]
    public string? FolderToken { get; set; }

    /// <summary>
    /// <para>电子表格的 URL 链接</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://example.feishu.cn/sheets/Iow7sNNEphp3WbtnbCscPqabcef</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>电子表格 token</para>
    /// <para>必填：否</para>
    /// <para>示例值：Iow7sNNEphp3WbtnbCscPqabcef</para>
    /// </summary>
    [JsonPropertyName("spreadsheet_token")]
    public string? SpreadsheetToken { get; set; }
}
