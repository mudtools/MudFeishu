// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.Task;

/// <summary>
/// 任务信息变更（租户维度） 事件体
/// <para>APP 订阅此事件后可接收到该 APP 所在租户的所有来源接口创建的任务的变更事件。</para>
/// <para>事件体为发生变更任务的相关用户的 open_id，可用此 open_id ，通过 获取任务列表接口获取与该用户相关的所有任务。</para>
/// <para>事件类型:contact.user.updated_v3</para>
/// <para>使用时请继承：<see cref="TaskUpdateTenantEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/task-v1/task/events/update_tenant</para>
/// </summary>
[EventHandler(EventType = FeishuEventTypes.TaskUpdateTenant, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom)]
public class TaskUpdateTenantResult
{

    /// <summary>
    /// <para>用户 ID 列表</para>
    /// </summary>
    [JsonPropertyName("user_id_list")]
    public UserIdInfoList? UserIdList { get; set; }

    /// <summary>
    /// <para>任务的id</para>
    /// </summary>
    [JsonPropertyName("task_id")]
    public string? TaskId { get; set; }

    /// <summary>
    /// <para>变更的数据类型，可选值：</para>
    /// <para>"task"，"collaborator"，"follower"</para>
    /// </summary>
    [JsonPropertyName("object_type")]
    public string? ObjectType { get; set; }

    /// <summary>
    /// <para>事件类型，可选值：</para>
    /// <para>"create"，"delete"，"update"</para>
    /// </summary>
    [JsonPropertyName("event_type")]
    public string? EventType { get; set; }
}
