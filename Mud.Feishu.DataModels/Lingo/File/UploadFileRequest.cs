// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Lingo;

/// <summary>
/// 上传词条图片请求体（multipart/form-data，含普通字段 name 与文件字段 file）
/// </summary>
[FormContent]
[HttpJsonSerializable(SerializerClassName = "Lingo")]
public partial class UploadFileRequest
{
    /// <summary>
    /// 默认构造函数
    /// </summary>
    public UploadFileRequest()
    {
    }

    /// <summary>
    /// 带文件名与文件全路径的构造函数
    /// </summary>
    /// <param name="name">文件名称，当前仅支持上传图片</param>
    /// <param name="filePath">图片文件本地路径，必须是绝对路径</param>
    public UploadFileRequest(string name, string filePath)
    {
        Name = name;
        FilePath = filePath;
    }

    /// <summary>
    /// <para>文件名称，当前仅支持上传图片且图片格式为 icon、bmp、gif、png、jpeg、webp 六种</para>
    /// <para>必填：是</para>
    /// <para>长度范围：1 ～ 100 字符</para>
    /// <para>示例值：示例图片.png</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>图片文件的本地路径，必须是绝对路径。上传前会先检查该路径下是否存在该文件，如果不存在则会抛出异常。</para>
    /// <para>对应 multipart 字段名 file；高宽像素在 320 - 4096 像素之间，大小在 3KB - 10MB。</para>
    /// <para>必填：是</para>
    /// </summary>
    [FilePath]
    [JsonPropertyName("file")]
    public string? FilePath { get; set; }
}
