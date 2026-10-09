// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Tools.Tools;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// B3 限流退避契约守卫（方案 §3.B3 守卫三条）。
/// </summary>
/// <remarks>
/// <para>
/// 守卫三条：
/// <list type="number">
/// <item>写工具默认零重试（行为断言，不是注释）；</item>
/// <item><c>AllowWriteRetry=true</c> 且无幂等键时仍零重试；</item>
/// <item>重试次数上界 = <c>MaxAttempts</c>。</item>
/// </list>
/// </para>
/// </remarks>
public class RetryPolicyContractGuards
{
    private static ToolRetryOptions DefaultOptions() => new()
    {
        Enabled = true,
        MaxAttempts = 3,
        BaseDelayMilliseconds = 200,
        MaxDelayMilliseconds = 5000,
        Jitter = true,
        AllowWriteRetry = false,
    };

    /// <summary>
    /// 守卫 ①：写工具默认零重试（行为断言）。
    /// </summary>
    [Fact]
    public void WriteTool_ShouldHaveZeroRetry_ByDefault()
    {
        var policy = new ToolRetryPolicy(DefaultOptions());
        var error = new ToolError("retryable", "rate_limited", Retryable: true);

        policy.ShouldRetry(error, isWrite: true, isDryRun: false, hasIdempotencyKey: true, attempt: 0)
            .Should().BeFalse("写工具默认零重试（安全优先，防重复副作用）");

        policy.ShouldRetry(error, isWrite: true, isDryRun: false, hasIdempotencyKey: false, attempt: 0)
            .Should().BeFalse("写工具默认零重试——无论是否有幂等键");
    }

    /// <summary>
    /// 守卫 ②：<c>AllowWriteRetry=true</c> 且无幂等键时仍零重试。
    /// </summary>
    [Fact]
    public void WriteTool_ShouldNotRetry_WhenNoIdempotencyKey_EvenIfAllowWriteRetry()
    {
        var options = DefaultOptions();
        options.AllowWriteRetry = true;
        var policy = new ToolRetryPolicy(options);

        var error = new ToolError("retryable", "rate_limited", Retryable: true);

        policy.ShouldRetry(error, isWrite: true, isDryRun: false, hasIdempotencyKey: false, attempt: 0)
            .Should().BeFalse("即使 AllowWriteRetry=true，无幂等键仍零重试（防重复副作用）");

        policy.ShouldRetry(error, isWrite: true, isDryRun: false, hasIdempotencyKey: true, attempt: 0)
            .Should().BeTrue("有幂等键 + AllowWriteRetry=true 时允许重试");
    }

    /// <summary>
    /// 守卫 ③：重试次数上界 = MaxAttempts。
    /// </summary>
    [Fact]
    public void RetryAttempts_ShouldBeBoundedByMaxAttempts()
    {
        var options = DefaultOptions();
        options.MaxAttempts = 3;
        var policy = new ToolRetryPolicy(options);

        var error = new ToolError("retryable", "rate_limited", Retryable: true);

        // attempt 0, 1, 2 应允许重试（MaxAttempts=3 意味着最多 3 次重试）
        policy.ShouldRetry(error, isWrite: false, isDryRun: false, hasIdempotencyKey: false, attempt: 0)
            .Should().BeTrue("attempt 0 < MaxAttempts=3");
        policy.ShouldRetry(error, isWrite: false, isDryRun: false, hasIdempotencyKey: false, attempt: 1)
            .Should().BeTrue("attempt 1 < MaxAttempts=3");
        policy.ShouldRetry(error, isWrite: false, isDryRun: false, hasIdempotencyKey: false, attempt: 2)
            .Should().BeTrue("attempt 2 < MaxAttempts=3");

        // attempt 3 不应重试（已耗尽）
        policy.ShouldRetry(error, isWrite: false, isDryRun: false, hasIdempotencyKey: false, attempt: 3)
            .Should().BeFalse("attempt 3 = MaxAttempts=3 → 不再重试");
    }

    /// <summary>
    /// 守卫 ④：FeishuAgentOptions 含 B3 重试配置项且默认值合理。
    /// </summary>
    [Fact]
    public void FeishuAgentOptions_ShouldHaveRetryDefaults()
    {
        var options = new FeishuAgentOptions();
        options.ToolRetry.Should().NotBeNull("ToolRetry 配置项必须存在");
        options.ToolRetry.Enabled.Should().BeTrue("默认启用重试");
        options.ToolRetry.MaxAttempts.Should().BeGreaterThan(0, "默认重试次数必须为正");
        options.ToolRetry.MaxAttempts.Should().BeLessThanOrEqualTo(10, "重试次数上界 10");
        options.ToolRetry.AllowWriteRetry.Should().BeFalse("默认禁止写工具重试（安全优先）");
        options.ToolRetry.BaseDelayMilliseconds.Should().BeGreaterThan(0, "基准延迟必须为正");
        options.ToolRetry.MaxDelayMilliseconds.Should().BeGreaterThanOrEqualTo(
            options.ToolRetry.BaseDelayMilliseconds, "最大延迟不得小于基准延迟");
    }

    /// <summary>
    /// 守卫 ⑤：非可重试错误不重试（确定性失败不应重试）。
    /// </summary>
    [Fact]
    public void NonRetryableError_ShouldNotRetry()
    {
        var policy = new ToolRetryPolicy(DefaultOptions());

        var validationError = new ToolError("validation", "invalid_args", Retryable: false);
        policy.ShouldRetry(validationError, isWrite: false, isDryRun: false, hasIdempotencyKey: false, attempt: 0)
            .Should().BeFalse("validation 错误不可重试");

        var authError = new ToolError("authorization", "authorization_denied", Retryable: false);
        policy.ShouldRetry(authError, isWrite: false, isDryRun: false, hasIdempotencyKey: false, attempt: 0)
            .Should().BeFalse("authorization 错误不可重试（确定性失败）");

        var policyError = new ToolError("policy", "tool_not_allowed", Retryable: false);
        policy.ShouldRetry(policyError, isWrite: false, isDryRun: false, hasIdempotencyKey: false, attempt: 0)
            .Should().BeFalse("policy 错误不可重试（确定性失败）");
    }

    /// <summary>
    /// 守卫 ⑥：dry_run 模式不重试。
    /// </summary>
    [Fact]
    public void DryRun_ShouldNotRetry()
    {
        var policy = new ToolRetryPolicy(DefaultOptions());
        var error = new ToolError("retryable", "rate_limited", Retryable: true);

        policy.ShouldRetry(error, isWrite: false, isDryRun: true, hasIdempotencyKey: false, attempt: 0)
            .Should().BeFalse("dry_run 模式不重试（不下发请求，无重试意义）");
    }
}
