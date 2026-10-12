// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Organization;

/// <summary>
/// OrganizationUserStatus
/// <para>用户状态。通过 is_frozen、is_resigned、is_activated、is_exited 布尔值类型参数进行展示。</para>
/// <para>用户状态的流转逻辑可参见[用户资源介绍](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/user/field-overview#4302b5a1)。</para>
/// <para>**字段权限要求（满足任一）**：</para>
/// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
/// <para>获取用户受雇信息(contact:user.employee:readonly)</para>
/// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
/// <para>读取通讯录(contact:contact:readonly)</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Organization")]
public class OrganizationUserStatus
{
    /// <summary>
    /// <para>是否为暂停状态。</para>
    /// <para>**可能值有**：</para>
    /// <para>- true：是</para>
    /// <para>- false：否</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_frozen")]
    public bool? IsFrozen { get; set; }

    /// <summary>
    /// <para>是否为离职状态。</para>
    /// <para>**可能值有**：</para>
    /// <para>- true：是</para>
    /// <para>- false：否</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_resigned")]
    public bool? IsResigned { get; set; }

    /// <summary>
    /// <para>是否为激活状态。</para>
    /// <para>**可能值有**：</para>
    /// <para>- true：是</para>
    /// <para>- false：否</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_activated")]
    public bool? IsActivated { get; set; }

    /// <summary>
    /// <para>是否为主动退出状态。主动退出一段时间后用户状态会自动转为已离职。</para>
    /// <para>**可能值有**：</para>
    /// <para>- true：是</para>
    /// <para>- false：否</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_exited")]
    public bool? IsExited { get; set; }

    /// <summary>
    /// <para>是否为未加入状态，需要用户自主确认才能加入企业或团队。</para>
    /// <para>**可能值有**：</para>
    /// <para>- true：是</para>
    /// <para>- false：否</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_unjoin")]
    public bool? IsUnjoin { get; set; }
}
