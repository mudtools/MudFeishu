// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Security;

/// <summary>
/// 行为审计日志的事件扩展字段（audit_event_extend，字段详情见飞书枚举值列表附录）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Security")]
public class SecurityAuditEventExtend
{
    /// <summary>
    /// <para>评论类型</para>
    /// </summary>
    [JsonPropertyName("comment_type")]
    public string? CommentType { get; set; }

    /// <summary>
    /// <para>应用详情</para>
    /// </summary>
    [JsonPropertyName("app_detail")]
    public string? AppDetail { get; set; }

    /// <summary>
    /// <para>是否两步验证</para>
    /// </summary>
    [JsonPropertyName("two_step_validation")]
    public bool? TwoStepValidation { get; set; }

    /// <summary>
    /// <para>登录方式</para>
    /// </summary>
    [JsonPropertyName("login_method")]
    public string? LoginMethod { get; set; }

    /// <summary>
    /// <para>视频中新增的内部人数</para>
    /// </summary>
    [JsonPropertyName("new_people_num_in_video")]
    public int? NewPeopleNumInVideo { get; set; }

    /// <summary>
    /// <para>视频中的外部人数</para>
    /// </summary>
    [JsonPropertyName("external_people_num_in_video")]
    public int? ExternalPeopleNumInVideo { get; set; }

    /// <summary>
    /// <para>会话中的外部人数</para>
    /// </summary>
    [JsonPropertyName("external_people_num_in_chat")]
    public int? ExternalPeopleNumInChat { get; set; }

    /// <summary>
    /// <para>加入群组数量</para>
    /// </summary>
    [JsonPropertyName("join_group")]
    public int? JoinGroup { get; set; }

    /// <summary>
    /// <para>退出群组数量</para>
    /// </summary>
    [JsonPropertyName("quit_group")]
    public int? QuitGroup { get; set; }

    /// <summary>
    /// <para>文档分享中的外部人数</para>
    /// </summary>
    [JsonPropertyName("external_people_num_in_doc_share")]
    public int? ExternalPeopleNumInDocShare { get; set; }
}
