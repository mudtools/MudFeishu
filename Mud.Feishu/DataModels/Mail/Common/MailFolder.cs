// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Mail;

/// <summary>
/// <para>邮件文件夹的完整信息，包含文件夹ID、名称、归属层级、类型及未读统计数据。</para>
/// </summary>
public class MailFolder
{
    /// <summary>
    /// <para>folder id</para>
    /// <para>必填：否</para>
    /// <para>示例值：7620095646711680541</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>文件夹名称</para>
    /// <para>必填：是</para>
    /// <para>示例值：newsletter 相关</para>
    /// <para>最大长度：250</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>父文件夹 id，该值为 0 表示根文件夹</para>
    /// <para>必填：是</para>
    /// <para>示例值：725627422334644</para>
    /// </summary>
    [JsonPropertyName("parent_folder_id")]
    public string ParentFolderId { get; set; } = string.Empty;

    /// <summary>
    /// <para>文件夹类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>最大值：2</para>
    /// <para>最小值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：系统文件夹</item>
    /// <item>2：用户文件夹</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("folder_type")]
    public int? FolderType { get; set; }

    /// <summary>
    /// <para>未读邮件数量</para>
    /// <para>必填：否</para>
    /// <para>示例值：3</para>
    /// </summary>
    [JsonPropertyName("unread_message_count")]
    public int? UnreadMessageCount { get; set; }

    /// <summary>
    /// <para>未读会话数量</para>
    /// <para>必填：否</para>
    /// <para>示例值：4</para>
    /// </summary>
    [JsonPropertyName("unread_thread_count")]
    public int? UnreadThreadCount { get; set; }
}