namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 创建或更新用户操作的结果。
/// </summary>
public class CreateOrUpdateUserResult
{
    /// <summary>
    /// 创建的用户详细信息。
    /// </summary>
    [JsonPropertyName("user")]
    public UserDetail? User { get; set; }
}