// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Aily;


/// <summary>
/// 发送 Aily 消息 请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Aily")]
public class CreateSessionAilyMessageRequest
{
    /// <summary>
    /// <para>幂等 ID（如使用 UUID 生成器或时间戳），同一会话下相同的幂等 ID 视为同一个消息（72h）</para>
    /// <para>必填：是</para>
    /// <para>示例值：idempotent_id_1</para>
    /// <para>最大长度：64</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("idempotent_id")]
    public string IdempotentId { get; set; } = string.Empty;

    /// <summary>
    /// <para>消息的类型，包括 `MDX` | `TEXT` 等</para>
    /// <para>- `MDX` 能够表达富文本信息结构，可参考 [Aily 消息节点 markdown 语法](https://bytedance.larkoffice.com/wiki/ZlHYw8jJci3o4dkkEZZcybM0nah)</para>
    /// <para>- `TEXT` 作为纯文本进行处理</para>
    /// <para>必填：是</para>
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
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// <para>消息内容</para>
    /// <para>必填：是</para>
    /// <para>示例值：你好</para>
    /// <para>最大长度：61440</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// <para>消息中包含的文件 ID 列表</para>
    /// <para>必填：否</para>
    /// <para>最大长度：32</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("file_ids")]
    public string[]? FileIds { get; set; }

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
    /// <para>被@的实体</para>
    /// <para>必填：否</para>
    /// <para>最大长度：32</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("mentions")]
    public AilyMention[]? Mentions { get; set; }


}
