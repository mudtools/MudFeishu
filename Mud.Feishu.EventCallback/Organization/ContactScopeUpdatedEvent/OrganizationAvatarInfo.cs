// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Organization;

/// <summary>
/// OrganizationAvatarInfo
/// <para>用户头像信息。</para>
/// <para>**字段权限要求（满足任一）**：</para>
/// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
/// <para>获取用户基本信息(contact:user.base:readonly)</para>
/// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
/// <para>读取通讯录(contact:contact:readonly)</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Organization")]
public class OrganizationAvatarInfo
{
    /// <summary>
    /// <para>72*72 像素头像链接。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("avatar_72")]
    public string? Avatar72 { get; set; }

    /// <summary>
    /// <para>240*240 像素头像链接。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("avatar_240")]
    public string? Avatar240 { get; set; }

    /// <summary>
    /// <para>640*640 像素头像链接。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("avatar_640")]
    public string? Avatar640 { get; set; }

    /// <summary>
    /// <para>原始头像链接。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("avatar_origin")]
    public string? AvatarOrigin { get; set; }
}
