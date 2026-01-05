// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.Approval;


/// <summary>
/// 审批实例状态变更事件体
/// <para>审批实例状态发生变更时会触发该事件。</para>
/// <para>事件类型:approval_instance</para>
/// <para>使用时请继承：<see cref="ApprovalInstanceEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/approval-v4/event/common-event/approval-task-event</para>
/// </summary>
[EventHandler(EventType = FeishuEventTypes.ApprovalInstance, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom)]
public class ApprovalInstanceResult : IEventResult
{

    /// <summary>
    /// 应用的 App ID。
    /// </summary>
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    /// <summary>
    /// 如果创建审批实例时传入了 uuid，则此处返回该实例的 uuid。
    /// </summary>
    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

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
    /// 事件发生时间
    /// </summary>
    [JsonPropertyName("instance_operate_time")]
    public string? InstanceOperateTime { get; set; }

    /// <summary>
    /// 实例状态
    /// <list type="bullet">
    /// <item>PENDING - 审批中</item>
    /// <item>APPROVED - 已通过</item>
    /// <item>REJECTED - 已拒绝</item>
    /// <item>CANCELED - 已撤回</item>
    /// <item>DELETED - 已删除</item>
    /// <item>REVERTED - 已撤销</item>
    /// <item>OVERTIME_CLOSE - 超时被关闭</item>
    /// <item>OVERTIME_RECOVER - 超时实例被恢复</item>
    /// </list>
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
