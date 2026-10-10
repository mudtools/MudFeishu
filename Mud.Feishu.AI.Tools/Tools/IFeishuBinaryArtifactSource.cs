// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.AI;

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// 待转换的<b>入向</b>素材请求（宿主侧 URL / 内存字节 → SDK 可用的上传载体）。
/// </summary>
/// <param name="Url">来源 URL（http/https；与 <paramref name="Content"/> 至少提供一个）。</param>
/// <param name="Content">内存字节（可选）。</param>
/// <param name="FileName">建议文件名（可空；宿主可据此定扩展名/MIME）。</param>
/// <param name="ContentType">内容类型（可空）。</param>
public readonly record struct BinaryArtifactRequest(
    string? Url,
    ReadOnlyMemory<byte>? Content,
    string? FileName,
    string? ContentType);

/// <summary>
/// 已就绪的入向素材：SDK 的 <c>[FormContent]</c> 上传参数 + 生命周期钩子。
/// </summary>
/// <param name="Upload">SDK 上传参数（<c>[FilePath] FileName</c> = 宿主落盘后的本地绝对路径）。</param>
/// <param name="Size">字节数（宿主已校验大小上限）。</param>
/// <param name="ContentType">内容类型（可空）。</param>
/// <param name="Cleanup">
/// 清理钩子：由<b>工具执行链在 <c>finally</c> 中调用</b>（与 <c>StagedAttachment.Cleanup</c> 同款，
/// 不进 GC/finalizer）。
/// </param>
public readonly record struct ResolvedBinaryArtifact(
    FileUploadRequest Upload,
    long Size,
    string? ContentType,
    Func<ValueTask> Cleanup);

/// <summary>
/// 二进制素材<b>入向</b>契约（宿主 → SDK；宿主注入，SDK 只定契约）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它（R7 / DP-C3-1）</b>：飞书 AI 能力里最有价值的三类（OCR、文档/票据识别、语音转文字）
/// 入参形态是 <b>base64 字符串</b>或 <c>[FormContent]</c> <b>本地文件路径</b>——进程内 Agent 两样都给不出
/// （模型没有文件系统，也不该把 MB 级 base64 塞进工具参数）。把它们放进工具面会产生三个反模式：
/// MB 级参数、上下文爆炸、以及"模型以为自己能读文件"的错误心智模型。
/// </para>
/// <para>
/// <b>正确形态</b>：宿主侧把 URL/字节转成 SDK 可消费的载体（本地落盘 + <see cref="FileUploadRequest"/>），
/// 由宿主代码调用 OCR/STT（约 5 行），再把<b>文本结果</b>交给模型。本契约即这条路径的接缝。
/// </para>
/// <para>
/// <b>与邻居接口的方向差异（RV-4）</b>：
/// <list type="table">
/// <item>
/// <term><c>IFeishuAttachmentStager</c>（入向，既有）</term>
/// <description>为 <c>im.send_image</c>/<c>im.send_file</c> 这类<b>工具参数</b>准备本地文件；
/// 未注册 ⇒ 依赖它的<b>工具不注册</b>。</description>
/// </item>
/// <item>
/// <term><see cref="IFeishuBinaryArtifactSource"/>（入向，本接口）</term>
/// <description>为<b>宿主侧调用</b>（非工具）准备上传载体；未注册 ⇒ 宿主这段代码没有接缝可用，
/// <b>不影响任何工具注册</b>（今天没有任何工具依赖它——依赖它的 OCR/STT 工具按 DP-C3-1 不策展）。</description>
/// </item>
/// <item>
/// <term><see cref="IFeishuBinaryArtifactSink"/>（出向）</term>
/// <description>SDK 字节 → 宿主句柄；与 Source 成对（入向/出向），方向相反。</description>
/// </item>
/// </list>
/// </para>
/// <para>
/// <b>安全默认</b>：返回 <see langword="null"/> 表示"该来源不被允许"（域名白名单/大小上限/MIME 校验
/// 都是宿主策略）；调用方据此<b>不发起</b>下游请求。
/// </para>
/// </remarks>
public interface IFeishuBinaryArtifactSource
{
    /// <summary>
    /// 把来源转换为 SDK 可消费的上传载体。
    /// </summary>
    /// <param name="request">素材来源（URL 或内存字节）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已就绪素材（含清理钩子）；返回 <see langword="null"/> 表示该来源不被允许。</returns>
    Task<ResolvedBinaryArtifact?> ResolveAsync(BinaryArtifactRequest request, CancellationToken cancellationToken = default);
}
