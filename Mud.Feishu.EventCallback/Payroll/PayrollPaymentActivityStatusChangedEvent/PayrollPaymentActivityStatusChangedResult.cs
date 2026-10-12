// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Payroll;

/// <summary>
/// 发薪活动变更事件
/// <para>当发薪活动发生变更后，订阅这个事件的应用会收到事件。当前仅审批通过、审批撤销、跳过审批、封存、取消封存，会发送该事件。</para>
/// <para>事件类型:payroll.payment_activity.status_changed_v1</para>
/// <para>使用时请继承：<see cref="PayrollPaymentActivityStatusChangedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/payroll-v1/payment_activity/events/status_changed</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.PayrollPaymentActivityStatusChanged, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Payroll")]
public class PayrollPaymentActivityStatusChangedResult : IEventResult
{
    /// <summary>
    /// <para>发薪活动ID，详情可查看[查询发薪活动列表](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/payroll-v1/payment_activity/list)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("activity_id")]
    public string? ActivityId { get; set; }

    /// <summary>
    /// <para>发薪活动变更类型</para>
    /// <para>- 2：封存</para>
    /// <para>- 3：取消封存</para>
    /// <para>- 4：撤销审批</para>
    /// <para>- 5：跳过审批</para>
    /// <para>- 6：审批通过</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 取值范围：`0` ～ `10000`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }
}
