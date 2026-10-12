// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHrV2;

/// <summary>
/// 入职流程状态变更
/// <para>待入职员工的入职流程流转时，例如调用流转入职任务接口会触发本事件。 注意事项：你需要在应用中配置事件订阅，这样才可以在事件触发时接收到事件数据。了解事件订阅可参见事件订阅概述。</para>
/// <para>事件类型:corehr.pre_hire.onboarding_task_changed_v2</para>
/// <para>使用时请继承：<see cref="CoreHrV2OnboardingTaskChangedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/pre_hire/events/onboarding_task_changed</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CoreHrV2OnboardingTaskChanged, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "CoreHrV2")]
public class CoreHrV2OnboardingTaskChangedResult : IEventResult
{
    /// <summary>
    /// <para>飞书人事租户ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("tenant_id")]
    public string? TenantId { get; set; }

    /// <summary>
    /// <para>待入职ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("pre_hire_id")]
    public string? PreHireId { get; set; }

    /// <summary>
    /// <para>入职任务状态变更</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `1000`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("onboarding_task_changes")]
    public CoreHrV2OnboardingTaskChange[]? OnboardingTaskChanges { get; set; }

    /// <summary>
    /// <para>入职流程状态变更</para>
    /// <para>- 当流程状态无变更，仅有任务状态变更时，此字段的会返回空</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("onboarding_flow_change")]
    public CoreHrV2OnboardingFlowChange? OnboardingFlowChange { get; set; }

    /// <summary>
    /// <para>待入职流程ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("onboarding_flow_id")]
    public string? OnboardingFlowId { get; set; }

    /// <summary>
    /// <para>流程信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("flow_info")]
    public CoreHrV2OnboardingFlow? FlowInfo { get; set; }
}
