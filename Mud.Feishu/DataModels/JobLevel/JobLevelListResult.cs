namespace Mud.Feishu.DataModels.JobLevel;

/// <summary>
/// 职级列表查询结果。
/// </summary>
public class JobLevelListResult : ListApiResult
{
    /// <summary>
    /// 职级列表
    /// </summary>
    [JsonPropertyName("items")]
    public List<JobLevelResult> Items { get; set; } = [];
}
