// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.TrustParty;

/// <summary>
/// 关联组织部门详情
/// </summary>
[HttpJsonSerializable(SerializerClassName = "TrustParty")]
public class CollaborationDepartment
{
    /// <summary>
    /// 关联组织的部门open id
    /// </summary>
    [JsonPropertyName("open_department_id")]
    public string? OpenDepartmentId { get; set; }

    /// <summary>
    /// 关联组织的部门id
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }

    /// <summary>
    /// 关联组织的部门名称
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// 目标组织的 i18n 部门名称
    /// </summary>
    [JsonPropertyName("i18n_name")]
    public I18nName? I18nName { get; set; }

    /// <summary>
    /// 关联组织的部门排序
    /// </summary>
    [JsonPropertyName("order")]
    public string? Order { get; set; }

    /// <summary>
    /// 部门负责人列表，必须对负责人有可见性权限才会返回
    /// </summary>
    [JsonPropertyName("leaders")]
    public CollaborationDepartmentLeader[]? Leaders { get; set; }

    /// <summary>
    /// 父部门ID，必须对父部门有可见性权限才会返回
    /// </summary>
    [JsonPropertyName("parent_department_id")]
    public CollaborationDepartmentId? ParentDepartmentId { get; set; }
}
