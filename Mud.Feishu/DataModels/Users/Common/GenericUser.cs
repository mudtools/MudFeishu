namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 定义引用人员。
/// </summary>
public class GenericUser
{
    /// <summary>
    /// 引用人员的用户 ID。
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// 用户类型。可选值有：1：用户
    /// </summary>
    [JsonPropertyName("type")]
    public required int Type { get; set; } = 1;
}