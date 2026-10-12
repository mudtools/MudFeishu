// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHrV2;

/// <summary>
/// 创建岗位事件
/// <para>飞书人事中「岗位被创建」时将触发此事件。注意：触发时间为岗位实际生效时间，如在 2022-01-01 创建岗位，岗位生效时间设置为 2022-05-01，事件将在 2022-05-01 进行推送。 注意事项：收到推送后，可通过【查询岗位信息】接口查询变更详情</para>
/// <para>事件类型:corehr.position.created_v2</para>
/// <para>使用时请继承：<see cref="CoreHrV2PositionCreatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/position/events/created</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CoreHrV2PositionCreated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "CoreHrV2")]
public class CoreHrV2PositionCreatedResult : IEventResult
{
    /// <summary>
    /// <para>岗位ID，可通过[查询岗位](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/position/query)接口去获取岗位详细信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("position_id")]
    public string? PositionId { get; set; }
}
