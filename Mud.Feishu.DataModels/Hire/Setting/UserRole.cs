// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 用户角色信息（获取用户角色列表响应体）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class UserRole
{
    /// <summary>
    /// <para>用户 ID，与入参 user_id_type 类型一致</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>角色 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("role_id")]
    public string? RoleId { get; set; }

    /// <summary>
    /// <para>修改时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("modify_time")]
    public string? ModifyTime { get; set; }

    /// <summary>
    /// <para>角色名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("role_name")]
    public I18nName? RoleName { get; set; }

    /// <summary>
    /// <para>角色描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("role_description")]
    public I18nName? RoleDescription { get; set; }

    /// <summary>
    /// <para>业务管理范围</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("business_management_scopes")]
    public UserBusinessManagementScope[]? BusinessManagementScopes { get; set; }
}
