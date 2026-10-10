// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;
using Microsoft.Extensions.Options;
using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Channels;

namespace Mud.Feishu.Agent.Demo;

/// <summary>
/// 控制台流式回复通道：<see cref="IMessageChannel"/> 的控制台实现（本 Demo 的<b>唯一</b>通道实现）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么不继承 <c>Mud.Feishu.AI.Tools.Channels.BufferedMessageChannel</c></b>（ADR-05）：
/// 该基类的节流双阈值（<c>ChunkLength = MaxStreamChunkLength</c> 默认 200 字符 +
/// <c>MinUpdateIntervalMs = 800</c>）是<b>为飞书「编辑消息」API 限频</b>设计的（避免 429）。
/// 控制台没有 429 风险，套用 800ms 节流会让终端「一顿一顿」；且继承会连带依赖
/// <c>EditMessageChannel</c> 的语义（<c>OnFlushed</c> 清理时序、<c>PlaceholderText = "…"</c> 占位）。
/// </para>
/// <para>
/// <b>但配置项不另起炉灶</b>：分片阈值仍从 <see cref="IOptions{TOptions}"/> 的
/// <c>FeishuAgentOptions.MaxStreamChunkLength</c> 读取（SDK 既有配置项的同一消费点），
/// 只有"率"阈值从 800ms 放宽到 <see cref="MinUpdateIntervalMs"/>（60ms ≈ 16fps）。
/// </para>
/// <para>
/// <b>契约符合性</b>（<see cref="IMessageChannel"/> 三条硬要求）：
/// ① <see cref="BeginAsync"/> 失败向上抛（本实现不调任何外部 API，仅分配本地 ID）；
/// ② <see cref="WriteStreamAsync"/> 自带失败隔离、不抛业务异常；
/// ③ 同一 <c>messageId</c> 上顺序 await（由 <c>AgentConsoleLoop</c> 的单线程 <c>await foreach</c> 保证）；
/// ④ per-<c>messageId</c> 状态在 <see cref="FlushAsync"/> 内清理（Singleton 无界字典增长防线）。
/// </para>
/// </remarks>
internal sealed class ConsoleMessageChannel : IMessageChannel
{
    /// <summary>"率"节流窗口（毫秒）。控制台无 429 风险，故远小于 <c>BufferedMessageChannel</c> 的 800ms。</summary>
    public const int MinUpdateIntervalMs = 60;

    private readonly ConsoleRenderer _renderer;
    private readonly int _chunkLength;
    private readonly Dictionary<string, MessageState> _states = new(StringComparer.Ordinal);
    private readonly object _sync = new();
    private int _seq;

    /// <summary>
    /// 初始化控制台通道。
    /// </summary>
    /// <param name="renderer">终端渲染器。</param>
    /// <param name="options">Agent 选项（分片阈值单一来源：<c>MaxStreamChunkLength</c>）。</param>
    public ConsoleMessageChannel(ConsoleRenderer renderer, IOptions<FeishuAgentOptions> options)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _chunkLength = Math.Max(1, options?.Value.MaxStreamChunkLength ?? 200);
    }

    /// <summary>当前分片阈值（<c>/help</c> 展示用）。</summary>
    public int ChunkLength => _chunkLength;

    /// <inheritdoc />
    public Task<string> BeginAsync(string appKey, string chatId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string messageId;
        lock (_sync)
        {
            messageId = $"console-{Interlocked.Increment(ref _seq):D4}";
            _states[messageId] = new MessageState();
        }

        _renderer.AgentPrefix();
        return Task.FromResult(messageId);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>失败隔离</b>：整体 try/catch，异常只以 <see cref="NoticeLevel.Warn"/> 呈现，绝不中断模型流。
    /// 锁内不含 <c>await</c>（仓库异步规范）。
    /// </remarks>
    public Task WriteStreamAsync(
        string appKey,
        string chatId,
        string messageId,
        string delta,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(delta))
            {
                return Task.CompletedTask;
            }

            MessageState? state;
            lock (_sync)
            {
                _states.TryGetValue(messageId, out state);
            }

            if (state is null)
            {
                // Begin 未调用（或已被 Flush 清理）：忽略增量而不抛错（契约：不向上抛业务异常）。
                return Task.CompletedTask;
            }

            string? pending = null;
            lock (state.Sync)
            {
                state.Buffer.Append(delta);

                var now = Environment.TickCount64;
                // 「量」与「率」双阈值：达到分片长度，或距上次落地已超过节流窗口。
                if (delta.Length >= _chunkLength || now - state.LastUpdateTicks >= MinUpdateIntervalMs)
                {
                    state.LastUpdateTicks = now;
                    pending = state.Buffer.ToString(state.Written, state.Buffer.Length - state.Written);
                    state.Written = state.Buffer.Length;
                }
            }

            if (pending is not null)
            {
                _renderer.AgentDelta(pending);
            }
        }
        catch (Exception ex)
        {
            // 输出失败（管道被关闭等）：停留上一次成功内容，不中断模型流（降级矩阵 §10.2）。
            _renderer.Notice(NoticeLevel.Warn, $"输出失败（停留上一次成功内容）：{ex.GetType().Name}");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>终态**无条件**落地（速率钳制不适用于收尾），随后清理 per-<c>messageId</c> 状态。</remarks>
    public Task FlushAsync(
        string appKey,
        string chatId,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            MessageState? state;
            lock (_sync)
            {
                if (_states.TryGetValue(messageId, out state))
                {
                    _states.Remove(messageId);
                }
            }

            if (state is null)
            {
                return Task.CompletedTask;
            }

            string? pending = null;
            lock (state.Sync)
            {
                pending = state.Buffer.ToString(state.Written, state.Buffer.Length - state.Written);
                state.Written = state.Buffer.Length;
            }

            if (!string.IsNullOrEmpty(pending))
            {
                _renderer.AgentDelta(pending);
            }
        }
        catch (Exception ex)
        {
            _renderer.Notice(NoticeLevel.Warn, $"收尾失败（停留上一次成功内容）：{ex.GetType().Name}");
        }

        return Task.CompletedTask;
    }

    /// <summary>当前在途消息数（诊断用；Flush 后应回落到 0）。</summary>
    public int ActiveMessages
    {
        get
        {
            lock (_sync)
            {
                return _states.Count;
            }
        }
    }

    private sealed class MessageState
    {
        internal StringBuilder Buffer { get; } = new();

        /// <summary>已落地到终端的字符数（只写新增部分，避免重复渲染）。</summary>
        internal int Written { get; set; }

        internal long LastUpdateTicks { get; set; }

        internal object Sync { get; } = new();
    }
}
