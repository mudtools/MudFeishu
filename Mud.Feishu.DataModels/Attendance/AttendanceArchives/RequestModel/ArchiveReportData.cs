// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceArchives;

/// <summary>
/// 归档报表内容(不超过50个)
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Attendance")]
public class ArchiveReportData
{
    /// <summary>
    /// <para>用户ID，对应employee_type</para>
    /// <para>必填：是</para>
    /// <para>示例值：1aaxxd</para>
    /// </summary>
    [JsonPropertyName("member_id")]
    public string MemberId { get; set; } = string.Empty;

    /// <summary>
    /// <para>考勤开始时间，格式为yyyyMMdd</para>
    /// <para>必填：是</para>
    /// <para>示例值：20210109</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string StartTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>考勤结束时间，格式为yyyyMMdd</para>
    /// <para>必填：是</para>
    /// <para>示例值：20210109</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string EndTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>字段结果(不超过200个)</para>
    /// <para>必填：否</para>
    /// <para>最大长度：200</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("field_datas")]
    public ArchiveFieldData[]? FieldDatas { get; set; }


}
