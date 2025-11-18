using Mud.Feishu.DataModels.Users;

namespace Mud.Feishu.DataModels.Departments;

/// <summary>
/// 部门创建更新请求基类，包含部门请求的通用属性。
/// </summary>
public abstract class DepartmentRequestBase
{
    /// <summary>
    /// 部门名称。
    /// <para>注意：不可包含斜杠（/）。不能与存量部门名称重复。</para> 
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 部门名称的国际化配置。
    /// </summary>
    [JsonPropertyName("i18n_name")]
    public I18nName? I18nName { get; set; }

    /// <summary>
    /// 父部门的 ID，ID 类型与查询参数的 department_id_type 取值一致。
    /// <para>如果当前是在根部门下创建部门，则该参数值为 0。</para>
    /// </summary>
    [JsonPropertyName("parent_department_id")]
    public string ParentDepartmentId { get; set; } = string.Empty;

    /// <summary>
    /// 部门主管的用户 ID。
    /// </summary>
    [JsonPropertyName("leader_user_id")]
    public string? LeaderUserId { get; set; }

    /// <summary>
    /// 部门的排序，即部门在其同级部门的展示顺序。取值格式为 String 类型的非负整数，数值越小，排序越靠前。
    /// </summary>
    [JsonPropertyName("order")]
    public string? Order { get; set; }

    /// <summary>
    /// 是否创建部门群。
    /// </summary>
    [JsonPropertyName("create_group_chat")]
    public bool CreateGroupChat { get; set; } = false;

    /// <summary>
    /// 部门负责人信息。
    /// </summary>
    [JsonPropertyName("leaders")]
    public List<DepartmentLeader> Leaders { get; set; } = [];

    /// <summary>
    /// 部门群的人员类型限制。
    /// </summary>
    /// <remarks> 
    /// <para>人员类型的取值范围如下。该参数支持设置多个类型值，若有多个，用英文 , 分隔：</para>
    /// <para>1：正式员工 2：实习生 3：外包 4：劳务 5：顾问</para>
    /// </remarks>
    [JsonPropertyName("group_chat_employee_types")]
    public List<int> GroupChatEmployeeTypes { get; set; } = [];
}