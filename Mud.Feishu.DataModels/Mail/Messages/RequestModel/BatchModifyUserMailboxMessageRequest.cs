// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Mail;


/// <summary>
/// 批量修改邮件请求体
/// </summary>
public class BatchModifyUserMailboxMessageRequest
{
    /// <summary>
    /// <para>需要修改的邮件ID，可通过列出邮件接口、收信事件通知等方式获得</para>
    /// <para>必填：否</para>
    /// <para>最大长度：20</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("message_ids")]
    public string[]? MessageIds { get; set; }

    /// <summary>
    /// <para>待添加的标签。可选值包括：UNREAD、IMPORTANT、OTHER、FLAGGED，以及自定义标签 ID。</para>
    /// <para>必填：否</para>
    /// <para>最大长度：20</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("add_label_ids")]
    public string[]? AddLabelIds { get; set; }

    /// <summary>
    /// <para>待移除的标签。可选值包括：UNREAD、IMPORTANT、OTHER、FLAGGED，以及自定义标签 ID。</para>
    /// <para>必填：否</para>
    /// <para>最大长度：20</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("remove_label_ids")]
    public string[]? RemoveLabelIds { get; set; }

    /// <summary>
    /// <para>需要移入的文件夹。支持INBOX、SENT、SPAM、ARCHIVED以及自定义文件夹ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：INBOX</para>
    /// <para>最大长度：100</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("add_folder")]
    public string? AddFolder { get; set; }
}
