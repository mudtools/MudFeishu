// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHrV2;

/// <summary>
/// 试用期状态变更
/// <para>当试用期记录状态发生变更时，触发该事件。</para>
/// <para>事件类型:corehr.probation.updated_v2</para>
/// <para>使用时请继承：<see cref="CoreHrV2ProbationUpdatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/probation/events/updated</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CoreHrV2ProbationUpdated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "CoreHrV2")]
public class CoreHrV2ProbationUpdatedResult : IEventResult
{
    /// <summary>
    /// <para>雇佣 ID，可通过[【搜索员工信息】](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/employee/search)接口获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("employment_id")]
    public string? EmploymentId { get; set; }

    /// <summary>
    /// <para>试用期状态</para>
    /// <para>**可选值有**：</para>
    /// <para>- pending：审批中</para>
    /// <para>- rejected：已拒绝</para>
    /// <para>- waiting：待发起转正</para>
    /// <para>- approved：审批通过</para>
    /// <para>- converted：已转正</para>
    /// <para>- offboarded：已离职</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("probation_status")]
    public string? ProbationStatus { get; set; }

    /// <summary>
    /// <para>试用期实际结束日期，格式："YYYY-MM-DD"</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("actual_probation_end_date")]
    public string? ActualProbationEndDate { get; set; }
}
