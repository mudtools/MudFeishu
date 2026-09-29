// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Mail;

/// <summary>
/// <para>邮件搜索过滤条件</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox/search"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class MailSearchFilter
{
    /// <summary>
    /// <para>发件人姓名或邮箱地址筛选，仅返回来自该发件人的邮件</para>
    /// <para>必填：否</para>
    /// <para>示例值：["zhangsan"]</para>
    /// <para>最大长度：100</para>
    /// </summary>
    [JsonPropertyName("from")]
    public string[]? From { get; set; }

    /// <summary>
    /// <para>收件人姓名或邮箱地址筛选，仅返回收件人列表包含该收件人的邮件</para>
    /// <para>必填：否</para>
    /// <para>示例值：["lisi"]</para>
    /// <para>最大长度：100</para>
    /// </summary>
    [JsonPropertyName("to")]
    public string[]? To { get; set; }

    /// <summary>
    /// <para>抄送人姓名或邮箱地址筛选，仅返回抄送人列表包含该抄送人的邮件</para>
    /// <para>必填：否</para>
    /// <para>示例值：["wangwu"]</para>
    /// <para>最大长度：100</para>
    /// </summary>
    [JsonPropertyName("cc")]
    public string[]? Cc { get; set; }

    /// <summary>
    /// <para>密送人姓名或邮箱地址筛选，仅返回密送人列表包含该密送人的邮件</para>
    /// <para>必填：否</para>
    /// <para>示例值：["yiming"]</para>
    /// <para>最大长度：100</para>
    /// </summary>
    [JsonPropertyName("bcc")]
    public string[]? Bcc { get; set; }

    /// <summary>
    /// <para>邮件主题搜索，仅返回主题中能搜到搜索词的结果</para>
    /// <para>必填：否</para>
    /// <para>示例值：合同签署</para>
    /// </summary>
    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    /// <summary>
    /// <para>文件夹名称筛选，仅返回在所选目录下的邮件。仅支持传入系统文件夹名称（固定值 inbox/sent/draft/trash/spam/archive/priority/flagged/other/scheduled，其中 priority 指重要邮件）或自定义文件夹名称。子文件夹需使用 parent_name/child_name 格式，可通过文件夹列表接口查看文件夹名称。</para>
    /// <para>必填：否</para>
    /// <para>示例值：["trash"]</para>
    /// <para>最大长度：100</para>
    /// </summary>
    [JsonPropertyName("folder")]
    public string[]? Folder { get; set; }

    /// <summary>
    /// <para>自定义标签名称筛选，仅返回包含指定自定义标签的邮件。子标签需使用 parent_name/child_name 格式，可通过标签列表接口查看标签名称。</para>
    /// <para>必填：否</para>
    /// <para>示例值：["自定义标签"]</para>
    /// <para>最大长度：100</para>
    /// </summary>
    [JsonPropertyName("label")]
    public string[]? Label { get; set; }

    /// <summary>
    /// <para>是否只筛选有附件的邮件</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// <para>默认值：false</para>
    /// </summary>
    [JsonPropertyName("has_attachment")]
    public bool? HasAttachment { get; set; }

    /// <summary>
    /// <para>是否只筛选未读邮件</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// <para>默认值：false</para>
    /// </summary>
    [JsonPropertyName("is_unread")]
    public bool? IsUnread { get; set; }

    /// <summary>
    /// <para>邮件接收时间范围筛选，只返回在所选时间范围内的邮件。一次搜索请求的时间范围不要超过 1 年。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public MailSearchTimeRange? CreateTime { get; set; }
}

/// <summary>
/// <para>邮件搜索时间范围</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox/search"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class MailSearchTimeRange
{
    /// <summary>
    /// <para>时间范围的开始时间（iso8601，精确到秒）</para>
    /// <para>必填：否</para>
    /// <para>示例值：2026-03-10T00:00:00+08:00</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    /// <summary>
    /// <para>时间范围的截止时间（iso8601，精确到秒）</para>
    /// <para>必填：否</para>
    /// <para>示例值：2026-03-10T00:00:00+08:00</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }
}
