// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Aily;

/// <summary>
/// 上传文件用于数据知识管理请求体
/// </summary>
[FormContent]
[HttpJsonSerializable(SerializerClassName = "Aily")]
public partial class UploadDataAssetFileRequest
{
    /// <summary>
    /// 默认构造函数
    /// </summary>
    public UploadDataAssetFileRequest()
    {
    }

    /// <summary>
    /// 带文件全路径名称参数的构造函数
    /// </summary>
    /// <param name="filePath">文件全路径名称，仅支持 docx、txt、pdf、pptx 类型</param>
    public UploadDataAssetFileRequest(string? filePath)
    {
        FilePath = filePath;
    }

    /// <summary>
    /// <para>需要上传的文件的本地路径，必须是绝对路径。上传前会先检查该路径下是否存在该文件，如果不存在则会抛出异常。</para>
    /// <para>仅支持上传 docx、txt、pdf、pptx 类型的文件。</para>
    /// </summary>
    [FilePath]
    [JsonPropertyName("file")]
    public string? FilePath { get; set; }
}
