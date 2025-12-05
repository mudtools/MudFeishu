// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.Organization;

/// <summary>
/// 员工离职事件处理器
/// <para>当应用订阅该事件后，如果有员工离职（例如，通过管理后台离职成员、调用删除用户 API），则会触发该事件。</para>
/// <para>事件类型:contact.user.deleted_v3</para>
/// <para>使用时请继承：<see cref="UserDeleteEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/contact-v3/user/events/deleted</para>
/// </summary>
[EventHandler(EventType = FeishuEventTypes.UserDeleted, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom)]
public class UserDeleteResult : IEventResult
{
    /// <summary>
    /// <para>员工信息。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("object")]
    public UserResultInfo? Object { get; set; }

    /// <summary>
    /// <para>删除前信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("old_object")]
    public OldUserObject? OldObject { get; set; }
}
