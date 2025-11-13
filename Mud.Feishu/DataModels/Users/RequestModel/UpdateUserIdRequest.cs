namespace Mud.Feishu.DataModels.Users;
/// <summary>
/// 更新用户 user_id 请求体。
/// </summary>
public class UpdateUserIdRequest
{
    /// <summary>
    /// 自定义新的用户 user_id。长度不能超过 64 字符。
    /// </summary>
    [JsonPropertyName("new_user_id")]
    public required string NewUserId { get; set; }
}