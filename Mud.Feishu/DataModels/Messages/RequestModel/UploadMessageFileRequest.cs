// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Messages;

/// <summary>
/// 文件上传请求体。
/// </summary>
[FormContent]
public partial class UploadMessageFileRequest
{
    /// <summary>
    /// <para>待上传的文件类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：mp4</para>
    /// <para>可选值：<list type="bullet">
    /// <item>opus：OPUS 音频文件。其他格式的音频文件，请转为 OPUS 格式后上传。可使用 ffmpeg 转换格式：`ffmpeg -i SourceFile.mp3 -acodec libopus -ac 1 -ar 16000 TargetFile.opus`</item>
    /// <item>mp4：MP4 格式视频文件</item>
    /// <item>pdf：PDF 格式文件</item>
    /// <item>doc：DOC 格式文件</item>
    /// <item>xls：XLS 格式文件</item>
    /// <item>ppt：PPT 格式文件</item>
    /// <item>stream：stream 格式文件。若上传文件不属于以上枚举类型，可以使用 stream 格式</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("file_type")]
    public string FileType { get; set; } = string.Empty;

    /// <summary>
    /// <para>带后缀的文件名</para>
    /// <para>必填：是</para>
    /// <para>示例值：测试视频.mp4</para>
    /// </summary>
    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// <para>文件的时长（视频、音频），单位：毫秒。不传值时无法显示文件的具体时长。</para>
    /// <para>必填：否</para>
    /// <para>示例值：3000</para>
    /// </summary>
    [JsonPropertyName("duration")]
    public int? Duration { get; set; }

    /// <summary>
    /// 需要上传的文件的本地路径，必须是绝对路径。上传前会先检查该路径下是否存在该文件，如果不存在则会抛出异常。
    /// </summary>
    [JsonPropertyName("file")]
    [FilePath]
    public string? FilePath { get; set; }
}
