// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Payroll;

/// <summary>
/// 发薪活动封存
/// <para>当发薪活动封存后，订阅这个事件的应用会收到事件。 一个发薪活动封存后，可能会向事件监听方发送多条 activity_id 相同的事件通知，事件监听方需要针对 activity_id 做好幂等处理。</para>
/// <para>事件类型:payroll.payment_activity.approved_v1</para>
/// <para>使用时请继承：<see cref="PayrollPaymentActivityApprovedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/payroll-v1/payment_activity/events/approved</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.PayrollPaymentActivityApproved, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Payroll")]
public class PayrollPaymentActivityApprovedResult : IEventResult
{
    /// <summary>
    /// <para>封存的发薪活动 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("activity_id")]
    public string? ActivityId { get; set; }
}
