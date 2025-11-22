// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Messages;

/// <summary>
/// 文件上传请求体。
/// </summary>
public class UploadFileRequest
{
    /// <summary>
    /// 待上传的文件类型
    /// 可选值有：
    /// <para> opus：OPUS 音频文件。其他格式的音频文件，请转为 OPUS 格式后上传。可使用 ffmpeg 转换格式：ffmpeg -i SourceFile.mp3 -acodec libopus -ac 1 -ar 16000 TargetFile.opus</para>
    /// <para> mp4：MP4 格式视频文件</para>
    /// <para> pdf：PDF 格式文件</para>
    /// <para> doc：DOC 格式文件</para>
    /// <para> xls：XLS 格式文件</para>
    /// <para> ppt：PPT 格式文件</para>
    /// <para> stream：stream 格式文件。若上传文件不属于以上枚举类型，可以使用 stream 格式</para>
    /// </summary>
    public
#if NET7_0_OR_GREATER
        required
#endif
        string? FileType
    { get; set; }

    /// <summary>
    /// 带后缀的文件名 示例值："测试视频.mp4"
    /// </summary>
    public
#if NET7_0_OR_GREATER
        required
#endif
        string? FileName
    { get; set; }

    /// <summary>
    /// 文件的时长（视频、音频），单位：毫秒。不传值时无法显示文件的具体时长。
    /// <para>示例值：3000</para>
    /// </summary>
    public int? Duration { get; set; }

    /// <summary>
    /// 文件全路径。
    /// <para>示例值："C:\\temp\\测试视频.mp4"</para>
    /// </summary>
    public
#if NET7_0_OR_GREATER
        required
#endif
        string? FullName
    { get; set; }
}
