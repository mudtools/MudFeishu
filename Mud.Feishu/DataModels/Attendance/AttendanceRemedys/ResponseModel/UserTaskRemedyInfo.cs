// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceRemedys;

/// <summary>
/// <para>补卡审批信息</para>
/// </summary>
public class UserTaskRemedyInfo
{
    /// <summary>
    /// <para>用户 ID，对应employee_type</para>
    /// <para>必填：是</para>
    /// <para>示例值：abd754f7</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// <para>补卡日期，日期格式yyyyMMdd</para>
    /// <para>必填：是</para>
    /// <para>示例值：20210701</para>
    /// </summary>
    [JsonPropertyName("remedy_date")]
    public int RemedyDate { get; set; }

    /// <summary>
    /// <para>第几次上下班，0：第 1 次上下班，1：第 2 次上下班，2：第 3 次上下班，自由班制填 0</para>
    /// <para>必填：是</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("punch_no")]
    public int PunchNo { get; set; }

    /// <summary>
    /// <para>上班 / 下班，1：上班，2：下班，自由班制填 0</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("work_type")]
    public int WorkType { get; set; }

    /// <summary>
    /// <para>审批 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：6737202939523236113</para>
    /// </summary>
    [JsonPropertyName("approval_id")]
    public string? ApprovalId { get; set; }

    /// <summary>
    /// <para>补卡时间，时间格式为 yyyy-MM-dd HH:mm</para>
    /// <para>必填：是</para>
    /// <para>示例值：2021-07-01 08:00</para>
    /// </summary>
    [JsonPropertyName("remedy_time")]
    public string RemedyTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>无效字段，默认数据全部返回的类型为审批中。</para>
    /// <para>必填：否</para>
    /// <para>示例值：2</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：审批中</item>
    /// <item>1：未通过</item>
    /// <item>2：已通过</item>
    /// <item>3：已取消</item>
    /// <item>4：通过后撤回</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// <para>补卡原因</para>
    /// <para>必填：是</para>
    /// <para>示例值：忘记打卡</para>
    /// </summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// <para>无效字段</para>
    /// <para>必填：否</para>
    /// <para>示例值：-</para>
    /// </summary>
    [JsonPropertyName("time")]
    public string? Time { get; set; }

    /// <summary>
    /// <para>无效字段</para>
    /// <para>必填：否</para>
    /// <para>示例值：-</para>
    /// </summary>
    [JsonPropertyName("time_zone")]
    public string? TimeZone { get; set; }

    /// <summary>
    /// <para>无效字段</para>
    /// <para>必填：否</para>
    /// <para>示例值：-</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>无效字段</para>
    /// <para>必填：否</para>
    /// <para>示例值：-</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }
}