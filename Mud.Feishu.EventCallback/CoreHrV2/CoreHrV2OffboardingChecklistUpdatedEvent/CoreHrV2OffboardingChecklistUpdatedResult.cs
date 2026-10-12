// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHrV2;

/// <summary>
/// 离职流转状态变更
/// <para>离职流转流程的状态变更消息，当离职流转流程发起和产生审批结果时，会触发该事件。离职流转流程是在离职申请审批通过之后发起的流程，一般用于审批核实离职员工的交接事宜。 注意事项：注意： 1.你需要在应用中配置事件订阅，这样才可以在事件触发时接收到事件数据。了解事件订阅可参见事件订阅概述。</para>
/// <para>事件类型:corehr.offboarding.checklist_updated_v2</para>
/// <para>使用时请继承：<see cref="CoreHrV2OffboardingChecklistUpdatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/offboarding/events/checklist_updated</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CoreHrV2OffboardingChecklistUpdated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "CoreHrV2")]
public class CoreHrV2OffboardingChecklistUpdatedResult : IEventResult
{
    /// <summary>
    /// <para>离职员工雇佣ID，可通过[批量查询员工信息</para>
    /// <para>](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/employee/batch_get)获取员工信息。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("employment_id")]
    public string? EmploymentId { get; set; }

    /// <summary>
    /// <para>离职员工的用户ID</para>
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
    /// <para>离职流转流程的实例 ID，可用于[查询流程相关信息](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/process/list)，例如：作为[获取单个流程详情](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/process/list)的process_id查询流程详情。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("checklist_process_id")]
    public string? ChecklistProcessId { get; set; }

    /// <summary>
    /// <para>离职流转状态</para>
    /// <para>**可选值有**：</para>
    /// <para>- 1：未发起</para>
    /// <para>- 2：进行中</para>
    /// <para>- 3：已完成</para>
    /// <para>- 4：已拒绝</para>
    /// <para>- 5：已撤销</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("checklist_status")]
    public int? ChecklistStatus { get; set; }
}
