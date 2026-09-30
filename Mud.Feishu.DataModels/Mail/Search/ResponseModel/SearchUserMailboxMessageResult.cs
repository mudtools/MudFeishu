// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Mail;

/// <summary>
/// <para>搜索用户邮箱邮件响应体</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox/search"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class SearchUserMailboxMessageResult
{
    /// <summary>
    /// <para>搜索结果列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("items")]
    public MailSearchItem[]? Items { get; set; }

    /// <summary>
    /// <para>搜索命中结果数</para>
    /// <para>必填：否</para>
    /// <para>示例值：99</para>
    /// </summary>
    [JsonPropertyName("total")]
    public int? Total { get; set; }

    /// <summary>
    /// <para>是否可继续翻页</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("has_more")]
    public bool? HasMore { get; set; }

    /// <summary>
    /// <para>翻页用的 token。当 has_more=true 时，可用这个 token 作为入参继续翻页搜索。</para>
    /// <para>必填：否</para>
    /// <para>示例值：eVQrYzJBNDNONlk4VFZBZVlSdzlKdFJ4bVVHVExENDNKVHoxaVdiVnViQT0=</para>
    /// </summary>
    [JsonPropertyName("page_token")]
    public string? PageToken { get; set; }

    /// <summary>
    /// <para>搜索补充提示信息，返回本次搜索的额外说明，例如：query 被截断；搜索结果不全 等</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("notice")]
    public string? Notice { get; set; }
}
