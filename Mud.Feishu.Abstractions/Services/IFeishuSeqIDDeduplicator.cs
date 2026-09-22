// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Services;

/// <summary>
/// WebSocket 消息 SeqID 去重服务接口
/// <para>用于处理 ProtoBuf 二进制消息中的序列号去重，防止重复处理</para>
/// <para>可与 EventId 去重配合使用，提供双重防护</para>
/// </summary>
/// <remarks>
/// <b>去重键为裸 <c>SeqID</c>，不含应用/租户维度——这是一条<b>有前提的</b>设计，不是疏漏。</b>
/// 前提是"同一进程内只有一个 WebSocket 连接"：<c>IFeishuWebSocketClient</c> 与
/// <see cref="IFeishuSeqIDDeduplicator"/> 均为单例，且 <c>FeishuWebSocketManager</c> 只绑定
/// <b>默认应用</b>（<c>IFeishuAppManager.GetDefaultApp()</c>），因此不存在"A 应用与 B 应用
/// 的 SeqID 序列在同一去重集合中交错"的场景。
/// <para>
/// <b>若将来引入多应用 WebSocket 装配</b>（每应用一个客户端/连接），则必须同时把
/// **应用维度**加入去重键（例如 <c>TryMarkAsProcessedAsync(seqId, scopeKey)</c>），
/// 否则 A 应用已处理的 SeqID 会抑制 B 应用的同号消息 —— 表现为**静默事件丢失**
/// （既无异常也无日志，因为"重复"是被当作正常路径跳过的）。
/// </para>
/// <para>
/// 该前提由架构守卫 <c>WebSocketContractGuards.SeqIdDeduplication_ShouldStaySingleAppScoped_OrGainAppDimension</c>
/// 在源码层守护：一旦检测到多应用装配，"裸 SeqID 调用点"的断言会同时失败并提示此处约束。
/// </para>
/// </remarks>
public interface IFeishuSeqIDDeduplicator : IAsyncDisposable
{
    /// <summary>
    /// 尝试标记 SeqID 为已处理
    /// </summary>
    /// <param name="seqId">消息序列号</param>
    /// <returns>
    /// <c>true</c> 表示该 SeqID 已被处理过，应跳过当前消息
    /// <c>false</c> 表示该 SeqID 未被处理过，是新的消息
    /// </returns>
    Task<bool> TryMarkAsProcessedAsync(ulong seqId);

    /// <summary>
    /// 回滚 SeqID 的处理状态，允许重新处理
    /// <para>当事件处理失败时调用此方法，移除 SeqID 的已处理标记，
    /// 使服务端重发该消息时能够被重新处理</para>
    /// </summary>
    /// <param name="seqId">消息序列号</param>
    /// <returns>异步任务</returns>
    Task RollbackAsync(ulong seqId);

    /// <summary>
    /// 检查指定 SeqID 是否已处理
    /// </summary>
    /// <param name="seqId">消息序列号</param>
    /// <returns><c>true</c> 表示已处理，<c>false</c> 表示未处理</returns>
    Task<bool> IsProcessedAsync(ulong seqId);

    /// <summary>
    /// 异步清空缓存
    /// </summary>
    Task ClearCacheAsync();

    /// <summary>
    /// 获取当前缓存的 SeqID 数量
    /// </summary>
    int GetCacheCount();

    /// <summary>
    /// 异步获取当前缓存的 SeqID 数量（WHF-R2/C6）。
    /// </summary>
    Task<int> GetCacheCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取已处理的最大 SeqID
    /// </summary>
    /// <returns>最大的 SeqID 值，如果没有则返回 0</returns>
    ulong GetMaxProcessedSeqId();

    /// <summary>
    /// 异步获取已处理的最大 SeqID（WHF-R2/C6）。
    /// </summary>
    /// <returns>最大的 SeqID 值，如果没有则返回 0</returns>
    Task<ulong> GetMaxProcessedSeqIdAsync(CancellationToken cancellationToken = default);
}
