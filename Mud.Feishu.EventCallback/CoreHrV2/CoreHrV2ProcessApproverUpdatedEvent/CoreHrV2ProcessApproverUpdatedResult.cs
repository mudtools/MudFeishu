// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHrV2;

/// <summary>
/// 审批任务状态变更
/// <para>单个审批任务的状态变化会触发该事件。例如，审批任务从待办变为已完成。审批任务（approver_id 是唯一标识），比如一个多人会签节点，会分别生成多人的审批任务（此功能不受数据权限范围控制）。 注意事项：你需要在应用内配置事件订阅，并订阅该事件，这样才可以在事件触发时接收到事件数据。了解事件订阅可参见事件概述。</para>
/// <para>事件类型:corehr.process.approver.updated_v2</para>
/// <para>使用时请继承：<see cref="CoreHrV2ProcessApproverUpdatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/process-approver/events/updated</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CoreHrV2ProcessApproverUpdated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "CoreHrV2")]
public class CoreHrV2ProcessApproverUpdatedResult : IEventResult
{
    /// <summary>
    /// <para>流程运行实例 id，详细信息可通过[获取单个流程详情](https://open.larkoffice.com/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/process/get)获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("process_id")]
    public string? ProcessId { get; set; }

    /// <summary>
    /// <para>单据ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("approver_id")]
    public string? ApproverId { get; set; }

    /// <summary>
    /// <para>单据类型</para>
    /// <para>**可选值有**：</para>
    /// <para>- 1：审批单</para>
    /// <para>- 5：表单</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("type")]
    public int? Type { get; set; }

    /// <summary>
    /// <para>单据状态</para>
    /// <para>**可选值有**：</para>
    /// <para>- 1：待办</para>
    /// <para>- 3：已完成</para>
    /// <para>- 2：拒绝</para>
    /// <para>- 4：取消</para>
    /// <para>- 18：已干预</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// <para>业务类型，详情请查看[接入指南](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/corehr-v1/process-form_variable_data/access-guide)</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`1` ～ `200` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("biz_type")]
    public string? BizType { get; set; }

    /// <summary>
    /// <para>流程定义id</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("flow_definition_id")]
    public string? FlowDefinitionId { get; set; }

    /// <summary>
    /// <para>节点定义id</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("node_definition_id")]
    public string? NodeDefinitionId { get; set; }

    /// <summary>
    /// <para>节点id（已废弃，请使用node_id_str字段)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("node_id")]
    public string? NodeId { get; set; }

    /// <summary>
    /// <para>节点id</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("node_id_str")]
    public string? NodeIdStr { get; set; }
}
