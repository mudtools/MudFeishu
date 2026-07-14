// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive.Files;

/// <summary>
/// 创建导出任务请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Drive")]
public class ExportTasksRequest
{
    /// <summary>
    /// <para>将云文档导出为本地文件后，本地文件的扩展名。了解各类云文档支持导出的文件格式，参考[导出云文档概述](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/drive-v1/export_task/export-user-guide)。</para>
    /// <para>必填：是</para>
    /// <para>示例值：csv</para>
    /// <para>可选值：<list type="bullet">
    /// <item>docx：Microsoft Word 格式</item>
    /// <item>pdf：PDF 格式</item>
    /// <item>xlsx：Microsoft Excel 格式</item>
    /// <item>csv：CSV 格式</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("file_extension")]
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>
    /// <para>要导出的云文档的 token。获取方式参考 [如何获取云文档相关 token](https://open.feishu.cn/document/ukTMukTMukTM/uczNzUjL3czM14yN3MTN#08bb5df6)。</para>
    /// <para>必填：是</para>
    /// <para>示例值：Fm7osyjtMh5o7Ktrv32c73abcef</para>
    /// <para>最大长度：27</para>
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// <para>要导出的云文档的类型 。可通过云文档的链接判断。</para>
    /// <para>必填：是</para>
    /// <para>示例值：sheet</para>
    /// <para>可选值：<list type="bullet">
    /// <item>doc：旧版飞书文档。支持导出扩展名为 docx 和 pdf 的文件。已不推荐使用。</item>
    /// <item>sheet：飞书电子表格。支持导出扩展名为 xlsx 和 csv 的文件。</item>
    /// <item>bitable：飞书多维表格。支持导出扩展名为 xlsx 和 csv 格式的文件。</item>
    /// <item>docx：新版飞书文档。支持导出扩展名为 docx 和 pdf 格式的文件。</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// <para>导出飞书电子表格或多维表格为 CSV 文件时，需传入电子表格工作表的 ID 或多维表格数据表的 ID：</para>
    /// <para>- 电子表格可调用[获取工作表](https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/sheets-v3/spreadsheet-sheet/query) 接口获取返回的 `sheet_id` 的值作为该参数的值</para>
    /// <para>- 多维表格可调用[列出数据表](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app-table/list)接口获取返回的 `table_id` 的值作为该参数的值</para>
    /// <para>必填：否</para>
    /// <para>示例值：6e5ed3</para>
    /// </summary>
    [JsonPropertyName("sub_id")]
    public string? SubId { get; set; }
}
