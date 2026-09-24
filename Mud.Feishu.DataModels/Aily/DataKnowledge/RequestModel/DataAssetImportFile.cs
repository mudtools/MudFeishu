// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Aily;

/// <summary>
/// <para>知识导入-文件</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Aily")]
public class DataAssetImportFile
{
    /// <summary>
    /// <para>文件标题</para>
    /// <para>必填：否</para>
    /// <para>示例值：文件标题</para>
    /// <para>最大长度：255</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>上传文件获取到的 token。和 content 二选一，优先使用 token</para>
    /// <para>必填：否</para>
    /// <para>示例值：bb690637b49440b08f39459a2fdcd2ca</para>
    /// <para>最大长度：255</para>
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    /// <summary>
    /// <para>文件内容。和 token 二选一，优先使用 token。有长度限制，大文件优先使用 token 方式</para>
    /// <para>必填：否</para>
    /// <para>示例值：这是文件内容</para>
    /// <para>最大长度：65536</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// <para>文件内容对应的 MIME 类型，必须填写</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>text/plain（.txt）</item>
    /// <item>application/pdf（.pdf）</item>
    /// <item>application/vnd.openxmlformats-officedocument.presentationml.presentation（.pptx）</item>
    /// <item>application/vnd.openxmlformats-officedocument.wordprocessingml.document（.docx）</item>
    /// </list></para>
    /// <para>示例值：application/pdf</para>
    /// <para>最大长度：255</para>
    /// </summary>
    [JsonPropertyName("mime_type")]
    public string? MimeType { get; set; }

    /// <summary>
    /// <para>文件源的 URL</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://document.com/1</para>
    /// <para>最大长度：65535</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
