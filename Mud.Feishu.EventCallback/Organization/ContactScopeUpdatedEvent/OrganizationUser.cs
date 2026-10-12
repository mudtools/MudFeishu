// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Organization;

/// <summary>
/// OrganizationUser
/// <para>用户对象信息。</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Organization")]
public class OrganizationUser
{
    /// <summary>
    /// <para>用户的 union_id，应用开发商发布的不同应用中同一用户的标识。关于用户 ID 的说明可参见 [用户相关的 ID 概念](https://open.feishu.cn/document/home/user-identity-introduction/introduction)。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("union_id")]
    public string? UnionId { get; set; }

    /// <summary>
    /// <para>用户的 user_id，租户内用户的唯一标识。关于用户 ID 的说明可参见 [用户相关的 ID 概念](https://open.feishu.cn/document/home/user-identity-introduction/introduction)。</para>
    /// <para>**字段权限要求**：</para>
    /// <para>获取用户 user ID(contact:user.employee_id:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>用户的 open_id，应用内用户的唯一标识。关于用户 ID 的说明可参见 [用户相关的 ID 概念](https://open.feishu.cn/document/home/user-identity-introduction/introduction)。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    /// <summary>
    /// <para>用户名。</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 最小长度：`1` 字符</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取用户基本信息(contact:user.base:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>英文名。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取用户基本信息(contact:user.base:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("en_name")]
    public string? EnName { get; set; }

    /// <summary>
    /// <para>别名。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取用户基本信息(contact:user.base:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("nickname")]
    public string? Nickname { get; set; }

    /// <summary>
    /// <para>邮箱。</para>
    /// <para>**字段权限要求**：</para>
    /// <para>获取用户邮箱信息(contact:user.email:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// <para>手机号。</para>
    /// <para>**字段权限要求**：</para>
    /// <para>获取用户手机号(contact:user.phone:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    /// <summary>
    /// <para>性别。</para>
    /// <para>**可选值有**：</para>
    /// <para>- 0：保密。</para>
    /// <para>- 1：男。</para>
    /// <para>- 2：女。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取用户性别(contact:user.gender:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("gender")]
    public int? Gender { get; set; }

    /// <summary>
    /// <para>用户头像信息。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取用户基本信息(contact:user.base:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("avatar")]
    public OrganizationAvatarInfo? Avatar { get; set; }

    /// <summary>
    /// <para>用户状态。通过 is_frozen、is_resigned、is_activated、is_exited 布尔值类型参数进行展示。</para>
    /// <para>用户状态的流转逻辑可参见[用户资源介绍](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/user/field-overview#4302b5a1)。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取用户受雇信息(contact:user.employee:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("status")]
    public OrganizationUserStatus? Status { get; set; }

    /// <summary>
    /// <para>用户的直接主管的 open_id。</para>
    /// <para>关于用户 ID 的说明可参见 [用户相关的 ID 概念](https://open.feishu.cn/document/home/user-identity-introduction/introduction)。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取用户组织架构信息(contact:user.department:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("leader_user_id")]
    public string? LeaderUserId { get; set; }

    /// <summary>
    /// <para>工作城市。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取用户受雇信息(contact:user.employee:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("city")]
    public string? City { get; set; }

    /// <summary>
    /// <para>国家或地区 Code 缩写。具体格式可参见 [国家/地区码表](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/user/country-code-description)。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取用户受雇信息(contact:user.employee:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    /// <summary>
    /// <para>工位。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取用户受雇信息(contact:user.employee:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("work_station")]
    public string? WorkStation { get; set; }

    /// <summary>
    /// <para>入职时间。秒级时间戳格式，表示从 1970 年 1 月 1 日开始所经过的秒数。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取用户受雇信息(contact:user.employee:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("join_time")]
    public int? JoinTime { get; set; }

    /// <summary>
    /// <para>工号。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>获取用户受雇信息(contact:user.employee:readonly)</para>
    /// <para>查看成员工号(contact:user.employee_number:read)</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("employee_no")]
    public string? EmployeeNo { get; set; }

    /// <summary>
    /// <para>员工类型。</para>
    /// <para>**可能值有：**</para>
    /// <para>- `1`：正式员工</para>
    /// <para>- `2`：实习生</para>
    /// <para>- `3`：外包</para>
    /// <para>- `4`：劳务</para>
    /// <para>- `5`：顾问</para>
    /// <para>同时可读取到自定义员工类型的 int 值，通过 int 值调用[获取人员类型](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/employee_type_enum/list)接口，进而获取到该租户的自定义员工类型的名称。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取用户受雇信息(contact:user.employee:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("employee_type")]
    public int? EmployeeType { get; set; }

    /// <summary>
    /// <para>自定义字段。如果企业管理员已在管理后台 &gt; 组织架构 &gt; 成员字段管理 &gt; 自定义字段管理 &gt; 全局设置中开启了 **允许开放平台 API 调用**，则该字段生效。</para>
    /// <para>更多信息可参见[用户接口相关问题](https://open.feishu.cn/document/ugTN1YjL4UTN24CO1UjN/uQzN1YjL0cTN24CN3UjN#77061525)。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取用户受雇信息(contact:user.employee:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("custom_attrs")]
    public OrganizationUserCustomAttr[]? CustomAttrs { get; set; }

    /// <summary>
    /// <para>企业邮箱。如果企业管理员已在管理后台启用飞书邮箱服务，则会返回该值。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取用户受雇信息(contact:user.employee:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("enterprise_email")]
    public string? EnterpriseEmail { get; set; }

    /// <summary>
    /// <para>职务。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取用户受雇信息(contact:user.employee:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_title")]
    public string? JobTitle { get; set; }

    /// <summary>
    /// <para>是否为暂停状态的用户。</para>
    /// <para>**可能值有：**</para>
    /// <para>- true：是</para>
    /// <para>- false：否</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_frozen")]
    public bool? IsFrozen { get; set; }

    /// <summary>
    /// <para>职级 ID。</para>
    /// <para>**字段权限要求**：</para>
    /// <para>查询用户职级(contact:user.job_level:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_level_id")]
    public string? JobLevelId { get; set; }

    /// <summary>
    /// <para>序列 ID。</para>
    /// <para>**字段权限要求**：</para>
    /// <para>查询用户所属的工作序列(contact:user.job_family:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_family_id")]
    public string? JobFamilyId { get; set; }

    /// <summary>
    /// <para>分配给用户的席位 ID 列表。需开通 **分配用户席位** 权限。</para>
    /// <para>**字段权限要求**：</para>
    /// <para>分配用户席位(contact:user.subscription_ids:write)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("subscription_ids")]
    public string[]? SubscriptionIds { get; set; }

    /// <summary>
    /// <para>用户席位列表。</para>
    /// <para>**字段权限要求**：</para>
    /// <para>查询用户席位信息(contact:user.assign_info:read)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("assign_info")]
    public OrganizationUserAssignInfo[]? AssignInfo { get; set; }

    /// <summary>
    /// <para>部门路径。</para>
    /// <para>**字段权限要求**：</para>
    /// <para>获取成员所在部门路径(contact:user.department_path:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("department_path")]
    public OrganizationDepartmentDetail[]? DepartmentPath { get; set; }
}
