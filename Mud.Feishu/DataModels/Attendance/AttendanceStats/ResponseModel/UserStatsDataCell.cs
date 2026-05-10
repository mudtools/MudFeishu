// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceStats;

/// <summary>
/// <para>用户的统计数据</para>
/// </summary>
public class UserStatsDataCell
{
    /// <summary>
    /// <para>字段编号</para>
    /// <para>必填：是</para>
    /// <para>示例值：50102</para>
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// <para>数据值</para>
    /// <para>必填：是</para>
    /// <para>示例值：无需打卡(-), 无需打卡(-)</para>
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// <para>数据属性</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("features")]
    public UserStatsDataFeature[]? Features { get; set; }



    /// <summary>
    /// <para>字段标题</para>
    /// <para>必填：否</para>
    /// <para>示例值：姓名</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>时长，这个字段是一个map，key位时间单位，value为对应的时长值</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("duration_num")]
    public UserStatsDataDuration? DurationNum { get; set; }


}