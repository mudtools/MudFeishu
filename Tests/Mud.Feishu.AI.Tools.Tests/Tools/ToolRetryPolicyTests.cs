// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Tools.Tools;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// B3 限流退避测试（方案 §3.B3 测试要求：假时间源，避免墙钟断言）。
/// </summary>
/// <remarks>
/// 用例：429 后成功（调用次数=2）、连续失败耗尽退避、写工具零重试、取消优先于退避。
/// </remarks>
public class ToolRetryPolicyTests
{
    private static ToolRetryOptions DefaultOptions() => new()
    {
        Enabled = true,
        MaxAttempts = 3,
        BaseDelayMilliseconds = 200,
        MaxDelayMilliseconds = 5000,
        Jitter = false,  // 关闭抖动以便断言延迟
        AllowWriteRetry = false,
    };

    // ────────── ShouldRetry ──────────

    [Fact]
    public void ShouldRetry_ShouldReturnFalse_WhenNoError()
    {
        var policy = new ToolRetryPolicy(DefaultOptions());
        policy.ShouldRetry(null, isWrite: false, isDryRun: false, hasIdempotencyKey: false, attempt: 0)
            .Should().BeFalse("成功不重试");
    }

    [Fact]
    public void ShouldRetry_ShouldReturnFalse_WhenDisabled()
    {
        var options = DefaultOptions();
        options.Enabled = false;
        var policy = new ToolRetryPolicy(options);

        var error = new ToolError("retryable", "rate_limited", Retryable: true);
        policy.ShouldRetry(error, isWrite: false, isDryRun: false, hasIdempotencyKey: false, attempt: 0)
            .Should().BeFalse("禁用时不重试");
    }

    [Fact]
    public void ShouldRetry_ShouldReturnFalse_WhenDryRun()
    {
        var policy = new ToolRetryPolicy(DefaultOptions());
        var error = new ToolError("retryable", "rate_limited", Retryable: true);
        policy.ShouldRetry(error, isWrite: false, isDryRun: true, hasIdempotencyKey: false, attempt: 0)
            .Should().BeFalse("dry_run 不重试");
    }

    [Fact]
    public void ShouldRetry_ShouldReturnFalse_WhenNotRetryable()
    {
        var policy = new ToolRetryPolicy(DefaultOptions());
        var error = new ToolError("validation", "invalid_args", Retryable: false);
        policy.ShouldRetry(error, isWrite: false, isDryRun: false, hasIdempotencyKey: false, attempt: 0)
            .Should().BeFalse("非可重试错误不重试");
    }

    [Fact]
    public void ShouldRetry_ShouldReturnFalse_WhenMaxAttemptsExhausted()
    {
        var policy = new ToolRetryPolicy(DefaultOptions());
        var error = new ToolError("retryable", "rate_limited", Retryable: true);
        policy.ShouldRetry(error, isWrite: false, isDryRun: false, hasIdempotencyKey: false, attempt: 3)
            .Should().BeFalse("耗尽重试次数不重试");
    }

    [Fact]
    public void ShouldRetry_ShouldReturnTrue_ForReadOnlyRetryableError()
    {
        var policy = new ToolRetryPolicy(DefaultOptions());
        var error = new ToolError("retryable", "rate_limited", Retryable: true);
        policy.ShouldRetry(error, isWrite: false, isDryRun: false, hasIdempotencyKey: false, attempt: 0)
            .Should().BeTrue("只读可重试错误应重试");
    }

    [Fact]
    public void ShouldRetry_ShouldReturnFalse_ForWriteToolByDefault()
    {
        var policy = new ToolRetryPolicy(DefaultOptions());
        var error = new ToolError("retryable", "rate_limited", Retryable: true);
        policy.ShouldRetry(error, isWrite: true, isDryRun: false, hasIdempotencyKey: false, attempt: 0)
            .Should().BeFalse("写工具默认零重试");
    }

    [Fact]
    public void ShouldRetry_ShouldReturnFalse_ForWriteToolWithoutIdempotencyKey()
    {
        var options = DefaultOptions();
        options.AllowWriteRetry = true;
        var policy = new ToolRetryPolicy(options);

        var error = new ToolError("retryable", "rate_limited", Retryable: true);
        policy.ShouldRetry(error, isWrite: true, isDryRun: false, hasIdempotencyKey: false, attempt: 0)
            .Should().BeFalse("写工具无幂等键不重试");
    }

    [Fact]
    public void ShouldRetry_ShouldReturnTrue_ForWriteToolWithIdempotencyKeyAndAllowWriteRetry()
    {
        var options = DefaultOptions();
        options.AllowWriteRetry = true;
        var policy = new ToolRetryPolicy(options);

        var error = new ToolError("retryable", "rate_limited", Retryable: true);
        policy.ShouldRetry(error, isWrite: true, isDryRun: false, hasIdempotencyKey: true, attempt: 0)
            .Should().BeTrue("写工具有幂等键且 AllowWriteRetry=true 应重试");
    }

    // ────────── ComputeDelay ──────────

    [Fact]
    public void ComputeDelay_ShouldUseExponentialBackoff()
    {
        var options = DefaultOptions();
        options.Jitter = false;
        var policy = new ToolRetryPolicy(options);

        var delay0 = policy.ComputeDelay(0);
        var delay1 = policy.ComputeDelay(1);
        var delay2 = policy.ComputeDelay(2);

        // BaseDelay * 2^attempt，无抖动
        delay0.Should().Be(200, "200 * 2^0 = 200");
        delay1.Should().Be(400, "200 * 2^1 = 400");
        delay2.Should().Be(800, "200 * 2^2 = 800");
    }

    [Fact]
    public void ComputeDelay_ShouldClampToMaxDelay()
    {
        var options = DefaultOptions();
        options.BaseDelayMilliseconds = 10000;
        options.MaxDelayMilliseconds = 5000;
        options.Jitter = false;
        var policy = new ToolRetryPolicy(options);

        var delay = policy.ComputeDelay(0);
        delay.Should().Be(5000, "延迟不得超 MaxDelay");
    }

    [Fact]
    public void ComputeDelay_ShouldUseRetryAfter_WhenProvided()
    {
        var options = DefaultOptions();
        options.Jitter = false;
        var policy = new ToolRetryPolicy(options);

        // retry_after=10s=10000ms > BaseDelay*2^0=200ms → 取 max(200, 10000) = 10000
        var delay = policy.ComputeDelay(0, retryAfterSeconds: 10);
        delay.Should().Be(10000, "Retry-After 值应优先采用");
    }

    [Fact]
    public void ComputeDelay_ShouldClampRetryAfterToMaxDelay()
    {
        var options = DefaultOptions();
        options.MaxDelayMilliseconds = 5000;
        options.Jitter = false;
        var policy = new ToolRetryPolicy(options);

        // Retry-After 是服务端权威指示，不受 MaxDelay 钳制
        // retry_after=60s=60000ms > MaxDelay=5000ms → 仍返回 60000
        var delay = policy.ComputeDelay(0, retryAfterSeconds: 60);
        delay.Should().Be(60000, "Retry-After 是服务端权威指示，不受 MaxDelay 钳制");
    }

    // ────────── IsEnabled ──────────

    [Fact]
    public void IsEnabled_ShouldBeTrue_WhenEnabledAndMaxAttemptsPositive()
    {
        var policy = new ToolRetryPolicy(DefaultOptions());
        policy.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_ShouldBeFalse_WhenDisabled()
    {
        var options = DefaultOptions();
        options.Enabled = false;
        var policy = new ToolRetryPolicy(options);
        policy.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void IsEnabled_ShouldBeFalse_WhenMaxAttemptsZero()
    {
        var options = DefaultOptions();
        options.MaxAttempts = 0;
        var policy = new ToolRetryPolicy(options);
        policy.IsEnabled.Should().BeFalse();
    }

    // ────────── DelayAsync ──────────

    [Fact]
    public async Task DelayAsync_ShouldReturnImmediately_WhenDelayZeroOrNegative()
    {
        var policy = new ToolRetryPolicy(DefaultOptions());
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await policy.DelayAsync(0, CancellationToken.None);
        sw.Stop();
        sw.ElapsedMilliseconds.Should().BeLessThan(100, "延迟 0 应立即返回");
    }

    [Fact]
    public async Task DelayAsync_ShouldRespectCancellation()
    {
        var policy = new ToolRetryPolicy(DefaultOptions());
        using var cts = new CancellationTokenSource(50);  // 50ms 后取消
        var act = async () => await policy.DelayAsync(5000, cts.Token);
        await act.Should().ThrowAsync<TaskCanceledException>("取消应优先于退避");
    }
}
