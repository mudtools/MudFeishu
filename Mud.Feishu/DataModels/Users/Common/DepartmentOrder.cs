namespace Mud.Feishu.DataModels.Users;


/// <summary>
/// 用户部门排序。
/// </summary>
public class DepartmentOrder
{
    /// <summary>
    /// 排序信息对应的部门 ID。表示用户所在的、且需要排序的部门。
    /// </summary>
    [JsonPropertyName("department_id")]
    public required string DepartmentId { get; set; }

    /// <summary>
    /// 用户在其直属部门内的排序。数值越大，排序越靠前。
    /// </summary>
    [JsonPropertyName("user_order")]
    public int? UserOrder { get; set; }

    /// <summary>
    /// 用户所属的多个部门之间的排序。数值越大，排序越靠前。
    /// </summary>
    [JsonPropertyName("department_order")]
    public int? DepartmentOrderValue { get; set; }

    /// <summary>
    /// 是否为主部门。
    /// </summary>
    [JsonPropertyName("is_primary_dept")]
    public bool IsPrimaryDept { get; set; }
}