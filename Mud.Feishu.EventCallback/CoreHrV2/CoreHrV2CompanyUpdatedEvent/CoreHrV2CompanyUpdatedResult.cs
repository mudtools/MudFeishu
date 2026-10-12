// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHrV2;

/// <summary>
/// 【事件】更新公司
/// <para>飞书人事中「公司被更新」时将触发此事件。 注意事项：- 【批量查询公司】、【查询单个公司】、【通过 ID 批量查询公司信息】数据查询存在一定延迟(2s以内)，不建议收到事件后立即查询以上接口。</para>
/// <para>事件类型:corehr.company.updated_v2</para>
/// <para>使用时请继承：<see cref="CoreHrV2CompanyUpdatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/company/events/updated</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CoreHrV2CompanyUpdated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "CoreHrV2")]
public class CoreHrV2CompanyUpdatedResult : IEventResult
{
    /// <summary>
    /// <para>公司ID。</para>
    /// <para>- 调用[【查询单个公司】](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/corehr-v1/company/get)、[【通过 ID 批量查询公司信息】](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/company/batch_get)接口返回公司详细信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("company_id")]
    public string? CompanyId { get; set; }

    /// <summary>
    /// <para>发生变更的字段</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `200`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("field_changes")]
    public string[]? FieldChanges { get; set; }

    /// <summary>
    /// <para>子变更事件</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("sub_events")]
    public CoreHrV2OrganizationDomainEventData[]? SubEvents { get; set; }
}
