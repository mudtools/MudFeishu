// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 角色详情（获取角色详情响应体）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class RoleDetail
{
    /// <summary>
    /// <para>角色 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>角色名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public I18nName? Name { get; set; }

    /// <summary>
    /// <para>角色描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description")]
    public I18nName? Description { get; set; }

    /// <summary>
    /// <para>更新时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("modify_time")]
    public string? ModifyTime { get; set; }

    /// <summary>
    /// <para>角色启用状态</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("role_status")]
    public int? RoleStatus { get; set; }

    /// <summary>
    /// <para>角色类型</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("role_type")]
    public int? RoleType { get; set; }

    /// <summary>
    /// <para>角色适用范围</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("scope_of_application")]
    public int? ScopeOfApplication { get; set; }

    /// <summary>
    /// <para>是否在角色上配置业务管理范围：true 已配置 / false 未配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("has_business_management_scope")]
    public bool? HasBusinessManagementScope { get; set; }

    /// <summary>
    /// <para>社招权限配置，仅当 scope_of_application 为「社招」或「都包含」时有值</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("socail_permission_collection")]
    public PermissionCollection? SocailPermissionCollection { get; set; }

    /// <summary>
    /// <para>校招权限配置，仅当 scope_of_application 为「校招」或「都包含」时有值（字段名保留官方拼写）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("campus_permission_collection")]
    public PermissionCollection? CampusPermissionCollection { get; set; }
}
