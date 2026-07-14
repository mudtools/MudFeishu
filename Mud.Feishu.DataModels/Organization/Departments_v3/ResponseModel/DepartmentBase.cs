// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Departments;

/// <summary>
/// 部门基础信息类，包含部门的基本信息和配置。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Organization")]
public abstract class DepartmentBase
{
    /// <summary>
    /// 部门名称。
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 部门国际化名称。
    /// </summary>
    [JsonPropertyName("i18n_name")]
    public I18nName I18nName { get; set; } = new I18nName();

    /// <summary>
    /// 父部门ID。
    /// </summary>
    [JsonPropertyName("parent_department_id")]
    public string ParentDepartmentId { get; set; } = string.Empty;

    /// <summary>
    /// 部门ID。
    /// </summary>
    [JsonPropertyName("department_id")]
    public string DepartmentId { get; set; } = string.Empty;

    /// <summary>
    /// 开放部门ID。
    /// </summary>
    [JsonPropertyName("open_department_id")]
    public string OpenDepartmentId { get; set; } = string.Empty;

    /// <summary>
    /// 部门负责人用户ID。
    /// </summary>
    [JsonPropertyName("leader_user_id")]
    public string LeaderUserId { get; set; } = string.Empty;

    /// <summary>
    /// 部门群聊ID。
    /// </summary>
    [JsonPropertyName("chat_id")]
    public string ChatId { get; set; } = string.Empty;

    /// <summary>
    /// 部门排序号。
    /// </summary>
    [JsonPropertyName("order")]
    public string Order { get; set; } = string.Empty;

    /// <summary>
    /// 部门关联的单位ID列表。
    /// </summary>
    [JsonPropertyName("unit_ids")]
    public List<string> UnitIds { get; set; } = new List<string>();

    /// <summary>
    /// 部门成员数量。
    /// </summary>
    [JsonPropertyName("member_count")]
    public int MemberCount { get; set; }

    /// <summary>
    /// 部门状态。
    /// </summary>
    [JsonPropertyName("status")]
    public DepartmentStatus Status { get; set; } = new DepartmentStatus();

    /// <summary>
    /// 部门负责人列表。
    /// </summary>
    [JsonPropertyName("leaders")]
    public List<DepartmentLeader> Leaders { get; set; } = new List<DepartmentLeader>();
}
