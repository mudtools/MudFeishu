// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceRemedys;

/// <summary>
/// 用户可补卡时间
/// </summary>
public class UserAllowedRemedyInfo
{
    /// <summary>
    /// <para>用户 ID，对应employe_type</para>
    /// <para>必填：是</para>
    /// <para>示例值：abd754f7</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// <para>补卡日期，格式为yyyyMMdd</para>
    /// <para>必填：是</para>
    /// <para>示例值：20210104</para>
    /// </summary>
    [JsonPropertyName("remedy_date")]
    public int RemedyDate { get; set; }

    /// <summary>
    /// <para>是否为自由班次，若为自由班次，则不用选择考虑第几次上下班，直接选择补卡时间即可</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("is_free_punch")]
    public bool? IsFreePunch { get; set; }

    /// <summary>
    /// <para>第几次上下班，0：第 1 次上下班，1：第 2 次上下班，2：第 3 次上下班</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("punch_no")]
    public int? PunchNo { get; set; }

    /// <summary>
    /// <para>上班 / 下班，1：上班，2：下班</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("work_type")]
    public int? WorkType { get; set; }

    /// <summary>
    /// <para>打卡状态，Early：早退，Late：迟到，Lack：缺卡</para>
    /// <para>必填：否</para>
    /// <para>示例值：Lack</para>
    /// </summary>
    [JsonPropertyName("punch_status")]
    public string? PunchStatus { get; set; }

    /// <summary>
    /// <para>正常的应打卡时间，时间格式为 yyyy-MM-dd HH:mm</para>
    /// <para>必填：否</para>
    /// <para>示例值：2021-07-01 09:00</para>
    /// </summary>
    [JsonPropertyName("normal_punch_time")]
    public string? NormalPunchTime { get; set; }

    /// <summary>
    /// <para>可选的补卡时间的最小值，时间格式为 yyyy-MM-dd HH:mm</para>
    /// <para>必填：否</para>
    /// <para>示例值：2021-07-01 08:00</para>
    /// </summary>
    [JsonPropertyName("remedy_start_time")]
    public string? RemedyStartTime { get; set; }

    /// <summary>
    /// <para>可选的补卡时间的最大值，时间格式为 yyyy-MM-dd HH:mm</para>
    /// <para>必填：否</para>
    /// <para>示例值：2021-07-01 10:00</para>
    /// </summary>
    [JsonPropertyName("remedy_end_time")]
    public string? RemedyEndTime { get; set; }
}
