// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHr;

/// <summary>
/// 【事件】个人信息删除
/// <para>个人信息删除</para>
/// <para>事件类型:corehr.person.deleted_v1</para>
/// <para>使用时请继承：<see cref="CoreHrPersonDeletedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/corehr-v1/person/events/deleted</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CoreHrPersonDeleted, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "CoreHr")]
public class CoreHrPersonDeletedResult : IEventResult
{
    /// <summary>
    /// <para>人员ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("person_id")]
    public string? PersonId { get; set; }
}
