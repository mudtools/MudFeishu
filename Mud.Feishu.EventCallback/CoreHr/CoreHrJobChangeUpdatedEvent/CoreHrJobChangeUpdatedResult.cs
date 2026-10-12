// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHr;

/// <summary>
/// 异动状态变更
/// <para>在异动审批状态变更、异动生效时都会触发该事件，审批结果产生的场景包括撤销、审批通过、审批拒绝。本事件没有数据范围鉴权。 注意事项：本事件不再推荐使用，请使用新版本异动状态变更事件</para>
/// <para>事件类型:corehr.job_change.updated_v1</para>
/// <para>使用时请继承：<see cref="CoreHrJobChangeUpdatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/corehr-v1/job_change/events/updated</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CoreHrJobChangeUpdated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "CoreHr")]
public class CoreHrJobChangeUpdatedResult : IEventResult
{
    /// <summary>
    /// <para>雇员ID，可通过[【搜索员工信息】](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/employee/search)接口获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("employment_id")]
    public string? EmploymentId { get; set; }

    /// <summary>
    /// <para>用户 ID，飞书相关ID，</para>
    /// <para>可通过[【通讯录接口】](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/user/field-overview)接口获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("target_user_id")]
    public UserIdInfo? TargetUserId { get; set; }

    /// <summary>
    /// <para>异动记录 id，可通过接口</para>
    /// <para>[搜索异动信息</para>
    /// <para>](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/job_change/search)获取详细信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_change_id")]
    public string? JobChangeId { get; set; }

    /// <summary>
    /// <para>异动属性/方式</para>
    /// <para>**可选值有**：</para>
    /// <para>- 1：直接异动：无需审批的异动，操作后异动状态为「无需审批」</para>
    /// <para>- 2：发起异动：需要走异动流程</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("transfer_mode")]
    public int? TransferMode { get; set; }

    /// <summary>
    /// <para>异动类型唯一标识，可通过接口</para>
    /// <para>[获取异动类型列表</para>
    /// <para>](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/corehr-v1/transfer_type/query)获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("transfer_type_unique_identifier")]
    public string? TransferTypeUniqueIdentifier { get; set; }

    /// <summary>
    /// <para>异动原因唯一标识，可通过接口</para>
    /// <para>[获取异动原因列表</para>
    /// <para>](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/corehr-v1/transfer_reason/query)获取详细信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("transfer_reason_unique_identifier")]
    public string? TransferReasonUniqueIdentifier { get; set; }

    /// <summary>
    /// <para>异动发起后的审批流程 id，如果是直接异动，则无需要审批流程id</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("process_id")]
    public string? ProcessId { get; set; }

    /// <summary>
    /// <para>异动生效日期，格式："YYYY-MM-DD"</para>
    /// <para>- 示例：2022-04-03</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("effective_date")]
    public string? EffectiveDate { get; set; }

    /// <summary>
    /// <para>异动状态</para>
    /// <para>**可选值有**：</para>
    /// <para>- 1：异动发起 = 审批中状态</para>
    /// <para>- 2：已撤销</para>
    /// <para>- 3：已拒绝</para>
    /// <para>- 4：审批通过</para>
    /// <para>- 5：已生效（到了生效日期后就生效）</para>
    /// <para>- 6：无需审批</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// <para>异动记录标识符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("transfer_key")]
    public string? TransferKey { get; set; }
}
