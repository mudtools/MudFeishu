// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 查询应用管理员列表响应体数据。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class GetAppAdminUsersResult
{
    /// <summary>
    /// <para>应用管理员列表（返回最近的 10 个管理员账号）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_list")]
    public List<AppAdminUser>? UserList { get; set; }
}

/// <summary>
/// 应用管理员的用户 ID 信息。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppAdminUser
{
    /// <summary>
    /// <para>管理员的 open_id</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_a3b5bea08516cc528cd5546030344a</para>
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    /// <summary>
    /// <para>管理员的 user_id</para>
    /// <para>必填：否</para>
    /// <para>示例值：e33ggbsd</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>管理员的 union_id</para>
    /// <para>必填：否</para>
    /// <para>示例值：on_8ed6aa67826108097d9ee143819da2</para>
    /// </summary>
    [JsonPropertyName("union_id")]
    public string? UnionId { get; set; }
}

/// <summary>
/// 获取应用管理员管理范围响应体数据。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class GetAppAdminScopeResult
{
    /// <summary>
    /// <para>管理员是否管理全部部门。true 时 departments_list 不返回</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_all")]
    public bool? IsAll { get; set; }

    /// <summary>
    /// <para>管理员管理的部门列表，is_all 为 false 时返回</para>
    /// <para>必填：否</para>
    /// <para>示例值：["od-a140b4eeb892b90a0ab3e616fc2054d6"]</para>
    /// </summary>
    [JsonPropertyName("departments_list")]
    public List<string>? DepartmentsList { get; set; }
}

/// <summary>
/// 校验应用管理员响应体数据。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class VerifyAppAdminResult
{
    /// <summary>
    /// <para>用户是否为应用管理员。true：是；false：否。此处的管理员指可进入企业管理后台审核与管理应用的管理员，而非应用开发者</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_app_admin")]
    public bool? IsAppAdmin { get; set; }
}
