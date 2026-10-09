// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Agents;

namespace Mud.Feishu.AI.Tools.Tools;

/// <summary>
/// 工具重试策略（B3 限流退避）：对只读工具的 retryable 错误自动重试，写工具默认零重试。
/// </summary>
/// <remarks>
/// <para>
/// <b>安全优先</b>语义：
/// <list type="bullet">
/// <item>只读工具：遇到 category=retryable（429/5xx/超时）时自动重试；</item>
/// <item>写工具：默认零重试（防重复副作用）；仅当 AllowWriteRetry=true 且幂等键存在时才重试；</item>
/// <item>dry_run=true：不下发请求，无重试意义；</item>
/// <item>授权/策略拒绝：属确定性失败，重试无意义。</item>
/// </list>
/// </para>
/// <para>
/// <b>退避策略</b>：<c>delay = min(MaxDelay, BaseDelay * 2^attempt) * jitter(0.5~1.5)</c>；
/// 若 B2 拿到 <c>retry_after_seconds</c> 则优先采用（取 max(delay, retry_after)）。
/// </para>
/// <para>
/// <b>禁止墙钟上界断言</b>：并发负载下必然抖动。重试延迟只允许用假时钟或调用次数断言。
/// </para>
/// </remarks>
internal sealed class ToolRetryPolicy
{
    private readonly ToolRetryOptions _options;
    private readonly Func<long> _tickCountProvider;

    /// <summary>
    /// 初始化重试策略。
    /// </summary>
    /// <param name="options">重试配置（从 <see cref="FeishuAgentOptions.ToolRetry"/> 传入）。</param>
    /// <param name="tickCountProvider">毫秒级时钟（可注入假时钟用于测试；默认 <see cref="Environment.TickCount64"/>）。</param>
    public ToolRetryPolicy(ToolRetryOptions options, Func<long>? tickCountProvider = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _tickCountProvider = tickCountProvider ?? DefaultTickCountProvider;
    }

    private static long DefaultTickCountProvider() => DateTimeOffset.UtcNow.Ticks;

    /// <summary>
    /// 是否已启用重试。
    /// </summary>
    public bool IsEnabled => _options.Enabled && _options.MaxAttempts > 0;

    /// <summary>
    /// 判定是否应该重试。
    /// </summary>
    /// <param name="error">上次执行的错误载荷（null = 成功）。</param>
    /// <param name="isWrite">是否写工具。</param>
    /// <param name="isDryRun">是否 dry_run 模式。</param>
    /// <param name="hasIdempotencyKey">调用方是否传了幂等键（仅写工具重试时检查）。</param>
    /// <param name="attempt">当前已尝试次数（0 起；0 = 首次调用）。</param>
    /// <returns>true = 应该重试；false = 不重试。</returns>
    public bool ShouldRetry(
        ToolError? error,
        bool isWrite,
        bool isDryRun,
        bool hasIdempotencyKey,
        int attempt)
    {
        // 成功不重试
        if (error is null)
        {
            return false;
        }

        // 未启用重试
        if (!IsEnabled)
        {
            return false;
        }

        // dry_run 不重试
        if (isDryRun)
        {
            return false;
        }

        // 非可重试错误不重试
        if (!error.Retryable)
        {
            return false;
        }

        // 已耗尽重试次数
        if (attempt >= _options.MaxAttempts)
        {
            return false;
        }

        // 写工具重试条件：AllowWriteRetry + 幂等键
        if (isWrite)
        {
            if (!_options.AllowWriteRetry)
            {
                return false;
            }

            if (!hasIdempotencyKey)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 计算下一次重试的延迟（毫秒）。
    /// </summary>
    /// <param name="attempt">当前已尝试次数（0 起）。</param>
    /// <param name="retryAfterSeconds">来自错误载荷的 Retry-After 值（null = 不提供）。</param>
    /// <returns>延迟毫秒数。</returns>
    public int ComputeDelay(int attempt, int? retryAfterSeconds = null)
    {
        // 指数退避：BaseDelay * 2^attempt
        var baseDelay = (double)_options.BaseDelayMilliseconds;
        var exponential = baseDelay * Math.Pow(2, attempt);

        // 钳制到 MaxDelay
        var delay = Math.Min(exponential, _options.MaxDelayMilliseconds);

        // 抖动：0.5~1.5 倍
        if (_options.Jitter)
        {
            var tick = _tickCountProvider();
            var jitterFactor = 0.5 + ((tick % 1000) / 1000.0);  // 0.5 ~ 1.5
            delay *= jitterFactor;
        }

        // 若有 Retry-After 值，取 max(delay, retry_after)
        // 注意：Retry-After 是服务端权威指示，不受 MaxDelay 钳制
        if (retryAfterSeconds.HasValue)
        {
            var retryAfterMs = retryAfterSeconds.Value * 1000.0;
            delay = Math.Max(delay, retryAfterMs);
            return (int)Math.Max(delay, 0);
        }

        // 无 Retry-After 时钳制到 [0, MaxDelay]
        return (int)Math.Min(Math.Max(delay, 0), _options.MaxDelayMilliseconds);
    }

    /// <summary>
    /// 异步延迟（支持取消）。
    /// </summary>
    /// <param name="delayMs">延迟毫秒数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public Task DelayAsync(int delayMs, CancellationToken cancellationToken)
    {
        if (delayMs <= 0)
        {
            return Task.CompletedTask;
        }

        return Task.Delay(delayMs, cancellationToken);
    }
}
