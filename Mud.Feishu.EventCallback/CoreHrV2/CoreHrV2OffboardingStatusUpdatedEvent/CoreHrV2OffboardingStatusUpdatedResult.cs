// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHrV2;

/// <summary>
/// 离职申请状态变更
/// <para>在发起离职审批、产生审批结果、离职生效、离职状态回退等离职申请状态变更时触发该事件推送对应消息。审批结果产生的场景包括撤销、通过、拒绝审批。 与原事件离职申请状态变更相比，该事件多了直接离职产生的事件，且支持「员工数据」范围控制 注意事项：注意： 1.你需要在应用中配置事件订阅，这样才可以在事件触发时接收到事件数据。</para>
/// <para>事件类型:corehr.offboarding.status_updated_v2</para>
/// <para>使用时请继承：<see cref="CoreHrV2OffboardingStatusUpdatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/offboarding/events/status_updated</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CoreHrV2OffboardingStatusUpdated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "CoreHrV2")]
public class CoreHrV2OffboardingStatusUpdatedResult : IEventResult
{
    /// <summary>
    /// <para>离职员工雇佣ID，可通过[批量查询员工信息</para>
    /// <para>](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/employee/batch_get)获取员工信息。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("employment_id")]
    public string? EmploymentId { get; set; }

    /// <summary>
    /// <para>离职员工的用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("target_user_id")]
    public UserIdInfo? TargetUserId { get; set; }

    /// <summary>
    /// <para>离职记录ID，每条离职记录的唯一标识。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("offboarding_id")]
    public string? OffboardingId { get; set; }

    /// <summary>
    /// <para>离职申请流程的实例 ID，可用于[查询流程相关信息](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/process/list)，例如：作为[获取单个流程详情](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/process/list)的process_id查询流程详情。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("process_id")]
    public string? ProcessId { get; set; }

    /// <summary>
    /// <para>离职状态</para>
    /// <para>**可选值有**：</para>
    /// <para>- 1：离职发起/审批中</para>
    /// <para>- 2：审批通过</para>
    /// <para>- 3：已生效（生效日期后自动生效）</para>
    /// <para>- 4：已拒绝</para>
    /// <para>- 5：已撤销</para>
    /// <para>- 6：无需审批</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }
}
