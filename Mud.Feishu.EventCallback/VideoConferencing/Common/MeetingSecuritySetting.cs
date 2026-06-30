// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.VideoConferencing;


/// <summary>
/// 
/// </summary>
public class MeetingSecuritySetting
{
    /// <summary>
    /// <para>安全级别</para>
    /// <para>**可选值有**：</para>
    /// <para>1:所有人可加入,2:仅企业内用户可加入,3:仅指定联系人和群可加入,4:仅主持人可参会（锁定会议）</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：所有人可加入</item>
    /// <item>2：仅企业内用户可加入</item>
    /// <item>3：仅指定联系人和群可加入</item>
    /// <item>4：仅主持人可参会（锁定会议）</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("security_level")]
    public int? SecurityLevel { get; set; }

    /// <summary>
    /// <para>允许入会的群组ID列表</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `200`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("group_ids")]
    public string[]? GroupIds { get; set; }

    /// <summary>
    /// <para>允许入会的用户ID列表</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `200`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_ids")]
    public UserIdInfo[]? UserIds { get; set; }

    /// <summary>
    /// <para>允许入会的会议室ID列表</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `200`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("room_ids")]
    public string[]? RoomIds { get; set; }

    /// <summary>
    /// <para>是否设置了仅指定联系人和群组可参会</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("has_set_security_contacts_and_group")]
    public bool? HasSetSecurityContactsAndGroup { get; set; }
}