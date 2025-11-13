namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 更新用户请求体。
/// </summary>
public class UpdateUserRequest : UserBaseRequest
{
    /// <summary>
    /// 是否冻结状态。
    /// </summary>
    [JsonPropertyName("is_frozen")]
    public bool IsFrozen { get; set; }
}
