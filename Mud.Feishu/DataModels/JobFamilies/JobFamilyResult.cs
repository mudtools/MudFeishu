namespace Mud.Feishu.DataModels.JobFamilies;

public class JobFamilyResult
{
    [JsonPropertyName("job_family")]
    public JobFamilyInfo JobFamily { get; set; }
}
