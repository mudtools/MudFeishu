// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.UserGroup;

/// <summary>
/// 用户组基础结果类，包含用户组的基本信息。
/// </summary>
public abstract class UserGroupBaseResult
{
    /// <summary>
    /// 用户组ID。
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 用户组名称。
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 用户组描述。
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 用户组成员数量。
    /// </summary>
    [JsonPropertyName("member_user_count")]
    public int MemberUserCount { get; set; }

    /// <summary>
    /// 用户组部门成员数量。
    /// </summary>
    [JsonPropertyName("member_department_count")]
    public int MemberDepartmentCount { get; set; }

    /// <summary>
    /// 用户组类型。
    /// </summary>
    [JsonPropertyName("type")]
    public int Type { get; set; }
}