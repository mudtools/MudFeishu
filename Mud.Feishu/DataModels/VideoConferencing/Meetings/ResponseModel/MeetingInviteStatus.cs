// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;


/// <summary>
/// <para>邀请结果</para>
/// </summary>
public class MeetingInviteStatus
{
    /// <summary>
    /// <para>用户ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_3ec3f6a28a0d08c45d895276e8e5e19b</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>用户类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：飞书用户</item>
    /// <item>2：rooms用户</item>
    /// <item>3：文档用户</item>
    /// <item>4：neo单品用户</item>
    /// <item>5：neo单品游客用户</item>
    /// <item>6：pstn用户</item>
    /// <item>7：sip用户</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("user_type")]
    public int? UserType { get; set; }

    /// <summary>
    /// <para>邀请结果</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：邀请成功</item>
    /// <item>2：邀请失败</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }
}