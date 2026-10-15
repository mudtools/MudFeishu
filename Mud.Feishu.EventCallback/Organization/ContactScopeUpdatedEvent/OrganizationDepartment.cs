// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Organization;

/// <summary>
/// OrganizationDepartment
/// <para>部门对象信息。</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Organization")]
public class OrganizationDepartment
{
    /// <summary>
    /// <para>部门名称。</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 最小长度：`1` 字符</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取部门基础信息(contact:department.base:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>国际化的部门名称。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取部门基础信息(contact:department.base:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("i18n_name")]
    public OrganizationDepartmentI18nName? I18nName { get; set; }

    /// <summary>
    /// <para>父部门的 ID。取值为 0 表示当前部门的父部门为根部门。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取通讯录部门组织架构信息(contact:department.organize:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("parent_department_id")]
    public string? ParentDepartmentId { get; set; }

    /// <summary>
    /// <para>当前部门的自定义部门 ID。</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 最大长度：`64` 字符</para>
    /// <para>- 正则校验：`^[a-zA-Z0-9][a-zA-Z0-9_\-@.]{0,63}$`</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取部门基础信息(contact:department.base:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }

    /// <summary>
    /// <para>部门的 open_department_id。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("open_department_id")]
    public string? OpenDepartmentId { get; set; }

    /// <summary>
    /// <para>部门主管的 open_id。关于用户 ID 的说明可参见 [用户相关的 ID 概念](https://open.feishu.cn/document/home/user-identity-introduction/introduction)。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取通讯录部门组织架构信息(contact:department.organize:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("leader_user_id")]
    public string? LeaderUserId { get; set; }

    /// <summary>
    /// <para>部门群 ID。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取部门基础信息(contact:department.base:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("chat_id")]
    public string? ChatId { get; set; }

    /// <summary>
    /// <para>部门的排序，即部门在其同级部门的展示顺序。取值越小排序越靠前。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取通讯录部门组织架构信息(contact:department.organize:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("order")]
    public string? Order { get; set; }

    /// <summary>
    /// <para>部门单位的自定义 ID 列表。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取通讯录部门组织架构信息(contact:department.organize:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("unit_ids")]
    public string[]? UnitIds { get; set; }

    /// <summary>
    /// <para>部门下用户的个数。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取通讯录部门组织架构信息(contact:department.organize:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("member_count")]
    public int? MemberCount { get; set; }

    /// <summary>
    /// <para>部门状态。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取部门基础信息(contact:department.base:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("status")]
    public OrganizationDepartmentStatus? Status { get; set; }

    /// <summary>
    /// <para>部门负责人信息。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("leaders")]
    public OrganizationDepartmentLeader[]? Leaders { get; set; }

    /// <summary>
    /// <para>部门群雇员类型限制。当该字段列表为空时，表示为无任何雇员类型。类型字段可包含以下值：</para>
    /// <para>- 1：正式员工</para>
    /// <para>- 2：实习生</para>
    /// <para>- 3：外包</para>
    /// <para>- 4：劳务</para>
    /// <para>- 5：顾问</para>
    /// <para>- 6：其他自定义类型字段。你可以调用[获取人员类型](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/employee_type_enum/list)接口，获取到该租户的自定义员工类型的名称。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("group_chat_employee_types")]
    public int[]? GroupChatEmployeeTypes { get; set; }

    /// <summary>
    /// <para>部门下主属用户的个数。</para>
    /// <para>**字段权限要求（满足任一）**：</para>
    /// <para>以应用身份读取通讯录(contact:contact:readonly_as_app)</para>
    /// <para>获取通讯录部门组织架构信息(contact:department.organize:readonly)</para>
    /// <para>以应用身份访问通讯录(contact:contact:access_as_app)</para>
    /// <para>读取通讯录(contact:contact:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("primary_member_count")]
    public int? PrimaryMemberCount { get; set; }
}
