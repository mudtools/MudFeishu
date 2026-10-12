// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 上传应用图标请求体（multipart/form-data）。
/// </summary>
[FormContent]
[HttpJsonSerializable(SerializerClassName = "Application")]
public partial class UploadAppAvatarRequest
{
    /// <summary>
    /// 默认构造函数
    /// </summary>
    public UploadAppAvatarRequest()
    {
    }

    /// <summary>
    /// 带图片全路径名称参数的构造函数
    /// </summary>
    /// <param name="fileName">图片文件全路径名称</param>
    public UploadAppAvatarRequest(string? fileName)
    {
        FileName = fileName;
    }

    /// <summary>
    /// <para>图片文件全路径名称。JPEG/PNG/SVG/BMP 格式，最大 2 MB，尺寸大于 240 * 240 px，不带圆角</para>
    /// <para>必填：是</para>
    /// </summary>
    [FilePath]
    [JsonPropertyName("avatar")]
    public string? FileName { get; set; }
}

/// <summary>
/// 上传应用图标响应体数据。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class UploadAppAvatarResult
{
    /// <summary>
    /// <para>应用图标的图片链接，可用于更新应用基础信息（avatar_url）</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://s3-imfile.feishucdn.com/static-resource/v1/v3_006n_24c0a858-0b0d-490b-a8d4-b1e2ef421c8g</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
