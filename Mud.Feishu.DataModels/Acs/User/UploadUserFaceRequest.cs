// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Acs;

/// <summary>
/// 上传人脸图片请求体（multipart/form-data）
/// </summary>
[FormContent]
[HttpJsonSerializable(SerializerClassName = "Acs")]
public partial class UploadUserFaceRequest
{
    /// <summary>
    /// 默认构造函数
    /// </summary>
    public UploadUserFaceRequest()
    {
    }

    /// <summary>
    /// 带文件全路径名称参数的构造函数
    /// </summary>
    /// <param name="filePath">人脸图片文件本地路径，必须是绝对路径</param>
    /// <param name="fileType">文件类型，可选的类型有 jpg、png</param>
    /// <param name="fileName">带后缀的文件名</param>
    public UploadUserFaceRequest(string filePath, string fileType, string fileName)
    {
        FilePath = filePath;
        FileType = fileType;
        FileName = fileName;
    }

    /// <summary>
    /// <para>文件类型，可选的类型有 jpg、png</para>
    /// <para>必填：是</para>
    /// <para>示例值：jpg</para>
    /// </summary>
    [JsonPropertyName("file_type")]
    public string FileType { get; set; } = string.Empty;

    /// <summary>
    /// <para>带后缀的文件名</para>
    /// <para>必填：是</para>
    /// <para>示例值：efeqz12f.jpg</para>
    /// </summary>
    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 人脸图片文件的本地路径，必须是绝对路径。上传前会先检查该路径下是否存在该文件，如果不存在则会抛出异常。
    /// </summary>
    [FilePath]
    [JsonPropertyName("files")]
    public string? FilePath { get; set; }
}
