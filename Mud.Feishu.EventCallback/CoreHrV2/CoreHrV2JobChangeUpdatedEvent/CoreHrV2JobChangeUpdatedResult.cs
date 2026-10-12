// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHrV2;

/// <summary>
/// 异动信息变更
/// <para>员工发起异动后，异动信息变更会触发该事件</para>
/// <para>事件类型:corehr.job_change.updated_v2</para>
/// <para>使用时请继承：<see cref="CoreHrV2JobChangeUpdatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/job_change/events/updated</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CoreHrV2JobChangeUpdated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "CoreHrV2")]
public class CoreHrV2JobChangeUpdatedResult : IEventResult
{
    /// <summary>
    /// <para>异动员工雇员ID，可通过[【搜索员工信息】](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/employee/search)接口获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("employment_id")]
    public string? EmploymentId { get; set; }

    /// <summary>
    /// <para>Saas租户ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("tenant_id")]
    public string? TenantId { get; set; }

    /// <summary>
    /// <para>流程ID，可通过【流程-获取单个流程详情】接口获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("process_id")]
    public string? ProcessId { get; set; }

    /// <summary>
    /// <para>发起人雇员ID，可通过[【搜索员工信息】](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/employee/search)接口获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("initiator")]
    public string? Initiator { get; set; }

    /// <summary>
    /// <para>操作人雇员ID，可通过[【搜索员工信息】](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/employee/search)接口获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operator")]
    public string? Operator { get; set; }

    /// <summary>
    /// <para>更新时间毫秒时间戳</para>
    /// <para>- 示例：1714985958000</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("updated_time")]
    public string? UpdatedTime { get; set; }

    /// <summary>
    /// <para>异动记录ID，可通过接口</para>
    /// <para>[搜索异动信息</para>
    /// <para>](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/job_change/search)获取详细信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_change_id")]
    public string? JobChangeId { get; set; }

    /// <summary>
    /// <para>变更后异动状态</para>
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
    /// <para>操作原因</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operate_reason")]
    public string? OperateReason { get; set; }

    /// <summary>
    /// <para>变更类型</para>
    /// <para>**可选值有**：</para>
    /// <para>- 1：异动页面编辑</para>
    /// <para>- 2：任职记录编辑</para>
    /// <para>- 3：花名册导入编辑</para>
    /// <para>- 4：试用期页面编辑</para>
    /// <para>- 5：处理异常编辑</para>
    /// <para>- 6：异动流程编辑</para>
    /// <para>- 7：合同编辑</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("transfer_type")]
    public int? TransferType { get; set; }

    /// <summary>
    /// <para>变更字段</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `200`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("updated_fields")]
    public string[]? UpdatedFields { get; set; }

    /// <summary>
    /// <para>异动类型，可通过接口</para>
    /// <para>[获取异动类型列表</para>
    /// <para>](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/corehr-v1/transfer_type/query)获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("transform_type")]
    public string? TransformType { get; set; }

    /// <summary>
    /// <para>异动原因，可通过接口</para>
    /// <para>[获取异动原因列表</para>
    /// <para>](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/corehr-v1/transfer_reason/query)获取详细信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("transform_reason")]
    public string? TransformReason { get; set; }
}
