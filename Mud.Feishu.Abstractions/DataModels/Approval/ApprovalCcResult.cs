// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.Approval;


/// <summary>
/// 审批抄送状态变更事件体
/// <para>当审批实例内创建抄送或者被抄送人已读时，会触发该事件。</para>
/// <para>事件类型:approval_cc</para>
/// <para>使用时请继承：<see cref="ApprovalCcEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/approval-v4/event/common-event/custom-approval-event</para>
/// </summary>
[EventHandler(EventType = FeishuEventTypes.ApprovalCc, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom)]
public class ApprovalCcResult : IEventResult
{
    /// <summary>
    /// 应用ID
    /// </summary>
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

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
    /// 审批定义 Code
    /// </summary>
    [JsonPropertyName("approval_code")]
    public string? ApprovalCode { get; set; }

    /// <summary>
    /// 审批实例 Code
    /// </summary>
    [JsonPropertyName("instance_code")]
    public string? InstanceCode { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonPropertyName("create_time")]
    public long CreateTime { get; set; }

    /// <summary>
    /// 抄送 ID。
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// 操作人 ID（当 task 为自动通过类型时，user_id 为空）
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// 操作类型。CREATE：创建抄送  READ：抄送人已读
    /// </summary>
    [JsonPropertyName("operate")]
    public string? Operate { get; set; }

    /// <summary>
    /// 执行抄送操作的用户 user_id，可能为空。
    /// </summary>
    [JsonPropertyName("from")]
    public string? From { get; set; }
}
