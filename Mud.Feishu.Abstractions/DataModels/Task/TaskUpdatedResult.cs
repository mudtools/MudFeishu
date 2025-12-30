// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.Task;

/// <summary>
/// 任务信息变更（应用维度） 事件体
/// <para>当 APP 订阅此事件后可以接收到由该 APP 创建的任务发生的变更，包括任务标题、描述、截止时间、协作者、关注者、提醒时间、状态（完成或取消完成）。</para>
/// <para>事件类型:task.task.updated_v1</para>
/// <para>使用时请继承：<see cref="TaskUpdatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/task-v1/task/events/update_tenant</para>
/// </summary>
[EventHandler(EventType = FeishuEventTypes.TaskUpdate, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom)]
public class TaskUpdatedResult : IEventResult
{
    /// <summary>
    /// <para>任务ID</para>
    /// </summary>
    [JsonPropertyName("task_id")]
    public string? TaskId { get; set; }

    /// <summary>
    /// <para>通知类型（1：任务详情发生变化，2：任务协作者发生变化，3：任务关注者发生变化，4：任务提醒时间发生变化，5：任务完成，6：任务取消完成，7：任务删除）</para>
    /// </summary>
    [JsonPropertyName("obj_type")]
    public int? ObjType { get; set; }
}
