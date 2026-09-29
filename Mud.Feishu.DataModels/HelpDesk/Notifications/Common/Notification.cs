// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.HelpDesk;

/// <summary>
/// <para>服务台推送任务</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "HelpDesk")]
public class Notification
{
    /// <summary>
    /// <para>非必填，创建成功后返回</para>
    /// <para>必填：否</para>
    /// <para>**示例值**："6981801914270744596"</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>任务名称</para>
    /// <para>必填：是</para>
    /// <para>**示例值**："Test the push task"</para>
    /// </summary>
    [JsonPropertyName("job_name")]
    public string? JobName { get; set; }

    /// <summary>
    /// <para>非必填，创建成功后返回</para>
    /// <para>必填：否</para>
    /// <para>**示例值**：0</para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// <para>创建人</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("create_user")]
    public NotificationUser? CreateUser { get; set; }

    /// <summary>
    /// <para>创建时间（时间戳，毫秒）</para>
    /// <para>必填：否</para>
    /// <para>**示例值**："1626332244719"</para>
    /// </summary>
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    /// <summary>
    /// <para>更新人</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("update_user")]
    public NotificationUser? UpdateUser { get; set; }

    /// <summary>
    /// <para>最近更新时间（时间戳，毫秒）</para>
    /// <para>必填：否</para>
    /// <para>**示例值**："1626332244719"</para>
    /// </summary>
    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    /// <summary>
    /// <para>推送目标用户总数</para>
    /// <para>必填：否</para>
    /// <para>**示例值**：1</para>
    /// </summary>
    [JsonPropertyName("target_user_count")]
    public int? TargetUserCount { get; set; }

    /// <summary>
    /// <para>已推送用户总数</para>
    /// <para>必填：否</para>
    /// <para>**示例值**：1</para>
    /// </summary>
    [JsonPropertyName("sent_user_count")]
    public int? SentUserCount { get; set; }

    /// <summary>
    /// <para>已读用户总数</para>
    /// <para>必填：否</para>
    /// <para>**示例值**：1</para>
    /// </summary>
    [JsonPropertyName("read_user_count")]
    public int? ReadUserCount { get; set; }

    /// <summary>
    /// <para>推送任务触发时间（时间戳，毫秒）</para>
    /// <para>必填：否</para>
    /// <para>**示例值**："1626332244719"</para>
    /// </summary>
    [JsonPropertyName("send_at")]
    public string? SendAt { get; set; }

    /// <summary>
    /// <para>推送内容，详情见 <see href="https://open.feishu.cn/tool/cardbuilder?from=howtoguide">卡片搭建工具</see></para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("push_content")]
    public string? PushContent { get; set; }

    /// <summary>
    /// <para>推送类型。0（定时推送：push_scope 不能等于 3）、1（新员工入职推送：push_scope 必须等于 1 或 3；new_staff_scope_type 不能为空）</para>
    /// <para>必填：是</para>
    /// <para>**示例值**：0</para>
    /// </summary>
    [JsonPropertyName("push_type")]
    public int? PushType { get; set; }

    /// <summary>
    /// <para>推送范围（服务台私信）。0：组织内全部成员（user_list 和 department_list 必须为空）、1：无任何成员（user_list 和 department_list 必须为空，chat_list 不能为空）、2：指定成员（user_list 或 department_list 不能为空）、3：新员工。这四种范围的 chat_list 相对独立，且仅在推送范围为 1 时必填。</para>
    /// <para>必填：是</para>
    /// <para>**示例值**：0</para>
    /// </summary>
    [JsonPropertyName("push_scope_type")]
    public int? PushScopeType { get; set; }

    /// <summary>
    /// <para>新员工入职范围类型（push_type 为 1 时生效）。0：组织内全部新员工、1：组织内指定部门（new_staff_scope_department_list 字段不能为空）</para>
    /// <para>必填：否</para>
    /// <para>**示例值**：0</para>
    /// </summary>
    [JsonPropertyName("new_staff_scope_type")]
    public int? NewStaffScopeType { get; set; }

    /// <summary>
    /// <para>入职新员工有效部门列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("new_staff_scope_department_list")]
    public NotificationDepartment[]? NewStaffScopeDepartmentList { get; set; }

    /// <summary>
    /// <para>推送的成员列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_list")]
    public NotificationUser[]? UserList { get; set; }

    /// <summary>
    /// <para>推送的部门信息列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("department_list")]
    public NotificationDepartment[]? DepartmentList { get; set; }

    /// <summary>
    /// <para>推送的会话列表（群）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("chat_list")]
    public NotificationChat[]? ChatList { get; set; }

    /// <summary>
    /// <para>预留的扩展字段</para>
    /// <para>必填：否</para>
    /// <para>**示例值**："{}"</para>
    /// </summary>
    [JsonPropertyName("ext")]
    public string? Ext { get; set; }
}
