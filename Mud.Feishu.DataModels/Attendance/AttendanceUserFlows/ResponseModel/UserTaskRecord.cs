// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceUserFlows;

/// <summary>
/// 用户考勤记录
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Attendance")]
public class UserTaskRecord
{
    /// <summary>
    /// <para>上班打卡记录 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：6709359313699356941</para>
    /// </summary>
    [JsonPropertyName("check_in_record_id")]
    public string CheckInRecordId { get; set; } = string.Empty;

    /// <summary>
    /// <para>上班打卡记录</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("check_in_record")]
    public UserFlowInfo? CheckInRecord { get; set; }

    /// <summary>
    /// <para>下班打卡记录 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：6709359313699356942</para>
    /// </summary>
    [JsonPropertyName("check_out_record_id")]
    public string CheckOutRecordId { get; set; } = string.Empty;

    /// <summary>
    /// <para>下班打卡记录</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("check_out_record")]
    public UserFlowInfo? CheckOutRecord { get; set; }

    /// <summary>
    /// <para>上班打卡结果</para>
    /// <para>必填：是</para>
    /// <para>示例值：SystemCheck</para>
    /// <para>可选值：<list type="bullet">
    /// <item>NoNeedCheck：无需打卡</item>
    /// <item>SystemCheck：系统打卡（已弃用）</item>
    /// <item>Normal：正常</item>
    /// <item>Early：早退</item>
    /// <item>Late：迟到</item>
    /// <item>Lack：缺卡</item>
    /// <item>Todo：未打卡</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("check_in_result")]
    public string CheckInResult { get; set; } = string.Empty;

    /// <summary>
    /// <para>下班打卡结果</para>
    /// <para>必填：是</para>
    /// <para>示例值：SystemCheck</para>
    /// <para>可选值：<list type="bullet">
    /// <item>NoNeedCheck：无需打卡</item>
    /// <item>SystemCheck：系统打卡（已弃用）</item>
    /// <item>Normal：正常</item>
    /// <item>Early：早退</item>
    /// <item>Late：迟到</item>
    /// <item>Lack：缺卡</item>
    /// <item>Todo：未打卡</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("check_out_result")]
    public string CheckOutResult { get; set; } = string.Empty;

    /// <summary>
    /// <para>上班打卡结果补充</para>
    /// <para>必填：是</para>
    /// <para>示例值：None</para>
    /// <para>可选值：<list type="bullet">
    /// <item>None：无</item>
    /// <item>ManagerModification：管理员修改</item>
    /// <item>CardReplacement：补卡通过</item>
    /// <item>ShiftChange：换班</item>
    /// <item>Travel：出差</item>
    /// <item>Leave：请假</item>
    /// <item>GoOut：外出</item>
    /// <item>CardReplacementApplication：补卡申请中</item>
    /// <item>FieldPunch：外勤打卡</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("check_in_result_supplement")]
    public string CheckInResultSupplement { get; set; } = string.Empty;

    /// <summary>
    /// <para>下班打卡结果补充</para>
    /// <para>必填：是</para>
    /// <para>示例值：None</para>
    /// <para>可选值：<list type="bullet">
    /// <item>None：无</item>
    /// <item>ManagerModification：管理员修改</item>
    /// <item>CardReplacement：补卡通过</item>
    /// <item>ShiftChange：换班</item>
    /// <item>Travel：出差</item>
    /// <item>Leave：请假</item>
    /// <item>GoOut：外出</item>
    /// <item>CardReplacementApplication：补卡申请中</item>
    /// <item>FieldPunch：外勤打卡</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("check_out_result_supplement")]
    public string CheckOutResultSupplement { get; set; } = string.Empty;

    /// <summary>
    /// <para>上班打卡时间，秒级时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1609722000</para>
    /// </summary>
    [JsonPropertyName("check_in_shift_time")]
    public string? CheckInShiftTime { get; set; }

    /// <summary>
    /// <para>下班打卡时间，秒级时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1609754400</para>
    /// </summary>
    [JsonPropertyName("check_out_shift_time")]
    public string? CheckOutShiftTime { get; set; }

    /// <summary>
    /// <para>班次类型，0正常，1加班班次</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("task_shift_type")]
    public int? TaskShiftType { get; set; }
}
