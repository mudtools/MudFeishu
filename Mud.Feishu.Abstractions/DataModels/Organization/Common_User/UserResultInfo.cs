// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.Organization;

/// <summary>
/// 用户操作结果信息类，包含飞书平台中用户的详细信息
/// </summary>
public class UserResultInfo
{
    /// <summary>
    /// <para>用户的 open_id，应用内用户的唯一标识。</para>
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    /// <summary>
    /// <para>用户的 union_id，是应用开发商发布的不同应用中同一用户的标识。</para>
    /// </summary>
    [JsonPropertyName("union_id")]
    public string? UnionId { get; set; }

    /// <summary>
    /// <para>用户的 user_id，租户内用户的唯一标识。</para>
    /// <para>**字段权限要求**：</para>
    /// <para>- contact:user.employee_id:readonly : 获取用户 user ID</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>用户名。</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 最小长度：`1` 字符</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>- contact:contact:readonly_as_app : 以应用身份读取通讯录</para>
    /// <para>- contact:user.base:readonly : 获取用户基本信息</para>
    /// <para>- contact:contact:access_as_app : 以应用身份访问通讯录</para>
    /// <para>- contact:contact:readonly : 读取通讯录</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>用户英文名。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>- contact:contact:readonly_as_app : 以应用身份读取通讯录</para>
    /// <para>- contact:user.base:readonly : 获取用户基本信息</para>
    /// <para>- contact:contact:access_as_app : 以应用身份访问通讯录</para>
    /// <para>- contact:contact:readonly : 读取通讯录</para>
    /// </summary>
    [JsonPropertyName("en_name")]
    public string? EnName { get; set; }

    /// <summary>
    /// <para>用户别名。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>- contact:contact:readonly_as_app : 以应用身份读取通讯录</para>
    /// <para>- contact:user.base:readonly : 获取用户基本信息</para>
    /// <para>- contact:contact:access_as_app : 以应用身份访问通讯录</para>
    /// <para>- contact:contact:readonly : 读取通讯录</para>
    /// </summary>
    [JsonPropertyName("nickname")]
    public string? Nickname { get; set; }

    /// <summary>
    /// <para>邮箱。</para>
    /// <para>**字段权限要求**：</para>
    /// <para>- contact:user.email:readonly : 获取用户邮箱信息</para>
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// <para>企业邮箱。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>- contact:contact:readonly_as_app : 以应用身份读取通讯录</para>
    /// <para>- contact:user.base:readonly : 获取用户基本信息</para>
    /// <para>- contact:contact:access_as_app : 以应用身份访问通讯录</para>
    /// <para>- contact:contact:readonly : 读取通讯录</para>
    /// </summary>
    [JsonPropertyName("enterprise_email")]
    public string? EnterpriseEmail { get; set; }

    /// <summary>
    /// <para>职务。</para>
    /// </summary>
    [JsonPropertyName("job_title")]
    public string? JobTitle { get; set; }

    /// <summary>
    /// <para>手机号。</para>
    /// <para>**字段权限要求**：</para>
    /// <para>- contact:user.phone:readonly : 获取用户手机号</para>
    /// </summary>
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    /// <summary>
    /// <para>性别。</para>
    /// <para>**可选值有**：</para>
    /// <para>0:未知,1:男,2:女,3:其他</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>- contact:contact:readonly_as_app : 以应用身份读取通讯录</para>
    /// <para>- contact:user.gender:readonly : 获取用户性别</para>
    /// <para>- contact:contact:access_as_app : 以应用身份访问通讯录</para>
    /// <para>- contact:contact:readonly : 读取通讯录</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：未知</item>
    /// <item>1：男</item>
    /// <item>2：女</item>
    /// <item>3：其他</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("gender")]
    public int? Gender { get; set; }

    /// <summary>
    /// <para>用户头像信息。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>- contact:contact:readonly_as_app : 以应用身份读取通讯录</para>
    /// <para>- contact:user.base:readonly : 获取用户基本信息</para>
    /// <para>- contact:contact:access_as_app : 以应用身份访问通讯录</para>
    /// <para>- contact:contact:readonly : 读取通讯录</para>
    /// </summary>
    [JsonPropertyName("avatar")]
    public UserAvatar? Avatar { get; set; }


    /// <summary>
    /// <para>用户状态。通过 is_frozen、is_resigned、is_activated、is_exited 布尔值类型参数进行展示。</para>
    /// <para>用户状态的转关逻辑可参见[用户资源介绍](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/user/field-overview#4302b5a1)。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>- contact:contact:readonly_as_app : 以应用身份读取通讯录</para>
    /// <para>- contact:user.employee:readonly : 获取用户受雇信息</para>
    /// <para>- contact:contact:access_as_app : 以应用身份访问通讯录</para>
    /// <para>- contact:contact:readonly : 读取通讯录</para>
    /// </summary>
    [JsonPropertyName("status")]
    public UserStatus? Status { get; set; }


    /// <summary>
    /// <para>用户所属部门的 ID 列表。部门 ID 类型为open_department_id，了解部门 ID 可参见[部门 ID 说明](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/department/field-overview#23857fe0)。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>- contact:contact:readonly_as_app : 以应用身份读取通讯录</para>
    /// <para>- contact:user.department:readonly : 获取用户组织架构信息</para>
    /// <para>- contact:contact:access_as_app : 以应用身份访问通讯录</para>
    /// <para>- contact:contact:readonly : 读取通讯录</para>
    /// </summary>
    [JsonPropertyName("department_ids")]
    public string[]? DepartmentIds { get; set; }

    /// <summary>
    /// <para>用户直属主管的用户 open_id 。了解用户 ID 可参见[用户相关的 ID 概念](https://open.feishu.cn/document/home/user-identity-introduction/introduction)。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>- contact:contact:readonly_as_app : 以应用身份读取通讯录</para>
    /// <para>- contact:user.employee:readonly : 获取用户受雇信息</para>
    /// <para>- contact:contact:access_as_app : 以应用身份访问通讯录</para>
    /// <para>- contact:contact:readonly : 读取通讯录</para>
    /// </summary>
    [JsonPropertyName("leader_user_id")]
    public string? LeaderUserId { get; set; }

    /// <summary>
    /// <para>城市。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>- contact:contact:readonly_as_app : 以应用身份读取通讯录</para>
    /// <para>- contact:user.employee:readonly : 获取用户受雇信息</para>
    /// <para>- contact:contact:access_as_app : 以应用身份访问通讯录</para>
    /// <para>- contact:contact:readonly : 读取通讯录</para>
    /// </summary>
    [JsonPropertyName("city")]
    public string? City { get; set; }

    /// <summary>
    /// <para>国家。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>- contact:contact:readonly_as_app : 以应用身份读取通讯录</para>
    /// <para>- contact:user.employee:readonly : 获取用户受雇信息</para>
    /// <para>- contact:contact:access_as_app : 以应用身份访问通讯录</para>
    /// <para>- contact:contact:readonly : 读取通讯录</para>
    /// </summary>
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    /// <summary>
    /// <para>工位。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>- contact:contact:readonly_as_app : 以应用身份读取通讯录</para>
    /// <para>- contact:user.employee:readonly : 获取用户受雇信息</para>
    /// <para>- contact:contact:access_as_app : 以应用身份访问通讯录</para>
    /// <para>- contact:contact:readonly : 读取通讯录</para>
    /// </summary>
    [JsonPropertyName("work_station")]
    public string? WorkStation { get; set; }

    /// <summary>
    /// <para>入职时间。秒级时间戳格式。</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 取值范围：`1` ～ `2147483647`</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>- contact:contact:readonly_as_app : 以应用身份读取通讯录</para>
    /// <para>- contact:user.employee:readonly : 获取用户受雇信息</para>
    /// <para>- contact:contact:access_as_app : 以应用身份访问通讯录</para>
    /// <para>- contact:contact:readonly : 读取通讯录</para>
    /// </summary>
    [JsonPropertyName("join_time")]
    public int? JoinTime { get; set; }

    /// <summary>
    /// <para>工号。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>- contact:user.employee:readonly : 获取用户受雇信息</para>
    /// <para>- contact:user.employee_number:read : 查看成员工号</para>
    /// <para>- contact:contact:readonly_as_app : 以应用身份读取通讯录</para>
    /// <para>- contact:contact:access_as_app : 以应用身份访问通讯录</para>
    /// <para>- contact:contact:readonly : 读取通讯录</para>
    /// </summary>
    [JsonPropertyName("employee_no")]
    public string? EmployeeNo { get; set; }

    /// <summary>
    /// <para>员工类型。</para>
    /// <para>**说明**：支持读取自定义员工类型的 int 值。如果该参数的取值不为 1 ~ 5，则你可调用[查询人员类型](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/employee_type_enum/list)接口查询相应的自定义员工类型信息（employee_type 对应 **查询人员类型** 接口返回的 enum_value）。</para>
    /// <para>**可选值有**：</para>
    /// <para>1:正式员工,2:实习生,3:外包,4:劳务,5:顾问</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>- contact:contact:readonly_as_app : 以应用身份读取通讯录</para>
    /// <para>- contact:user.employee:readonly : 获取用户受雇信息</para>
    /// <para>- contact:contact:access_as_app : 以应用身份访问通讯录</para>
    /// <para>- contact:contact:readonly : 读取通讯录</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：正式员工</item>
    /// <item>2：实习生</item>
    /// <item>3：外包</item>
    /// <item>4：劳务</item>
    /// <item>5：顾问</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("employee_type")]
    public int? EmployeeType { get; set; }

    /// <summary>
    /// <para>用户排序信息。用于标记通讯录下组织架构的人员顺序，人员可能存在多个部门中，且有不同的排序。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>- contact:contact:readonly_as_app : 以应用身份读取通讯录</para>
    /// <para>- contact:user.department:readonly : 获取用户组织架构信息</para>
    /// <para>- contact:contact:access_as_app : 以应用身份访问通讯录</para>
    /// <para>- contact:contact:readonly : 读取通讯录</para>
    /// </summary>
    [JsonPropertyName("orders")]
    public UserOrder[]? Orders { get; set; }

    /// <summary>
    /// <para>自定义字段信息。了解自定义字段可参见[自定义字段资源介绍](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/custom_attr/overview)。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>- contact:contact:readonly_as_app : 以应用身份读取通讯录</para>
    /// <para>- contact:user.employee:readonly : 获取用户受雇信息</para>
    /// <para>- contact:contact:access_as_app : 以应用身份访问通讯录</para>
    /// <para>- contact:contact:readonly : 读取通讯录</para>
    /// </summary>
    [JsonPropertyName("custom_attrs")]
    public UserCustomAttr[]? CustomAttrs { get; set; }

    /// <summary>
    /// <para>职级 ID。</para>
    /// <para>**字段权限要求**：</para>
    /// <para>- contact:user.job_level:readonly : 查询用户职级</para>
    /// </summary>
    [JsonPropertyName("job_level_id")]
    public string? JobLevelId { get; set; }

    /// <summary>
    /// <para>序列 ID。</para>
    /// <para>**字段权限要求**：</para>
    /// <para>- contact:user.job_family:readonly : 查询用户所属的工作序列</para>
    /// </summary>
    [JsonPropertyName("job_family_id")]
    public string? JobFamilyId { get; set; }

    /// <summary>
    /// <para>虚线上级的用户 ID。</para>
    /// <para>**字段权限要求**：</para>
    /// <para>- contact:user.dotted_line_leader_info.read : 查看成员的虚线上级 ID</para>
    /// </summary>
    [JsonPropertyName("dotted_line_leader_user_ids")]
    public string[]? DottedLineLeaderUserIds { get; set; }
}
