namespace Mud.Feishu.DataModels.JobTitles;

public class JobTitleResult
{
    [JsonPropertyName("job_title")]
    public JobTitle JobTitle { get; set; }
}
