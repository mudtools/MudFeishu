// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Calendar;


/// <summary>
/// 创建 ACL
/// <para>当订阅的日历上有访问控制被创建时，将会触发此事件。</para>
/// <para>事件类型:calendar.calendar.acl.created_v4</para>
/// <para>使用时请继承：<see cref="CalendarAclCreatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/calendar-v4/calendar-acl/events/created</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CalendarAclCreated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Calendar")]
public class CalendarAclCreatedResult : IEventResult
{
    /// <summary>
    /// <para>访问控制 ID。该 ID 在单个日历实体内唯一，不同日历实体可能存在重复的访问控制 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("acl_id")]
    public string? AclId { get; set; }

    /// <summary>
    /// <para>对日历的访问权限。</para>
    /// <para>**可选值有**：</para>
    /// <para>unknown:未知权限。,free_busy_reader:游客，只能看到忙碌、空闲信息。,reader:订阅者，可查看所有日程详情。,writer:编辑者，可创建及修改日程。,owner:管理员，可管理日历及共享设置。</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>unknown：未知权限。</item>
    /// <item>free_busy_reader：游客，只能看到忙碌、空闲信息。</item>
    /// <item>reader：订阅者，可查看所有日程详情。</item>
    /// <item>writer：编辑者，可创建及修改日程。</item>
    /// <item>owner：管理员，可管理日历及共享设置。</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    /// <summary>
    /// <para>权限生效范围。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("scope")]
    public AclScopeEvent? Scope { get; set; }

    /// <summary>
    /// <para>需要推送事件的用户列表。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_id_list")]
    public UserIdInfo[]? UserIdList { get; set; }
}

