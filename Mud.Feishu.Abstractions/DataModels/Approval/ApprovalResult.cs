// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.Approval;


/// <summary>
/// 审批任务状态变更事件体
/// <para>审批任务状态发生变更时会触发该事件。</para>
/// <para>事件类型:approval_task</para>
/// <para>使用时请继承：<see cref="ApprovalTaskEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/approval-v4/event/common-event/approval-task-event</para>
/// </summary>
[EventHandler(EventType = FeishuEventTypes.ApprovalTask, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom)]
public class ApprovalTaskResult : IEventResult
{

    /// <summary>
    /// 应用的 App ID。
    /// </summary>
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }
    /// <summary>
    /// 审批任务操作人的 open_id。说明：如果审批任务为自动通过类型，open_id 会返回空值。
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    /// <summary>
    /// 企业唯一标识
    /// </summary>
    [JsonPropertyName("tenant_key")]
    public string? TenantKey { get; set; }

    /// <summary>
    /// 事件类型
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("approval_code")]
    public string? ApprovalCode { get; set; }

    /// <summary>
    /// 审批实例 Code
    /// </summary>
    [JsonPropertyName("instance_code")]
    public string? InstanceCode { get; set; }

    /// <summary>
    /// 事件发生时间
    /// </summary>
    [JsonPropertyName("operate_time")]
    public string? OperateTime { get; set; }

    /// <summary>
    /// 审批任务 ID
    /// </summary>
    [JsonPropertyName("task_id")]
    public string? TaskId { get; set; }

    /// <summary>
    /// 操作人 ID（当 task 为自动通过类型时，user_id 为空）
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// 任务状态
    /// <list type="bullet">
    /// <item>REVERTED - 已还原</item>
    /// <item>PENDING - 进行中</item>
    /// <item>APPROVED - 已通过</item>
    /// <item>REJECTED - 已拒绝</item>
    /// <item>TRANSFERRED - 已转交</item>
    /// <item>ROLLBACK - 已退回</item>
    /// <item>DONE - 已完成</item>
    /// </list>
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// 节点自定义ID
    /// </summary>
    [JsonPropertyName("custom_key")]
    public string? CustomKey { get; set; }

    /// <summary>
    /// 节点系统生成唯一ID
    /// </summary>
    [JsonPropertyName("def_key")]
    public string? DefKey { get; set; }

    /// <summary>
    /// 扩展数据, 当前只有退回事件才有此字段，rollback_node_ids退回的节点列表，rollback_custom_node_ids用户自定义配置的节点列表
    /// </summary>
    [JsonPropertyName("extra")]
    public string? Extra { get; set; }
}
