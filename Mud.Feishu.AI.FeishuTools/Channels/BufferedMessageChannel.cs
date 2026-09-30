// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Text;
using Mud.Feishu.AI.Channels;

namespace Mud.Feishu.AI.FeishuTools.Channels;

/// <summary>
/// 分片缓冲流式通道基类（AI-FD-D12 P2D-2c 抽取）：增量缓冲达
/// <see cref="FeishuAgentOptions.MaxStreamChunkLength"/> <b>且</b>距上次下游更新 ≥
/// <see cref="MinUpdateInterval"/>（常量 800ms，防飞书频率限制——时间常数为字面量，I16 纪律）
/// 才更新一次下游；<see cref="FlushAsync"/> 终态无条件落地。
/// </summary>
/// <remarks>
/// <para>
/// <b>双阈值语义</b>：分片长度为「量」阈值、最小间隔为「率」阈值——长增量场景按分片更新；
/// 短增量连发场景被间隔钳制继续缓冲（不出字粒度略粗，换频率安全）。两通道
/// （<see cref="EditMessageChannel"/>/<see cref="CardStreamMessageChannel"/>）缓冲语义同构，用例参数化覆盖。
/// </para>
/// <para>
/// <b>失败语义</b>（接口契约，子类共用）：<see cref="BeginAsync"/> 失败向上抛（调用方/降级链回退，
/// 模型尚未调用、零重复成本）；单次更新的异常经 <see cref="UpdateWithIsolationAsync"/> 隔离——
/// 占位消息停留上一次成功内容，不中断模型流。子类 <see cref="UpdateCoreAsync"/> 内的业务级失败
/// （如 outcome.Ok=false）自行记日志返回，同样不中断模型流。
/// </para>
/// </remarks>
public abstract class BufferedMessageChannel : IMessageChannel
{
    /// <summary>两次下游更新的最小间隔（毫秒；P2D-2c 速率自适应，字面量常数）。</summary>
    public const int MinUpdateIntervalMs = 800;

    /// <summary>默认最小更新间隔（测试可注入 0 恢复「仅分片阈值」语义）。</summary>
    internal static readonly TimeSpan MinUpdateInterval = TimeSpan.FromMilliseconds(MinUpdateIntervalMs);

    private readonly ConcurrentDictionary<string, StringBuilder> _buffers = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, long> _lastUpdateTicks = new(StringComparer.Ordinal);
    private readonly TimeSpan _minUpdateInterval;

    /// <summary>初始化缓冲基类。</summary>
    /// <param name="options">Agent 配置（<see cref="FeishuAgentOptions.MaxStreamChunkLength"/> 消费点）。</param>
    /// <param name="minUpdateInterval">最小更新间隔（缺省 <see cref="MinUpdateIntervalMs"/> 毫秒；测试可传 0）。</param>
    protected BufferedMessageChannel(IOptions<FeishuAgentOptions> options, TimeSpan? minUpdateInterval = null)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));
        if (options.Value.MaxStreamChunkLength < 1)
            throw new ArgumentOutOfRangeException(nameof(options), "MaxStreamChunkLength 须 ≥ 1");
        ChunkLength = options.Value.MaxStreamChunkLength;
        _minUpdateInterval = minUpdateInterval ?? MinUpdateInterval;
    }

    /// <summary>分片编辑阈值（字符）。</summary>
    protected int ChunkLength { get; }

    /// <inheritdoc />
    public abstract Task<string> BeginAsync(string appKey, string chatId, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public async Task WriteStreamAsync(string appKey, string chatId, string messageId, string delta, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(delta))
        {
            return;
        }

        var buffer = _buffers.GetOrAdd(messageId, static _ => new StringBuilder());
        bool shouldUpdate;
        lock (buffer)
        {
            buffer.Append(delta);
            shouldUpdate = buffer.Length >= ChunkLength && IntervalElapsed(messageId);
        }

        if (shouldUpdate)
        {
            await UpdateBufferedAsync(appKey, chatId, messageId, buffer, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task FlushAsync(string appKey, string chatId, string messageId, CancellationToken cancellationToken = default)
    {
        // WP4（W1 修复）：与 _buffers 同生命周期清理——_lastUpdateTicks 在 FlushAsync 后不得持有已终止的 messageId。
        _lastUpdateTicks.TryRemove(messageId, out _);
        if (_buffers.TryRemove(messageId, out var buffer) && buffer.Length > 0)
        {
            // 终态无条件落地（速率钳制不适用于收尾）。
            await UpdateWithIsolationAsync(appKey, chatId, messageId, buffer, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>距上次下游更新是否已达最小间隔（首次更新恒允许）。</summary>
    private bool IntervalElapsed(string messageId)
    {
        var now = DateTime.UtcNow.Ticks;
        if (!_lastUpdateTicks.TryGetValue(messageId, out var last))
        {
            return true;
        }

        return now - last >= _minUpdateInterval.Ticks;
    }

    /// <summary>按缓冲累计全文更新一次下游并登记时间（量阈值 + 率阈值均已通过）。</summary>
    private async Task UpdateBufferedAsync(string appKey, string chatId, string messageId, StringBuilder buffer, CancellationToken cancellationToken)
    {
        // 先登记时间再更新：失败也消耗本窗口（防失败风暴式重试）。
        _lastUpdateTicks[messageId] = DateTime.UtcNow.Ticks;
        await UpdateWithIsolationAsync(appKey, chatId, messageId, buffer, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>取出缓冲累计全文执行一次下游更新（异常隔离，子类共用）。</summary>
    private async Task UpdateWithIsolationAsync(string appKey, string chatId, string messageId, StringBuilder buffer, CancellationToken cancellationToken)
    {
        string fullText;
        lock (buffer)
        {
            fullText = buffer.ToString();
        }

        try
        {
            await UpdateCoreAsync(appKey, chatId, messageId, fullText, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await OnUpdateFailedAsync(messageId, ex).ConfigureAwait(false);
        }
    }

    /// <summary>执行一次下游「全量」更新（累计全文替换语义；业务级失败自行记日志返回）。</summary>
    protected abstract Task UpdateCoreAsync(string appKey, string chatId, string messageId, string fullText, CancellationToken cancellationToken);

    /// <summary>单次更新异常钩子（子类记日志；默认吞入日志——不中断模型流）。</summary>
    protected virtual Task OnUpdateFailedAsync(string messageId, Exception exception)
    {
        _ = messageId;
        return Task.CompletedTask;
    }
}
