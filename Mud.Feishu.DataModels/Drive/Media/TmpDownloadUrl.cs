// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive.Media;

/// <summary>
/// <para>临时下载列表</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Drive")]
public class TmpDownloadUrl
{
    /// <summary>
    /// <para>素材的 token</para>
    /// <para>必填：是</para>
    /// <para>示例值：boxcnrHpsg1QDqXAAAyachabcef</para>
    /// </summary>
    [JsonPropertyName("file_token")]
    public string FileToken { get; set; } = string.Empty;

    /// <summary>
    /// <para>素材的临时下载链接</para>
    /// <para>必填：是</para>
    /// <para>示例值：https://internal-api-drive-stream.feishu.cn/space/api/box/stream/download/authcode/?code=ZDA3MzNiNmUwMjE2MGUzZmQ1OGZlOWYzMWQ4YmI0ZjdfMDYzOWNlZjgyMmI1MmY5NTUxZmM0MjJlYWIyMGVjOWZfSUQ6Njk3NjgzMTY0Mjc5OTI5MjQyMl8xNjI0NDMxMDY3OjE2MjQ1MTc0NjdfVjM</para>
    /// </summary>
    [JsonPropertyName("tmp_download_url")]
    public string TmpDownloadUrlSuffix { get; set; } = string.Empty;
}
