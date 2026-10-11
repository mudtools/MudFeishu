// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// 待落盘的附件来源（<b>URL 优先</b>；进程内 Agent 拿到的通常是 URL 或内存字节，不能假设本地路径）。
/// </summary>
/// <param name="Url">来源 URL（http/https；与 <paramref name="Content"/> 至少提供一个）。</param>
/// <param name="Content">内存字节（可选）。</param>
/// <param name="FileName">建议文件名（可空；宿主可据此定扩展名/MIME）。</param>
public readonly record struct AttachmentSource(string? Url, ReadOnlyMemory<byte>? Content, string? FileName);

/// <summary>
/// 已落盘的附件：SDK 上传接口所需的本地绝对路径 + 生命周期钩子。
/// </summary>
/// <param name="LocalPath">本地绝对路径（SDK 的 <c>[FilePath]</c> 参数消费）。</param>
/// <param name="Size">字节数（宿主已校验大小上限）。</param>
/// <param name="ContentType">内容类型（可空；宿主可空实现）。</param>
/// <param name="Cleanup">
/// 清理钩子：由<b>工具执行链在 <c>finally</c> 中调用</b>（不进 GC/finalizer）——
/// 工具执行完毕后临时文件必须消失。
/// </param>
public readonly record struct StagedAttachment(string LocalPath, long Size, string? ContentType, Func<ValueTask> Cleanup);

/// <summary>
/// 附件落盘器（<b>宿主注入，SDK 只定契约</b>——与 <see cref="IToolExecutionAuthorizer"/> 同款定位）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么 SDK 不实现下载/落盘（U-5）</b>：SDK 的既有品格是"零外部进程、零临时目录假设"。
/// 落盘涉及 ① 域名白名单（防 SSRF 打到内网）② 大小上限 ③ 扩展名/MIME 校验 ④ 磁盘生命周期——
/// <b>这些是宿主策略</b>，SDK 既不知道宿主的网络边界，也不该替它决定写哪里。
/// </para>
/// <para>
/// <b>软缺席语义</b>：宿主未注册本接口时，依赖落盘的上传类工具（<c>im.send_image</c> /
/// <c>im.send_file</c>）<b>不注册</b>——与"域客户端缺席 → 该域工具不进注册表"完全同一机制，
/// 不是新语义，也不会让宿主在运行期才发现能力缺失。
/// </para>
/// <para>
/// <b>安全默认（宿主实现的责任，SDK 不假装能做）</b>：返回 <see langword="null"/> 表示"该来源不被允许"，
/// 工具层据此回填结构化 <c>invalid_args</c>（<b>不降级为跳过校验</b>）。
/// </para>
/// </remarks>
public interface IFeishuAttachmentStager
{
    /// <summary>
    /// 落盘并返回本地绝对路径。
    /// </summary>
    /// <param name="source">来源（URL 或内存字节）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已落盘附件；返回 <see langword="null"/> 表示该来源不被允许（工具层转为结构化错误）。</returns>
    Task<StagedAttachment?> StageAsync(AttachmentSource source, CancellationToken cancellationToken = default);
}
