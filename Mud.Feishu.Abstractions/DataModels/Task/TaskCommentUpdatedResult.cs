// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.Task;

/// <summary>
/// 任务评论信息变更事件体
/// <para>当 APP 创建的任务评论信息发生变更时触发此事件，包括任务评论的创建、回复、更新、删除。</para>
/// <para>事件类型:task.task.comment.updated_v1</para>
/// <para>使用时请继承：<see cref="TaskCommentUpdatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/task-v1/task-comment/events/updated</para>
/// </summary>
[EventHandler(EventType = FeishuEventTypes.TaskCommentUpdated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom)]
public class TaskCommentUpdatedResult : IEventResult
{
    /// <summary>
    /// <para>任务ID</para>
    /// </summary>
    [JsonPropertyName("task_id")]
    public string? TaskId { get; set; }

    /// <summary>
    /// <para>任务评论ID</para>
    /// </summary>
    [JsonPropertyName("comment_id")]
    public string? CommentId { get; set; }

    /// <summary>
    /// <para>任务评论父ID</para>
    /// </summary>
    [JsonPropertyName("parent_id")]
    public string? ParentId { get; set; }

    /// <summary>
    /// <para>通知类型（1：创建评论，2：回复评论，3：更新评论，4：删除评论）</para>
    /// </summary>
    [JsonPropertyName("obj_type")]
    public int? ObjType { get; set; }
}
