// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Mud.Feishu.WebSocket.Core;

/// <summary>
/// 指数退避重连策略
/// </summary>
public class ExponentialBackoffReconnectStrategy : IReconnectStrategy
{
    private readonly FeishuWebSocketOptions _options;
    private readonly ILogger<ExponentialBackoffReconnectStrategy>? _logger;

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
    /// 计算延迟时间：delay = baseDelay * (2^attempt)，不超过最大延迟
    /// </summary>
    /// <param name="attemptCount">当前尝试次数（从1开始）</param>
    /// <returns>延迟时间</returns>
    public TimeSpan CalculateDelay(int attemptCount)
    {
        if (attemptCount < 1)
            throw new ArgumentOutOfRangeException(nameof(attemptCount), "尝试次数必须大于0");

        var baseDelay = TimeSpan.FromMilliseconds(_options.ReconnectDelayMs);
        var exponentialDelay = TimeSpan.FromMilliseconds(
            baseDelay.TotalMilliseconds * Math.Pow(2, attemptCount - 1));
        var maxDelay = TimeSpan.FromMilliseconds(_options.MaxReconnectDelayMs);

        var delay = exponentialDelay > maxDelay ? maxDelay : exponentialDelay;

        _logger?.LogDebug("计算重连延迟: 尝试次数={Attempt}, 基础延迟={BaseDelay}ms, 指数延迟={ExponentialDelay}ms, 最终延迟={FinalDelay}ms",
            attemptCount, baseDelay.TotalMilliseconds, exponentialDelay.TotalMilliseconds, delay.TotalMilliseconds);

        return delay;
    }

    /// <summary>
    /// 判断是否继续重连：检查次数和时间限制
    /// </summary>
    /// <param name="attemptCount">当前尝试次数</param>
    /// <param name="totalElapsedTime">已消耗的总时间</param>
    /// <returns>是否应该继续重连</returns>
    public bool ShouldContinueReconnect(int attemptCount, TimeSpan totalElapsedTime)
    {
        if (attemptCount > _options.MaxReconnectAttempts)
        {
            _logger?.LogDebug("已达到最大重连次数限制: {AttemptCount}/{MaxAttempts}",
                attemptCount, _options.MaxReconnectAttempts);
            return false;
        }

        if (totalElapsedTime > _options.MaxTotalReconnectTime)
        {
            _logger?.LogDebug("已达到最大重连时间限制: {ElapsedTime}/{MaxTime}",
                totalElapsedTime, _options.MaxTotalReconnectTime);
            return false;
        }

        return true;
    }
}
