namespace Mud.Feishu.DataModels.Employees;

/// <summary>
/// 员工创建接口拓展选项
/// </summary>
public class EmployeeCreateOptions
{
    /// <summary>
    /// 员工的数据驻留地。仅限开通了Multi-Geo的企业可选填，且仅能填入企业数据驻留地列表中的Geo。
    /// </summary>
    [JsonPropertyName("geo_name")]
    public string? GeoName { get; set; }

    /// <summary>
    /// 分配给员工的席位ID列表。
    /// </summary>
    [JsonPropertyName("subscription_ids")]
    public List<string> SubscriptionIds { get; set; } = [];
}