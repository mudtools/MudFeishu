// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Helpdesk;

/// <summary>
/// 服务台推送审批通知
/// <para>推送审批状态通知事件。</para>
/// <para>事件类型:helpdesk.notification.approve_v1</para>
/// <para>使用时请继承：<see cref="HelpdeskNotificationApproveEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/helpdesk-v1/notification/events/approve</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.HelpdeskNotificationApprove, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Helpdesk")]
public class HelpdeskNotificationApproveResult : IEventResult
{
    /// <summary>
    /// <para>推送任务唯一 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("notification_id")]
    public string? NotificationId { get; set; }

    /// <summary>
    /// <para>服务台唯一 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("helpdesk_id")]
    public string? HelpdeskId { get; set; }

    /// <summary>
    /// <para>审批状态。枚举值有：</para>
    /// <para>- REJECTED：拒绝</para>
    /// <para>- APPROVED：通过</para>
    /// <para>- CANCELED：取消</para>
    /// <para>- DELETED：删除</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("approve_status")]
    public string? ApproveStatus { get; set; }
}
