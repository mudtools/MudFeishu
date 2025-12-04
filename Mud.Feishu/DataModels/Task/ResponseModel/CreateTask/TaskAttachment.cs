// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Task;

/// <summary>
/// <para>任务的附件列表</para>
/// </summary>
public class TaskAttachment
{
    /// <summary>
    /// <para>附件guid</para>
    /// </summary>
    [JsonPropertyName("guid")]
    public string? Guid { get; set; }

    /// <summary>
    /// <para>附件在云文档系统中的token</para>
    /// </summary>
    [JsonPropertyName("file_token")]
    public string? FileToken { get; set; }

    /// <summary>
    /// <para>附件名</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>附件的字节大小</para>
    /// </summary>
    [JsonPropertyName("size")]
    public int? Size { get; set; }

    /// <summary>
    /// <para>附件归属的资源</para>
    /// </summary>
    [JsonPropertyName("resource")]
    public TaskAttachmentResource? Resource { get; set; }

    /// <summary>
    /// <para>附件上传者</para>
    /// </summary>
    [JsonPropertyName("uploader")]
    public TaskMember? Uploader { get; set; }

    /// <summary>
    /// <para>是否是封面图</para>
    /// </summary>
    [JsonPropertyName("is_cover")]
    public bool? IsCover { get; set; }

    /// <summary>
    /// <para>上传时间戳(ms)</para>
    /// </summary>
    [JsonPropertyName("uploaded_at")]
    public string? UploadedAt { get; set; }
}