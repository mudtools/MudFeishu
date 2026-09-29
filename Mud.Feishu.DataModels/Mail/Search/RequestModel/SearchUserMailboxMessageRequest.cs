// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Mail;

/// <summary>
/// <para>搜索用户邮箱邮件请求体</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox/search"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class SearchUserMailboxMessageRequest
{
    /// <summary>
    /// <para>搜索关键词</para>
    /// <para>必填：否</para>
    /// <para>示例值：合同审批通知</para>
    /// <para>最大长度：50</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("query")]
    public string? Query { get; set; }

    /// <summary>
    /// <para>过滤条件，支持按发件人、收件人、文件夹、时间范围等多维组合过滤，缩小查询范围、提升搜索准确性</para>
    /// <para>必填：否</para>
    /// <para>示例值：{"from": ["user@example.com"], "is_unread": true}</para>
    /// </summary>
    [JsonPropertyName("filter")]
    public MailSearchFilter? Filter { get; set; }
}
