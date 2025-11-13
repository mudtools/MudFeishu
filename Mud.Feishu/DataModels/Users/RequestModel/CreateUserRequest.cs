namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 创建用户请求体。
/// </summary>
public class CreateUserRequest : UserBaseRequest
{
    /// <summary>
    /// 手机号。
    /// </summary>
    [JsonPropertyName("mobile")]
    public new required string Mobile { get; set; }

    /// <summary>
    /// 工号。字符长度上限为 255。
    /// </summary>
    [JsonPropertyName("employee_no")]
    public new string? EmployeeNo { get; set; }

    /// <summary>
    /// 企业邮箱。
    /// </summary>
    [JsonPropertyName("enterprise_email")]
    public new string? EnterpriseEmail { get; set; }

    /// <summary>
    /// 数据驻留地。
    /// </summary>
    [JsonPropertyName("geo")]
    public string? Geo { get; set; }
}



