// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive.Files;

/// <summary>
/// 创建导入任务请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Drive")]
public class ImportTasksRequest
{
    /// <summary>
    /// <para>要导入的文件的扩展名。</para>
    /// <para>**注意**：此处填写的文件扩展名需与实际文件的后缀名保持严格一致。请注意区分后缀为 “markdown”、“md”、“mark” 的 Markdown 文件，并在填写相关参数时保持后缀名一致。否则将返回 1069910 错误码。</para>
    /// <para>必填：是</para>
    /// <para>示例值：xlsx</para>
    /// </summary>
    [JsonPropertyName("file_extension")]
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>
    /// <para>要导入文件的 token。</para>
    /// <para>必填：是</para>
    /// <para>示例值：boxcnrHpsg1QDqXAAAyachabcef</para>
    /// <para>最大长度：27</para>
    /// </summary>
    [JsonPropertyName("file_token")]
    public string FileToken { get; set; } = string.Empty;

    /// <summary>
    /// <para>目标云文档格式。不同文件支持的云文档格式不同。可选值如下所示：</para>
    /// <para>- `docx`：新版文档</para>
    /// <para>- `sheet`：电子表格</para>
    /// <para>- `bitable`：多维表格</para>
    /// <para>必填：是</para>
    /// <para>示例值：sheet</para>
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// <para>导入后的在线云文档名称。参数为空时，使用上传本地文件时的文件名。</para>
    /// <para>必填：否</para>
    /// <para>示例值：销售表</para>
    /// </summary>
    [JsonPropertyName("file_name")]
    public string? FileName { get; set; }

    /// <summary>
    /// <para>挂载点（导入后的云文档所在位置）</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("point")]
    public ImportTaskMountPoint Point { get; set; } = new();

}
