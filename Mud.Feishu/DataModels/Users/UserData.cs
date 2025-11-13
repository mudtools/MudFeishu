
namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 用户基础数据模型，包含所有用户类共有的属性。
/// </summary>
public abstract class UserData
{
    /// <summary>
    /// 自定义用户的 user_id。长度不能超过 64 字符。
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// 用户名。长度不能超过 255 字符。
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// 英文名。长度不能超过 255 字符。
    /// </summary>
    [JsonPropertyName("en_name")]
    public string? EnName { get; set; }

    /// <summary>
    /// 别名。长度不能超过 255 字符。
    /// </summary>
    [JsonPropertyName("nickname")]
    public string? Nickname { get; set; }

    /// <summary>
    /// 邮箱。
    /// <para>当设置非中国大陆的手机号时，必须同时设置邮箱。</para>
    /// <para>在当前租户下，邮箱不可重复。</para>
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// 手机号。
    /// </summary>
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    /// <summary>
    /// 企业邮箱。
    /// </summary>
    [JsonPropertyName("enterprise_email")]
    public string? EnterpriseEmail { get; set; }

    /// <summary>
    /// 工号。字符长度上限为 255。
    /// </summary>
    [JsonPropertyName("employee_no")]
    public string? EmployeeNo { get; set; }
}
