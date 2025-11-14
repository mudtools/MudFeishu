namespace Mud.Feishu.DataModels.Departments;

public class GetDepartmentInfoResult
{
    [JsonPropertyName("department")]
    public GetDepartmentInfo Department { get; set; }
}
