// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHrV2;

/// <summary>
/// 抄送单据状态变更
/// <para>流程中生成抄送单据后会触发该事件。抄送节点会生成抄送单据任务。如果一个节点有多个人抄送人，则会生成多个抄送单据（此功能不受数据权限范围控制）。 注意事项：你需要在应用内配置事件订阅，并订阅该事件，这样才可以在事件触发时接收到事件数据。了解事件订阅可参见事件概述。</para>
/// <para>事件类型:corehr.process.cc.updated_v2</para>
/// <para>使用时请继承：<see cref="CoreHrV2ProcessCcUpdatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/process-cc/events/updated</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CoreHrV2ProcessCcUpdated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "CoreHrV2")]
public class CoreHrV2ProcessCcUpdatedResult : IEventResult
{
    /// <summary>
    /// <para>流程运行实例 id，详细信息可通过[获取单个流程详情](https://open.larkoffice.com/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/process/get)获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("process_id")]
    public string? ProcessId { get; set; }

    /// <summary>
    /// <para>抄送单据ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("approver_id")]
    public string? ApproverId { get; set; }

    /// <summary>
    /// <para>单据状态</para>
    /// <para>**可选值有**：</para>
    /// <para>- 5：抄送到达</para>
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
}
