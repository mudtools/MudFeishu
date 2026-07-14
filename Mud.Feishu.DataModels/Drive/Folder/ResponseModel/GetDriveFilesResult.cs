// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive.Folder;

/// <summary>
/// 获取文件夹中的文件清单 响应体
/// <para>该接口用于获取用户云空间指定文件夹中文件信息清单。文件的信息包括名称、类型、token、创建时间、所有者 ID 等。</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Drive")]
public class GetDriveFilesResult : ApiPageListResult
{
    /// <summary>
    /// <para>文件夹中的文件清单列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("files")]
    public FileInfo[]? Files { get; set; }

    /// <summary>
    /// <para>分页标记，当 has_more 为 true 时，会同时返回下一次遍历的 page_token，否则不返回。</para>
    /// <para>必填：否</para>
    /// <para>示例值：MTY1NTA3MTA1OXw3MTA4NDc2MDc1NzkyOTI0Nabcef</para>
    /// </summary>
    [JsonPropertyName("next_page_token")]
    public string? NextPageToken { get; set; }

    /// <inheritdoc/>
    public override string? PageToken { get => NextPageToken; set => NextPageToken = value; }
}
