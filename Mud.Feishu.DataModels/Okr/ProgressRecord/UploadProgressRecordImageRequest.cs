// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Okr;

/// <summary>
/// 上传进展记录图片请求体（multipart/form-data）
/// </summary>
[FormContent]
[HttpJsonSerializable(SerializerClassName = "Okr")]
public partial class UploadProgressRecordImageRequest
{
    /// <summary>
    /// <para>图片文件本地路径（必填）。目前仅支持上传 JPG、JPEG、PNG、WEBP、GIF、BMP、ICO、TIFF、HEIC 格式的图片</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("data")]
    [FilePath]
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// <para>图片所在的目标 id（必填），插入图片所在的待创建 / 修改的进展记录对应的目标 ID，可通过批量获取 OKR 或获取用户的 OKR 列表接口获取对应的 Objective 或 KR 的 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：6974586812998174252</para>
    /// </summary>
    [JsonPropertyName("target_id")]
    public string TargetId { get; set; } = string.Empty;

    /// <summary>
    /// <para>图片所在的目标类型（必填）：2 Objective、3 Key Result</para>
    /// <para>必填：是</para>
    /// <para>示例值：2</para>
    /// </summary>
    [JsonPropertyName("target_type")]
    public int TargetType { get; set; }
}
