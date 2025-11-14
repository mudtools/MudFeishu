namespace Mud.Feishu.DataModels.EmployeeType;

/// <summary>
/// 人员类型信息查询结果
/// </summary>
public class EmployeeTypeEnumListResult : ListApiResult
{
    /// <summary>
    /// 人员类型信息结果集合。
    /// </summary>
    [JsonPropertyName("items")]
    public List<EmployeeTypeEnum> Items { get; set; } = [];
}
