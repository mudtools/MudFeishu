/// <summary>
/// 表示部门领导者的数据模型
/// </summary>
public class DepartmentLeader
{
    /// <summary>
    /// 领导者类型
    /// </summary>
    [JsonPropertyName("leaderType")]
    public int LeaderType { get; set; }

    /// <summary>
    /// 部门领导者的唯一标识符
    /// </summary>
    [JsonPropertyName("leaderID")]
    public string LeaderId { get; set; }
}