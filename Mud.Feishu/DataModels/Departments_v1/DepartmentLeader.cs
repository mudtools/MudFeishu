namespace Mud.Feishu.DataModels.DepartmentsV1;

/// <summary>
/// 部门负责人
/// </summary>
public class DepartmentLeader
{
    /// <summary>
    /// 部门负责人类型 可选值有：1：主 2：副
    /// </summary>
    [JsonPropertyName("leader_type")]
    public int LeaderType { get; set; }

    /// <summary>
    /// 部门负责人ID，与employee_id_type类型保持一致
    /// </summary>
    [JsonPropertyName("leader_id")]
    public required string LeaderId { get; set; }
}
