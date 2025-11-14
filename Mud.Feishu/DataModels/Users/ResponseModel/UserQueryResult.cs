namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 用户查询结果，包含手机号或者邮箱对应的用户 ID 信息。
/// </summary>
public class UserQueryResult
{
    /// <summary>
    /// 自定义用户的 user_id。长度不能超过 64 字符。
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

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
    /// 用户状态信息。
    /// </summary>
    [JsonPropertyName("status")]
    public UserStatus? Status { get; set; }
}
