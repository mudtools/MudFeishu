namespace Mud.Feishu.DataModels.Employees;

/// <summary>
/// 人员字段值
/// </summary>
public class UserValue
{
    /// <summary>
    /// 人员ID
    /// </summary>
    [JsonPropertyName("ids")]
    public List<string> Ids { get; set; } = [];

    /// <summary>
    /// 用户类型。
    /// </summary>
    [JsonPropertyName("user_type")]
    public string? UserType { get; set; }
}