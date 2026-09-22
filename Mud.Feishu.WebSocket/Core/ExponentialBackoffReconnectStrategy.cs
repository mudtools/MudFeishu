// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 指数退避重连策略
/// </summary>
public class ExponentialBackoffReconnectStrategy : IReconnectStrategy
{
    private readonly FeishuWebSocketOptions _options;
    private readonly ILogger<ExponentialBackoffReconnectStrategy>? _logger;

    /// <summary>
    /// 指数上限（P2-6 修复）。
    /// </summary>
    /// <remarks>
    /// 2^30 × <see cref="FeishuWebSocketOptions.Reconnect"/>.<see cref="WebSocketReconnectOptions.BaseDelayMs"/>（默认 1000ms）≈ 34 年，
    /// 远超任何 <see cref="WebSocketReconnectOptions.MaxDelayMs"/>，
    /// 钳制后必然被最大延迟截断，故不影响退避语义，只消除 double 溢出。
    /// </remarks>
    private const int MaxExponent = 30;
    // WS-14 修复（P1-11）：static Random 非线程安全，并发调用会损坏内部状态并持续返回 0。
    // 照抄 RetryHelper 的条件编译模式：net6+ 使用 Random.Shared，ns2.0 使用 [ThreadStatic]。
#if NET6_0_OR_GREATER
    private static Random JitterRandom => Random.Shared;
#else
    [ThreadStatic]
    private static Random? _jitterRandom;
    private static Random JitterRandom => _jitterRandom ??= new Random(Guid.NewGuid().GetHashCode());
#endif

    /// <summary>
    /// 初始化指数退避重连策略
    /// </summary>
    /// <param name="options">WebSocket配置选项</param>
    /// <param name="logger">日志记录器（可选）</param>
    public ExponentialBackoffReconnectStrategy(
        FeishuWebSocketOptions options,
        ILogger<ExponentialBackoffReconnectStrategy>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    /// <summary>
    /// 计算延迟时间：delay = min(baseDelay * (2^attempt), maxDelay) + jitter（抖动为 delay 的 0~25%）
    /// </summary>
    /// <param name="attemptCount">当前尝试次数（从 1 开始）</param>
    /// <returns>延迟时间</returns>
    /// <remarks>
    /// <b>顺序约束（WS2-04 ④）</b>：必须"**先在 <c>double</c> 域钳制到 <c>MaxDelayMs</c>，再构造 <see cref="TimeSpan"/>**"。
    /// 此前实现先 <c>TimeSpan.FromMilliseconds(指数放大后的毫秒)</c> 再比较大小——当
    /// <c>BaseDelayMs × 2^attempt</c> 超过 <see cref="TimeSpan.MaxValue"/> 的毫秒数（约 9.22e14）时，
    /// <see cref="TimeSpan.FromMilliseconds(double)"/> 直接抛 <see cref="OverflowException"/>，
    /// 异常穿透整轮重连（依赖 <c>ReconnectOrchestrator</c> 的 catch 才不至于崩溃）。
    /// 现在溢出点在钳制之后，数学上不可能到达。
    /// <para>
    /// 另有 P2-6 修复：指数本身也必须钳制——<c>Math.Pow(2, attemptCount - 1)</c> 在
    /// <c>attemptCount &gt; 1024</c> 时得到 <c>double.PositiveInfinity</c>
    /// （触发条件：<c>MaxAttempts = 0</c> 无限重连 + 长时间断网）。
    /// </para>
    /// </remarks>
    public TimeSpan CalculateDelay(int attemptCount)
    {
        if (attemptCount < 1)
            throw new ArgumentOutOfRangeException(nameof(attemptCount), "尝试次数必须大于0");

        var baseDelayMs = (double)_options.Reconnect.BaseDelayMs;
        var maxDelayMs = (double)_options.Reconnect.MaxDelayMs;

        // P2-6：指数钳制（避免 double.PositiveInfinity）
        var clampedExponent = Math.Min(attemptCount - 1, MaxExponent);
        var exponentialDelayMs = baseDelayMs * Math.Pow(2, clampedExponent);

        // WS2-04 ④：在 double 域完成钳制，再构造 TimeSpan —— 消除"溢出先于钳制"的顺序缺陷
        var clampedDelayMs = exponentialDelayMs < 0 || exponentialDelayMs > maxDelayMs
            ? maxDelayMs
            : exponentialDelayMs;

        // 添加随机抖动（0~25% 的延迟），避免多个客户端同时重连造成雪崩
        var jitterMs = JitterRandom.NextDouble() * clampedDelayMs * 0.25;
        var finalDelayMs = clampedDelayMs + jitterMs;

        var delay = TimeSpan.FromMilliseconds(finalDelayMs);

        _logger?.LogDebug("计算重连延迟: 尝试次数={Attempt}, 基础延迟={BaseDelay}ms, 指数延迟={ExponentialDelay}ms, 抖动={Jitter}ms, 最终延迟={FinalDelay}ms",
            attemptCount, baseDelayMs, exponentialDelayMs, jitterMs, finalDelayMs);

        return delay;
    }

    /// <summary>
    /// 判断是否继续重连：检查次数和时间限制。
    /// 当 MaxReconnectAttempts = 0 时表示无限重连，仅受 MaxTotalReconnectTime 限制。
    /// </summary>
    /// <param name="attemptCount">当前尝试次数</param>
    /// <param name="totalElapsedTime">已消耗的总时间</param>
    /// <returns>是否应该继续重连</returns>
    public bool ShouldContinueReconnect(int attemptCount, TimeSpan totalElapsedTime)
    {
        // MaxReconnectAttempts = 0 表示无限重连（仅受时间限制）
        if (_options.Reconnect.MaxAttempts > 0 && attemptCount > _options.Reconnect.MaxAttempts)
        {
            _logger?.LogDebug("已达到最大重连次数限制: {AttemptCount}/{MaxAttempts}",
                attemptCount, _options.Reconnect.MaxAttempts);
            return false;
        }

        if (totalElapsedTime > _options.Reconnect.TotalBudget)
        {
            _logger?.LogDebug("已达到最大重连时间限制: {ElapsedTime}/{MaxTime}",
                totalElapsedTime, _options.Reconnect.TotalBudget);
            return false;
        }

        return true;
    }
}
