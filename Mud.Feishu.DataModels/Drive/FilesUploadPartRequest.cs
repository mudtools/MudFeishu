// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive.Files;

/// <summary>
/// 分片上传文件-上传分片请求体
/// </summary>
[FormContent]
[HttpJsonSerializable(SerializerClassName = "Drive")]
public partial class FilesUploadPartRequest
{
    /// <summary>
    /// <para>分片上传事务 ID。</para>
    /// <para>必填：是</para>
    /// <para>示例值：7111211691345512356</para>
    /// </summary>
    [JsonPropertyName("upload_id")]
    public string UploadId { get; set; } = string.Empty;

    /// <summary>
    /// <para>文件分片的序号，从 0 开始计数。</para>
    /// <para>必填：是</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("seq")]
    public int Seq { get; set; }

    /// <summary>
    /// <para>分片的大小，单位为字节。</para>
    /// <para>必填：是</para>
    /// <para>示例值：4194304</para>
    /// </summary>
    [JsonPropertyName("size")]
    public int Size { get; set; }

    /// <summary>
    /// <para>文件分片的 Adler-32 校验和</para>
    /// <para>必填：否</para>
    /// <para>示例值：3248270248</para>
    /// </summary>
    [JsonPropertyName("checksum")]
    public string? Checksum { get; set; }


    /// <summary>
    /// 文件名称。
    /// </summary>
    [FilePath]
    [JsonPropertyName("file")]
    public string? FileName { get; set; }


    /// <summary>
    /// 需要上传的文件分片内容，必须是分片上传事务中指定的分片大小。
    /// </summary>
    public byte[] FileContent { get; set; } = [];
}
