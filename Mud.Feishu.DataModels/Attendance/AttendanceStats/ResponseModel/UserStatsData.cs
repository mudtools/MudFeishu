// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceStats;


/// <summary>
/// <para>用户统计数据（限制1000条，超过1000条会截断）</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Attendance")]
public class UserStatsData
{
    /// <summary>
    /// <para>用户姓名</para>
    /// <para>必填：是</para>
    /// <para>示例值：小李</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>用户 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：ec8ddg56</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// <para>用户的统计数据</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("datas")]
    public UserStatsDataCell[]? Datas { get; set; }

}
