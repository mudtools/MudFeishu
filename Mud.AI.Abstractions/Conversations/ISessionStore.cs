// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Abstractions.Conversations;

/// <summary>
/// 会话持久化端口（<b>产品中立</b>：Get / Save / Delete 三方法，零依赖）。
/// </summary>
/// <remarks>
/// <para>
/// <b>载荷形态</b>：存的是 <c>AIAgent.SerializeSessionAsync</c> 产出的会话 JSON <b>字符串</b>——
/// 序列化 / 反序列化由持有 <c>AgentSession</c> 的 Agent 负责（见
/// <c>Mud.AI.Agent.Sessions.AgentSessionStore</c>），本端口只做「原始字符串 + TTL」存取，
/// 从而内存 / Redis / 任意后端的行为逐字节一致。
/// </para>
/// <para>
/// <b>为什么端口在中立包而不是各产品包里（P1-H3 的定式）</b>：会话门面
/// （<c>AgentSessionStore</c>）住在中立的 AI 运行期包里，若它依赖某个<b>产品</b>的存储契约，
/// 依赖方向就倒置（中立包 → 产品契约），P3 上游化当场不可能。反过来把产品侧的存储契约搬到
/// <c>Mud.AI.*</c> 也不行：那会让<b>已发布</b>的核心 SDK 包反向依赖<b>未发布</b>的 AI 包族
/// （由 <c>CoreSdkProjects_ShouldNotDependOnMudAiFamily</c> 机械锁定）。
/// ⇒ 正解是「中立端口在此 + 产品侧适配器」：
/// 产品包（<c>Mud.Feishu.AI</c>）用一个 internal 适配器把它的存储契约桥接到本端口，
/// 核心 SDK 与已发布包<b>一个字节都不用改</b>。
/// </para>
/// <para>
/// <b>键的所有权</b>：键的构造规则（租户 / 会话维度前缀）是产品的键治理，不属于本端口；
/// 本端口只按调用方给的字符串存取。键的<b>状态键</b>（会话历史在状态袋内的命名空间）同理——
/// 由产品侧注入给门面，中立包不硬编码任何产品前缀。
/// </para>
/// </remarks>
public interface ISessionStore
{
    /// <summary>
    /// 读取会话 JSON。不存在、已过期或载荷损坏时返回 <see langword="null"/>（视为 miss，由调用方重建会话）。
    /// </summary>
    /// <param name="key">会话键（由产品侧的键治理构造）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>会话 JSON 字符串；miss 时为 <see langword="null"/>。</returns>
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存会话 JSON 并刷新 TTL。
    /// </summary>
    /// <param name="key">会话键（由产品侧的键治理构造）。</param>
    /// <param name="serializedSession">会话 JSON（<c>AIAgent.SerializeSessionAsync</c> 产物）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task SaveAsync(string key, string serializedSession, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除会话。键不存在时静默成功。
    /// </summary>
    /// <param name="key">会话键（由产品侧的键治理构造）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
