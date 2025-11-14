namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 恢复已删除用户请求体
/// </summary>
public class ResurrectUserRequest
{
    /// <summary>
    /// 用户排序信息。用户可能存在多个部门中，且有不同的排序，该参数用于设置用户部门排序。
    /// </summary>
    [JsonPropertyName("departments")]
    public List<DepartmentOrder> Departments { get; set; } = [];

    /// <summary>
    /// 如果用户正常状态时分配了席位，则可以通过该参数指定恢复后分配的席位 ID。
    /// </summary>
    [JsonPropertyName("subscription_ids")]
    public List<string> SubscriptionIds { get; set; } = [];
}
