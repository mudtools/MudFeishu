namespace Mud.Feishu.DataModels.Departments;

/// <summary>
/// 获取部门信息的响结果
/// 用于一次请求获取多个部门的信息。
/// </summary>
public class BatchGetDepartmentRequest
{
    /// <summary>
    /// 部门信息项列表。
    /// 包含需要批量获取的部门信息配置。
    /// </summary>
    [JsonPropertyName("items")]
    public List<GetDepartmentInfo> Items { get; set; } = [];
}
