namespace Mud.Feishu.DataModels.Departments;

public class DepartmentLeader
{
    [JsonPropertyName("leaderType")]
    public int LeaderType { get; set; }

    [JsonPropertyName("leaderID")]
    public string LeaderId { get; set; }
}