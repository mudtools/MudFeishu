// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.Abstractions.Conversations;

namespace Mud.AI.Agent.Sessions;

/// <summary>
/// 会话门面（<b>产品中立</b>）：按键存取「序列化会话 JSON」，并承担坏值自愈。
/// </summary>
/// <remarks>
/// <b>职责边界</b>：序列化 / 反序列化的归属方是持有 <c>AgentSession</c> 的那个 Agent（构造时以 <c>codec</c> 传入，通常是产品 Agent 自身
/// ——它经 MAF 的 <c>DelegatingAIAgent</c> 委托给内层 <c>ChatClientAgent</c>）。本门面只做三件事：① 键 → 会话的往返；
/// ② 载荷损坏时「按 miss 处理并重建」；③ 把「历史状态键」这类<b>产品事实</b>以参数接收，绝不硬编码（故是组合件而非基类）。
/// <para>
/// <b>下列语义由 <c>Tests/Mud.AI.Agent.Tests/Sessions/AgentSessionStoreTests.cs</c> 与
/// <c>AgentSessionStoreSelfHealingContractGuards</c> 锁定，迁移 / 重构时不得"顺手简化"</b>：<list type="number">
/// <item><b>收口式 catch</b>：catch 面必须是 <c>when (ex is not OperationCanceledException)</c>——枚举白名单会让残余异常类型
/// （FormatException / KeyNotFoundException / NullReferenceException …）逃出守护区，坏载荷每次重投递都在区外再抛
/// ⇒ 会话在其 TTL 内<b>永久毒化</b>；取消必须原样传播。</item>
/// <item><b>清库用 <see cref="CancellationToken.None"/></b>：用调用方 token 会因取消留下坏值（会话继续毒化）。</item>
/// <item><b>删除失败不得掩盖重建主路径</b>：坏值由 TTL 兜底，但失败必须可观测（告警）。</item>
/// <item><b>急切校验</b>：反序列化后必须立刻触发一次历史状态读取——状态袋的值<b>惰性</b>反序列化（只包 JsonElement、不解析），
/// 内层载荷损坏要到首次类型化读取才抛，而那次读取在本守护区<b>之外</b>（摘要器 / Provider 内部）⇒ 不触发就等于坏值永不删除。</item>
/// </list>
/// </para>
/// </remarks>
public sealed class AgentSessionStore
{
    private readonly AIAgent _codec;
    private readonly ISessionStore? _store;
    private readonly string _historyStateKey;
    private readonly ILogger? _logger;

    /// <summary>
    /// 初始化 <see cref="AgentSessionStore"/>。
    /// </summary>
    /// <param name="codec">序列化 / 反序列化的归属方（持有 <see cref="AgentSession"/> 的 Agent）。</param>
    /// <param name="store">会话持久化端口；为 <see langword="null"/> 时不持久化（仅内存会话）。</param>
    /// <param name="historyStateKey">会话历史在 Agent 状态袋内的状态键（<b>产品事实，由产品侧注入</b>）。</param>
    /// <param name="logger">日志（可空；坏值自愈的三条告警都走它）。</param>
    /// <exception cref="ArgumentNullException"><paramref name="codec"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="ArgumentException"><paramref name="historyStateKey"/> 为空。</exception>
    public AgentSessionStore(
        AIAgent codec,
        ISessionStore? store,
        string historyStateKey,
        ILogger? logger = null)
    {
        _codec = codec ?? throw new ArgumentNullException(nameof(codec));
        if (string.IsNullOrEmpty(historyStateKey))
        {
            throw new ArgumentException(
                "历史状态键不能为空——它是状态袋读写的同源锚点（本门面刻意不提供默认值："
                + "中立包不得硬编码任何产品前缀）",
                nameof(historyStateKey));
        }

        _store = store;
        _historyStateKey = historyStateKey;
        _logger = logger;
    }

    /// <summary>
    /// 按会话键读取会话；miss（不存在、TTL 过期、载荷损坏）时创建新会话。
    /// </summary>
    /// <param name="key">会话键（由产品侧的键治理构造）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>可用会话。</returns>
    /// <exception cref="ArgumentException"><paramref name="key"/> 为空。</exception>
    public async Task<AgentSession> GetOrCreateAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("会话键不能为空", nameof(key));
        }

        var stored = _store is null
            ? null
            : await _store.GetAsync(key, cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(stored))
        {
            try
            {
                using var document = JsonDocument.Parse(stored!);
                var session = await _codec
                    .DeserializeSessionAsync(document.RootElement.Clone(), jsonSerializerOptions: null, cancellationToken)
                    .ConfigureAwait(false);

                // 急切校验（见类型 remarks 第 4 条）：代价是每次会话恢复多一次历史反序列化，
                // 历史经裁剪器有界，成本可接受。
                _ = session.TryGetInMemoryChatHistory(out _, _historyStateKey, null);
                return session;
            }
            // 收口式 catch（见类型 remarks 第 1 条）：**不得**改成枚举白名单。
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger?.LogWarning(ex, "会话载荷不可用，已删除并重建（sessionKey: {SessionKey}）", key);
                if (_store is not null)
                {
                    try
                    {
                        // 用 None 而非调用方 token（见类型 remarks 第 2 条）。
                        await _store.DeleteAsync(key, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception deleteEx)
                    {
                        // 删除失败不得掩盖"重建"主路径（见类型 remarks 第 3 条）。
                        _logger?.LogWarning(deleteEx, "删除损坏会话失败（sessionKey: {SessionKey}）", key);
                    }
                }
            }
        }

        return await _codec.CreateSessionAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 序列化并保存会话（刷新存储 TTL）。
    /// </summary>
    /// <param name="key">会话键（由产品侧的键治理构造）。</param>
    /// <param name="session">要持久化的会话（须由 <see cref="_codec"/> 创建）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <exception cref="ArgumentException"><paramref name="key"/> 为空。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> 为 <see langword="null"/>。</exception>
    public async Task SaveAsync(string key, AgentSession session, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("会话键不能为空", nameof(key));
        }

        if (session is null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        if (_store is null)
        {
            return;
        }

        var serialized = await _codec
            .SerializeSessionAsync(session, jsonSerializerOptions: null, cancellationToken)
            .ConfigureAwait(false);
        await _store.SaveAsync(key, serialized.GetRawText(), cancellationToken).ConfigureAwait(false);
    }
}
