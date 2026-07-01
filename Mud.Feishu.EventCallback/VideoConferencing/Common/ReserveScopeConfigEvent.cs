// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.VideoConferencing;


/// <summary>
/// 预定范围设置
/// </summary>
public class ReserveScopeConfigEvent
{
    /// <summary>
    /// <para>可预定成员范围，0部分成员，1全部成员</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 取值范围：`0` ～ `1`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("allow_all_users")]
    public int? AllowAllUsers { get; set; }

    /// <summary>
    /// <para>可预定成员列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("allow_users")]
    public SubscribeUserEvent[]? AllowUsers { get; set; }


    /// <summary>
    /// <para>可预定部门列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("allow_depts")]
    public SubscribeDepartment[]? AllowDepts { get; set; }

}

/// <summary>
/// 可预定部门列表
/// </summary>
public class SubscribeDepartment
{
    /// <summary>
    /// <para>预定管理部门ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }
}