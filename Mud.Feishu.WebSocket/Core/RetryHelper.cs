// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 重试帮助类
/// </summary>
public static class RetryHelper
{
    private static readonly Random JitterRandom = new();

    /// <summary>
    /// 重试执行异步操作，使用指数退避策略和随机抖动。
    /// </summary>
    public static async Task<T> RetryWithExponentialBackoffAsync<T>(
        ILogger logger,
        Func<Task<T>> operation,
        int maxRetries,
        int baseDelayMs,
        string operationName,
        CancellationToken cancellationToken)
    {
        for (int i = 0; i <= maxRetries; i++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (i < maxRetries)
            {
                // 添加随机抖动，避免多个客户端同时重试造成雪崩
                var baseDelay = Math.Pow(2, i) * baseDelayMs;
                var jitter = JitterRandom.NextDouble() * baseDelayMs; // 0~baseDelayMs 的随机抖动
                var delay = TimeSpan.FromMilliseconds(baseDelay + jitter);

                logger.LogWarning(ex, "{OperationName}失败，将在{Delay}ms后重试 (尝试 {RetryCount}/{MaxRetries}, 抖动 {Jitter}ms)",
                    operationName, delay.TotalMilliseconds, i + 1, maxRetries + 1, jitter);

                await Task.Delay(delay, cancellationToken);
            }
        }

        logger.LogError("{OperationName}失败，已达到最大重试次数 {MaxRetries}", operationName, maxRetries + 1);
        return await operation();
    }
}
