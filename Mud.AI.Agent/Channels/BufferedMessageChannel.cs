// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Text;
using Mud.AI.Abstractions.Channels;
using Mud.AI.Abstractions.Observability;

namespace Mud.AI.Agent.Channels;

/// <summary>
/// 分片缓冲流式通道基类（AI-FD-D12 P2D-2c 抽取）：增量缓冲达 <b>分片长度</b> <b>且</b>
/// 距上次下游更新 ≥ <see cref="MinUpdateInterval"/>（常量 800ms，防平台频率限制——时间常数为字面量）
/// 才更新一次下游；<see cref="FlushAsync"/> 终态无条件落地。
/// </summary>
/// <remarks>
/// <para>
/// <b>中立化说明（N2-b，方案 v2 §5.3 C3）</b>：本类型自产品适配层原样迁入，产品事实按定式剥离——
/// ① 分片阈值由构造参数 <c>chunkLength</c> 注入（产品侧传自己的配置值，中立侧不留产品默认值）；
/// ② 降级可观测经 <see cref="IAgentTelemetry"/> 端口（默认 Noop），降级<b>原因文案</b>由
/// <see cref="DegradedReason"/> 虚拟属性给出（中立默认值；产品子类可覆写为自己的指标枚举值）。
/// 产品侧若需在降级指标上带租户维度（如 app_key tag），覆写 <see cref="OnUpdateDegraded"/>
/// 而不是改端口签名——维度纪律（键只进 Span 属性）是端口侧契约。
/// </para>
/// <para>
/// <b>双阈值语义</b>：分片长度为「量」阈值、最小间隔为「率」阈值——长增量场景按分片更新；
/// 短增量连发场景被间隔钳制继续缓冲（不出字粒度略粗，换频率安全）。两个产品通道
/// （编辑消息 / 卡片流）缓冲语义同构，用例参数化覆盖。
/// </para>
/// <para>
/// <b>失败语义</b>（接口契约，子类共用）：<see cref="BeginAsync"/> 失败向上抛（调用方/降级链回退，
/// 模型尚未调用、零重复成本）；单次更新的异常经 <see cref="UpdateWithIsolationAsync"/> 隔离——
/// 占位消息停留上一次成功内容，不中断模型流。子类 <see cref="UpdateCoreAsync"/> 内的业务级失败
/// （如 outcome.Ok=false）自行记日志返回，同样不中断模型流。
/// </para>
/// <para>
/// <b>状态生命周期契约（R2-02 / 根因 R-B）</b>：通道注册为 <c>Singleton</c>（进程寿命），故
/// <b>子类新增的任何 per-messageId 状态（字典/集合）必须在 <see cref="OnFlushed"/> 中移除</b>——
/// 基类在 <c>FlushAsync</c> 的终态落地之后无条件调用该钩子。未覆写即泄漏，且由
/// <c>ChannelStateLifecycleContractGuards</c> 反射断言（Begin→Write→Flush 后全部
/// <c>ConcurrentDictionary&lt;string, *&gt;</c> 字段计数归零）兜住。
/// </para>
/// </remarks>
public abstract class BufferedMessageChannel : IMessageChannel
{
    /// <summary>两次下游更新的最小间隔（毫秒；P2D-2c 速率自适应，字面量常数）。</summary>
    public const int MinUpdateIntervalMs = 800;

    /// <summary>默认最小更新间隔（测试可注入 0 恢复「仅分片阈值」语义）。</summary>
    public static readonly TimeSpan MinUpdateInterval = TimeSpan.FromMilliseconds(MinUpdateIntervalMs);

    private readonly ConcurrentDictionary<string, StringBuilder> _buffers = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, long> _lastUpdateTicks = new(StringComparer.Ordinal);
    private readonly int _chunkLength;
    private readonly TimeSpan _minUpdateInterval;
    private readonly IAgentTelemetry _telemetry;
    private readonly Func<DateTime> _clock;

    /// <summary>
    /// 初始化缓冲基类。
    /// </summary>
    /// <param name="chunkLength">分片编辑阈值（字符；① 式参数注入，必须 ≥ 1）。</param>
    /// <param name="telemetry">遥测端口（可空 ⇒ Noop；② 式，降级可观测的注入点）。</param>
    /// <param name="minUpdateInterval">最小更新间隔（缺省 <see cref="MinUpdateIntervalMs"/> 毫秒；测试可传 0）。</param>
    /// <param name="clock">UTC 时钟（可空；测试注入确定性时钟，缺省 <see cref="DateTime.UtcNow"/>）。</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="chunkLength"/> 小于 1。</exception>
    protected BufferedMessageChannel(
        int chunkLength,
        IAgentTelemetry? telemetry = null,
        TimeSpan? minUpdateInterval = null,
        Func<DateTime>? clock = null)
    {
        if (chunkLength < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkLength), chunkLength, "分片编辑阈值（chunkLength）须 ≥ 1");
        }

        _chunkLength = chunkLength;
        _minUpdateInterval = minUpdateInterval ?? MinUpdateInterval;
        _telemetry = telemetry ?? NoopAgentTelemetry.Instance;
        _clock = clock ?? (static () => DateTime.UtcNow);
    }

    /// <summary>分片编辑阈值（字符）。</summary>
    protected int ChunkLength => _chunkLength;

    /// <summary>
    /// 降级原因文案（② 式：中立默认值；产品子类覆写为自己的受控枚举值，
    /// 产品指标名因此留在产品侧——中立包不持有产品词汇）。
    /// </summary>
    protected virtual string DegradedReason => "channel_update_failed";

    /// <inheritdoc />
    public abstract Task<string> BeginAsync(string tenantKey, string chatId, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    /// <remarks>
    /// <b>并发契约（R2-11）</b>：同一 <paramref name="messageId"/> 的写入与 <see cref="FlushAsync"/> 必须由调用方
    /// <b>顺序</b> <c>await</c>（事件处理器即如此），并发调用不在本接口契约内——<see cref="IMessageChannel"/> 已声明同款约束。
    /// </remarks>
    public async Task WriteStreamAsync(string tenantKey, string chatId, string messageId, string delta, CancellationToken cancellationToken = default)
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

            // R2-11：率阈值判定与时间戳登记必须在<b>同一把锁</b>内完成——此前登记在锁外的
            // UpdateBufferedAsync 中，两次并发写可同时看到"间隔已过"而各自触发一次下游更新。
            // 先登记再更新（失败也消耗本窗口，防失败风暴式重试）的语义保持不变。
            var now = _clock().Ticks;
            shouldUpdate = buffer.Length >= _chunkLength && IntervalElapsed(messageId, now);
            if (shouldUpdate)
            {
                _lastUpdateTicks[messageId] = now;
            }
        }

        if (shouldUpdate)
        {
            await UpdateWithIsolationAsync(tenantKey, chatId, messageId, buffer, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task FlushAsync(string tenantKey, string chatId, string messageId, CancellationToken cancellationToken = default)
    {
        // WP4（W1 修复）：与 _buffers 同生命周期清理——_lastUpdateTicks 在 FlushAsync 后不得持有已终止的 messageId。
        _lastUpdateTicks.TryRemove(messageId, out _);
        try
        {
            if (_buffers.TryRemove(messageId, out var buffer) && buffer.Length > 0)
            {
                // 终态无条件落地（速率钳制不适用于收尾）。
                await UpdateWithIsolationAsync(tenantKey, chatId, messageId, buffer, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            // R2-02（根因 R-B）：子类登记的 per-messageId 状态与基类状态<b>同生命周期</b>——
            // 由基类统一触发清理，子类不可能漏（清理职责从"谁定义"上移为"谁新增谁覆写钩子"）。
            // 位置必须在**终态落地之后**：终态更新本身仍需要子类状态
            // （如卡片流通道要用 Begin 期登记的投放目标 open_id），提前清理会让终态更新静默跳过。
            OnFlushed(messageId);
        }
    }

    /// <summary>
    /// 状态清理钩子（R2-02）：<b>子类新增的任何 per-messageId 状态必须在此移除</b>。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 本钩子是"长生命周期对象的状态生命周期"契约点：通道实例注册为 <c>Singleton</c>（与进程同寿命），
    /// 若子类在 <see cref="BeginAsync"/> 中登记的字典键（<c>biz_id</c> / <c>messageId</c> 等）单调新增且永不重复，
    /// 而清理又只做在基类自己的字典上，则泄漏是<b>确定性</b>的（不是"疏忽"而是"缺少强制机制"）。
    /// </para>
    /// <para>
    /// 为什么不是"补两行 <c>TryRemove</c>"：把清理从<b>私有实现细节</b>提升为<b>可覆写的契约点</b>后，
    /// "新增一个字典字段"在写代码时就必须在"覆写本钩子 / 明确不覆写"之间做出选择；
    /// 并被元守卫 <c>ChannelStateLifecycleContractGuards</c>（反射枚举全部 <see cref="IMessageChannel"/>
    /// 实现的 <c>ConcurrentDictionary&lt;string, *&gt;</c> 字段，断言 Begin→Write→Flush 后计数归零）机械锁定。
    /// </para>
    /// <para>
    /// 因此<b>未覆写本钩子的新增状态字段会被守卫直接报红</b>，而新增通道实现亦须在守卫中登记构造工厂
    /// （守卫对"未登记"同样报红），覆盖面不依赖评审者是否想到。
    /// </para>
    /// </remarks>
    /// <param name="messageId">已终结的占位消息 ID。</param>
    protected virtual void OnFlushed(string messageId)
    {
        _ = messageId;
    }

    /// <summary>距上次下游更新是否已达最小间隔（首次更新恒允许）。</summary>
    private bool IntervalElapsed(string messageId, long now)
    {
        if (!_lastUpdateTicks.TryGetValue(messageId, out var last))
        {
            return true;
        }

        return now - last >= _minUpdateInterval.Ticks;
    }

    /// <summary>取出缓冲累计全文执行一次下游更新（异常隔离，子类共用）。</summary>
    private async Task UpdateWithIsolationAsync(string tenantKey, string chatId, string messageId, StringBuilder buffer, CancellationToken cancellationToken)
    {
        string fullText;
        lock (buffer)
        {
            fullText = buffer.ToString();
        }

        try
        {
            await UpdateCoreAsync(tenantKey, chatId, messageId, fullText, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // 有意静默（守卫白名单）：异常**不是**被吞掉，而是交给子类钩子 OnUpdateFailedAsync 记录
            // （子类各自记得更准的上下文：卡片流记 bizId、编辑通道记 messageId）。基类不重复记一遍。
            // R3-12：降级路径指标化——通道更新失败补计数使可告警。
            OnUpdateDegraded(tenantKey, messageId, ex);
            await OnUpdateFailedAsync(messageId, ex).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 单次更新失败的降级记录钩子（R3-12）：默认经 <see cref="IAgentTelemetry"/> 端口记一次
    /// （<see cref="DegradedReason"/> 文案）；产品子类覆写以补产品维度（如 app_key tag）——
    /// 迁移期产品指标因此逐字节不变（方案 v2 §5.3 C3 ②式）。
    /// </summary>
    /// <param name="tenantKey">租户/应用标识。</param>
    /// <param name="messageId">占位消息 ID。</param>
    /// <param name="exception">被隔离的异常。</param>
    protected virtual void OnUpdateDegraded(string tenantKey, string messageId, Exception exception)
    {
        _ = tenantKey;
        _ = messageId;
        _ = exception;
        _telemetry.Degraded(DegradedReason);
    }

    /// <summary>执行一次下游「全量」更新（累计全文替换语义；业务级失败自行记日志返回）。</summary>
    protected abstract Task UpdateCoreAsync(string tenantKey, string chatId, string messageId, string fullText, CancellationToken cancellationToken);

    /// <summary>单次更新异常钩子（子类记日志；默认吞入日志——不中断模型流）。</summary>
    protected virtual Task OnUpdateFailedAsync(string messageId, Exception exception)
    {
        _ = messageId;
        return Task.CompletedTask;
    }
}
