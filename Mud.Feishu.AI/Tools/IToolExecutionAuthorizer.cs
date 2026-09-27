// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// 工具执行结果的三态授权判定（Phase 2/3 契约统一类型，Phase 1 仅用 Allowed/Denied 两态）。
/// </summary>
public enum AuthorizationDecision
{
    /// <summary>放行：允许执行工具。</summary>
    Allowed = 0,

    /// <summary>拒绝：不得调用下游接口，拒绝原因结构化回填模型。</summary>
    Denied = 1,

    /// <summary>需用户确认（HITL，Phase 3）：挂起等待用户批准。</summary>
    NeedsUserConfirmation = 2,
}

/// <summary>
/// 一次工具授权判定结果。
/// </summary>
/// <param name="Decision">三态判定。</param>
/// <param name="Reason">拒绝/待确认原因（结构化回填模型与审计日志；放行时可空）。</param>
public sealed record AuthorizationResult(AuthorizationDecision Decision, string? Reason = null)
{
    /// <summary>放行便捷实例。</summary>
    public static AuthorizationResult Allowed { get; } = new(AuthorizationDecision.Allowed);

    /// <summary>构造拒绝结果。</summary>
    public static AuthorizationResult Deny(string reason) => new(AuthorizationDecision.Denied, reason);

    /// <summary>构造待确认结果。</summary>
    public static AuthorizationResult Confirm(string reason) => new(AuthorizationDecision.NeedsUserConfirmation, reason);
}

/// <summary>
/// 工具执行上下文：当前调用所在的应用/会话环境（多租户隔离的事实来源）。
/// </summary>
/// <remarks>
/// 缺失 <see cref="AppKey"/> 即失败——工具执行期间令牌/端点切换依赖
/// <c>IFeishuAppContextSwitcher.BeginScope(appKey)</c>（租户=应用上下文，TMA2-20）。
/// </remarks>
/// <param name="AppKey">应用唯一标识（必填）。</param>
/// <param name="ConversationKey">会话键（可空）。</param>
/// <param name="ChatId">触发会话的 chat_id（可空）。</param>
/// <param name="UserId">触发用户 ID（可空）。</param>
public sealed record FeishuToolContext(
    string AppKey,
    string? ConversationKey = null,
    string? ChatId = null,
    string? UserId = null);

/// <summary>
/// 工具执行授权钩子：SDK 只给钩子、不内实现策略（对齐 <c>IAppAccessAuthorizer</c> 提示模式）。
/// </summary>
/// <remarks>
/// <para>
/// 与官方飞书 CLI 的定位区分（已决策⑥）：CLI 的 <c>--dry-run</c> 是调用方自愿的执行前预览；
/// 本授权器是<b>宿主强制的服务端门禁</b>——写工具未过授权即拒绝，不可绕过。
/// </para>
/// <para>
/// 拒绝时执行链<b>不得</b>调用下游接口，拒绝原因结构化回填模型（总体设计 §4 不变式）。
/// </para>
/// </remarks>
public interface IToolExecutionAuthorizer
{
    /// <summary>
    /// 判定是否允许执行工具。
    /// </summary>
    /// <param name="toolName">工具名（注册表契约名）。</param>
    /// <param name="requiredScopes">工具声明的权限点（来自 <see cref="FeishuToolAttribute.RequiredScopes"/>）。</param>
    /// <param name="isWrite">是否写操作。</param>
    /// <param name="arguments">工具入参（模型 tool_call 反序列化结果）。</param>
    /// <param name="context">执行上下文（appKey/chat/user）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>三态授权结果。</returns>
    Task<AuthorizationResult> AuthorizeAsync(
        string toolName,
        IReadOnlyList<string> requiredScopes,
        bool isWrite,
        IReadOnlyDictionary<string, object?> arguments,
        FeishuToolContext context,
        CancellationToken cancellationToken = default);
}
