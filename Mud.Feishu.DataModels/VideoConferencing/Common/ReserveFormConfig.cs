// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;



/// <summary>
/// <para>预定表单</para>
/// </summary>
public class ReserveFormConfig
{
    /// <summary>
    /// <para>是否覆盖子层级及会议室</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("if_cover_child_scope")]
    public bool? IfCoverChildScope { get; set; }

    /// <summary>
    /// <para>预定表单开关，true表示打开，false表示关闭</para>
    /// <para>必填：是</para>
    /// <para>示例值：false</para>
    /// <para>默认值：false</para>
    /// </summary>
    [JsonPropertyName("reserve_form")]
    public bool ReserveForm { get; set; }

    /// <summary>
    /// <para>通知人列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("notified_users")]
    public SubscribeUser[]? NotifiedUsers { get; set; }


    /// <summary>
    /// <para>最晚于会议开始前 notified_time收到通知（单位：分/时/天）</para>
    /// <para>必填：否</para>
    /// <para>示例值：3</para>
    /// </summary>
    [JsonPropertyName("notified_time")]
    public int? NotifiedTime { get; set; }

    /// <summary>
    /// <para>时间单位，1为分钟；2为小时；3为天，默认为天</para>
    /// <para>必填：否</para>
    /// <para>示例值：3</para>
    /// <para>最大值：3</para>
    /// <para>最小值：1</para>
    /// </summary>
    [JsonPropertyName("time_unit")]
    public int? TimeUnit { get; set; }
}