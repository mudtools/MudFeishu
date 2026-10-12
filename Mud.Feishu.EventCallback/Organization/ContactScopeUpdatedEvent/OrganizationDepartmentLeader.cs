// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Organization;

/// <summary>
/// OrganizationDepartmentLeader
/// <para>部门负责人信息。</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Organization")]
public class OrganizationDepartmentLeader
{
    /// <summary>
    /// <para>负责人类型。</para>
    /// <para>**可选值有**：</para>
    /// <para>- 1：主负责人。</para>
    /// <para>- 2：副负责人。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("leaderType")]
    public int? LeaderType { get; set; }

    /// <summary>
    /// <para>负责人的用户 open_id。了解用户 ID 可参见[用户相关的 ID 概念](https://open.feishu.cn/document/home/user-identity-introduction/introduction)。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取通讯录部门组织架构信息(contact:department.organize:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("leaderID")]
    public string? LeaderID { get; set; }
}
