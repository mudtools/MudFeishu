// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Mud.Feishu.Abstractions;

namespace Mud.Feishu.AI.Tools.Hosting;

/// <summary>
/// <see cref="IFeishuDiagnosticsProbe"/> 的默认实现（同步、零网络、零令牌刷新）。
/// </summary>
/// <remarks>
/// <para>
/// <b>错误处理口径</b>：装配期即失败的两处（<c>Validate()</c> 与注册表工厂）在真实启动路径上会抛，
/// 本类把它们转成结论回填——自检面对的是一个「可能装配坏了」的容器，抛异常等于让诊断本身不可诊断。
/// 其余判据只读已注册服务，不构造、不调用任何执行器。
/// </para>
/// <para>
/// <b>只报「必然」</b>：<c>error</c> 级结论一律是运行期确定发生的事实（写工具必然被拒、
/// 风险必然超额、域必然被丢），需要猜测的只到 <c>warning</c>。诊断面把猜测写成确定结论，
/// 宿主就会开始忽略整份报告。
/// </para>
/// </remarks>
internal sealed class FeishuDiagnosticsProbe : IFeishuDiagnosticsProbe
{
    /// <summary>结论正文里最多列出的工具/域名数（超出以 <c>…</c> 收口，防报告本身失控）。</summary>
    private const int MaxListedNames = 10;

    /// <summary>guidance 预算余量低于该百分比时告警（整域丢弃的前兆）。</summary>
    private const int BudgetLowWatermarkPercent = 20;

    private readonly IServiceProvider _services;

    public FeishuDiagnosticsProbe(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <inheritdoc />
    public FeishuDiagnosticsReport Probe()
    {
        var report = new Collector();
        try
        {
            var options = ProbeConfiguration(report);
            var registry = ProbeRegistry(report);
            var enabled = ProbeToolSurface(report, options, registry);
            ProbeHostWiring(report, options, enabled);
            ProbeGuidance(report, options, registry);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // 有意留痕（守卫白名单）：判据本身抛了也要出报告——异常转成 probe_fault 结论，
            // 丢弃整份报告等于让「自检」不可自检。
            report.Error("probe_fault", $"{ex.GetType().Name}: {ex.Message}（自检中止，已产出的结论仍有效）");
        }

        return report.Build();
    }

    /// <summary>配置完整性：<c>Validate()</c> 的抛点转成结论（非法取值不再等到第一次运行才暴露）。</summary>
    private FeishuAgentOptions ProbeConfiguration(Collector report)
    {
        var options = _services.GetService<IOptions<FeishuAgentOptions>>()?.Value;
        if (options is null)
        {
            report.Info(
                "options_missing",
                $"{FeishuAgentOptions.SectionName} 未注册——以下策略判据按类型默认值评估（宿主未绑定配置节时的真实形态）");

            return new FeishuAgentOptions();
        }

        try
        {
            options.Validate();
        }
        catch (InvalidOperationException ex)
        {
            // 有意留痕（守卫白名单）：配置校验的抛点转成 config_invalid 结论——自检面的契约是「出报告」，不是「抛出」。
            report.Error("config_invalid", ex.Message);
        }

        return options;
    }

    /// <summary>注册表构建（白名单含未注册工具、身份未放行等装配错误在此转成结论而非抛出）。</summary>
    private FeishuToolRegistry? ProbeRegistry(Collector report)
    {
        try
        {
            return _services.GetService<FeishuToolRegistry>();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // 有意留痕（守卫白名单）：注册表构建失败转 registry_build_failed 结论并让后续段落继续，
            // 装配错误在自检里应当是可读的一行，而不是让诊断命令自己崩掉。
            report.Error("registry_build_failed", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// 工具面读数 + 白名单/策略一致性（策展契约 ↔ 注册表 ↔ 已启用集三者对账）。
    /// </summary>
    private IReadOnlyList<FeishuToolDefinition> ProbeToolSurface(
        Collector report, FeishuAgentOptions options, FeishuToolRegistry? registry)
    {
        var enabled = registry?.EnabledTools ?? Array.Empty<FeishuToolDefinition>();
        var registered = registry?.AllTools ?? Array.Empty<FeishuToolDefinition>();
        var contract = FeishuToolSchemas.SchemaByToolName;

        report.Fact("tools.registered", Count(registered.Count));
        report.Fact("tools.enabled", Count(enabled.Count));
        report.Fact("tools.enabled_read", Count(enabled.Count(static t => !t.IsWrite)));
        report.Fact("tools.enabled_write", Count(enabled.Count(static t => t.IsWrite)));
        report.Fact("tools.enabled_high_risk_write", Count(enabled.Count(static t => t.Risk == FeishuToolRisk.HighRiskWrite)));
        report.Fact("tools.contract_surface", Count(contract.Count));
        report.Fact("tools.domains_enabled", Count(enabled.Select(Domain).Distinct(StringComparer.Ordinal).Count()));

        report.Fact($"{PolicyPrefix}.{nameof(FeishuAgentOptions.MaxToolRisk)}", options.MaxToolRisk);
        report.Fact($"{PolicyPrefix}.{nameof(FeishuAgentOptions.AllowedIdentities)}", string.Join(",", options.AllowedIdentities));
        report.Fact($"{PolicyPrefix}.{nameof(FeishuAgentOptions.EnforceToolAuthorization)}", Bool(options.EnforceToolAuthorization));
        report.Fact($"{PolicyPrefix}.{nameof(FeishuAgentOptions.ContentSafetyMode)}", options.ContentSafetyMode);
        report.Fact($"{PolicyPrefix}.{nameof(FeishuAgentOptions.GuidanceMode)}", options.GuidanceMode);
        report.Fact($"{PolicyPrefix}.{nameof(FeishuAgentOptions.UserTokenPrecheck)}", options.UserTokenPrecheck);
        report.Fact($"{PolicyPrefix}.{nameof(FeishuAgentOptions.MaxToolResultLength)}", Count(options.MaxToolResultLength));
        report.Fact($"{PolicyPrefix}.{nameof(FeishuAgentOptions.MaxToolCallsPerTurn)}", Count(options.MaxToolCallsPerTurn));
        report.Fact($"{PolicyPrefix}.{nameof(FeishuAgentOptions.ToolPolicy)}.configured", Bool(options.ToolPolicy.IsConfigured));
        report.Fact($"{PolicyPrefix}.{nameof(FeishuAgentOptions.Tools)}", Count(options.Tools.Length));
        report.Fact($"{PolicyPrefix}.{nameof(FeishuAgentOptions.WriteAllowList)}", Count(options.WriteAllowList.Length));

        if (enabled.Count == 0)
        {
            report.Error("no_tools_enabled",
                "已启用工具为 0——Agent 将以无工具形态运行（装配链不含任何 feishu.* 工具）");
        }

        // 契约面 ↔ 注册表：按域装配时缺口是预期的；全域入口下缺口即「域执行器未接线 ⇒ 工具静默不入表」。
        var registeredNames = new HashSet<string>(registered.Select(static t => t.Name), StringComparer.Ordinal);
        var contractNames = contract.Keys.ToArray();
        var missing = contractNames.Where(name => !registeredNames.Contains(name)).ToArray();
        if (missing.Length > 0)
        {
            report.Warn("contract_registry_gap",
                $"策展契约有 {Count(missing.Length)} 个工具未进注册表（域执行器未接线或按域装配）：{Names(missing)}"
                + "——这些工具对模型完全不可见，也不会有任何报错");
        }

        var uncontracted = registeredNames.Where(name => !contract.ContainsKey(name)).ToArray();
        if (uncontracted.Length > 0)
        {
            report.Error("registry_tools_without_contract",
                $"注册表有 {Count(uncontracted.Length)} 个工具不在编译期契约表内：{Names(uncontracted)}"
                + "——构桥与描述装配会跳过或失败，白名单里的它们永远不会可用");
        }

        // 风险轴 ↔ 已启用集：超额工具在校验阶段即 risk_exceeded，装配期无任何提示。
        if (FeishuToolRiskNames.TryParse(options.MaxToolRisk, out var maxRisk))
        {
            var overRisk = enabled
                .Where(t => t.Risk > maxRisk)
                .Select(static t => t.Name)
                .OrderBy(static n => n, StringComparer.Ordinal)
                .ToArray();
            if (overRisk.Length > 0)
            {
                report.Error("risk_policy_mismatch",
                    $"已启用工具的 ({FeishuToolRiskNames.ToLiteral(maxRisk)}) 风险上限之上：{Names(overRisk)}"
                    + $"——这些工具每次调用都返回 risk_exceeded；请提高 {PolicyPrefix}.{nameof(FeishuAgentOptions.MaxToolRisk)} "
                    + "或从白名单移除");
            }
        }

        var highRisk = enabled
            .Where(static t => t.Risk == FeishuToolRisk.HighRiskWrite)
            .Select(static t => t.Name)
            .OrderBy(static n => n, StringComparer.Ordinal)
            .ToArray();
        if (highRisk.Length > 0)
        {
            report.Warn("high_risk_write_enabled",
                $"已启用 {Count(highRisk.Length)} 个高危写工具（不可撤销）：{Names(highRisk)}"
                + $"——一键收回请把 {PolicyPrefix}.{nameof(FeishuAgentOptions.MaxToolRisk)} 设为 '{FeishuToolRiskNames.Write}'");
        }

        return enabled;
    }

    /// <summary>宿主扩展点与身份/应用接线（缺席的后果按「必然」与「隐患」分档）。</summary>
    private void ProbeHostWiring(Collector report, FeishuAgentOptions options, IReadOnlyList<FeishuToolDefinition> enabled)
    {
        var authorizer = _services.GetService<IToolExecutionAuthorizer>();
        var approvalChannel = _services.GetService<IFeishuToolApprovalChannel>();
        var auditSink = _services.GetService<IToolExecutionAuditSink>();
        var resultShaper = _services.GetService<IToolResultShaper>();
        var attachmentStager = _services.GetService<IFeishuAttachmentStager>();
        var currentUserContext = _services.GetService<IFeishuCurrentUserContext>();
        var tokenResolver = _services.GetService<IFeishuTokenManagerResolver>();
        var appManager = _services.GetService<IFeishuAppManager>();

        report.Fact($"{HostPrefix}.{nameof(IToolExecutionAuthorizer)}", Bool(authorizer is not null));
        report.Fact($"{HostPrefix}.{nameof(IFeishuToolApprovalChannel)}", Bool(approvalChannel is not null));
        report.Fact($"{HostPrefix}.{nameof(IToolExecutionAuditSink)}", Bool(auditSink is not null));
        report.Fact($"{HostPrefix}.{nameof(IToolResultShaper)}", Bool(resultShaper is not null));
        report.Fact($"{HostPrefix}.{nameof(IFeishuAttachmentStager)}", Bool(attachmentStager is not null));
        report.Fact($"{HostPrefix}.{nameof(IFeishuCurrentUserContext)}", Bool(currentUserContext is not null));
        report.Fact($"{HostPrefix}.{nameof(IFeishuTokenManagerResolver)}", Bool(tokenResolver is not null));
        report.Fact($"{HostPrefix}.{nameof(IFeishuAppManager)}", Bool(appManager is not null));

        var writeCount = enabled.Count(static t => t.IsWrite);
        if (writeCount > 0 && authorizer is null)
        {
            if (options.EnforceToolAuthorization)
            {
                report.Error("write_tools_without_authorizer",
                    $"已启用 {Count(writeCount)} 个写类工具，但未注册 {nameof(IToolExecutionAuthorizer)} "
                    + $"且 {PolicyPrefix}.{nameof(FeishuAgentOptions.EnforceToolAuthorization)}=true"
                    + "——每次写调用必然 authorization_denied（安全默认，不会静默放行）");
            }
            else
            {
                report.Warn("authorization_not_enforced",
                    $"已启用 {Count(writeCount)} 个写类工具，但未注册 {nameof(IToolExecutionAuthorizer)} 且 "
                    + $"{PolicyPrefix}.{nameof(FeishuAgentOptions.EnforceToolAuthorization)}=false"
                    + "——写类工具不经过任何服务端门禁即执行");
            }
        }

        if (authorizer is not null && approvalChannel is null)
        {
            report.Warn("approval_channel_missing",
                $"已注册 {nameof(IToolExecutionAuthorizer)} 但未注册 {nameof(IFeishuToolApprovalChannel)}"
                + "——授权器返回 NeedsUserConfirmation 时模型只会收到中性拒绝，宿主收不到任何待批准通知（HITL 闭环不成立）");
        }

        if (auditSink is null && enabled.Count > 0)
        {
            report.Info("audit_sink_missing",
                $"{nameof(IToolExecutionAuditSink)} 未注册（SDK 内建默认即无实现，OTel Span 已覆盖基础审计）"
                + "——宿主侧需要落库审计时请自行注册");
        }

        if (string.Equals(options.ContentSafetyMode, ContentSafetyModes.Off, StringComparison.Ordinal))
        {
            report.Warn("content_safety_off",
                $"{PolicyPrefix}.{nameof(FeishuAgentOptions.ContentSafetyMode)}=off"
                + $"——出站内容安全扫描不生效（默认 '{ContentSafetyModes.Warn}'）；强制净化不受影响");
        }

        ProbeUserToolWiring(report, options, enabled, currentUserContext, tokenResolver, appManager);

        if (enabled.Count > 0)
        {
            var appKeys = appManager?.ConfiguredAppKeys;
            report.Fact($"{HostPrefix}.apps_configured", Count(appKeys?.Count ?? 0));
            if (appKeys is null || appKeys.Count == 0)
            {
                report.Warn("no_apps_configured",
                    $"已启用 {Count(enabled.Count)} 个工具但没有已配置应用"
                    + $"（{nameof(IFeishuAppManager)} 缺席或 {nameof(IFeishuAppManager.ConfiguredAppKeys)} 为空）"
                    + "——运行期需要会话上下文解析 appKey，缺失时工具调用被结构化拒绝");
            }
        }
    }

    /// <summary>user 身份工具的接线（缺席 = 必然拒绝，探测缺席 = fail-open 放行）。</summary>
    private void ProbeUserToolWiring(
        Collector report,
        FeishuAgentOptions options,
        IReadOnlyList<FeishuToolDefinition> enabled,
        IFeishuCurrentUserContext? currentUserContext,
        IFeishuTokenManagerResolver? tokenResolver,
        IFeishuAppManager? appManager)
    {
        var userTools = enabled
            .Where(static t => string.Equals(t.Identity, FeishuToolIdentityNames.User, StringComparison.Ordinal))
            .Select(static t => t.Name)
            .OrderBy(static n => n, StringComparer.Ordinal)
            .ToArray();
        if (userTools.Length == 0)
        {
            return;
        }

        report.Fact("tools.enabled_user_identity", Count(userTools.Length));

        if (currentUserContext is null)
        {
            report.Error("user_tool_wiring_missing",
                $"已启用 user 身份工具：{Names(userTools)}，但 {nameof(IFeishuCurrentUserContext)} 未注册"
                + "——每次调用必然 authorization_denied（令牌缓存查找键无来源）");
        }

        if (tokenResolver is null)
        {
            report.Warn("user_token_resolver_missing",
                $"已启用 user 身份工具，但 {nameof(IFeishuTokenManagerResolver)} 未注册"
                + "——用户令牌就绪探测 fail-open 放行，缺令牌时由下游 401 兜底（不再有 user_auth_required 语义）");
        }
        else if (string.Equals(options.UserTokenPrecheck, UserTokenPrecheckModes.Off, StringComparison.Ordinal))
        {
            report.Warn("user_token_precheck_off",
                $"{PolicyPrefix}.{nameof(FeishuAgentOptions.UserTokenPrecheck)}=off——前置探测不执行，user_auth_required 语义不生效");
        }
        else if (appManager?.ConfiguredAppKeys is { Count: > 0 } appKeys)
        {
            // 令牌「缓存健康」的可得口径 = 管理器可达性（按序取首个已配置应用，保证重复调用逐字节相同）。
            // 有效期需要触达存储并可能触发刷新，一条诊断命令不该变成一次真实调用。
            var appKey = appKeys.OrderBy(static k => k, StringComparer.Ordinal).First();
            if (tokenResolver.TryGetUserTokenManager(appKey) is null)
            {
                report.Warn("user_token_manager_unreachable",
                    $"应用 '{appKey}' 的用户令牌管理器不可达（未初始化 / 已退休 / 未配置 OAuth 凭据）"
                    + "——该应用上的 user 工具探测 fail-open 放行");
            }
        }
    }

    /// <summary>
    /// guidance 预算余量与整域丢弃——用与运行期同一份装配实现（<see cref="DomainGuidanceBlocks"/>）
    /// 和同一个中性装配器，故读数就是真实下发的那一份。
    /// </summary>
    private void ProbeGuidance(
        Collector report, FeishuAgentOptions options, FeishuToolRegistry? registry)
    {
        var blocks = DomainGuidanceBlocks.Create(_services, registry);
        var composed = GuidanceComposer.Compose(options.Instructions, blocks, FeishuGuidanceBudget.MaxGuidanceLength);
        var total = composed.Instructions.Length;
        var headroom = FeishuGuidanceBudget.MaxGuidanceLength - total;

        report.Fact("guidance.domains", Count(blocks.Count));
        report.Fact("guidance.host_instruction_chars", Count(options.Instructions.Length));
        report.Fact("guidance.composed_chars", Count(total));
        report.Fact("guidance.budget_chars", Count(FeishuGuidanceBudget.MaxGuidanceLength));
        report.Fact("guidance.headroom_chars", Count(headroom));
        report.Fact("guidance.headroom_percent", Percent(headroom, FeishuGuidanceBudget.MaxGuidanceLength));
        report.Fact("guidance.omitted_domains", composed.OmittedDomains.Count == 0
            ? string.Empty
            : Names(composed.OmittedDomains.ToArray()));

        if (composed.Truncated)
        {
            report.Error("guidance_domains_omitted",
                $"指令装配超过预算 {Count(FeishuGuidanceBudget.MaxGuidanceLength)} 字符，已整域丢弃 "
                + $"{Count(composed.OmittedDomains.Count)} 个域：{Names(composed.OmittedDomains.ToArray())}"
                + $"——这些域的避坑文本对模型完全不可见；请改用 {PolicyPrefix}.{nameof(FeishuAgentOptions.GuidanceMode)}="
                + $"'{GuidanceModes.Routing}' 或收窄启用域");
        }
        else if (headroom * 100 < FeishuGuidanceBudget.MaxGuidanceLength * BudgetLowWatermarkPercent)
        {
            report.Warn("guidance_budget_low",
                $"guidance 预算余量仅 {Percent(headroom, FeishuGuidanceBudget.MaxGuidanceLength)}%（低于 "
                + $"{Count(BudgetLowWatermarkPercent)}%）——再启用一个域即触发整域丢弃");
        }
    }

    /// <summary>策略轴读数前缀（配置节名同词汇，宿主可直接对照 appsettings）。</summary>
    private const string PolicyPrefix = "policy";

    /// <summary>宿主扩展点读数前缀。</summary>
    private const string HostPrefix = "host";

    private static string Domain(FeishuToolDefinition tool)
    {
        var index = tool.Name.IndexOf(".", StringComparison.Ordinal);
        return index > 0 ? tool.Name.Substring(0, index) : tool.Name;
    }

    private static string Count(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Bool(bool value) => value ? "true" : "false";

    private static string Percent(int part, int whole)
        => whole <= 0
            ? "0"
            : (part * 100 / whole).ToString(CultureInfo.InvariantCulture);

    /// <summary>名字清单正文（超上限收口为 <c>…(+N)</c>，报告本身不得失控）。</summary>
    private static string Names(string[] names)
        => names.Length <= MaxListedNames
            ? string.Join(",", names)
            : string.Join(",", names.Take(MaxListedNames)) + $"…(+{Count(names.Length - MaxListedNames)})";

    /// <summary>结论与读数的收集器（一次 <see cref="Probe"/> 一个实例，故重复调用互不干扰）。</summary>
    private sealed class Collector
    {
        private readonly List<FeishuDiagnosticsFinding> _findings = new();

        private readonly List<FeishuDiagnosticsFact> _facts = new();

        public void Fact(string name, string value) => _facts.Add(new FeishuDiagnosticsFact(name, value));

        public void Info(string code, string message) => Add(FeishuDiagnosticsSeverities.Info, code, message);

        public void Warn(string code, string message) => Add(FeishuDiagnosticsSeverities.Warning, code, message);

        public void Error(string code, string message) => Add(FeishuDiagnosticsSeverities.Error, code, message);

        private void Add(string severity, string code, string message)
            => _findings.Add(new FeishuDiagnosticsFinding(severity, code, message));

        public FeishuDiagnosticsReport Build() => new(_findings.ToArray(), _facts.ToArray());
    }
}
