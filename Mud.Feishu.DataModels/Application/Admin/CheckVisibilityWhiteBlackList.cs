// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 查询用户、部门或用户组是否在应用的可用或禁用名单请求体。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class CheckVisibilityWhiteBlackListRequest
{
    /// <summary>
    /// <para>待查询的用户 ID 列表，按 user_id_type 填写，最多 100 个</para>
    /// <para>必填：否</para>
    /// <para>示例值：["ou_d317f090b7258ad0372aa53963cda70d"]</para>
    /// </summary>
    [JsonPropertyName("user_ids")]
    public List<string>? UserIds { get; set; }

    /// <summary>
    /// <para>待查询的部门 ID 列表，最多 100 个</para>
    /// <para>必填：否</para>
    /// <para>示例值：["od-aa2c50a04769feefededb7a05b7525a8"]</para>
    /// </summary>
    [JsonPropertyName("department_ids")]
    public List<string>? DepartmentIds { get; set; }

    /// <summary>
    /// <para>待查询的用户群 ID 列表，最多 100 个</para>
    /// <para>必填：否</para>
    /// <para>示例值：["96815a9cd9beg8g4"]</para>
    /// </summary>
    [JsonPropertyName("group_ids")]
    public List<string>? GroupIds { get; set; }
}

/// <summary>
/// 查询用户、部门或用户组是否在应用的可用或禁用名单响应体数据。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class CheckVisibilityWhiteBlackListResult
{
    /// <summary>
    /// <para>用户的黑白名单查询结果列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_visibility_list")]
    public List<UserVisibilityItem>? UserVisibilityList { get; set; }

    /// <summary>
    /// <para>部门的黑白名单查询结果列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("department_visibility_list")]
    public List<DepartmentVisibilityItem>? DepartmentVisibilityList { get; set; }

    /// <summary>
    /// <para>用户群的黑白名单查询结果列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("group_visibility_list")]
    public List<GroupVisibilityItem>? GroupVisibilityList { get; set; }
}

/// <summary>
/// 用户黑白名单查询结果。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class UserVisibilityItem
{
    /// <summary>
    /// <para>用户 ID，按 user_id_type 返回</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_d317f090b7258ad0372aa53963cda70d</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>是否在可用（白）名单中</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("in_white_list")]
    public bool? InWhiteList { get; set; }

    /// <summary>
    /// <para>是否在禁用（黑）名单中</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("in_black_list")]
    public bool? InBlackList { get; set; }

    /// <summary>
    /// <para>是否在付费名单中</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("in_paid_list")]
    public bool? InPaidList { get; set; }
}

/// <summary>
/// 部门黑白名单查询结果。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class DepartmentVisibilityItem
{
    /// <summary>
    /// <para>部门 ID，按 department_id_type 返回</para>
    /// <para>必填：否</para>
    /// <para>示例值：od-aa2c50a04769feefededb7a05b7525a8</para>
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }

    /// <summary>
    /// <para>是否在可用（白）名单中</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("in_white_list")]
    public bool? InWhiteList { get; set; }

    /// <summary>
    /// <para>是否在禁用（黑）名单中</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("in_black_list")]
    public bool? InBlackList { get; set; }
}

/// <summary>
/// 用户群黑白名单查询结果。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class GroupVisibilityItem
{
    /// <summary>
    /// <para>用户群 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：96815a9cd9beg8g4</para>
    /// </summary>
    [JsonPropertyName("group_id")]
    public string? GroupId { get; set; }

    /// <summary>
    /// <para>是否在可用（白）名单中</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("in_white_list")]
    public bool? InWhiteList { get; set; }

    /// <summary>
    /// <para>是否在禁用（黑）名单中</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("in_black_list")]
    public bool? InBlackList { get; set; }
}
