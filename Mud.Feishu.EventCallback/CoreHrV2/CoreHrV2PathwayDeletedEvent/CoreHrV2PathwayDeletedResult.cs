// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHrV2;

/// <summary>
/// 通道删除事件
/// <para>通道删除后会发送该事件 注意事项：收到删除事件后，获取通道信息接口将查询不到该通道</para>
/// <para>事件类型:corehr.pathway.deleted_v2</para>
/// <para>使用时请继承：<see cref="CoreHrV2PathwayDeletedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/pathway/events/deleted</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CoreHrV2PathwayDeleted, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "CoreHrV2")]
public class CoreHrV2PathwayDeletedResult : IEventResult
{
    /// <summary>
    /// <para>通道ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("pathway_id")]
    public string? PathwayId { get; set; }
}
