// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Agents;

/// <summary>
/// 工具重试策略配置（B3 限流退避）。
/// </summary>
/// <remarks>
/// <para>
/// 只读工具遇到 <c>retryable</c> 错误（429/5xx/超时）时自动重试；写工具默认零重试
/// （仅在 <see cref="AllowWriteRetry"/> = true 且工具暴露 idempotency_key 且调用方传了键时才重试）。
/// </para>
/// <para>
/// 退避策略：<c>delay = min(MaxDelay, BaseDelay * 2^attempt) * jitter(0.5~1.5)</c>；
/// 若 B2 拿到 <c>retry_after_seconds</c> 则优先采用（取 max(delay, retry_after)）。
/// </para>
/// </remarks>
public sealed class ToolRetryOptions
{
    /// <summary>
    /// 是否启用自动重试（消费点：<c>ToolRetryPolicy.ShouldRetry</c>）。
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 最大重试次数（不含首次调用；消费点：<c>ToolRetryPolicy.ShouldRetry</c>）。
    /// </summary>
    /// <remarks>默认 3，意味着最多调用 4 次（1 次原始 + 3 次重试）。</remarks>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// 退避基准延迟（毫秒；消费点：<c>ToolRetryPolicy.ComputeDelay</c>）。
    /// </summary>
    public int BaseDelayMilliseconds { get; set; } = 200;

    /// <summary>
    /// 退避最大延迟（毫秒；消费点：<c>ToolRetryPolicy.ComputeDelay</c>）。
    /// </summary>
    public int MaxDelayMilliseconds { get; set; } = 5000;

    /// <summary>
    /// 是否启用抖动（消费点：<c>ToolRetryPolicy.ComputeDelay</c>）。
    /// </summary>
    /// <remarks>抖动因子 0.5~1.5，防止重试风暴。</remarks>
    public bool Jitter { get; set; } = true;

    /// <summary>
    /// 是否允许写工具重试（消费点：<c>ToolRetryPolicy.ShouldRetry</c>）。
    /// </summary>
    /// <remarks>
    /// 默认 false（安全优先）。仅当 true 且工具暴露 idempotency_key 且调用方传了键时才重试写工具。
    /// </remarks>
    public bool AllowWriteRetry { get; set; } = false;
}
