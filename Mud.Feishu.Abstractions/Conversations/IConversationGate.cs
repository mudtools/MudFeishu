// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Conversations;

/// <summary>
/// 会话闸门（AI-FD-D12 P2D-1）：同一会话键串行、跨键并行，
/// 落实总体设计 §4 不变式「同一 conversation_id 的 RunAsync 串行」——防并发 Run 共享同一
/// 会话导致的 last-writer-wins 历史丢失。
/// </summary>
/// <remarks>
/// <para>
/// 契约下沉 Abstractions（对齐 <see cref="IConversationStore"/> 迁移先例）：进程内默认实现
/// <c>KeyedConversationGate</c>（Mud.Feishu.AI，<c>AddFeishuAgent</c> 默认注册）；多实例部署经
/// Mud.Feishu.Redis 的 <c>AddFeishuRedisConversationGate</c> 替换（SET NX + TTL 租约）。
/// </para>
/// <para>
/// 快速失败语义：分布式实现获取失败（有限次重试后）抛 <see cref="ConversationBusyException"/>——
/// 事件层是 at-least-once + 幂等键，排队会把背压藏进 SDK；快速失败把节奏还给事件层重试与退避
/// （幂等回滚 → 事件重投递）。键必须由 <see cref="ConversationKeyBuilder"/> 构造（D8 精神）。
/// </para>
/// </remarks>
public interface IConversationGate
{
    /// <summary>获取会话处理权；同一键串行，跨键并行。返回的释放器 Dispose 时放行。</summary>
    /// <param name="conversationKey">由 <see cref="ConversationKeyBuilder"/> 构造的会话键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>闸门句柄（Dispose 释放）。</returns>
    /// <exception cref="ConversationBusyException">分布式实现获取失败（同键被其他实例持有且重试耗尽）。</exception>
    Task<IConversationGateHandle> AcquireAsync(string conversationKey, CancellationToken cancellationToken = default);
}

/// <summary>会话闸门句柄：Dispose 时释放处理权（finally 语义；幂等于重复 Dispose）。</summary>
public interface IConversationGateHandle : IDisposable
{
}

/// <summary>
/// 会话闸门忙异常（AI-FD-D12 P2D-1）：同键正被其他实例处理且有限次重试耗尽时抛出——
/// 调用方（事件层）经既有幂等回滚 + 事件重投递机制承接，不静默排队。
/// </summary>
public sealed class ConversationBusyException : InvalidOperationException
{
    /// <summary>构造忙异常。</summary>
    /// <param name="conversationKey">被占用的会话键。</param>
    /// <param name="message">可读原因。</param>
    public ConversationBusyException(string conversationKey, string message)
        : base(message)
        => ConversationKey = conversationKey;

    /// <summary>被占用的会话键。</summary>
    public string ConversationKey { get; }
}
