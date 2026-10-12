// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 获取企业内应用可用范围响应体数据。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class GetAppVisibilityResult
{
    /// <summary>
    /// <para>可使用应用的部门列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("departments")]
    public List<AppVisibilityIdItem>? Departments { get; set; }

    /// <summary>
    /// <para>不可使用应用的部门列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("invisible_departments")]
    public List<AppVisibilityIdItem>? InvisibleDepartments { get; set; }

    /// <summary>
    /// <para>可使用应用的用户列表（仅返回单独设置的成员，可用范围内部门和用户群中的成员不返回）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("users")]
    public List<AppVisibilityUser>? Users { get; set; }

    /// <summary>
    /// <para>不可使用应用的用户列表（仅返回单独设置的成员）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("invisible_users")]
    public List<AppVisibilityUser>? InvisibleUsers { get; set; }

    /// <summary>
    /// <para>可使用应用的用户群列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("groups")]
    public List<AppVisibilityIdItem>? Groups { get; set; }

    /// <summary>
    /// <para>不可使用应用的用户群列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("invisible_groups")]
    public List<AppVisibilityIdItem>? InvisibleGroups { get; set; }

    /// <summary>
    /// <para>应用是否全员可见。0：否；1：是</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_visible_to_all")]
    public int? IsVisibleToAll { get; set; }

    /// <summary>
    /// <para>是否还有更多可见用户。1：是；0：否</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("has_more_users")]
    public int? HasMoreUsers { get; set; }

    /// <summary>
    /// <para>拉取下一页用户使用的 user_page_token</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_page_token")]
    public string? UserPageToken { get; set; }
}

/// <summary>
/// 应用可用范围中的 ID 条目（部门或用户群）。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppVisibilityIdItem
{
    /// <summary>
    /// <para>自定义部门 ID 或用户群 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：od-aa2c50a04769feefededb7a05b7525a8</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

/// <summary>
/// 应用可用范围中的用户条目。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppVisibilityUser
{
    /// <summary>
    /// <para>用户的 user_id，仅已申请 user_id 权限的自建应用返回</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>用户的 open_id</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_d317f090b7258ad0372aa53963cda70d</para>
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }
}
