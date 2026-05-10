// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive.Files;

/// <summary>
/// <para>导出任务结果</para>
/// </summary>
public class ExportTaskInfo
{
    /// <summary>
    /// <para>导出的文件的扩展名</para>
    /// <para>必填：是</para>
    /// <para>示例值：pdf</para>
    /// <para>可选值：<list type="bullet">
    /// <item>docx：Microsoft Word 格式</item>
    /// <item>pdf：PDF 格式</item>
    /// <item>xlsx：Microsoft Excel (XLSX) 格式</item>
    /// <item>csv：CSV 格式</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("file_extension")]
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>
    /// <para>要导出的云文档的类型。可通过云文档的链接判断。</para>
    /// <para>必填：是</para>
    /// <para>示例值：doc</para>
    /// <para>可选值：<list type="bullet">
    /// <item>doc：旧版飞书文档。支持导出扩展名为 docx 和 pdf 的文件。已不推荐使用。</item>
    /// <item>sheet：飞书电子表格。支持导出扩展名为 xlsx 和 csv 的文件</item>
    /// <item>bitable：飞书多维表格。支持导出扩展名为 xlsx 和 csv 格式的文件</item>
    /// <item>docx：新版飞书文档。支持导出扩展名为 docx 和 pdf 格式的文件</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// <para>导出的文件名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：docName</para>
    /// </summary>
    [JsonPropertyName("file_name")]
    public string? FileName { get; set; }

    /// <summary>
    /// <para>导出的文件的 token。</para>
    /// <para>必填：否</para>
    /// <para>示例值：boxcnxe5OdjlAkNgSNdsJvabcef</para>
    /// </summary>
    [JsonPropertyName("file_token")]
    public string? FileToken { get; set; }

    /// <summary>
    /// <para>导出文件的大小，单位字节。</para>
    /// <para>必填：否</para>
    /// <para>示例值：34356</para>
    /// </summary>
    [JsonPropertyName("file_size")]
    public int? FileSize { get; set; }

    /// <summary>
    /// <para>导出任务失败的原因</para>
    /// <para>必填：否</para>
    /// <para>示例值：success</para>
    /// </summary>
    [JsonPropertyName("job_error_msg")]
    public string? JobErrorMsg { get; set; }

    /// <summary>
    /// <para>导出任务状态</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：成功</item>
    /// <item>1：初始化</item>
    /// <item>2：处理中</item>
    /// <item>3：内部错误</item>
    /// <item>107：导出文档过大</item>
    /// <item>108：处理超时</item>
    /// <item>109：导出内容块无权限</item>
    /// <item>110：无权限</item>
    /// <item>111：导出文档已删除</item>
    /// <item>122：创建副本中禁止导出</item>
    /// <item>123：导出文档不存在</item>
    /// <item>6000：导出文档图片过多</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("job_status")]
    public int? JobStatus { get; set; }
}