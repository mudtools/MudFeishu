namespace Mud.Feishu.DataModels.Departments;

/// <summary>
/// 获取部门信息结果响应模型。
/// 用于封装单个部门信息查询的响应结果。
/// </summary>
public class GetDepartmentInfoResult
{
    /// <summary>
    /// 部门信息。
    /// 包含查询到的部门详细数据。
    /// </summary>
    [JsonPropertyName("department")]
    public GetDepartmentInfo Department { get; set; }
}
