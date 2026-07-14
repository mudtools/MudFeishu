// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceUserDailyShifts;

/// <summary>
/// 创建或修改临时排班请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Attendance")]
public class UserTmpDailyShiftRequest
{
    /// <summary>
    /// <para>临时班表信息列表（数量限制50以内）</para>
    /// <para>必填：是</para>
    /// <para>最大长度：50</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("user_tmp_daily_shifts")]
    public UserTmpDailyShift[] UserTmpDailyShifts { get; set; } = [];

    /// <summary>
    /// <para>操作人uid，与employee_type对应</para>
    /// <para>必填：否</para>
    /// <para>示例值：dd31248a</para>
    /// </summary>
    [JsonPropertyName("operator_id")]
    public string? OperatorId { get; set; }
}
