// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHrV2;

/// <summary>
/// 流程实例信息变更
/// <para>流程实例是指用户发起的具体流程(process_id是其唯一标识)，流程实例在以下时机会触发信息变更事件：流程中有审批人操作、流程数据更新、流程状态变化等。 注意事项：若节点中有多个人时，可能会同时触发多个事件。例如流程运行到该节点，同时为多个人都生成了待办任务，就会导致触发多次事件（此功能不受数据权限范围控制）。</para>
/// <para>事件类型:corehr.process.updated_v2</para>
/// <para>使用时请继承：<see cref="CoreHrV2ProcessUpdatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/process/events/updated</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CoreHrV2ProcessUpdated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "CoreHrV2")]
public class CoreHrV2ProcessUpdatedResult : IEventResult
{
    /// <summary>
    /// <para>流程运行实例 id，详细信息可通过[获取单个流程详情](https://open.larkoffice.com/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/process/get)获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("process_id")]
    public string? ProcessId { get; set; }

    /// <summary>
    /// <para>变更后状态</para>
    /// <para>**可选值有**：</para>
    /// <para>- 1：发起/进行中</para>
    /// <para>- 9：完成</para>
    /// <para>- 2：拒绝</para>
    /// <para>- 4：撤回</para>
    /// <para>- 8：撤销</para>
    /// <para>- 15：撤销中</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// <para>业务类型，</para>
    /// <para>详情请查看[接入指南](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/corehr-v1/process-form_variable_data/access-guide)</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`1` ～ `200` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("biz_type")]
    public string? BizType { get; set; }

    /// <summary>
    /// <para>流程定义id。流程定义是流程的模板，由流程定义可以创建流程实例。</para>
    /// <para>**示例值**：</para>
    /// <para>"people_7023711013443944467_7382148112896872236"</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("flow_definition_id")]
    public string? FlowDefinitionId { get; set; }

    /// <summary>
    /// <para>流程属性</para>
    /// <para>**可选值有**：</para>
    /// <para>- 1：普通流程</para>
    /// <para>- 2：撤销流程</para>
    /// <para>- 3：更正流程</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("properties")]
    public int? Properties { get; set; }
}
