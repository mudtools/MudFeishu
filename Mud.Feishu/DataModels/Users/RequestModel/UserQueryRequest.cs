namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 用户信息查询请求体
/// </summary>
public class UserQueryRequest
{
    /// <summary>
    /// 要查询的用户邮箱，最多可传入 50 条。
    /// </summary>
    [JsonPropertyName("emails")]
    public List<string> Emails { get; set; } = [];

    /// <summary>
    /// 要查询的用户手机号，最多可传入 50 条。
    /// </summary>
    [JsonPropertyName("mobiles")]
    public List<string> Mobiles { get; set; } = [];

    /// <summary>
    /// 查询结果是否包含离职员工的用户信息。
    /// </summary>
    [JsonPropertyName("include_resigned")]
    public bool IncludeResigned { get; set; } = false;
}