// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Events;

/// <summary>
/// 媒体转写契约（T3 / D3）：把图片/音频/文件转成可注入 prompt 的文本。
/// </summary>
/// <remarks>
/// <para>
/// <b>软缺席</b>：宿主未注册时，装配器降级为占位文本（不阻断对话）。
/// </para>
/// <para>
/// <b>安全约束</b>（A17）：实现方不得将原始 base64 / 转写文本写入日志或 Metrics tag；
/// 返回的 <see cref="MediaTranscription.Text"/> 经装配器截断后注入 prompt。
/// </para>
/// </remarks>
public interface IFeishuMediaTranscriber
{
    /// <summary>
    /// 把媒体资源转成可注入 prompt 的文本。
    /// </summary>
    /// <param name="request">转写请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>转写结果；返回 null 表示不处理（装配器输出占位说明）。</returns>
    Task<MediaTranscription?> TranscribeAsync(MediaTranscriptionRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// 媒体转写请求。
/// </summary>
/// <param name="AppKey">应用键。</param>
/// <param name="MessageId">消息 ID。</param>
/// <param name="FileKey">文件键（image_key / file_key）。</param>
/// <param name="MediaKind">媒体类型（image / audio / file / media）。</param>
/// <param name="FileName">文件名（可空）。</param>
public sealed record MediaTranscriptionRequest(string AppKey, string MessageId, string FileKey, string MediaKind, string? FileName);

/// <summary>
/// 媒体转写结果。
/// </summary>
/// <param name="Text">转写文本。</param>
/// <param name="Kind">转写类型（ocr / stt / metadata）。</param>
/// <param name="Truncated">是否被截断。</param>
public sealed record MediaTranscription(string Text, string Kind, bool Truncated);
