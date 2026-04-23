// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive;

/// <summary>
/// 上传文件响应体
/// <para>将指定文件上传至云空间指定目录中。</para>
/// <para>## 使用限制</para>
/// <para>- 文件大小不得超过 20 MB，且不可上传空文件。要上传大于 20 MB 的文件，你需使用分片上传文件相关接口。详情参考[上传文件概述](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/drive-v1/file/multipart-upload-file-/introduction)。</para>
/// <para>- 该接口调用频率上限为 5 QPS，10000 次/天。否则会返回 1061045 错误码，可通过稍后重试解决。</para>
/// </summary>
public class FilesUploadAllResult
{
    /// <summary>
    /// <para>已上传的文件的 token</para>
    /// <para>必填：否</para>
    /// <para>示例值：boxcnrHpsg1QDqXAAAyachabcef</para>
    /// </summary>
    [JsonPropertyName("file_token")]
    public string? FileToken { get; set; }
}
