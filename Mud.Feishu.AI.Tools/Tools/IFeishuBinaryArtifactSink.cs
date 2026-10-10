// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// 待落盘的<b>出向</b>二进制产物（SDK 侧字节 → 宿主侧存储）。
/// </summary>
/// <param name="FileName">建议文件名（宿主可据此定扩展名/对象键）。</param>
/// <param name="Content">产物字节（<b>只进宿主，绝不进工具结果</b>——A10 二进制防线）。</param>
/// <param name="ContentType">内容类型（可空；宿主可空实现）。</param>
public readonly record struct BinaryArtifact(string FileName, ReadOnlyMemory<byte> Content, string? ContentType);

/// <summary>
/// 已落盘的产物引用：<b>只回传句柄与元信息，字节留在宿主侧</b>。
/// </summary>
/// <param name="Handle">宿主侧可引用句柄（URL / 对象键 / 路径）。</param>
/// <param name="Size">字节数。</param>
/// <param name="ContentType">内容类型（可空）。</param>
public readonly record struct StoredArtifact(string Handle, long Size, string? ContentType);

/// <summary>
/// 二进制产物落盘器（<b>出向</b>：SDK → 宿主；宿主注入，SDK 只定契约）。
/// </summary>
/// <remarks>
/// <para>
/// <b>方向差异（务必与邻居接口区分，RV-4）</b>：
/// <list type="table">
/// <item>
/// <term><c>IFeishuAttachmentStager</c>（入向，既有）</term>
/// <description>URL/内存字节 → <b>本地路径</b>，供 SDK 的 <c>[FormContent]/[FilePath]</c> 上传参数消费。</description>
/// </item>
/// <item>
/// <term><see cref="IFeishuBinaryArtifactSink"/>（出向，本接口）</term>
/// <description>SDK 拿到的 <c>byte[]</c> → <b>宿主侧句柄</b>，供模型引用/落盘/上云。</description>
/// </item>
/// </list>
/// 两者形态对称（都是"宿主策略、SDK 只声明"），但<b>方向相反</b>，不可互相替代。
/// </para>
/// <para>
/// <b>为什么需要它</b>：工具面禁止二进制穿越（A10），于是所有返回 <c>byte[]</c> 的 SDK 方法
/// （画板缩略图 / 文件下载 / 妙记逐字稿 / Spark 存储对象 / 考勤附件）在"只有禁止"的形态下
/// <b>永久不可用</b>；宿主若真有产物需求，只能绕过工具执行链直连 SDK（丢掉授权/审计/净化三件套）。
/// 本契约给出第三条路：<b>字节经宿主落盘，工具结果只带句柄</b>。
/// </para>
/// <para>
/// <b>软缺席语义（与 <c>IFeishuAttachmentStager</c> 完全对称）</b>：宿主未注册实现 ⇒
/// 依赖它的工具<b>不注册</b>（模型看不到，而不是运行期才失败）。本契约<b>不</b>由 SDK 提供默认实现
/// （落盘位置/大小上限/MIME 白名单/生命周期全是宿主策略）。
/// </para>
/// <para>
/// <b>安全默认</b>：返回 <see langword="null"/> 表示"该产物不被允许落盘"
/// （工具层据此回填结构化 <c>invalid_args</c>，<b>不</b>降级为静默跳过）。
/// </para>
/// </remarks>
public interface IFeishuBinaryArtifactSink
{
    /// <summary>
    /// 落盘一个出向产物并返回宿主侧句柄。
    /// </summary>
    /// <param name="artifact">产物（字节 + 建议元信息）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已落盘引用；返回 <see langword="null"/> 表示该产物不被允许（工具层转为结构化错误）。</returns>
    Task<StoredArtifact?> StoreAsync(BinaryArtifact artifact, CancellationToken cancellationToken = default);
}
