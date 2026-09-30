// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Mail;

/// <summary>
/// <para>邮件搜索结果项</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox/search"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class MailSearchItem
{
    /// <summary>
    /// <para>邮件唯一标识</para>
    /// <para>必填：否</para>
    /// <para>示例值：msg_XXX</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>包含邮件基本信息的卡片，用户搜索关键词命中的文本片段，使用 &lt;h&gt;&lt;/h&gt; 标签包裹标注</para>
    /// <para>必填：否</para>
    /// <para>示例值：{}</para>
    /// </summary>
    [JsonPropertyName("display_info")]
    public string? DisplayInfo { get; set; }

    /// <summary>
    /// <para>邮件元信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("meta_data")]
    public MailSearchMeta? MetaData { get; set; }
}

/// <summary>
/// <para>邮件搜索结果元信息</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox/search"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class MailSearchMeta
{
    /// <summary>
    /// <para>邮件主题</para>
    /// <para>必填：否</para>
    /// <para>示例值：测试邮件</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>邮件会话 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：thread_id_XXX</para>
    /// </summary>
    [JsonPropertyName("thread_id")]
    public string? ThreadId { get; set; }

    /// <summary>
    /// <para>邮件接收时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：2026-03-15T14:30:00+08:00</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>邮件唯一标识</para>
    /// <para>必填：否</para>
    /// <para>示例值：msg_id_xxx</para>
    /// </summary>
    [JsonPropertyName("message_biz_id")]
    public string? MessageBizId { get; set; }

    /// <summary>
    /// <para>邮件发件人</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("from")]
    public MailAddress? From { get; set; }
}
