// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;



/// <summary>
/// <para>预定范围设置</para>
/// </summary>
public class ReserveScopeConfig
{
    /// <summary>
    /// <para>是否覆盖子层级及会议室</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("if_cover_child_scope")]
    public bool? IfCoverChildScope { get; set; }

    /// <summary>
    /// <para>可预定成员范围：0 代表部分成员，1 代表全部成员。</para>
    /// <para>说明：</para>
    /// <para>- 此值必填。</para>
    /// <para>- 当设置为 0 时，至少需要 1 个预定部门或预定人</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// <para>最大值：1</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("allow_all_users")]
    public int? AllowAllUsers { get; set; }

    /// <summary>
    /// <para>可预定成员列表</para>
    /// <para>必填：否</para>
    /// <para>示例值：[{user_id:"ou_e8bce6c3935ef1fc1b432992fd9d3db8"}]</para>
    /// </summary>
    [JsonPropertyName("allow_users")]
    public SubscribeUser[]? AllowUsers { get; set; }



    /// <summary>
    /// <para>可预定部门列表</para>
    /// <para>必填：否</para>
    /// <para>示例值：[{department_id:"od-5c07f0c117cf8795f25610a69363ce31"}]</para>
    /// </summary>
    [JsonPropertyName("allow_depts")]
    public SubscribeDepartment[]? AllowDepts { get; set; }

}