// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spark;

/// <summary>
/// 分片上传文件 - 创建上传请求响应体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Spark")]
public class UploadStorageInitializeResult
{
    /// <summary>
    /// <para>上传请求 ID，有效期 24h</para>
    /// <para>必填：否</para>
    /// <para>示例值：upload_abc123xyz456</para>
    /// </summary>
    [JsonPropertyName("upload_id")]
    public string? UploadId { get; set; }

    /// <summary>
    /// <para>建议的分片大小（字节）</para>
    /// <para>必填：否</para>
    /// <para>示例值：4194304</para>
    /// </summary>
    [JsonPropertyName("chunk_size")]
    public int? ChunkSize { get; set; }

    /// <summary>
    /// <para>预估的总分片数</para>
    /// <para>必填：否</para>
    /// <para>示例值：25</para>
    /// </summary>
    [JsonPropertyName("chunk_numbers")]
    public int? ChunkNumbers { get; set; }
}
