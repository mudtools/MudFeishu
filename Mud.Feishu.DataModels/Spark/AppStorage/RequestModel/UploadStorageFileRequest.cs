// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spark;

/// <summary>
/// 上传文件请求体
/// </summary>
[FormContent]
[HttpJsonSerializable(SerializerClassName = "Spark")]
public partial class UploadStorageFileRequest
{
    /// <summary>
    /// 默认构造函数
    /// </summary>
    public UploadStorageFileRequest()
    {
    }

    /// <summary>
    /// 带文件全路径名称参数的构造函数
    /// </summary>
    /// <param name="fileName">文件名称</param>
    /// <param name="filePath">文件本地路径，必须是绝对路径</param>
    public UploadStorageFileRequest(string fileName, string? filePath)
    {
        FileName = fileName;
        FilePath = filePath;
    }

    /// <summary>
    /// <para>文件名称</para>
    /// <para>必填：是</para>
    /// <para>示例值：file_name</para>
    /// </summary>
    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// <para>文件的十六进制 SHA-256 值，用于文件一致性校验。如果传入此值，服务端会在上传完成后对比接收到文件的 SHA-256 值，如果不一致，会返回上传失败。</para>
    /// <para>必填：否</para>
    /// <para>示例值：f8d80a7f68b820d99f5612b952140319991d6599d95f29699d076684b0977f99</para>
    /// </summary>
    [JsonPropertyName("check_sum")]
    public string? CheckSum { get; set; }

    /// <summary>
    /// 需要上传的文件的本地路径，必须是绝对路径。上传前会先检查该路径下是否存在该文件，如果不存在则会抛出异常。
    /// </summary>
    [FilePath]
    [JsonPropertyName("file")]
    public string? FilePath { get; set; }
}
