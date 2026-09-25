// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.TrustParty;

/// <summary>
/// 关联组织可见实体（部门/用户/用户组），同一实体在不同类型下仅对应字段有值
/// </summary>
[HttpJsonSerializable(SerializerClassName = "TrustParty")]
public class CollaborationEntity
{
    /// <summary>
    /// 关联组织实体类型：user（用户）/ department（部门）/ group（用户组）
    /// </summary>
    [JsonPropertyName("collaboration_entity_type")]
    public string? CollaborationEntityType { get; set; }

    /// <summary>
    /// 部门ID
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }

    /// <summary>
    /// 部门的open ID
    /// </summary>
    [JsonPropertyName("open_department_id")]
    public string? OpenDepartmentId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// 用户的open ID
    /// </summary>
    [JsonPropertyName("open_user_id")]
    public string? OpenUserId { get; set; }

    /// <summary>
    /// 用户的union_id
    /// </summary>
    [JsonPropertyName("union_user_id")]
    public string? UnionUserId { get; set; }

    /// <summary>
    /// 部门名称
    /// </summary>
    [JsonPropertyName("department_name")]
    public string? DepartmentName { get; set; }

    /// <summary>
    /// 目标组织的部门 i18n 名称
    /// </summary>
    [JsonPropertyName("i18n_department_name")]
    public I18nName? I18nDepartmentName { get; set; }

    /// <summary>
    /// 部门顺序
    /// </summary>
    [JsonPropertyName("department_order")]
    public string? DepartmentOrder { get; set; }

    /// <summary>
    /// 对方成员名称
    /// </summary>
    [JsonPropertyName("user_name")]
    public string? UserName { get; set; }

    /// <summary>
    /// 目标组织的成员 i18n 名称
    /// </summary>
    [JsonPropertyName("i18n_user_name")]
    public I18nName? I18nUserName { get; set; }

    /// <summary>
    /// 成员头像信息
    /// </summary>
    [JsonPropertyName("user_avatar")]
    public AvatarInfo? UserAvatar { get; set; }

    /// <summary>
    /// 用户组ID
    /// </summary>
    [JsonPropertyName("group_id")]
    public string? GroupId { get; set; }

    /// <summary>
    /// 用户组的open ID
    /// </summary>
    [JsonPropertyName("open_group_id")]
    public string? OpenGroupId { get; set; }

    /// <summary>
    /// 对方用户组名称
    /// </summary>
    [JsonPropertyName("group_name")]
    public string? GroupName { get; set; }

    /// <summary>
    /// 目标组织的用户组 i18n 名称
    /// </summary>
    [JsonPropertyName("i18n_group_name")]
    public I18nName? I18nGroupName { get; set; }
}
