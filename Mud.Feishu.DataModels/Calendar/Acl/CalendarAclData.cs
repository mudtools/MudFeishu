// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;

/// <summary>
/// <para>日历访问控制数据。</para>
/// </summary>
public class CalendarAclData
{
    /// <summary>
    /// <para>对日历的访问权限。</para>
    /// <para>必填：是</para>
    /// <para>示例值：writer</para>
    /// <para>可选值：<list type="bullet">
    /// <item>unknown：未知权限。unknown 是 role 参数枚举值之一，但 role 作为请求参数时，不支持传入 unknown。</item>
    /// <item>free_busy_reader：游客，只能看到忙碌、空闲信息。</item>
    /// <item>reader：订阅者，可查看所有日程详情。</item>
    /// <item>writer：编辑者，可创建及修改日程。</item>
    /// <item>owner：管理员，可管理日历及共享设置。</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// <para>权限的生效范围。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("scope")]
    public AclScope Scope { get; set; } = new();
}