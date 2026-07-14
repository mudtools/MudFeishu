// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;


/// <summary>
/// <para>会议设置</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class ReserveMeetingSettingInfo
{
    /// <summary>
    /// <para>会议主题</para>
    /// <para>必填：否</para>
    /// <para>示例值：my meeting</para>
    /// </summary>
    [JsonPropertyName("topic")]
    public string? Topic { get; set; }

    /// <summary>
    /// <para>会议权限配置列表，如果存在相同的权限配置项则它们之间为"逻辑或"的关系（即 有一个为true则拥有该权限）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("action_permissions")]
    public ReserveActionPermission[]? ActionPermissions { get; set; }

    /// <summary>
    /// <para>会议初始类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：多人会议</item>
    /// <item>2：1v1呼叫</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("meeting_initial_type")]
    public int? MeetingInitialType { get; set; }

    /// <summary>
    /// <para>该会议是否支持互通，不支持更新（注：该字段内测中）</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("meeting_connect")]
    public bool? MeetingConnect { get; set; }

    /// <summary>
    /// <para>1v1呼叫相关参数</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("call_setting")]
    public ReserveCallSetting? CallSetting { get; set; }


    /// <summary>
    /// <para>使用飞书视频会议时，是否开启自动录制，默认false</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("auto_record")]
    public bool? AutoRecord { get; set; }

    /// <summary>
    /// <para>指定主持人列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("assign_host_list")]
    public ReserveAssignHost[]? AssignHostLists { get; set; }
}
