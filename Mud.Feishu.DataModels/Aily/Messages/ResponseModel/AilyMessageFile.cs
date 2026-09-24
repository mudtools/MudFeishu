// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Aily;


/// <summary>
/// <para>消息中包含的文件</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Aily")]
public class AilyMessageFile
{
    /// <summary>
    /// <para>文件 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：file_4d9nu1ev3a2rq</para>
    /// <para>最大长度：32</para>
    /// <para>最小长度：6</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>文件类型，参见[MIME 类型（IANA 媒体类型）](https://developer.mozilla.org/zh-CN/docs/Web/HTTP/Basics_of_HTTP/MIME_types)</para>
    /// <para>必填：否</para>
    /// <para>示例值：image/png</para>
    /// <para>最大长度：128</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("mime_type")]
    public string? MimeType { get; set; }

    /// <summary>
    /// <para>文件名</para>
    /// <para>必填：否</para>
    /// <para>示例值：发票.png</para>
    /// <para>最大长度：64</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("file_name")]
    public string? FileName { get; set; }

    /// <summary>
    /// <para>其他透传信息</para>
    /// <para>必填：否</para>
    /// <para>示例值：{}</para>
    /// <para>最大长度：255</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("metadata")]
    public string? Metadata { get; set; }

    /// <summary>
    /// <para>文件的创建时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1711975665710</para>
    /// <para>最大长度：13</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    /// <summary>
    /// <para>文件预览链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("preview_url")]
    public AilyMessageFilePreview? PreviewUrl { get; set; }

}