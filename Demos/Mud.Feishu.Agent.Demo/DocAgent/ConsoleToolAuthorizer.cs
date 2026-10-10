// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools;
using Mud.Feishu.AI.Tools.Generated;

namespace Mud.Feishu.Agent.Demo;

/// <summary>
/// 工具风险/权限点的<b>编译期</b>查询出口（Demo 内部共用）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么不用容器里的 <c>FeishuToolRegistry</c>（这是一处必须写下来的坑）</b>：
/// 注册表本身要在装配期构造，而它的构造会解析 <c>FeishuToolBinding</c>，
/// 后者**可选注入** <c>IToolExecutionAuthorizer</c> / <c>IToolExecutionAuditSink</c>。
/// 若授权器/审计 sink 反过来依赖 <c>FeishuToolRegistry</c>，就形成
/// <c>注册表 → 注册器 → Binding → 授权器 → 注册表</c> 的<b>运行期循环依赖</b>：
/// .NET DI 的静态环检测看不见 lambda 工厂里的服务定位调用，表现为**启动期无输出地卡死**
/// （而不是抛异常），排障成本极高。
/// </para>
/// <para>
/// 契约表 <c>FeishuToolContracts.ByToolName</c> 是<b>编译期常量</b>（与 Schema 同源同 pass），
/// 既能给出同一份风险事实（"运行期不得手写风险值"的纪律照旧成立），又不引入任何 DI 依赖。
/// </para>
/// </remarks>
internal static class DocAgentToolFacts
{
    /// <summary>取工具的风险分级（查不到按读写布尔兜底）。</summary>
    /// <param name="toolName">工具名。</param>
    /// <returns>风险分级。</returns>
    public static FeishuToolRisk RiskOf(string toolName)
        => FeishuToolContracts.ByToolName.TryGetValue(toolName, out var contract)
            ? contract.Risk
            : FeishuToolRisk.Read;

    /// <summary>取风险的 Schema 字面量（<c>read</c> / <c>write</c> / <c>high-risk-write</c>）。</summary>
    /// <param name="toolName">工具名。</param>
    /// <param name="isWrite">工具是否为写操作（查不到契约时用于兜底）。</param>
    /// <returns>风险字面量。</returns>
    public static string RiskLiteralOf(string toolName, bool isWrite)
        => FeishuToolContracts.ByToolName.TryGetValue(toolName, out var contract)
            ? FeishuToolRiskNames.ToLiteral(contract.Risk)
            : (isWrite ? FeishuToolRiskNames.Write : FeishuToolRiskNames.Read);
}

/// <summary>
/// 授权器可变状态：当前策略（<c>/policy</c> 可切换）、期望 appKey 与预授权权限面。
/// </summary>
/// <remarks>
/// 本类型是 <c>strict</c> 策略的"现场可切换"载体——<b>可变状态必须是单例</b>，
/// 且与授权器分离（授权器自身无状态、可安全并发调用）。
/// </remarks>
internal sealed class ConsoleToolAuthorizerState
{
    private volatile string _policy;

    /// <summary>
    /// 初始化状态。
    /// </summary>
    /// <param name="expectedAppKey">期望的应用键（工具执行上下文必须与之一致）。</param>
    /// <param name="allowedScopes">预授权权限面（授权器第 ③ 条判定的闭集）。</param>
    /// <param name="initialPolicy">初始策略。</param>
    public ConsoleToolAuthorizerState(
        string expectedAppKey,
        IReadOnlyList<string> allowedScopes,
        string initialPolicy)
    {
        ExpectedAppKey = expectedAppKey ?? throw new ArgumentNullException(nameof(expectedAppKey));
        AllowedScopes = allowedScopes ?? throw new ArgumentNullException(nameof(allowedScopes));
        _policy = initialPolicy;
    }

    /// <summary>期望的应用键。</summary>
    public string ExpectedAppKey { get; }

    /// <summary>预授权权限面。</summary>
    public IReadOnlyList<string> AllowedScopes { get; }

    /// <summary>当前策略（<c>strict</c> / <c>ask</c> / <c>readonly</c>）。</summary>
    public string Current
    {
        get => _policy;
        set => _policy = value;
    }
}

/// <summary>
/// 宿主授权器（闸 2）：<c>IToolExecutionAuthorizer</c> 的控制台实现——<b>只做 Allowed / Denied 策略判定</b>。
/// </summary>
/// <remarks>
/// <para>
/// <b>「批准 ≠ 放行」（本 Demo 的核心教学点）</b>：框架审批（闸 1）通过后，
/// <c>RunApprovalContinuationAsync</c> 的续跑轮里工具才真正进入 <c>FeishuToolBinding</c>，
/// 此时仍会被本授权器咨询。因此"人类批准过"并不等于"必然执行"——
/// <c>strict</c> 策略下高风险写工具（如 <c>docx.delete_blocks</c>）会被本授权器<b>独立再拒一次</b>。
/// </para>
/// <para>
/// <b>为何不返回 <c>Confirm</c></b>：P4-1 之后授权器已退化为纯 <c>Allowed</c>/<c>Denied</c>
/// 策略判定，<c>NeedsUserConfirmation</c> 由 MAF 的 <c>ApprovalRequiredAIFunction</c> 包装承载。
/// 若本实现同时返回 <c>Confirm</c>，演示的会是一条<b>实际不可达</b>的路径，误导读者。
/// 契约守卫 <c>DocAgentDemoContractGuards</c> 与单测共同锁死"任何路径都不返回 Confirm"。
/// </para>
/// <para>
/// <b>未注册的后果</b>：<c>EnforceToolAuthorization = true</c>（默认）且未注册授权器时，
/// 写工具会以 <c>authorization_denied</c> 失败（fail-closed）。本 Demo 始终注册。
/// </para>
/// </remarks>
internal sealed class ConsoleToolAuthorizer : IToolExecutionAuthorizer
{
    private readonly ConsoleToolAuthorizerState _state;
    private readonly ConsoleRenderer _renderer;

    /// <summary>
    /// 初始化授权器。
    /// </summary>
    /// <param name="state">可变策略状态。</param>
    /// <param name="renderer">终端渲染器（Trace 输出）。</param>
    /// <remarks>
    /// <b>刻意不注入 <c>FeishuToolRegistry</c></b>：那会形成
    /// <c>注册表 → 注册器 → FeishuToolBinding → 授权器 → 注册表</c> 的运行期循环依赖，
    /// 表现为启动期无输出卡死（见 <see cref="DocAgentToolFacts"/> 的说明）。
    /// 风险事实改从编译期契约表读取。
    /// </remarks>
    public ConsoleToolAuthorizer(
        ConsoleToolAuthorizerState state,
        ConsoleRenderer renderer)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    /// <inheritdoc />
    public Task<AuthorizationResult> AuthorizeAsync(
        string toolName,
        IReadOnlyList<string> requiredScopes,
        bool isWrite,
        IReadOnlyDictionary<string, object?> arguments,
        FeishuToolContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var policy = _state.Current;

        // ① 租户一致性自检：文档业务域应全部为 tenant 身份、且 appKey 必须与宿主期望一致。
        //    多租户隔离不允许"默认 appKey 兜底"（TMA2-20）——不一致即拒，而不是猜测。
        if (!string.Equals(context.AppKey, _state.ExpectedAppKey, StringComparison.Ordinal))
        {
            return Deny(toolName, policy,
                $"appKey 不匹配：期望 {_state.ExpectedAppKey}，实际 {context.AppKey}（多租户隔离禁止默认应用兜底）");
        }

        // ② readonly 策略：所有写工具一律拒绝（无论框架是否批准）。
        if (isWrite && string.Equals(policy, DocAgentSettings.PolicyReadonly, StringComparison.Ordinal))
        {
            return Deny(toolName, policy,
                $"当前策略 readonly：写工具被拒绝（输入 /policy strict 恢复）");
        }

        // ③ strict 策略：高风险写工具（删除/清空类）由授权器**独立**拒绝。
        //    这是"批准 ≠ 放行"的现场证据：框架审批已通过，第二道闸仍可拦下。
        if (isWrite
            && string.Equals(policy, DocAgentSettings.PolicyStrict, StringComparison.Ordinal)
            && DocAgentToolFacts.RiskOf(toolName) == FeishuToolRisk.HighRiskWrite)
        {
            return Deny(toolName, policy,
                $"当前策略 strict：高风险写工具被授权器独立拒绝（框架审批已通过，但授权器是第二道闸；"
                + "如需真删请先 /policy ask）");
        }

        // ④ 权限面白名单：requiredScopes 必须落在预授权集合内。
        //    判据是 `[FeishuTool]` 声明的权限点（而非工具名硬编码），故工具改名不影响本判定。
        var unknown = requiredScopes
            .Where(scope => !_state.AllowedScopes.Contains(scope, StringComparer.Ordinal))
            .ToArray();

        if (unknown.Length > 0)
        {
            return Deny(toolName, policy, $"权限点未预授权：{string.Join("、", unknown)}");
        }

        // ⑤ 默认放行（**不返回 Confirm**——见类型级注释）。
        _renderer.Trace(
            $"授权器放行 {toolName}（policy={policy}, write={isWrite}, scopes={(requiredScopes.Count == 0 ? "-" : string.Join(',', requiredScopes))}）");
        return Task.FromResult(AuthorizationResult.Allowed);
    }

    private Task<AuthorizationResult> Deny(string toolName, string policy, string reason)
    {
        _renderer.Trace($"授权器拒绝 {toolName}（policy={policy}）：{reason}");
        return Task.FromResult(AuthorizationResult.Deny(reason));
    }
}
