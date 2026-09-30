// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.HelpDesk;

/// <summary>
/// <para>知识库FAQ</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "HelpDesk")]
public class Faq
{
    /// <summary>
    /// <para>知识库FAQ ID</para>
    /// <para>必填：否</para>
    /// <para>示例值："6936004780707807231"</para>
    /// </summary>
    [JsonPropertyName("faq_id")]
    public string? FaqId { get; set; }

    /// <summary>
    /// <para>知识库FAQ ID（旧版本字段，建议使用 faq_id）</para>
    /// <para>必填：否</para>
    /// <para>示例值："6936004780707807231"</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>服务台 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值："6936004780707807251"</para>
    /// </summary>
    [JsonPropertyName("helpdesk_id")]
    public string? HelpdeskId { get; set; }

    /// <summary>
    /// <para>问题</para>
    /// <para>必填：否</para>
    /// <para>示例值："Question"</para>
    /// </summary>
    [JsonPropertyName("question")]
    public string? Question { get; set; }

    /// <summary>
    /// <para>答案</para>
    /// <para>必填：否</para>
    /// <para>示例值："Answer"</para>
    /// </summary>
    [JsonPropertyName("answer")]
    public string? Answer { get; set; }

    /// <summary>
    /// <para>富文本答案</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("answer_richtext")]
    public FaqRichtext[]? AnswerRichtext { get; set; }

    /// <summary>
    /// <para>内容</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// <para>类型</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>创建时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：1596379008</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public long? CreateTime { get; set; }

    /// <summary>
    /// <para>修改时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：1596379008</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public long? UpdateTime { get; set; }

    /// <summary>
    /// <para>分类</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("categories")]
    public Category[]? Categories { get; set; }

    /// <summary>
    /// <para>相似问题列表</para>
    /// <para>必填：否</para>
    /// <para>示例值：["Similar questions"]</para>
    /// </summary>
    [JsonPropertyName("tags")]
    public string[]? Tags { get; set; }

    /// <summary>
    /// <para>失效时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：1596379008</para>
    /// </summary>
    [JsonPropertyName("expire_time")]
    public long? ExpireTime { get; set; }

    /// <summary>
    /// <para>更新人</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("update_user")]
    public TicketUser? UpdateUser { get; set; }

    /// <summary>
    /// <para>创建人</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("create_user")]
    public TicketUser? CreateUser { get; set; }
}
