// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Conversations;

/// <summary>
/// 会话持久化抽象（Get / Save / Delete + TTL）。
/// </summary>
/// <remarks>
/// <para>
/// 存储载荷是 <b>MAF <c>AIAgent.SerializeSessionAsync</c> 产出的会话 JSON 字符串</b>——
/// 序列化/反序列化由持有 AgentSession 的 Agent 实现（Mud.Feishu.AI 的 FeishuAgent）负责，
/// Store 只做「原始字符串 + TTL」存取（Phase 0 §3.3），从而 Memory/Redis 两端行为逐字节一致。
/// </para>
/// <para>
/// 键必须由 <see cref="ConversationKeyBuilder"/> 构造，禁止调用点内联拼接；
/// TTL 单一阈值源为 <see cref="Configuration.FeishuConversationOptions.SessionTtl"/>。
/// </para>
/// </remarks>
public interface IConversationStore
{
    /// <summary>
    /// 读取会话 JSON。不存在、已过期或载荷损坏时返回 <see langword="null"/>（视为 miss，由调用方重建会话）。
    /// </summary>
    /// <param name="key">由 <see cref="ConversationKeyBuilder"/> 构造的会话键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>会话 JSON 字符串；miss 时为 <see langword="null"/>。</returns>
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存会话 JSON 并刷新 TTL。
    /// </summary>
    /// <param name="key">由 <see cref="ConversationKeyBuilder"/> 构造的会话键。</param>
    /// <param name="serializedSession">会话 JSON（<c>AIAgent.SerializeSessionAsync</c> 产物）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task SaveAsync(string key, string serializedSession, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除会话。键不存在时静默成功。
    /// </summary>
    /// <param name="key">由 <see cref="ConversationKeyBuilder"/> 构造的会话键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
