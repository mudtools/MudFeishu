// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.TaskAttachments;

/// <summary>
/// 上传附件请求体
/// </summary>
public class UploadTaskAttachmentsRequest
{
    /// <summary>
    /// <para>附件归属资源的类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：task</para>
    /// <para>默认值：task</para>
    /// </summary>
    [JsonPropertyName("resource_type")]
    public string? ResourceType { get; set; }

    /// <summary>
    /// <para>附件要归属资源的id。例如，要给任务添加附件，这里要填入任务GUID。任务GUID可以通过[任务相关接口](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/task-v2/task/overview)获得。</para>
    /// <para>必填：是</para>
    /// <para>示例值：fe96108d-b004-4a47-b2f8-6886e758b3a5</para>
    /// </summary>
    [JsonPropertyName("resource_id")]
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>
    /// 要上传的文件，单请求支持最多5个文件。上传结果的顺序将和请求中文件的顺序保持一致。
    /// <para>必填：是</para>
    /// </summary>
    public string File { get; set; } = string.Empty;
}
