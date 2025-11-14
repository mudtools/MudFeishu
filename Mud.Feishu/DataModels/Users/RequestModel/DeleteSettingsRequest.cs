namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 用户删除参数请求体。
/// </summary>
public class DeleteSettingsRequest
{
    /// <summary>
    /// 部门群接收者的用户 ID。被删除用户为部门群群主时，转让群主给指定接收者。
    /// </summary>
    [JsonPropertyName("department_chat_acceptor_user_id")]
    public string? DepartmentChatAcceptorUserId { get; set; }

    /// <summary>
    /// 外部群接收者的用户 ID。被删除用户为外部群群主时，转让群主给指定接收者。
    /// </summary>
    [JsonPropertyName("external_chat_acceptor_user_id")]
    public string? ExternalChatAcceptorUserId { get; set; }

    /// <summary>
    /// 文档接收者的用户 ID。用户被删除时，其拥有的文档转让给接收者。
    /// </summary>
    [JsonPropertyName("docs_acceptor_user_id")]
    public string? DocsAcceptorUserId { get; set; }

    /// <summary>
    /// 日程接收者的用户 ID。用户被删除时，其拥有的日程转让给接收者。
    /// </summary>
    [JsonPropertyName("calendar_acceptor_user_id")]
    public string? CalendarAcceptorUserId { get; set; }

    /// <summary>
    /// 应用接受者的用户 ID。用户被删除时，其创建的应用转让给接收者。
    /// </summary>
    [JsonPropertyName("application_acceptor_user_id")]
    public string? ApplicationAcceptorUserId { get; set; }

    /// <summary>
    /// 妙记接收者的用户 ID。用户被删除时，其拥有的妙记资源转让给接收者。
    /// </summary>
    [JsonPropertyName("minutes_acceptor_user_id")]
    public string? MinutesAcceptorUserId { get; set; }

    /// <summary>
    /// 飞书问卷接收者的用户 ID。用户被删除时，其拥有的飞书问卷资源转让给接收者。
    /// </summary>
    [JsonPropertyName("survey_acceptor_user_id")]
    public string? SurveyAcceptorUserId { get; set; }

    /// <summary>
    /// 用户邮件资源的处理方式。
    /// </summary>
    [JsonPropertyName("email_acceptor")]
    public EmailAcceptor? EmailAcceptor { get; set; }

    /// <summary>
    /// 用户集成平台资源的接收者的用户 ID。
    /// </summary>
    [JsonPropertyName("anycross_acceptor_user_id")]
    public string? AnycrossAcceptorUserId { get; set; }
}

/// <summary>
/// 用户邮件资源的处理方式。
/// </summary>
public class EmailAcceptor
{
    /// <summary>
    /// 处理方式。可选值有：1：转移资源 2：保留资源 3：删除资源
    /// </summary>
    [JsonPropertyName("processing_type")]
    public string? ProcessingType { get; set; }

    /// <summary>
    /// 邮件资源接收者的用户 ID。ID 类型需要与查询参数中的 user_id_type 类型保持一致。
    /// </summary>
    [JsonPropertyName("acceptor_user_id")]
    public string? AcceptorUserId { get; set; }
}
