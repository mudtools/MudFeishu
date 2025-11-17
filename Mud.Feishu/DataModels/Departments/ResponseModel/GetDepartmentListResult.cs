namespace Mud.Feishu.DataModels.Departments;

public class GetDepartmentListResult : ListApiResult
{
    [JsonPropertyName("items")]
    public List<GetDepartmentInfo> Items { get; set; }
}
