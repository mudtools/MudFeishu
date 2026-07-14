// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceStats;

/// <summary>
/// 统计数据表头
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Attendance")]
public class QueryStatsFieldsInfo
{
    /// <summary>
    /// <para>统计类型</para>
    /// <para>必填：是</para>
    /// <para>示例值："daily"</para>
    /// <para>可选值：<list type="bullet">
    /// <item>daily：日度统计</item>
    /// <item>month：月度统计</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("stats_type")]
    public string StatsType { get; set; } = string.Empty;

    /// <summary>
    /// <para>用户 ID，字段已废弃</para>
    /// <para>必填：是</para>
    /// <para>示例值：""</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// <para>字段列表</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("fields")]
    public StatsFiledInfo[] Fields { get; set; } = [];
}
