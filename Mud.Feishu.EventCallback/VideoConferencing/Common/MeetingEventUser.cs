// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.VideoConferencing;

/// <summary>
/// 事件操作人
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class MeetingEventUser
{
    /// <summary>
    /// <para>用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public UserIdInfo? Id { get; set; }

    /// <summary>
    /// <para>用户会中角色</para>
    /// <para>**可选值有**：</para>
    /// <para>1:普通参会人,2:主持人,3:联席主持人</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：普通参会人</item>
    /// <item>2：主持人</item>
    /// <item>3：联席主持人</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("user_role")]
    public int? UserRole { get; set; }

    /// <summary>
    /// <para>用户类型</para>
    /// <para>**可选值有**：</para>
    /// <para>1:飞书用户,2:rooms用户,3:文档用户,4:neo单品用户,5:neo单品游客用户,6:pstn用户,7:sip用户,8:sharebox用户,9:开放平台应用</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：飞书用户</item>
    /// <item>2：rooms用户</item>
    /// <item>3：文档用户</item>
    /// <item>4：neo单品用户</item>
    /// <item>5：neo单品游客用户</item>
    /// <item>6：pstn用户</item>
    /// <item>7：sip用户</item>
    /// <item>8：sharebox用户</item>
    /// <item>9：开放平台应用</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("user_type")]
    public int? UserType { get; set; }
}
