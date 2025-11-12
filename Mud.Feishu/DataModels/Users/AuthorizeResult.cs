namespace Mud.Feishu.DataModels.Users;

public class AuthorizeResult
{
    /// <summary>
    /// 授权码，用于获取 user_access_token。
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// 打开授权页时传入的 state 参数的原值，如未传入此处不会返回。
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }
}
