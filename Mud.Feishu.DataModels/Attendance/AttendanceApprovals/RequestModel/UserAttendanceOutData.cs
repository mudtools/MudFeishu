// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceApprovals;

/// <summary>
/// 外出信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Attendance")]
public class UserAttendanceOutData
{
    /// <summary>
    /// <para>外出类型唯一 ID，代表一种外出类型，长度小于 14</para>
    /// <para>* 如何获取？可以选择填入三方的外出类型id。如市内外出、市外外出的id</para>
    /// <para>必填：是</para>
    /// <para>示例值：9496E43696967658A512969523E89870</para>
    /// </summary>
    [JsonPropertyName("uniq_id")]
    public string UniqId { get; set; } = string.Empty;

    /// <summary>
    /// <para>外出时长单位</para>
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
    /// <para>关联审批单外出时长，单位为秒，与unit无关</para>
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
    /// <para>外出多语言展示，格式为 map，key 为 ["ch"、"en"、"ja"]，其中 ch 代表中文、en 代表英语、ja 代表日语</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("i18n_names")]
    public UserAttendance18nNames I18nNames { get; set; } = new();


    /// <summary>
    /// <para>默认语言类型，由于飞书客户端支持中、英、日三种语言，当用户切换语言时，如果假期名称没有所对应的语言，会使用默认语言的名称</para>
    /// <para>必填：是</para>
    /// <para>示例值：ch</para>
    /// </summary>
    [JsonPropertyName("default_locale")]
    public string DefaultLocale { get; set; } = string.Empty;

    /// <summary>
    /// <para>外出理由</para>
    /// <para>必填：是</para>
    /// <para>示例值：外出办事</para>
    /// </summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// <para>外出记录的唯一幂等键，用于避免外出记录重复创建，可以填入三方的外出记录id</para>
    /// <para>必填：否</para>
    /// <para>示例值：1233432312</para>
    /// </summary>
    [JsonPropertyName("idempotent_id")]
    public string? IdempotentId { get; set; }

    /// <summary>
    /// <para>更正流程实例 ID。该字段由系统自动生成，在写入审批结果时，无需传入该参数。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("correct_process_id")]
    public string[]? CorrectProcessId { get; set; }

    /// <summary>
    /// <para>撤销流程实例 ID。该字段由系统自动生成，在写入审批结果时，无需传入该参数。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("cancel_process_id")]
    public string[]? CancelProcessId { get; set; }

    /// <summary>
    /// <para>发起流程实例 ID。该字段由系统自动生成，在写入审批结果时，无需传入该参数。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("process_id")]
    public string[]? ProcessId { get; set; }
}
