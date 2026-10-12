// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHrV2;

/// <summary>
/// 【事件】创建成本中心
/// <para>飞书人事中「成本中心被创建」时将触发此事件。 注意事项：【搜索成本中心信息】接口数据查询存在一定延迟(2s内)，不建议收到事件后立即查询以上接口。 注意事项：- 该接口只会推送当前生效成本中心的变更事件。 未来生效的版本数据，会在生效日期当天凌晨推送事件。</para>
/// <para>事件类型:corehr.cost_center.created_v2</para>
/// <para>使用时请继承：<see cref="CoreHrV2CostCenterCreatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/cost_center/events/created</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CoreHrV2CostCenterCreated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "CoreHrV2")]
public class CoreHrV2CostCenterCreatedResult : IEventResult
{
    /// <summary>
    /// <para>成本中心ID。</para>
    /// <para>- 调用[【搜索成本中心信息】](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/cost_center/search)接口返回成本中心详细信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("cost_center_id")]
    public string? CostCenterId { get; set; }
}
