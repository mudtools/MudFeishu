// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spark;

/// <summary>
/// 分片上传文件 - 创建上传请求请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Spark")]
public class UploadStorageInitializeRequest
{
    /// <summary>
    /// <para>文件的名称，建议最大长度不超过 100</para>
    /// <para>必填：是</para>
    /// <para>示例值：测试文本文件.txt</para>
    /// </summary>
    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// <para>文件的大小，单位为字节</para>
    /// <para>必填：是</para>
    /// <para>示例值：104857600</para>
    /// <para>取值范围：1 ～ 2147483648</para>
    /// </summary>
    [JsonPropertyName("file_size")]
    public long FileSize { get; set; }

    /// <summary>
    /// <para>文件 MIME 类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：text/plain; charset=utf-8</para>
    /// </summary>
    [JsonPropertyName("mime_type")]
    public string? MimeType { get; set; }
}
