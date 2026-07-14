// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;



/// <summary>
/// <para>预定时间设置</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class ReserveScopeTimeConfig
{
    /// <summary>
    /// <para>是否覆盖子层级及会议室</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("if_cover_child_scope")]
    public bool? IfCoverChildScope { get; set; }

    /// <summary>
    /// <para>预定时间开关：0 代表关闭，1 代表开启</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// <para>最大值：1</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("time_switch")]
    public int TimeSwitch { get; set; }

    /// <summary>
    /// <para>最早可提前</para>
    /// <para>days_in_advance 预定会议室（单位：天，取值范围[1-730]）</para>
    /// <para>说明：不填写时，默认更新为 365</para>
    /// <para>必填：否</para>
    /// <para>示例值：30</para>
    /// </summary>
    [JsonPropertyName("days_in_advance")]
    public int? DaysInAdvance { get; set; }

    /// <summary>
    /// <para>开放当天可于</para>
    /// <para>opening_hour 开始预定（单位：秒，取值范围[0,86400]）</para>
    /// <para>说明：</para>
    /// <para>- 不填写时默认更新为</para>
    /// <para>28800</para>
    /// <para>- 如果填写的值不是 60</para>
    /// <para>的倍数，则自动会更新为离其最近的 60 整数倍的值。</para>
    /// <para>必填：否</para>
    /// <para>示例值：27900</para>
    /// </summary>
    [JsonPropertyName("opening_hour")]
    public string? OpeningHour { get; set; }

    /// <summary>
    /// <para>每日可预定时间范围的开始时间（单位：秒，取值范围[0,86400]）</para>
    /// <para>说明：</para>
    /// <para>- 不填写时，默认更新为 0 ，此时填写的 end_time 不得小于 30。</para>
    /// <para>- 当 start_time 与</para>
    /// <para>end_time 均填写时</para>
    /// <para>end_time 至少超过</para>
    /// <para>start_time 30 。</para>
    /// <para>- 如果填写的值不是 60 的倍数，则自动会更新为离其最近的 60 整数倍的值。</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    /// <summary>
    /// <para>每日可预定时间范围结束时间（单位：秒，取值范围[0,86400]）</para>
    /// <para>说明：</para>
    /// <para>- 不填写时，默认更新为 86400 ，此时填写的</para>
    /// <para>start_time 不得大于等于 86370 。</para>
    /// <para>- 当 start_time 与</para>
    /// <para>end_time 均填写时</para>
    /// <para>end_time 至少要超过</para>
    /// <para>start_time 30。</para>
    /// <para>- 如果填写的值不是 60 的倍数，则自动会更新为离其最近的 60 整数倍的值。</para>
    /// <para>必填：否</para>
    /// <para>示例值：86400</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }

    /// <summary>
    /// <para>单次会议室可预定时长上限（单位：小时，取值范围[1,99]）</para>
    /// <para>说明：不填写时默认更新为 2</para>
    /// <para>必填：否</para>
    /// <para>示例值：24</para>
    /// </summary>
    [JsonPropertyName("max_duration")]
    public int? MaxDuration { get; set; }
}
