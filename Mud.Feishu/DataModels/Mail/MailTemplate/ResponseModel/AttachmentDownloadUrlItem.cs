// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Mail;


/// <summary>
/// <para>下载链接列表</para>
/// </summary>
public class AttachmentDownloadUrlItem
{
    /// <summary>
    /// <para>附件 id</para>
    /// <para>必填：否</para>
    /// <para>示例值：YQqYbQHoQoDqXjxWKhJbo8Gicjf</para>
    /// </summary>
    [JsonPropertyName("attachment_id")]
    public string? AttachmentId { get; set; }

    /// <summary>
    /// <para>下载链接</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://api-drive-stream.feishu.cn/space/api/box/stream/download/authcode/?code=YTZiZGViMDg3NzRjMzEwOWRkMGI1MTJlYmQxYTFmYTBfZTA5ZjZiOWU4NDYzMzkxMDUyOTIxMzBmNTVjMjAyZTFfSUQ6NzI4MTE4Nzg1OTE5NTc3Mjk0N18xNjk1ODg4NjQyOjE2OTU4ODg3MDJfVjM</para>
    /// </summary>
    [JsonPropertyName("download_url")]
    public string? DownloadUrl { get; set; }
}

