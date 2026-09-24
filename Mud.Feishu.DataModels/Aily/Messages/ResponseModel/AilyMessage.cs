// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Aily;

/// <summary>
/// <para>消息信息</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Aily")]
public class AilyMessage
{
    /// <summary>
    /// <para>消息 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：message_4df45f2xknvcc</para>
    /// <para>最大长度：32</para>
    /// <para>最小长度：9</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>会话 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：session_4dfunz7sp1g8m</para>
    /// <para>最大长度：32</para>
    /// <para>最小长度：9</para>
    /// </summary>
    [JsonPropertyName("session_id")]
    public string? SessionId { get; set; }

    /// <summary>
    /// <para>运行 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：run_4dfrxvctjqzzj</para>
    /// <para>最大长度：32</para>
    /// <para>最小长度：5</para>
    /// </summary>
    [JsonPropertyName("run_id")]
    public string? RunId { get; set; }

    /// <summary>
    /// <para>消息内容类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：MDX</para>
    /// <para>可选值：<list type="bullet">
    /// <item>MDX：MDX</item>
    /// <item>TEXT：TEXT</item>
    /// <item>CLIP：GUI 卡片</item>
    /// <item>SmartCard：SmartCard</item>
    /// <item>JSON：JSON</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("content_type")]
    public string? ContentType { get; set; }

    /// <summary>
    /// <para>消息内容</para>
    /// <para>必填：否</para>
    /// <para>示例值：你好</para>
    /// <para>最大长度：16777216</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// <para>消息中包含的文件</para>
    /// <para>必填：否</para>
    /// <para>最大长度：32</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("files")]
    public AilyMessageFile[]? Files { get; set; }


    /// <summary>
    /// <para>引用的消息 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：message_4de9bpg70qskh</para>
    /// <para>最大长度：32</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("quote_message_id")]
    public string? QuoteMessageId { get; set; }

    /// <summary>
    /// <para>发送者</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("sender")]
    public AilySender? Sender { get; set; }


    /// <summary>
    /// <para>被@的实体</para>
    /// <para>必填：否</para>
    /// <para>最大长度：32</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("mentions")]
    public AilyMention[]? Mentions { get; set; }


    /// <summary>
    /// <para>消息体的纯文本表达</para>
    /// <para>必填：否</para>
    /// <para>示例值：你好</para>
    /// <para>最大长度：16777216</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("plain_text")]
    public string? PlainText { get; set; }

    /// <summary>
    /// <para>消息的创建时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1711975665710</para>
    /// <para>最大长度：13</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    /// <summary>
    /// <para>状态</para>
    /// <para>必填：否</para>
    /// <para>示例值：COMPLETED</para>
    /// <para>可选值：<list type="bullet">
    /// <item>IN_PROGRESS：生成中</item>
    /// <item>COMPLETED：已完成</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
