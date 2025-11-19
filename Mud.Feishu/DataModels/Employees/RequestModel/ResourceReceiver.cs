// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Employees;

/// <summary>
/// 离职员工的资源转移方式。
/// </summary>
public class DeleteEmployeeOptions
{
    /// <summary>
    /// 离职员工的资源转移方式。
    /// </summary>
    [JsonPropertyName("resigned_employee_resource_receiver")]
    public DeleteEmployeeResourceReceiver ResignedEmployeeResourceReceiver { get; set; } = new DeleteEmployeeResourceReceiver();
}

/// <summary>
/// 离职员工的资源转移方式。
/// </summary>
public class DeleteEmployeeResourceReceiver
{
    /// <summary>
    /// 部门群接收者。ID值与查询参数中的employee_id_type 对应。被删除用户为部门群群主时，转让群主给指定接收者，不指定接收者则默认转让给群内第一个入群的人。
    /// </summary>
    [JsonPropertyName("department_chat_acceptor_employee_id")]
    public string? DepartmentChatAcceptorEmployeeId { get; set; }

    /// <summary>
    /// 外部群接收者。ID值与查询参数中的employee_id_type 对应。被删除用户为外部群群主时，转让群主给指定接收者，不指定接收者则默认转让给群内与被删除用户在同一组织的第一个入群的人，如果组织内只有该用户在群里，则解散外部群。
    /// </summary>
    [JsonPropertyName("external_chat_acceptor_employee_id")]
    public string? ExternalChatAcceptorEmployeeId { get; set; }

    /// <summary>
    /// 文档接收者。ID值与查询参数中的employee_id_type 对应。用户被删除时，其拥有的文档转让给接收者。不指定接收者则将文档资源保留在该用户名下。
    /// </summary>
    [JsonPropertyName("docs_acceptor_employee_id")]
    public string? DocsAcceptorEmployeeId { get; set; }

    /// <summary>
    /// 日程接收者。ID值与查询参数中的employee_id_type 对应。用户被删除时，其拥有的日程转让给接收者。不指定接收者则将日程资源保留在该用户名下。
    /// </summary>
    [JsonPropertyName("calendar_acceptor_employee_id")]
    public string? CalendarAcceptorEmployeeId { get; set; }

    /// <summary>
    /// 应用接受者。ID值与查询参数中的employee_id_type 对应。用户被删除时，其创建的应用转让给接收者，不指定接收者则保留应用在该用户名下，但该用户无法登录开发者后台进行应用管理，管理员可以在管理后台手动转移应用给其他人。
    /// </summary>
    [JsonPropertyName("application_acceptor_employee_id")]
    public string? ApplicationAcceptorEmployeeId { get; set; }

    /// <summary>
    /// 服务台资源接收者。ID值与查询参数中的employee_id_type 对应。用户被删除时，其拥有的服务台资源转让给接收者，不指定接收者时保留服务台资源在该用户名下。
    /// </summary>
    [JsonPropertyName("helpdesk_acceptor_employee_id")]
    public string? HelpdeskAcceptorEmployeeId { get; set; }

    /// <summary>
    /// 审批资源接收者。ID值与查询参数中的employee_id_type 对应。用户被删除时，其拥有的审批资源转让给接收者，不指定接收者时保留审批资源在该用户名下。
    /// </summary>
    [JsonPropertyName("approval_acceptor_employee_id")]
    public string? ApprovalAcceptorEmployeeId { get; set; }

    /// <summary>
    /// 用户邮件资源接收者。ID值与查询参数中的employee_id_type 对应。用户被删除时，其拥有的邮件资源转让给接收者，不指定接受者则保留邮件资源在该用户名下。
    /// </summary>
    [JsonPropertyName("email_acceptor_employee_id")]
    public string? EmailAcceptorEmployeeId { get; set; }

    /// <summary>
    /// 妙记接收者。ID值与查询参数中的employee_id_type 对应。用户被删除时，其拥有的妙记资源转让给接收者。如果不指定接收者则将妙记保留在该用户名下。
    /// </summary>
    [JsonPropertyName("minutes_acceptor_employee_id")]
    public string? MinutesAcceptorEmployeeId { get; set; }

    /// <summary>
    /// 飞书问卷接收者。ID值与查询参数中的employee_id_type 对应。用户被删除时，其拥有的飞书问卷资源转让给接收者，不指定接收者则直接删除飞书问卷资源。
    /// </summary>
    [JsonPropertyName("survey_acceptor_employee_id")]
    public string? SurveyAcceptorEmployeeId { get; set; }

    /// <summary>
    /// 集成平台资源Owner
    /// </summary>
    [JsonPropertyName("anycross_acceptor_employee_id")]
    public string? AnycrossAcceptorEmployeeId { get; set; }
}
