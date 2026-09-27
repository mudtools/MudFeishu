// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Mud.Feishu.AI.FeishuTools;

/// <summary>
/// 工具执行链（护城河核心，已决策⑥）：把模型 tool_call 接到强类型飞书接口。
/// </summary>
/// <remarks>
/// <para>
/// 执行顺序（不可变式）：
/// ① 上下文校验（缺 <c>AppKey</c> 即失败——多租户隔离禁止默认应用兜底，TMA2-20）→
/// ② 授权门禁（<see cref="IToolExecutionAuthorizer"/>；写类工具在
/// <c>EnforceToolAuthorization=true</c> 且未注册授权器时默认拒绝——拒绝时<b>不切换</b>租户上下文、
/// <b>零调用</b>下游接口）→
/// ③ <see cref="IFeishuAppContextScopeFactory.BeginScope"/> 租户切换（先于下游调用，
/// <c>finally</c> 释放）→
/// ④ 分域下游调用（强类型接口 + 解包 + 白名单投影 + 截断，由调用方委托承载）→
/// ⑤ 异常归一（除取消外转结构化错误文本回填模型，不抛裸异常）。
/// </para>
/// <para>
/// 全程 OTel：Span <c>feishu.agent.tool</c> 携带工具名/appKey/scopes/判定结果
/// （护城河硬验收③，Phase 1 §5）。
/// </para>
/// </remarks>
public sealed class FeishuToolBinding
{
    private readonly IFeishuAppContextScopeFactory _scopeFactory;
    private readonly FeishuAgentOptions _options;
    private readonly IToolExecutionAuthorizer? _authorizer;
    private readonly ILogger? _logger;

    /// <summary>
    /// 初始化 <see cref="FeishuToolBinding"/>。
    /// </summary>
    /// <param name="scopeFactory">租户上下文作用域工厂。</param>
    /// <param name="options">Agent 配置（<see cref="FeishuAgentOptions.EnforceToolAuthorization"/> 消费点）。</param>
    /// <param name="authorizer">工具授权钩子（可空；SDK 不内建策略）。</param>
    /// <param name="logger">日志（可空）。</param>
    public FeishuToolBinding(
        IFeishuAppContextScopeFactory scopeFactory,
        IOptions<FeishuAgentOptions> options,
        IToolExecutionAuthorizer? authorizer = null,
        ILogger<FeishuToolBinding>? logger = null)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        _authorizer = authorizer;
        _logger = logger;
    }

    /// <summary>
    /// 执行一次工具调用：授权门禁 → 租户上下文切换 → 下游委托 → 异常归一。
    /// </summary>
    /// <param name="tool">工具定义（注册表条目，携带 scopes/IsWrite 元数据）。</param>
    /// <param name="arguments">模型 tool_call 入参。</param>
    /// <param name="context">执行上下文（appKey/chat/user，经 <c>IFeishuToolContextAccessor</c> 注入）。</param>
    /// <param name="invokeDownstream">分域下游调用（参数映射 → 强类型接口 → 解包/投影/截断）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>回填模型的文本（成功结果或结构化错误）。</returns>
    public async Task<string> ExecuteAsync(
        FeishuToolDefinition tool,
        IReadOnlyDictionary<string, object?> arguments,
        FeishuToolContext context,
        Func<CancellationToken, Task<string>> invokeDownstream,
        CancellationToken cancellationToken = default)
    {
        if (tool is null)
            throw new ArgumentNullException(nameof(tool));
        if (arguments is null)
            throw new ArgumentNullException(nameof(arguments));
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        if (invokeDownstream is null)
            throw new ArgumentNullException(nameof(invokeDownstream));

        // ① 上下文校验：多租户隔离事实来源，缺 appKey 即失败（TMA2-20）。
        if (string.IsNullOrWhiteSpace(context.AppKey))
        {
            return StructuredError(tool.Name, "工具执行上下文缺少 appKey——多租户隔离禁止默认应用兜底（TMA2-20）");
        }

        using var activity = FeishuToolDiagnostics.StartToolActivity(
            tool.Name, context.AppKey, tool.RequiredScopes, tool.IsWrite);

        // ② 授权门禁：拒绝时零调用下游、不切换租户上下文。
        var gate = await AuthorizeGateAsync(tool, arguments, context, cancellationToken).ConfigureAwait(false);
        activity?.SetTag(FeishuToolDiagnostics.TagDecision,
            gate.Allowed ? FeishuToolDiagnostics.DecisionAllowed : FeishuToolDiagnostics.DecisionDenied);
        if (!gate.Allowed)
        {
            return StructuredError(tool.Name, gate.Reason);
        }

        // ③ 租户上下文切换（先于下游调用，作用域 finally 释放）。
        try
        {
            using var scope = _scopeFactory.BeginScope(context.AppKey);
            var result = await invokeDownstream(cancellationToken).ConfigureAwait(false);
            activity?.SetTag(FeishuToolDiagnostics.TagTruncated, ToolResultText.IsTruncated(result));
            return result;
        }
        catch (OperationCanceledException)
        {
            // 取消即时传播（D15：TryMark 类进入处理态不受补偿约束）。
            throw;
        }
        catch (Exception ex)
        {
            // ⑤ 异常归一：结构化错误回填模型，不抛裸异常（总体设计 §4 不变式）。
            _logger?.LogWarning(ex, "工具 {ToolName} 执行失败（appKey: {AppKey}）", tool.Name, context.AppKey);
            return StructuredError(tool.Name, $"工具执行异常: {ex.Message}");
        }
    }

    /// <summary>构造结构化错误文本（模型可读、带工具名前缀）。</summary>
    public static string StructuredError(string toolName, string reason)
        => $"[tool_error] {toolName}: {reason}";

    private async Task<(bool Allowed, string Reason)> AuthorizeGateAsync(
        FeishuToolDefinition tool,
        IReadOnlyDictionary<string, object?> arguments,
        FeishuToolContext context,
        CancellationToken cancellationToken)
    {
        if (_authorizer is null)
        {
            if (tool.IsWrite && _options.EnforceToolAuthorization)
            {
                return (false, "写类工具未注册 IToolExecutionAuthorizer，且 EnforceToolAuthorization=true——默认拒绝（安全默认，Phase 1 §3.3.4）");
            }

            // 只读工具默认 NotRequired（授权钩子预留；scopes 随 Schema 供宿主审计）。
            return (true, string.Empty);
        }

        var result = await _authorizer
            .AuthorizeAsync(tool.Name, tool.RequiredScopes, tool.IsWrite, arguments, context, cancellationToken)
            .ConfigureAwait(false);
        if (result is null)
        {
            return (false, "授权器返回空结果——按拒绝处理（fail-closed）");
        }

        return result.Decision switch
        {
            AuthorizationDecision.Allowed => (true, string.Empty),
            AuthorizationDecision.Denied => (false, result.Reason ?? "授权被拒绝"),
            AuthorizationDecision.NeedsUserConfirmation =>
                (false, $"需要用户确认后才能执行（HITL，Phase 3 交付）：{result.Reason ?? "未提供原因"}"),
            _ => (false, $"未知授权判定 {result.Decision}——按拒绝处理（fail-closed）"),
        };
    }
}
