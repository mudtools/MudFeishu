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
    /// <summary>
    /// 退避指数上限（P2-13 修复）。
    /// </summary>
    /// <remarks>2^30 × baseDelayMs 已足够大（默认 base 1000ms 时约 34 年），钳制只消除 double 溢出，不改变退避语义。</remarks>
    private const int MaxExponent = 30;

#if NET6_0_OR_GREATER
    private static Random JitterRandom => Random.Shared;
#else
    // netstandard2.0 无 Random.Shared；Random 实例非线程安全，
    // 共享静态实例在并发调用下会损坏内部状态并持续返回 0（P2-5）。
    [ThreadStatic]
    private static Random? _jitterRandom;
    private static Random JitterRandom => _jitterRandom ??= new Random(Guid.NewGuid().GetHashCode());
#endif

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
        // 负的重试次数无意义，钳制为 0（至少执行一次操作）
        if (maxRetries < 0)
            maxRetries = 0;

        for (int i = 0; i <= maxRetries; i++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (i < maxRetries)
            {
                // 添加随机抖动，避免多个客户端同时重试造成雪崩
                // P2-13 修复：指数必须钳制。此前 Math.Pow(2, i) 在 i > 1024 时得到 double.PositiveInfinity，
                // TimeSpan.FromMilliseconds(∞) 抛 OverflowException；而且该异常发生在 catch 块内，
                // 会以"重试退避溢出"的形式向上抛出，掩盖真实的失败原因。
                var clampedExponent = Math.Min(i, MaxExponent);
                var baseDelay = Math.Pow(2, clampedExponent) * baseDelayMs;
                var jitter = JitterRandom.NextDouble() * baseDelayMs; // 0~baseDelayMs 的随机抖动
                var delay = TimeSpan.FromMilliseconds(baseDelay + jitter);

                logger.LogWarning(ex, "{OperationName}失败，将在{Delay}ms后重试 (尝试 {RetryCount}/{MaxRetries}, 抖动 {Jitter}ms)",
                    operationName, delay.TotalMilliseconds, i + 1, maxRetries + 1, jitter);

                await Task.Delay(delay, cancellationToken);
            }
        }

        // 说明（P2-13）：for 循环最后一次迭代（i == maxRetries）失败时，catch 过滤器 when (i < maxRetries)
        // 不成立，异常会直接抛出，因此本分支在运行期<b>不可达</b>。但 C# 编译器无法证明"循环必然返回或抛出"，
        // 删除此处会触发 CS0161（并非所有代码路径都返回值），故保留为"编译期必需的兜底"，
        // 仅把此前"已移除/纯死代码"的误导性注释更正为事实描述。
        logger.LogError("{OperationName}失败，已达到最大重试次数 {MaxRetries}", operationName, maxRetries + 1);
        throw new InvalidOperationException($"{operationName}失败，已达到最大重试次数 {maxRetries + 1}");
    }
}
