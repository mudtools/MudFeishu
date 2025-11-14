namespace Mud.Feishu.DataModels.Users;


/// <summary>
/// 用户状态信息。
/// </summary>
public class UserStatus
{
    /// <summary>
    /// 用户是否被冻结。
    /// </summary>
    [JsonPropertyName("is_frozen")]
    public bool IsFrozen { get; set; }

    /// <summary>
    /// 用户是否已离职。
    /// </summary>
    [JsonPropertyName("is_resigned")]
    public bool IsResigned { get; set; }

    /// <summary>
    /// 用户是否已激活。
    /// </summary>
    [JsonPropertyName("is_activated")]
    public bool IsActivated { get; set; }

    /// <summary>
    /// 用户是否已退出。
    /// </summary>
    [JsonPropertyName("is_exited")]
    public bool IsExited { get; set; }

    /// <summary>
    /// 用户是否未加入。
    /// </summary>
    [JsonPropertyName("is_unjoin")]
    public bool IsUnjoin { get; set; }
}