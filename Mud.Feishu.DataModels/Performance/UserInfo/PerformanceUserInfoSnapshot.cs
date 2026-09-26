// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 绩效周期人员快照信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceUserInfoSnapshot
{
    /// <summary>
    /// <para>人员的用户 ID（open_id / user_id 对象）</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public UserIdInfo? UserId { get; set; }

    /// <summary>
    /// <para>人员的直属上级的用户 ID；字段权限 performance:user_snapshot.direct_leader:read</para>
    /// </summary>
    [JsonPropertyName("direct_leader_user_id")]
    public UserIdInfo? DirectLeaderUserId { get; set; }

    /// <summary>
    /// <para>人员的部门；字段权限 performance:user_snapshot.department:read</para>
    /// </summary>
    [JsonPropertyName("department")]
    public PerformanceSnapshotDepartment? Department { get; set; }

    /// <summary>
    /// <para>人员的序列；字段权限 performance:user_snapshot.job_family:read</para>
    /// </summary>
    [JsonPropertyName("job_family")]
    public PerformanceSnapshotJobFamily? JobFamily { get; set; }

    /// <summary>
    /// <para>人员的职级；字段权限 performance:user_snapshot.job_level:read</para>
    /// </summary>
    [JsonPropertyName("job_level")]
    public PerformanceSnapshotJobLevel? JobLevel { get; set; }
}
