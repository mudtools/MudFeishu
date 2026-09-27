// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Security;

/// <summary>
/// 用户数据迁移信息（user_migration）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Security")]
public class SecurityUserMigration
{
    /// <summary>
    /// <para>用户 ID</para>
    /// <para>示例值：ou_1234567890abcdef1234567890abcdef</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>目标地理位置区域</para>
    /// <para>示例值：us</para>
    /// </summary>
    [JsonPropertyName("dest_geo")]
    public string? DestGeo { get; set; }

    /// <summary>
    /// <para>迁移任务 ID</para>
    /// <para>示例值：task_1234567890abcdef</para>
    /// </summary>
    [JsonPropertyName("task_id")]
    public string? TaskId { get; set; }

    /// <summary>
    /// <para>用户迁移状态：0（用户迁移进行中）/ 1（用户迁移已完成）/ 2（用户迁移已取消）</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// <para>迁移进度百分比，取值 0-100</para>
    /// <para>示例值：20</para>
    /// </summary>
    [JsonPropertyName("progress")]
    public int? Progress { get; set; }
}
