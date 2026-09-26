// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 创建人才备注请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class CreateNoteRequest
{
    /// <summary>
    /// <para>人才 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：6916472453069883661</para>
    /// </summary>
    [JsonPropertyName("talent_id")]
    public string? TalentId { get; set; }

    /// <summary>
    /// <para>投递 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：6891565253964859661</para>
    /// </summary>
    [JsonPropertyName("application_id")]
    public string? ApplicationId { get; set; }

    /// <summary>
    /// <para>创建人 ID，请传入与 user_id_type 相匹配的 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_f476cb099ac9227c9bae09ce46112579</para>
    /// </summary>
    [JsonPropertyName("creator_id")]
    public string? CreatorId { get; set; }

    /// <summary>
    /// <para>备注内容</para>
    /// <para>必填：是</para>
    /// <para>示例值：test</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// <para>备注私密属性（默认为公开）：1-私密，2-公开</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("privacy")]
    public int? Privacy { get; set; }

    /// <summary>
    /// <para>是否通知被 @ 的用户，默认 false</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("notify_mentioned_user")]
    public bool? NotifyMentionedUser { get; set; }

    /// <summary>
    /// <para>被 @ 用户列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mention_entity_list")]
    public MentionEntity[]? MentionEntityList { get; set; }
}
