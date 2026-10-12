// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHrV2;

/// <summary>
/// 通道更新事件
/// <para>通道更新后会发送该事件 注意事项：- 收到推送后，可通过获取通道信息接口查询变更详情。 接口数据查询存在2s左右延迟，不建议收到事件后立即查询以上接口。 field_changes 数组用于描述发生变更的字段，可以是以下值： code（编码）、name（名称）、active（启停用状态）、description（描述）</para>
/// <para>事件类型:corehr.pathway.updated_v2</para>
/// <para>使用时请继承：<see cref="CoreHrV2PathwayUpdatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/pathway/events/updated</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CoreHrV2PathwayUpdated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "CoreHrV2")]
public class CoreHrV2PathwayUpdatedResult : IEventResult
{
    /// <summary>
    /// <para>通道 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("pathway_id")]
    public string? PathwayId { get; set; }

    /// <summary>
    /// <para>发生变更的字段</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `200`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("field_changes")]
    public string[]? FieldChanges { get; set; }
}
