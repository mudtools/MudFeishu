// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Hire;

/// <summary>
/// 人才删除
/// <para>人才被删除时触发此事件。注意：该事件仅通知人才 ID 的变化，需配合其他查询接口获取实际的人才信息。</para>
/// <para>事件类型:hire.talent.deleted_v1</para>
/// <para>使用时请继承：<see cref="HireTalentDeletedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/talent/events/deleted</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.HireTalentDeleted, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class HireTalentDeletedResult : IEventResult
{
    /// <summary>
    /// <para>人才 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("talent_id")]
    public string? TalentId { get; set; }
}
