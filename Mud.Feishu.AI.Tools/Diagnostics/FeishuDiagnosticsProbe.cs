// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任。
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mud.AI.Abstractions.Observability;
using Mud.AI.Agent.Tools;
using Mud.Feishu.AI.AgentTools;
using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Diagnostics;
using Mud.Feishu.AI.Tools.Catalog;
using Mud.Feishu.AI.Tools.Hosting;

namespace Mud.Feishu.AI.Tools.Diagnostics;

/// <summary>诊断项状态（<see cref="Fail"/> 表示"该部署必然失效"，<see cref="Warn"/> 表示"能跑但能力缺失"）。</summary>
public enum FeishuDiagnosticsStatus
{
    /// <summary>通过。</summary>
    Ok,

    /// <summary>降级可跑（能力面缺席，功能受影响）。</summary>
    Warn,

    /// <summary>失败（配置/装配错误，工具调用必然不可用）。</summary>
    Fail,
}

/// <summary>一条诊断项的结论。</summary>
/// <param name="Name">诊断项标识（稳定字面量，宿主看板可聚合）。</param>
/// <param name="Status">结论状态。</param>
/// <param name="Detail">可读明细（含实测数字；不得含凭据/入参原文）。</param>
public sealed record FeishuDiagnosticsCheck(string Name, FeishuDiagnosticsStatus Status, string Detail);

/// <summary>诊断报告（整体状态取各项最坏值）。</summary>
/// <param name="GeneratedAt">生成时刻（UTC）。</param>
/// <param name="Status">整体状态。</param>
/// <param name="Checks">各项结论。</param>
public sealed record FeishuDiagnosticsReport(
    DateTimeOffset GeneratedAt,
    FeishuDiagnosticsStatus Status,
    IReadOnlyList<FeishuDiagnosticsCheck> Checks);

/// <summary>
/// 进程内自检面（E-2）：一条调用产出结构化诊断报告，供宿主做启动自检/运维巡检。
/// </summary>
/// <remarks>
/// <b>为什么是 API 而不是 CLI</b>：本包是库形态（不新增可执行进程，见 AGENTS.md 非目标），
/// 自检必须是宿主可编排的一次方法调用；Demo 的 <c>--doctor</c> 只是它的一个消费者。
/// <b>只读不写</b>：探针不改任何配置、不发任何网络请求（令牌缓存健康以"注册状态"表达，
/// 不在这里触发刷新——自检自身不得产生副作用）。
/// </remarks>
public interface IFeishuDiagnosticsProbe
{
    /// <summary>执行一次自检。</summary>
    /// <returns>结构化报告（同步、无 IO、无异常——失败以 <see cref="FeishuDiagnosticsStatus.Fail"/> 项表达）。</returns>
    FeishuDiagnosticsReport Probe();
}

/// <summary>
/// <see cref="IFeishuDiagnosticsProbe"/> 的默认实现：逐项检查装配面与配置面的一致性。
/// </summary>
/// <remarks>
/// <b>判据取向</b>：<see cref="FeishuDiagnosticsStatus.Fail"/> 只留给"装配错误导致工具必然不可用"
/// （无注册表 / 启用集为空 / 白名单含未注册工具）；能力面缺席（审计、用量、遥测、审批通道）
/// 一律 <see cref="FeishuDiagnosticsStatus.Warn"/>——它们让部署"盲"或"HITL 无处投递"，但调用链本身还通。
/// </remarks>
/// <param name="services">根服务提供器（只做 GetService，不创建作用域）。</param>
/// <param name="options">Agent 配置。</param>
public sealed class FeishuDiagnosticsProbe(
    IServiceProvider services,
    IOptions<FeishuAgentOptions> options) : IFeishuDiagnosticsProbe
{
    private readonly IServiceProvider _services = services ?? throw new ArgumentNullException(nameof(services));
    private readonly FeishuAgentOptions _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;

    /// <inheritdoc />
    public FeishuDiagnosticsReport Probe()
    {
        var checks = new List<FeishuDiagnosticsCheck>
        {
            ProbeRegistry(out var registry),
            ProbeWhitelist(registry),
            ProbeApprovalChannel(registry),
            ProbeAuthorizer(),
            ProbeAuditSink(),
            ProbeUsageMeter(),
            ProbeTelemetry(),
            ProbeGuidanceBudget(registry),
        };

        var status = checks.Any(c => c.Status == FeishuDiagnosticsStatus.Fail)
            ? FeishuDiagnosticsStatus.Fail
            : checks.Any(c => c.Status == FeishuDiagnosticsStatus.Warn)
                ? FeishuDiagnosticsStatus.Warn
                : FeishuDiagnosticsStatus.Ok;

        return new FeishuDiagnosticsReport(DateTimeOffset.UtcNow, status, checks);
    }

    /// <summary>注册表装配：缺席或启用集为空都是"必然不可用"（模型看不到任何工具）。</summary>
    private FeishuDiagnosticsCheck ProbeRegistry(out FeishuToolRegistry? registry)
    {
        registry = _services.GetService<FeishuToolRegistry>();
        if (registry is null)
        {
            return new FeishuDiagnosticsCheck(
                "toolSurface.registry",
                FeishuDiagnosticsStatus.Fail,
                "未装配工具面（FeishuToolRegistry 缺席）：请先 AddFeishuToolInfrastructure + AddFeishuTools");
        }

        var enabled = registry.EnabledTools.Count;
        return enabled == 0
            ? new FeishuDiagnosticsCheck(
                "toolSurface.registry",
                FeishuDiagnosticsStatus.Fail,
                $"注册表有 {registry.AllTools.Count} 枚工具但启用集为空（检查 Tools / WriteAllowList 配置）")
            : new FeishuDiagnosticsCheck(
                "toolSurface.registry",
                FeishuDiagnosticsStatus.Ok,
                $"已启用 {enabled} / 已注册 {registry.AllTools.Count}");
    }

    /// <summary>白名单漂移：配置里写了未注册的工具名 = 拼写漂移（静默失效），必须响亮。</summary>
    private FeishuDiagnosticsCheck ProbeWhitelist(FeishuToolRegistry? registry)
    {
        if (registry is null)
        {
            return new FeishuDiagnosticsCheck("toolSurface.whitelist", FeishuDiagnosticsStatus.Fail, "注册表缺席，无法校验白名单");
        }

        var configured = _options.Tools.Concat(_options.WriteAllowList).Where(static n => !string.IsNullOrWhiteSpace(n)).ToArray();
        if (configured.Length == 0)
        {
            return new FeishuDiagnosticsCheck(
                "toolSurface.whitelist", FeishuDiagnosticsStatus.Ok, "未声明白名单（注册表默认全部启用）");
        }

        var unknown = configured
            .Where(name => !registry.TryGet(name, out _))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static n => n, StringComparer.Ordinal)
            .ToArray();

        return unknown.Length > 0
            ? new FeishuDiagnosticsCheck(
                "toolSurface.whitelist",
                FeishuDiagnosticsStatus.Fail,
                $"白名单含 {unknown.Length} 个未注册工具名（不会生效）：{string.Join(", ", unknown)}")
            : new FeishuDiagnosticsCheck(
                "toolSurface.whitelist", FeishuDiagnosticsStatus.Ok, $"白名单 {configured.Length} 项全部命中注册表");
    }

    /// <summary>写工具已启用但审批通道缺席：HITL 请求无处投递（写操作会停在等待态）。</summary>
    private FeishuDiagnosticsCheck ProbeApprovalChannel(FeishuToolRegistry? registry)
    {
        var writeEnabled = registry?.EnabledTools.Count(static t => t.IsWrite) ?? 0;
        if (writeEnabled == 0)
        {
            return new FeishuDiagnosticsCheck(
                "approval.channel", FeishuDiagnosticsStatus.Ok, "未启用写工具，审批通道不参与");
        }

        var channel = _services.GetService<IFeishuToolApprovalChannel>();
        return channel is null
            ? new FeishuDiagnosticsCheck(
                "approval.channel",
                FeishuDiagnosticsStatus.Warn,
                $"已启用 {writeEnabled} 枚写工具但未注册 IFeishuToolApprovalChannel：人工确认请求无法投递")
            : new FeishuDiagnosticsCheck(
                "approval.channel", FeishuDiagnosticsStatus.Ok, $"{channel.GetType().Name}（写工具 {writeEnabled} 枚）");
    }

    /// <summary>授权器：<c>EnforceToolAuthorization=true</c> 时缺席即"全拒"，属 Fail。</summary>
    private FeishuDiagnosticsCheck ProbeAuthorizer()
    {
        var authorizer = _services.GetService<IToolExecutionAuthorizer>();
        if (authorizer is not null)
        {
            return new FeishuDiagnosticsCheck(
                "security.authorizer", FeishuDiagnosticsStatus.Ok, authorizer.GetType().Name);
        }

        return _options.EnforceToolAuthorization
            ? new FeishuDiagnosticsCheck(
                "security.authorizer",
                FeishuDiagnosticsStatus.Fail,
                "EnforceToolAuthorization=true 但未注册 IToolExecutionAuthorizer：全部工具调用会被拒绝")
            : new FeishuDiagnosticsCheck(
                "security.authorizer", FeishuDiagnosticsStatus.Warn, "未注册授权器（已关闭强制授权）");
    }

    /// <summary>审计出口缺席 = 调用不可追溯（能力面 Warn）。</summary>
    private FeishuDiagnosticsCheck ProbeAuditSink()
    {
        var sink = _services.GetService<IToolExecutionAuditSink>();
        return sink is null
            ? new FeishuDiagnosticsCheck(
                "observability.auditSink", FeishuDiagnosticsStatus.Warn, "未注册 IToolExecutionAuditSink：工具调用不可追溯")
            : new FeishuDiagnosticsCheck(
                "observability.auditSink", FeishuDiagnosticsStatus.Ok, sink.GetType().Name);
    }

    /// <summary>成本核算出口缺席 = 拿不到按会话/工具的用量（能力面 Warn）。</summary>
    private FeishuDiagnosticsCheck ProbeUsageMeter()
    {
        var meter = _services.GetService<IFeishuUsageMeter>();
        return meter is null
            ? new FeishuDiagnosticsCheck(
                "observability.usageMeter", FeishuDiagnosticsStatus.Warn, "未注册 IFeishuUsageMeter：无成本核算聚合")
            : new FeishuDiagnosticsCheck(
                "observability.usageMeter", FeishuDiagnosticsStatus.Ok, meter.GetType().Name);
    }

    /// <summary>遥测端口缺席 = 无 Span/指标（能力面 Warn；中立侧有 Noop 兜底故不是 Fail）。</summary>
    private FeishuDiagnosticsCheck ProbeTelemetry()
    {
        var telemetry = _services.GetService<IAgentTelemetry>();
        var toolTelemetry = _services.GetService<IAgentToolTelemetry>();
        if (telemetry is null)
        {
            return new FeishuDiagnosticsCheck(
                "observability.telemetry", FeishuDiagnosticsStatus.Warn, "未注册 IAgentTelemetry（中立侧回落 Noop）");
        }

        return toolTelemetry is null
            ? new FeishuDiagnosticsCheck(
                "observability.telemetry",
                FeishuDiagnosticsStatus.Warn,
                $"{telemetry.GetType().Name} 未实现 IAgentToolTelemetry：工具面埋点缺失")
            : new FeishuDiagnosticsCheck(
                "observability.telemetry",
                FeishuDiagnosticsStatus.Ok,
                $"{telemetry.GetType().Name}（含工具面端口）");
    }

    /// <summary>guidance 预算余量：全文形态总长 vs 预算，以及"已启用域无 L1"的覆盖缺口。</summary>
    private FeishuDiagnosticsCheck ProbeGuidanceBudget(FeishuToolRegistry? registry)
    {
        var domains = FeishuToolGuidance.ByDomain;
        var totalChars = domains.Values.Sum(static v => v.Length);
        var headroom = FeishuGuidanceBudget.MaxGuidanceLength - totalChars;

        var enabledDomains = (registry?.EnabledTools ?? [])
            .Select(static t => ToolPolicyMatcher.DomainOf(t.Name))
            .Where(static d => d.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var missing = enabledDomains
            .Where(d => !domains.ContainsKey(d))
            .OrderBy(static d => d, StringComparer.Ordinal)
            .ToArray();

        var detail = $"域 {domains.Count} 篇 / {totalChars} 字符，预算 {FeishuGuidanceBudget.MaxGuidanceLength}，余量 {headroom}";

        if (missing.Length > 0)
        {
            return new FeishuDiagnosticsCheck(
                "guidance.budget",
                FeishuDiagnosticsStatus.Warn,
                detail + $"；已启用域缺 L1 guidance：{string.Join(", ", missing)}");
        }

        // 只在「真的会丢域」时告警：full 形态超预算会整域丢弃尾部域；routing 形态（默认）按域摘要下发，不受该预算约束。
        if (headroom < 0 && string.Equals(_options.GuidanceMode, GuidanceModes.Full, StringComparison.Ordinal))
        {
            return new FeishuDiagnosticsCheck(
                "guidance.budget", FeishuDiagnosticsStatus.Warn, detail + "（full 形态会丢尾部域——改用 routing）");
        }

        return new FeishuDiagnosticsCheck(
            "guidance.budget",
            FeishuDiagnosticsStatus.Ok,
            headroom < 0 ? detail + $"（full 形态会超预算，当前模式 {_options.GuidanceMode}）" : detail);
    }
}
