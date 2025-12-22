// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Task;

namespace Mud.Feishu.DataModels.TaskComments;

/// <summary>
/// 评论详情
/// </summary>
public class TaskCommentInfo
{
    /// <summary>
    /// <para>评论id</para>
    /// <para>示例值：7197020628442939411</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>评论内容</para>
    /// <para>示例值：这是一条评论</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// <para>评论创建人</para>
    /// </summary>
    [JsonPropertyName("creator")]
    public TaskMember? Creator { get; set; }

    /// <summary>
    /// <para>评论回复评论的id。如果不是回复评论，则为空。</para>
    /// <para>示例值：7166825117308174356</para>
    /// </summary>
    [JsonPropertyName("reply_to_comment_id")]
    public string? ReplyToCommentId { get; set; }

    /// <summary>
    /// <para>评论创建时间戳（ms)</para>
    /// <para>示例值：1675742789470</para>
    /// </summary>
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    /// <summary>
    /// <para>评论更新时间戳（ms）</para>
    /// <para>示例值：1675742789470</para>
    /// </summary>
    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    /// <summary>
    /// <para>任务关联的资源类型</para>
    /// <para>示例值：task</para>
    /// </summary>
    [JsonPropertyName("resource_type")]
    public string? ResourceType { get; set; }

    /// <summary>
    /// <para>任务关联的资源ID</para>
    /// <para>示例值：ccb55625-95d2-2e80-655f-0e40bf67953f</para>
    /// </summary>
    [JsonPropertyName("resource_id")]
    public string? ResourceId { get; set; }
}
