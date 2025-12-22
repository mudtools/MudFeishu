// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.TaskComments;

/// <summary>
/// 创建评论请求体
/// </summary>
public class CreateCommentRequest
{
    /// <summary>
    /// <para>评论内容。不允许为空，最长3000个utf8字符。</para>
    /// <para>必填：是</para>
    /// <para>示例值：这是一条评论。</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// <para>回复给评论的评论ID。如果不填写表示创建非回复评论。</para>
    /// <para>必填：否</para>
    /// <para>示例值：6937231762296684564</para>
    /// </summary>
    [JsonPropertyName("reply_to_comment_id")]
    public string? ReplyToCommentId { get; set; }

    /// <summary>
    /// <para>评论归属的资源类型，目前只支持任务“task”，默认为"task"。</para>
    /// <para>必填：否</para>
    /// <para>示例值：task</para>
    /// <para>默认值：task</para>
    /// </summary>
    [JsonPropertyName("resource_type")]
    public string? ResourceType { get; set; }

    /// <summary>
    /// <para>评论归属的资源ID。当归属资源类型为"task"时，这里应填写任务的GUID。</para>
    /// <para>必填：否</para>
    /// <para>示例值：ccb55625-95d2-2e80-655f-0e40bf67953f</para>
    /// </summary>
    [JsonPropertyName("resource_id")]
    public string? ResourceId { get; set; }
}
