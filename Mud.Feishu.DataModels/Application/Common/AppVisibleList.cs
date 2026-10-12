// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 可见范围的用户与部门 ID 列表（应用开发配置，v7 请求侧）。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppVisibleList
{
    /// <summary>
    /// <para>用户 ID 列表，类型由 user_id_type 指定</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_ids")]
    public List<string>? UserIds { get; set; }

    /// <summary>
    /// <para>部门 ID 列表，类型由 department_id_type 指定</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("department_ids")]
    public List<string>? DepartmentIds { get; set; }
}

/// <summary>
/// 可见范围的用户、部门与群组 ID 列表（应用版本侧，用户为 open_id）。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class ContactsRangeVisibleList
{
    /// <summary>
    /// <para>用户 open_id 列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("open_ids")]
    public List<string>? OpenIds { get; set; }

    /// <summary>
    /// <para>部门 ID 列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("department_ids")]
    public List<string>? DepartmentIds { get; set; }

    /// <summary>
    /// <para>群 ID 列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("group_ids")]
    public List<string>? GroupIds { get; set; }
}

/// <summary>
/// 应用通讯录权限范围。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class ContactsRange
{
    /// <summary>
    /// <para>通讯录权限范围类型</para>
    /// <para>必填：否</para>
    /// <para>可选值：all（全部成员）、equal_to_availability（与应用可用范围一致）、some（部分成员）</para>
    /// <para>示例值：some</para>
    /// </summary>
    [JsonPropertyName("contacts_scope_type")]
    public string? ContactsScopeType { get; set; }

    /// <summary>
    /// <para>可见范围成员列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("visible_list")]
    public ContactsRangeVisibleList? VisibleList { get; set; }
}
