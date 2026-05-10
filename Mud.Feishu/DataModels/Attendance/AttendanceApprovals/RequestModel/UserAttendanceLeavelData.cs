// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceApprovals;

/// <summary>
/// <para>请假信息</para>
/// </summary>
public class UserAttendanceLeavelData
{
    /// <summary>
    /// <para>审批实例id</para>
    /// <para>必填：否</para>
    /// <para>示例值：6852582717813440527</para>
    /// </summary>
    [JsonPropertyName("uniq_id")]
    public string? UniqId { get; set; }

    /// <summary>
    /// <para>假期时长单位</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：天</item>
    /// <item>2：小时</item>
    /// <item>3：半天</item>
    /// <item>4：半小时</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("unit")]
    public int Unit { get; set; }

    /// <summary>
    /// <para>关联审批单假期时长，单位为秒，与unit无关</para>
    /// <para>必填：是</para>
    /// <para>示例值：3600</para>
    /// </summary>
    [JsonPropertyName("interval")]
    public int Interval { get; set; }

    /// <summary>
    /// <para>开始时间，时间格式为 yyyy-MM-dd HH:mm:ss</para>
    /// <para>必填：是</para>
    /// <para>示例值：2021-01-04 09:00:00</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string StartTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>结束时间，时间格式为 yyyy-MM-dd HH:mm:ss</para>
    /// <para>必填：是</para>
    /// <para>示例值：2021-01-04 19:00:00</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string EndTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>假期多语言展示，格式为 map，key 为 ["ch"、"en"、"ja"]，其中 ch 代表中文、en 代表英语、ja 代表日语</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("i18n_names")]
    public UserAttendance18nNames I18nNames { get; set; } = new();

    /// <summary>
    /// <para>默认语言类型，由于飞书客户端支持中、英、日三种语言，当用户切换语言时，如果假期名称没有所对应的语言，会使用默认语言的名称</para>
    /// <para>必填：是</para>
    /// <para>示例值：ch</para>
    /// <para>可选值：<list type="bullet">
    /// <item>ch：中文</item>
    /// <item>en：英文</item>
    /// <item>ja：日文</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("default_locale")]
    public string DefaultLocale { get; set; } = string.Empty;

    /// <summary>
    /// <para>请假理由，必选字段</para>
    /// <para>必填：是</para>
    /// <para>示例值：家里有事</para>
    /// </summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// <para>请假记录的唯一幂等键，用于避免请假记录重复创建，可以填入三方的请假记录id</para>
    /// <para>必填：否</para>
    /// <para>示例值：1233432312</para>
    /// </summary>
    [JsonPropertyName("idempotent_id")]
    public string? IdempotentId { get; set; }
}
