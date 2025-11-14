namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 用户排序信息列表。
/// </summary>
public class UserOrder
{
    /// <summary>
    /// 排序信息对应的部门 ID。表示用户所在的、且需要排序的部门。
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }

    /// <summary>
    /// 用户在其直属部门内的排序。数值越大，排序越靠前。
    /// </summary>
    [JsonPropertyName("user_order")]
    public int? UserOrderValue { get; set; }

    /// <summary>
    /// 用户所属的多个部门之间的排序。数值越大，排序越靠前。
    /// </summary>
    [JsonPropertyName("department_order")]
    public int? DepartmentOrder { get; set; }

    /// <summary>
    /// 标识是否为用户的唯一主部门，主部门为用户所属部门中排序第一的部门（department_order 最大）。
    /// </summary>
    [JsonPropertyName("is_primary_dept")]
    public bool? IsPrimaryDept { get; set; }
}