// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Organization;

/// <summary>
/// 通讯录权限范围变更
/// <para>当应用订阅该事件后，如果应用的通讯录权限范围发生变更，则会触发该事件。</para>
/// <para>事件类型:contact.scope.updated_v3</para>
/// <para>使用时请继承：<see cref="ContactScopeUpdatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/scope/events/updated</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.ContactScopeUpdated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Organization")]
public class ContactScopeUpdatedResult : IEventResult
{
    /// <summary>
    /// <para>当通讯录权限范围变更时，新增的对象信息。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("added")]
    public OrganizationScope? Added { get; set; }

    /// <summary>
    /// <para>当通讯录权限范围发生变更时，移除的对象信息。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("removed")]
    public OrganizationScope? Removed { get; set; }
}
