// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.AttendanceGroups.Common;

namespace Mud.Feishu.DataModels.AttendanceGroups;

/// <summary>
/// <para>需要打卡的人员集合（仅当不传「bind_dept_ids」和「bind_user_ids」时，才会使用该字段）</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Attendance")]
public class AttendancePunchMember
{
    /// <summary>
    /// <para>圈人方式：</para>
    /// <para>* `0`：无</para>
    /// <para>* `1`：全部</para>
    /// <para>* `2`：自定义</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// <para>默认值：0</para>
    /// </summary>
    [JsonPropertyName("rule_scope_type")]
    public int? RuleScopeType { get; set; }

    /// <summary>
    /// <para>圈人规则列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("scope_group_list")]
    public AttendanceScopeGroup? ScopeGroupList { get; set; }
}
