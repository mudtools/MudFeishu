// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

using Mud.Feishu.AI.CapabilityDemo.Common;

namespace Mud.Feishu.AI.CapabilityDemo.Host;

/// <summary>授权器的可变状态（策略 / 显式封禁 / 高风险集）。</summary>
internal sealed class ConsoleAuthorizerState(string policy)
{
    private volatile string _policy = policy;

    public string Policy
    {
        get => _policy;
        set => _policy = value;
    }

    /// <summary>A4-c 用：显式封禁的只读工具（证明门禁本身可拒）。</summary>
    public HashSet<string> DeniedTools { get; } = new(StringComparer.Ordinal);

    /// <summary>A5 用：进入名单的高危写（strict 下由本授权器独立拒绝）。</summary>
    public HashSet<string> HighRiskTools { get; } = new(StringComparer.Ordinal);
}

/// <summary>
/// 宿主授权器（闸 2）：<b>只做 Allowed / Denied</b>，从不返回 Confirm。
/// </summary>
/// <remarks>
/// <para>
/// <b>「批准 ≠ 放行」</b>：框架审批通过后，续跑轮仍会到这里；只有 <c>/approve</c> 发放的
/// 一次性 grant 能放行写工具。strict 策略还会对高危写独立再拒一次。
/// </para>
/// <para>
/// <b>为何不返回 <c>NeedsUserConfirmation</c></b>：该语义已由 MAF 的
/// <c>ApprovalRequiredAIFunction</c> 承载；这里再返回会让同一请求被登记两次，
/// 演示的是一条实际不可达的双路。
/// </para>
/// </remarks>
internal sealed class ConsoleToolAuthorizer(PendingApprovalBoard board, ConsoleAuthorizerState state)
    : IToolExecutionAuthorizer
{
    public Task<AuthorizationResult> AuthorizeAsync(
        string toolName,
        IReadOnlyList<string> requiredScopes,
        bool isWrite,
        IReadOnlyDictionary<string, object?> arguments,
        FeishuToolContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (state.DeniedTools.Contains(toolName))
        {
            return Deny($"演示策略：工具 {toolName} 被宿主显式封禁");
        }

        if (isWrite && string.Equals(state.Policy, DemoSettings.PolicyReadonly, StringComparison.Ordinal))
        {
            return Deny("演示策略 readonly：禁止一切写操作");
        }

        if (!isWrite)
        {
            return Task.FromResult(AuthorizationResult.Allowed);
        }

        if (string.Equals(state.Policy, DemoSettings.PolicyStrict, StringComparison.Ordinal)
            && state.HighRiskTools.Contains(toolName))
        {
            return Deny($"演示策略 strict：高危写 {toolName} 被授权器独立拒绝（框架审批已通过，但批准不等于放行）");
        }

        return board.TryConsumeGrant(toolName)
            ? Task.FromResult(AuthorizationResult.Allowed)
            : Deny($"写工具 {toolName} 等待人工确认（先 /approve；批准后仍需本授权器放行）");

        Task<AuthorizationResult> Deny(string reason)
            => Task.FromResult(AuthorizationResult.Deny(reason));
    }
}
