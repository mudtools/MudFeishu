// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions.Conversations;
using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Channels;
using Mud.Feishu.AI.AgentTools;
using Mud.Feishu.AI.Tools;
using Mud.Feishu.AI.Tools.Generated;

namespace Mud.Feishu.Agent.Demo;

/// <summary>一条已被识别的斜杠命令。</summary>
/// <param name="Name">规范化后的命令名（不含 <c>/</c>，别名已归一）。</param>
/// <param name="Argument">命令参数（原始文本，未解析；无参数时为空串）。</param>
internal sealed record SlashCommand(string Name, string Argument);

/// <summary>
/// 控制台 REPL（L5 交互层）：斜杠命令分发、审批挂起态管理、剧本自动应答、流式渲染。
/// </summary>
/// <remarks>
/// <para>
/// <b>ADR-07：命令与模型工具面完全隔离</b>——斜杠命令在进入 <c>agent.RunStreamingAsync</c> <b>之前</b>拦截，
/// <b>不发给模型</b>。理由：① 避免命令文本消耗 token 与污染会话历史；② 命令需要同步访问 Demo 侧状态
/// （授权策略表、审计 sink、挂起审批表），而这些在模型执行期不可达。
/// 人机边界 = 「以 <c>/</c> 开头且首 token 匹配已知命令」；其余以 <c>/</c> 开头的输入<b>照常发给模型</b>
/// （模型侧可能真的有 <c>/help</c> 类需求）。
/// </para>
/// <para>
/// <b>R5-12 挂起态守卫（本类最重要的安全逻辑）</b>：发起确认的那一轮会把「待应答审批请求」写进会话历史，
/// MEAI <c>FunctionInvokingChatClient</c> 对整段入站历史做审批配对校验——<b>残留未应答的审批请求会让
/// 该会话后续每一轮直接抛 <c>InvalidOperationException</c></b>（工具未放行，但会话不可用）。
/// SDK 在<b>事件层</b>做了自愈（新的用户轮次放弃该待确认项），<b>控制台没有事件层</b>，
/// 因此本类必须自己收口：挂起态下<b>阻断新问题</b>，并给出三条出口
/// （<c>/approve</c> / <c>/deny</c> / <c>/abandon</c>）。
/// </para>
/// </remarks>
internal sealed class AgentConsoleLoop
{
    /// <summary>
    /// 控制台通道的 <c>chatId</c> 占位常量（控制台通道不调飞书 API，仅为满足 <see cref="IMessageChannel"/> 签名）。
    /// </summary>
    public const string LocalChatId = "local";

    /// <summary>
    /// 斜杠命令全集（规范化名；与设计文档 §5.2 命令表逐行对应）。
    /// </summary>
    /// <remarks>
    /// <b>契约守卫消费点</b>：<c>DocAgentDemoContractGuards</c> 断言本数组与命令分发实现一致，
    /// 防止"文档列了命令但代码没实现"或反之的漂移。
    /// </remarks>
    internal static readonly string[] CommandNames =
    [
        "help", "tools", "guidance", "pending", "approve", "deny", "abandon",
        "policy", "dryrun", "audit", "export", "usage", "history", "reset",
        "scenario", "verbose", "exit",
    ];

    /// <summary>别名 → 规范名映射（含 <c>?</c> 与 <c>quit</c>）。</summary>
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.Ordinal)
    {
        ["?"] = "help",
        ["t"] = "tools",
        ["g"] = "guidance",
        ["p"] = "pending",
        ["y"] = "approve",
        ["n"] = "deny",
        ["e"] = "export",
        ["u"] = "usage",
        ["h"] = "history",
        ["r"] = "reset",
        ["s"] = "scenario",
        ["v"] = "verbose",
        ["quit"] = "exit",
    };

    private readonly DocAgentSettings _settings;
    private readonly FeishuAgent _agent;
    private readonly IFeishuToolContextAccessor _toolContextAccessor;
    private readonly IConversationGate _conversationGate;
    private readonly IConversationStore _conversationStore;
    private readonly IMessageChannel _channel;
    private readonly ConsoleMessageChannel _consoleChannel;
    private readonly ConsoleRenderer _renderer;
    private readonly IFeishuToolApprovalChannel _approvalChannel;
    private readonly ConsoleApprovalChannelState _approvalState;
    private readonly ConsoleToolAuthorizerState _authorizerState;
    private readonly InMemoryAuditSink _auditSink;
    private readonly FeishuToolRegistry _registry;
    private readonly ScenarioBook _scenarioBook;
    private readonly ILogger? _logger;

    private readonly string _conversationKey;
    private readonly Queue<string> _autoAnswers = new();
    private readonly Queue<string> _recentTurns = new();

    private AgentScenario? _activeScenario;
    private bool _dryRunPrompting;
    private bool _exitConfirmed;
    private long _inputTokens;
    private long _outputTokens;
    private int _turnCount;

    /// <summary>
    /// 初始化 REPL。
    /// </summary>
    /// <param name="settings">配置。</param>
    /// <param name="agent">飞书 Agent（单例；审批续跑必须用同一实例）。</param>
    /// <param name="toolContextAccessor">工具执行上下文访问器（每轮重建租户上下文）。</param>
    /// <param name="conversationGate">会话闸门（同键串行；单线程 REPL 下无实际竞争，但保留标准装配）。</param>
    /// <param name="conversationStore">会话存储（<c>/reset</c> 与 <c>/abandon</c> 消费）。</param>
    /// <param name="channel">回复通道（<see cref="IMessageChannel"/>）。</param>
    /// <param name="consoleChannel">控制台通道具体类型（诊断用）。</param>
    /// <param name="renderer">终端渲染器。</param>
    /// <param name="approvalChannel">宿主批准通道。</param>
    /// <param name="approvalState">挂起审批登记表。</param>
    /// <param name="authorizerState">授权策略可变状态（<c>/policy</c> 消费）。</param>
    /// <param name="auditSink">审计 sink（<c>/audit</c>、<c>/export</c>、<c>/usage</c> 消费）。</param>
    /// <param name="registry">工具注册表（<c>/tools</c> 消费）。</param>
    /// <param name="scenarioBook">剧本注册表。</param>
    /// <param name="logger">日志（可空）。</param>
    public AgentConsoleLoop(
        DocAgentSettings settings,
        FeishuAgent agent,
        IFeishuToolContextAccessor toolContextAccessor,
        IConversationGate conversationGate,
        IConversationStore conversationStore,
        IMessageChannel channel,
        ConsoleMessageChannel consoleChannel,
        ConsoleRenderer renderer,
        IFeishuToolApprovalChannel approvalChannel,
        ConsoleApprovalChannelState approvalState,
        ConsoleToolAuthorizerState authorizerState,
        InMemoryAuditSink auditSink,
        FeishuToolRegistry registry,
        ScenarioBook scenarioBook,
        ILogger? logger = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _toolContextAccessor = toolContextAccessor ?? throw new ArgumentNullException(nameof(toolContextAccessor));
        _conversationGate = conversationGate ?? throw new ArgumentNullException(nameof(conversationGate));
        _conversationStore = conversationStore ?? throw new ArgumentNullException(nameof(conversationStore));
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        _consoleChannel = consoleChannel ?? throw new ArgumentNullException(nameof(consoleChannel));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _approvalChannel = approvalChannel ?? throw new ArgumentNullException(nameof(approvalChannel));
        _approvalState = approvalState ?? throw new ArgumentNullException(nameof(approvalState));
        _authorizerState = authorizerState ?? throw new ArgumentNullException(nameof(authorizerState));
        _auditSink = auditSink ?? throw new ArgumentNullException(nameof(auditSink));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _scenarioBook = scenarioBook ?? throw new ArgumentNullException(nameof(scenarioBook));
        _logger = logger;

        // 会话键：{prefix}:{appKey}:conversation:user:{subjectId}（/help 会打印完整键以展示键布局）。
        _conversationKey = ConversationKeyBuilder.Build(
            settings.AppKey, ConversationScope.P2P(), settings.UserId);
    }

    /// <summary>本 Demo 使用的会话键（<c>/help</c> 与审计展示用）。</summary>
    public string ConversationKey => _conversationKey;

    /// <summary>
    /// 解析一行输入是否为已知斜杠命令（ADR-07 的人机边界判定）。
    /// </summary>
    /// <param name="input">用户原始输入。</param>
    /// <param name="command">解析结果（命中时有效）。</param>
    /// <returns>是否命中已知命令。</returns>
    internal static bool IsSlashCommand(string input, out SlashCommand command)
    {
        command = new SlashCommand(string.Empty, string.Empty);
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var trimmed = NormalizeInput(input);
        if (trimmed.Length < 2 || trimmed[0] != '/')
        {
            return false;
        }

        var spaceIndex = trimmed.IndexOf(' ');
        var token = spaceIndex < 0 ? trimmed[1..] : trimmed[1..spaceIndex];
        var argument = spaceIndex < 0 ? string.Empty : trimmed[(spaceIndex + 1)..].Trim();
        var name = token.ToLowerInvariant();

        if (Aliases.TryGetValue(name, out var canonical))
        {
            name = canonical;
        }

        if (!CommandNames.Contains(name, StringComparer.Ordinal))
        {
            // 未命中已知命令：照常发给模型（模型侧可能真需要 "/xyz" 这类输入）。
            return false;
        }

        command = new SlashCommand(name, argument);
        return true;
    }

    /// <summary>
    /// 挂起态下是否应阻断新问题（R5-12 守卫的纯函数形态，供单测直接断言）。
    /// </summary>
    /// <param name="hasPendingApproval">是否存在挂起审批。</param>
    /// <returns>是否阻断。</returns>
    internal static bool ShouldBlockTurn(bool hasPendingApproval) => hasPendingApproval;

    /// <summary>
    /// 运行 REPL 主循环。
    /// </summary>
    /// <param name="cancellationToken">取消令牌（Ctrl+C 触发；取消当前轮而非硬杀进程）。</param>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _renderer.Notice(NoticeLevel.Info, "输入 /help 查看命令；输入 /exit 或 Ctrl+C 退出。");

        while (!cancellationToken.IsCancellationRequested && !_renderer.ExitRequested)
        {
            _renderer.InputPrompt();
            var input = Console.ReadLine();
            if (input is null)
            {
                break;
            }

            var text = NormalizeInput(input);
            if (text.Length == 0)
            {
                continue;
            }

            // 终端自身的回显不经我们手，无法过滤其控制码；凡原始输入含控制字符即再打印一次净化副本，
            // 让操作者看到"实际被捕获的文本"（飞书文档正文是常见粘贴来源，属不可信输入）。
            if (ContainsControlCharacters(input))
            {
                _renderer.UserEcho(input);
                _renderer.Notice(NoticeLevel.Warn, "输入含控制字符，已净化后再处理（终端原始回显无法拦截）。");
            }

            if (IsSlashCommand(text, out var command))
            {
                await DispatchCommandAsync(command, cancellationToken).ConfigureAwait(false);
                if (_renderer.ExitRequested)
                {
                    break;
                }

                continue;
            }

            // R5-12 守卫：挂起态下不得累积新的待应答审批（否则一次 /abandon 无法清理多个框架记录）。
            if (ShouldBlockTurn(_approvalState.HasPending))
            {
                _renderer.Notice(
                    NoticeLevel.Warn,
                    $"仍有 {_approvalState.Count} 个待确认写操作，已阻断本轮。"
                    + "请先 /approve <关联号>、/deny <关联号>，或 /abandon 放弃并重置会话"
                    + "（否则该会话后续轮次会因未应答审批而不可用）。");
                continue;
            }

            await RunTurnAsync(text, cancellationToken).ConfigureAwait(false);
            await DrainAutoAnswersAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    // ──────────────────────────── 命令分发 ────────────────────────────

    private async Task DispatchCommandAsync(SlashCommand command, CancellationToken cancellationToken)
    {
        // 除 /exit 外任何命令都视为"用户仍在操作"，重置退出确认态。
        if (!string.Equals(command.Name, "exit", StringComparison.Ordinal))
        {
            _exitConfirmed = false;
        }

        switch (command.Name)
        {
            case "help":
                PrintHelp();
                break;
            case "tools":
                PrintTools();
                break;
            case "guidance":
                PrintGuidance(command.Argument);
                break;
            case "pending":
                PrintPending();
                break;
            case "approve":
                await ApproveAsync(command.Argument, approved: true, cancellationToken).ConfigureAwait(false);
                break;
            case "deny":
                await ApproveAsync(command.Argument, approved: false, cancellationToken).ConfigureAwait(false);
                break;
            case "abandon":
                await AbandonAsync(cancellationToken).ConfigureAwait(false);
                break;
            case "policy":
                SetPolicy(command.Argument);
                break;
            case "dryrun":
                SetDryRun(command.Argument);
                break;
            case "audit":
                PrintAudit();
                break;
            case "export":
                ExportAudit();
                break;
            case "usage":
                PrintUsage();
                break;
            case "history":
                PrintHistory();
                break;
            case "reset":
                await ResetAsync(cancellationToken).ConfigureAwait(false);
                break;
            case "scenario":
                await HandleScenarioAsync(command.Argument, cancellationToken).ConfigureAwait(false);
                break;
            case "verbose":
                ToggleVerbose();
                break;
            case "exit":
                Exit();
                break;
            default:
                _renderer.Notice(NoticeLevel.Warn, $"未知命令：/{command.Name}（/help 查看命令表）");
                break;
        }
    }

    private void PrintHelp()
    {
        _renderer.Table(
            "斜杠命令（首 token 命中才拦截；其余以 / 开头的输入照常发给模型）",
            ["命令", "别名", "说明", "副作用"],
            [
                ["/help", "?", "本帮助 + 当前关键配置", "无"],
                ["/tools", "t", "分组列出已启用工具（只读/写/元工具）", "无"],
                ["/guidance <域>", "g", "打印该域 guidance 全文", "无"],
                ["/pending", "p", "列出待人工确认的写操作", "无"],
                ["/approve <id>", "y", "批准并续跑（写操作真实执行）", "执行写操作"],
                ["/deny <id>", "n", "拒绝并以中性结果续跑", "不执行写操作"],
                ["/abandon", "-", "放弃全部挂起项并重置会话", "清空会话历史"],
                ["/policy [值]", "-", $"查看/切换授权策略（{string.Join("/", DocAgentSettings.Policies)}）", "影响后续判定"],
                ["/dryrun 开关", "-", "切换提示词级 dry-run 引导（on / off）", "下一轮生效"],
                ["/audit", "-", "打印审计表（最近 20 条）", "无"],
                ["/export", "e", "导出全量审计为 JSONL", "落盘"],
                ["/usage", "u", "累计 token 与工具调用计数", "无"],
                ["/history", "h", "会话轮次与最近 5 轮摘要", "无"],
                ["/reset", "r", "丢弃会话并新建（有挂起项时被拒）", "清空会话历史"],
                ["/scenario [名]", "s", "列清单 / 注入剧本（clear 清空）", "可能触发写操作"],
                ["/verbose", "v", "切换工具结果全文与 Trace", "无"],
                ["/exit", "quit", "退出（有挂起项时需二次确认）", "退出"],
            ]);

        var guidance = _agent.Guidance;
        var guidanceLength = Math.Max(0, guidance.Instructions.Length - DocAgentPrompt.Instructions.Length);
        _renderer.Table(
            "当前关键配置",
            ["项", "值"],
            [
                ["模式", DocAgentSettings.ConfigEnabledKey + "=true（文档业务智能体）"],
                ["应用键", _settings.AppKey],
                ["模型", $"{_settings.ModelId}{(_settings.Endpoint is null ? "（默认端点）" : " @ " + _settings.Endpoint)}"],
                ["已启用工具", $"{_registry.EnabledTools.Count} 个（只读 {DocAgentSettings.ReadonlyTools.Length} / 写 {DocAgentSettings.WriteTools.Length}）"],
                ["身份闭集", "tenant（文档业务域全部 tenant；这是按域事实推导，不是复制粘贴）"],
                ["风险上限", "high-risk-write（允许 docx.delete_blocks 进入审批管线）"],
                ["授权强制", "on（写工具未注册授权器即拒绝 → 本 Demo 已注册 ConsoleToolAuthorizer）"],
                ["授权策略", _authorizerState.Current],
                ["工具结果截断", "4000 字符（MaxToolResultLength）"],
                ["会话记忆", $"条数 50 / token 8000 / 摘要阈值 {_settings.SummaryThreshold}"],
                ["guidance", $"{guidance.IncludedDomains.Count} 个域，共 {guidanceLength} / {FeishuGuidanceComposer.MaxGuidanceLength} 字符"
                    + (guidance.Truncated ? $"（已截断，丢弃：{string.Join("、", guidance.OmittedDomains)}）" : "（未截断）")],
                ["回复通道", $"控制台（唯一通道；分片阈值 {_consoleChannel.ChunkLength} 字符 / ≥{ConsoleMessageChannel.MinUpdateIntervalMs}ms）"],
                ["会话键", _conversationKey],
                ["会话闸门", "KeyedConversationGate（同键串行；单线程 REPL 下无实际竞争，保留以展示标准装配）"],
                ["剧本", $"{_scenarioBook.All.Count} 部（/scenario 列清单，/scenario kb 直达）"],
                ["dry-run 引导", _dryRunPrompting ? "on" : "off"],
            ]);
    }

    private void PrintTools()
    {
        var rows = _registry.EnabledTools
            .OrderBy(static t => DomainOf(t.Name), StringComparer.Ordinal)
            .ThenByDescending(static t => t.IsWrite)
            .ThenBy(static t => t.Name, StringComparer.Ordinal)
            .Select(t => new[]
            {
                t.Name,
                t.IsWrite ? "写" : "只读",
                FeishuToolRiskNames.ToLiteral(t.Risk),
                t.Identity,
                t.RequiredScopes.Count == 0 ? "-" : string.Join("、", t.RequiredScopes),
            })
            .ToArray();

        _renderer.Table(
            $"已启用工具（{rows.Length} 个）",
            ["工具名", "类别", "风险", "身份", "权限点"],
            rows);
        _renderer.Notice(NoticeLevel.Info, "全部写工具均经框架审批管线（ApprovalRequiredAIFunction）包装，且执行期仍受授权器二次把关。");
    }

    private void PrintGuidance(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            _renderer.Notice(
                NoticeLevel.Warn,
                $"/guidance 需要一个域名参数：{(FeishuToolGuidance.ByDomain.Count == 0 ? "（未生成任何域资产）" : string.Join("、", FeishuToolGuidance.ByDomain.Keys.OrderBy(static k => k, StringComparer.Ordinal)))}");
            return;
        }

        var key = domain.Trim();
        if (!FeishuToolGuidance.ByDomain.TryGetValue(key, out var content))
        {
            _renderer.Notice(NoticeLevel.Warn, $"没有名为 {key} 的域 guidance 资产（/guidance 查看可用域）。");
            return;
        }

        var included = _agent.Guidance.IncludedDomains.Contains(key, StringComparer.Ordinal);
        _renderer.Notice(
            included ? NoticeLevel.Info : NoticeLevel.Warn,
            included
                ? $"域 {key} 的 guidance 已注入本次会话（{content.Length} 字符）。"
                : $"域 {key} 的 guidance 未注入本次会话（不在 IncludedDomains 内）——以下为原始资产全文。");

        // guidance 是编译期常量（可信），但仍统一走渲染器出口。
        _renderer.Block($"guidance · {key}（{content.Length} 字符）", content);
    }

    private void PrintPending()
    {
        var pending = _approvalState.Snapshot();
        if (pending.Count == 0)
        {
            _renderer.Notice(NoticeLevel.Info, "当前没有待人工确认的写操作。");
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var rows = pending.Select(p => new[]
        {
            p.CorrelationId,
            p.Framework.ToolName,
            p.Framework.ArgumentsDigest ?? "（无）",
            RemainingText(p.ExpiresAt - now),
        }).ToArray();

        _renderer.Table($"待人工确认（{pending.Count} 项）", ["关联号", "工具", "参数摘要", "剩余有效期"], rows);
        _renderer.Notice(NoticeLevel.Info, "/approve <关联号> 放行；/deny <关联号> 拒绝；/abandon 放弃并重置会话。");
    }

    private async Task ApproveAsync(string correlationId, bool approved, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            _renderer.Notice(
                NoticeLevel.Warn,
                $"/{(approved ? "approve" : "deny")} 需要一个关联号（/pending 查看）。");
            return;
        }

        var pending = _approvalState.FindByCorrelationId(correlationId.Trim());
        if (pending is null)
        {
            _renderer.Notice(NoticeLevel.Warn, $"没有找到挂起项 {correlationId}（/pending 查看）。");
            return;
        }

        if (DateTimeOffset.UtcNow > pending.ExpiresAt)
        {
            _renderer.Notice(NoticeLevel.Warn, "该挂起项已过有效期（框架侧已失配），/approve 不会执行任何操作。请 /abandon 重置会话后重试。");
            return;
        }

        // 先摘除：避免续跑期间 /pending 仍显示该项（续跑可能耗时较长）。
        _approvalState.Remove(pending.Framework.RequestId);
        _renderer.Notice(NoticeLevel.Info, approved
            ? $"已批准 {pending.CorrelationId}（{pending.Framework.ToolName}），续跑中…"
            : $"已拒绝 {pending.CorrelationId}（{pending.Framework.ToolName}），以中性结果续跑…");

        try
        {
            // SDK 闭环一次完成四步：同键同实例加载会话 → 取框架原始请求 → 重建租户上下文 → 续跑并落库。
            var response = await _agent.RunApprovalContinuationAsync(
                toolContextAccessor: _toolContextAccessor,
                approval: pending.Framework,
                approved: approved,
                reason: approved ? "控制台人工批准" : "控制台人工拒绝",
                conversationGate: _conversationGate,
                approvalChannel: _approvalChannel,
                logger: _logger,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            await RenderResponseAsync(response, cancellationToken).ConfigureAwait(false);

            if (_approvalState.HasPending)
            {
                _renderer.Notice(
                    NoticeLevel.Info,
                    $"续跑轮又产生了 {_approvalState.Count} 个新的待确认写操作（审批套审批，SDK 已防）——请继续 /pending 处理。");
            }
        }
        catch (InvalidOperationException ex)
        {
            // fail-closed：会话已被重建、该确认已被放弃或已回灌过 ⇒ 绝不凭空放行。
            _renderer.Notice(NoticeLevel.Warn, $"续跑失败（fail-closed，不凭空放行）：{ex.Message}");
        }
    }

    private async Task AbandonAsync(CancellationToken cancellationToken)
    {
        var cleared = _approvalState.Clear();
        ClearScenarioStack();
        await _conversationStore.DeleteAsync(_conversationKey, cancellationToken).ConfigureAwait(false);
        _ = await _agent.GetOrCreateSessionAsync(_conversationKey, cancellationToken).ConfigureAwait(false);
        _turnCount = 0;
        _recentTurns.Clear();
        _renderer.Notice(
            NoticeLevel.Info,
            $"已放弃 {cleared} 个挂起项并重置会话历史（等价于 SDK 事件层「新的用户轮次放弃待确认项」的保守替代）。");
    }

    private void SetPolicy(string argument)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            _renderer.Notice(
                NoticeLevel.Info,
                $"当前授权策略：{_authorizerState.Current}（可切换为 {string.Join(" / ", DocAgentSettings.Policies)}）。"
                + "strict 下高风险写工具会被授权器独立拒绝——这是「批准 ≠ 放行」的现场证据。");
            return;
        }

        var value = argument.Trim().ToLowerInvariant();
        if (!DocAgentSettings.Policies.Contains(value, StringComparer.Ordinal))
        {
            _renderer.Notice(NoticeLevel.Warn, $"策略取值非法：{argument}（合法值 {string.Join(" / ", DocAgentSettings.Policies)}）。");
            return;
        }

        var previous = _authorizerState.Current;
        _authorizerState.Current = value;
        _renderer.Notice(NoticeLevel.Info, $"授权策略：{previous} → {value}（立即影响后续轮次的授权器判定）。");
    }

    private void SetDryRun(string argument)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            _dryRunPrompting = !_dryRunPrompting;
        }
        else
        {
            var value = argument.Trim().ToLowerInvariant();
            switch (value)
            {
                case "on":
                case "true":
                    _dryRunPrompting = true;
                    break;
                case "off":
                case "false":
                    _dryRunPrompting = false;
                    break;
                default:
                    _renderer.Notice(NoticeLevel.Warn, $"/dryrun 参数非法：{argument}（on / off）。");
                    return;
            }
        }

        _renderer.Notice(
            NoticeLevel.Info,
            _dryRunPrompting
                ? "dry-run 引导：on（本轮起每轮附加「写工具先 dry_run 预演」的宿主指令）"
                : "dry-run 引导：off");
    }

    private void PrintAudit()
    {
        var recent = _auditSink.Recent(20);
        if (recent.Count == 0)
        {
            _renderer.Notice(NoticeLevel.Info, "审计记录为空（还没有任何工具调用）。");
            return;
        }

        var counts = _auditSink.CountByDecision();
        _renderer.Table(
            $"审计（最近 {recent.Count} 条 / 累计 {_auditSink.Count} 条：allowed {counts.Allowed}、denied {counts.Denied}、error {counts.Error}）",
            ["序号", "工具", "判定", "风险", "耗时", "会话键后 12 位", "参数摘要"],
            recent
                .Select((record, index) => new[]
                {
                    (index + 1).ToString(CultureInfo.InvariantCulture),
                    record.ToolName,
                    record.Decision,
                    ResolveRisk(record),
                    $"{record.DurationMs}ms",
                    TailKey(record.ConversationKey),
                    record.ArgsDigest ?? "-",
                })
                .ToArray());

        _renderer.Notice(
            NoticeLevel.Info,
            "审计记录由 SDK 生成：ArgsDigest 已脱敏（参数名 + 值长度 + 敏感键掩码），本 Demo 不接触原始参数；"
            + "SDK 记录不含时间戳，故此处以序号代替（序号即写入顺序）。");
    }

    private void ExportAudit()
    {
        if (_auditSink.Count == 0)
        {
            _renderer.Notice(NoticeLevel.Warn, "审计记录为空，未导出任何内容。");
            return;
        }

        try
        {
            var path = _auditSink.ExportJsonl(_settings.AuditExportPath);
            _renderer.Notice(NoticeLevel.Info, $"已导出 {_auditSink.Count} 条审计记录（JSONL，全量）→ {path}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            _renderer.Notice(NoticeLevel.Error, $"导出失败：{ex.GetType().Name}（可用 {DocAgentSettings.SectionName}:{DocAgentSettings.KeyAuditExportPath} 指定可写路径）。");
        }
    }

    private void PrintUsage()
    {
        var counts = _auditSink.CountByDecision();
        _renderer.Table(
            "用量",
            ["项", "值"],
            [
                ["累计输入 token", _inputTokens.ToString(CultureInfo.InvariantCulture)],
                ["累计输出 token", _outputTokens.ToString(CultureInfo.InvariantCulture)],
                ["轮次", _turnCount.ToString(CultureInfo.InvariantCulture)],
                ["工具调用（allowed）", counts.Allowed.ToString(CultureInfo.InvariantCulture)],
                ["工具调用（denied）", counts.Denied.ToString(CultureInfo.InvariantCulture)],
                ["工具调用（error）", counts.Error.ToString(CultureInfo.InvariantCulture)],
                ["在途流式消息", _consoleChannel.ActiveMessages.ToString(CultureInfo.InvariantCulture)],
            ]);
    }

    private void PrintHistory()
    {
        var rows = _recentTurns
            .Reverse()
            .Select((turn, index) => new[] { (index + 1).ToString(CultureInfo.InvariantCulture), StripForTable(turn) })
            .ToArray();

        IReadOnlyList<string[]> tableRows = rows.Length == 0
            ? [["-", "（还没有任何轮次）"]]
            : rows;

        _renderer.Table(
            $"会话概览（本 Demo 侧计数：{_turnCount} 轮完成；最近 {rows.Length} 轮摘要）",
            ["#", "摘要"],
            tableRows);

        _renderer.Notice(
            NoticeLevel.Info,
            "说明：SDK 会话状态袋的键（feishu.agent.history）是 internal 常量，Demo 不反射读取；"
            + "此处展示的是宿主侧计数。历史裁剪（MaxHistoryMessages=50）不可逆，"
            + "含 FunctionCallContent/FunctionResultContent 的消息会被永久丢弃。");
    }

    private async Task ResetAsync(CancellationToken cancellationToken)
    {
        if (_approvalState.HasPending)
        {
            // /reset 只重建会话、不处理挂起项：那会让框架记录的待审批请求失配 ⇒ 会话不可用。
            _renderer.Notice(
                NoticeLevel.Warn,
                $"存在 {_approvalState.Count} 个挂起审批，/reset 会使框架记录失配。请改用 /abandon（它会同时放弃挂起项并重置会话）。");
            return;
        }

        await _conversationStore.DeleteAsync(_conversationKey, cancellationToken).ConfigureAwait(false);
        _ = await _agent.GetOrCreateSessionAsync(_conversationKey, cancellationToken).ConfigureAwait(false);
        ClearScenarioStack();
        _turnCount = 0;
        _recentTurns.Clear();
        _renderer.Notice(NoticeLevel.Info, "会话已重置（IConversationStore.DeleteAsync + GetOrCreateSessionAsync）。");
    }

    private async Task HandleScenarioAsync(string argument, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(argument) || string.Equals(argument.Trim(), ScenarioBook.ListArgument, StringComparison.OrdinalIgnoreCase))
        {
            _renderer.Table(
                $"剧本（{_scenarioBook.All.Count} 部；/scenario <名字或别名> 直达）",
                ["名称", "别名", "说明"],
                _scenarioBook.All.Select(s => new[] { s.Name, string.Join("、", s.Aliases), s.Description }).ToArray());
            return;
        }

        if (string.Equals(argument.Trim(), ScenarioBook.ClearArgument, StringComparison.OrdinalIgnoreCase))
        {
            ClearScenarioStack();
            _renderer.Notice(NoticeLevel.Info, "剧本应答栈已清空。");
            return;
        }

        var scenario = _scenarioBook.Find(argument.Trim());
        if (scenario is null)
        {
            _renderer.Notice(NoticeLevel.Warn, $"没有找到剧本 {argument}（/scenario 列清单）。");
            return;
        }

        if (ShouldBlockTurn(_approvalState.HasPending))
        {
            _renderer.Notice(NoticeLevel.Warn, "存在挂起审批，先处理挂起项再运行剧本（/pending）。");
            return;
        }

        ClearScenarioStack();
        _activeScenario = scenario;
        foreach (var answer in scenario.AutoAnswers)
        {
            _autoAnswers.Enqueue(answer);
        }

        _renderer.Notice(
            NoticeLevel.Info,
            $"▶ 剧本 {scenario.Name}（{scenario.Description}）：自动应答 {scenario.AutoAnswers.Count} 条；"
            + "写动作一律需要人工确认，自动应答不会批准。");
        _renderer.UserEcho(scenario.Prompt);

        await RunTurnAsync(scenario.Prompt, cancellationToken).ConfigureAwait(false);
        await DrainAutoAnswersAsync(cancellationToken).ConfigureAwait(false);
    }

    private void ToggleVerbose()
    {
        _renderer.Verbose = !_renderer.Verbose;
        _renderer.Notice(
            NoticeLevel.Info,
            _renderer.Verbose
                ? $"verbose：on（工具结果全文展示；Trace 行恒开，对齐 SDK 的 Span/指标语义，本 Demo 不接 OTel Exporter）"
                : "verbose：off");
    }

    private void Exit()
    {
        if (_approvalState.HasPending && !_exitConfirmed)
        {
            _exitConfirmed = true;
            _renderer.Notice(
                NoticeLevel.Warn,
                $"仍有 {_approvalState.Count} 个待确认写操作。直接退出会放弃这些确认（fail-closed，写工具不执行）；"
                + "再输入一次 /exit 确认退出，或 /approve / /deny / /abandon。");
            return;
        }

        _renderer.Notice(NoticeLevel.Info, "退出。");
        _renderer.RequestExit();
    }

    // ──────────────────────────── 轮次执行 ────────────────────────────

    private async Task RunTurnAsync(string userText, CancellationToken cancellationToken)
    {
        if (_dryRunPrompting)
        {
            // 提示词级 dry-run 引导：作为本轮用户消息的前导块下发（指令本体是构造期装配的，运行期不重建 Agent）。
            userText = DocAgentPrompt.DryRunHint + "\n\n" + userText;
        }

        var gateHandle = await _conversationGate.AcquireAsync(_conversationKey, cancellationToken).ConfigureAwait(false);
        var stopwatch = Stopwatch.StartNew();
        var messageId = string.Empty;
        var succeeded = false;
        long inputTokens = 0;
        long outputTokens = 0;

        try
        {
            var session = await _agent.GetOrCreateSessionAsync(_conversationKey, cancellationToken).ConfigureAwait(false);

            // 工具执行上下文沿异步流注入（多租户隔离的事实来源）；审批续跑轮由 SDK 另行重建。
            using var toolScope = _toolContextAccessor.Begin(new FeishuToolContext(
                _settings.AppKey,
                _conversationKey,
                ChatId: LocalChatId,
                UserId: _settings.UserId));

            messageId = await _channel.BeginAsync(_settings.AppKey, LocalChatId, cancellationToken).ConfigureAwait(false);

            await foreach (var update in _agent
                .RunStreamingAsync(userText, session, options: null, cancellationToken)
                .ConfigureAwait(false))
            {
                if (!string.IsNullOrEmpty(update.Text))
                {
                    await _channel
                        .WriteStreamAsync(_settings.AppKey, LocalChatId, messageId, update.Text, cancellationToken)
                        .ConfigureAwait(false);
                }

                if (update.Contents.OfType<UsageContent>().LastOrDefault() is { Details: { } details })
                {
                    inputTokens = details.InputTokenCount ?? inputTokens;
                    outputTokens = details.OutputTokenCount ?? outputTokens;
                }
            }

            await _agent.SaveSessionAsync(_conversationKey, session, cancellationToken).ConfigureAwait(false);
            succeeded = true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _renderer.Notice(NoticeLevel.Warn, "本轮已取消（Ctrl+C）；会话保留，可继续提问。");
        }
        catch (Exception ex)
        {
            // 降级矩阵 §10.2：一轮异常不清空会话、允许继续提问；不打印敏感详情。
            _renderer.Notice(NoticeLevel.Error, $"本轮失败：{ex.GetType().Name}（会话已保留，可直接重试）");
            _logger?.LogError(ex, "文档业务智能体轮次失败（conversationKey: {ConversationKey}）", _conversationKey);
        }
        finally
        {
            if (messageId.Length > 0)
            {
                // 收尾补偿：用不可取消令牌（任何终止路径都必须使 per-messageId 状态归零，R3-14）。
                await _channel
                    .FlushAsync(_settings.AppKey, LocalChatId, messageId, CancellationToken.None)
                    .ConfigureAwait(false);
            }

            _renderer.AgentEnd();
            gateHandle.Dispose();
            stopwatch.Stop();
        }

        if (succeeded)
        {
            _inputTokens += inputTokens;
            _outputTokens += outputTokens;
            _turnCount++;
            RememberTurn(userText);
        }

        _renderer.Trace(
            $"usage in={inputTokens} out={outputTokens}，本轮 {stopwatch.ElapsedMilliseconds}ms，"
            + $"pending={_approvalState.Count}，ok={succeeded}");
    }

    /// <summary>把一次非流式响应按流式通道渲染（审批续跑轮走此路径）。</summary>
    private async Task RenderResponseAsync(AgentResponse response, CancellationToken cancellationToken)
    {
        var messageId = await _channel.BeginAsync(_settings.AppKey, LocalChatId, cancellationToken).ConfigureAwait(false);
        try
        {
            if (!string.IsNullOrEmpty(response.Text))
            {
                await _channel
                    .WriteStreamAsync(_settings.AppKey, LocalChatId, messageId, response.Text, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (response.Usage is { } usage)
            {
                _inputTokens += usage.InputTokenCount ?? 0;
                _outputTokens += usage.OutputTokenCount ?? 0;
            }
        }
        finally
        {
            await _channel
                .FlushAsync(_settings.AppKey, LocalChatId, messageId, CancellationToken.None)
                .ConfigureAwait(false);
            _renderer.AgentEnd();
        }

        _turnCount++;
        RememberTurn("（审批续跑轮）" + (response.Text ?? string.Empty));
    }

    private async Task DrainAutoAnswersAsync(CancellationToken cancellationToken)
    {
        while (_autoAnswers.Count > 0)
        {
            if (_approvalState.HasPending)
            {
                // 剧本只自动化只读链路与前置换行提问；写动作一律要人确认（ADR-04）。
                _renderer.Notice(
                    NoticeLevel.Warn,
                    "本轮产生了挂起审批：剧本自动应答已停止（写动作一律要人工确认）。请 /approve、/deny 或 /abandon。");
                _autoAnswers.Clear();
                return;
            }

            var next = _autoAnswers.Dequeue();
            _renderer.Notice(NoticeLevel.Info, $"[scenario:{_activeScenario?.Name}] 自动应答 → {next}");
            await RunTurnAsync(next, cancellationToken).ConfigureAwait(false);
        }
    }

    private void ClearScenarioStack()
    {
        _autoAnswers.Clear();
        _activeScenario = null;
    }

    private void RememberTurn(string text)
    {
        _recentTurns.Enqueue(text.Length <= 80 ? text : text[..80] + "…");
        while (_recentTurns.Count > 5)
        {
            _recentTurns.Dequeue();
        }
    }

    private static string ResolveRisk(ToolExecutionAuditRecord record)
        => DocAgentToolFacts.RiskLiteralOf(record.ToolName, record.IsWrite);

    private static string TailKey(string? conversationKey)
        => string.IsNullOrEmpty(conversationKey)
            ? "-"
            : conversationKey.Length <= 12 ? conversationKey : conversationKey[^12..];

    private static string StripForTable(string text)
        => ConsoleRenderer.StripAnsi(text).Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);

    private static string RemainingText(TimeSpan remaining)
        => remaining <= TimeSpan.Zero
            ? "已过期"
            : $"{(int)remaining.TotalMinutes} 分 {remaining.Seconds} 秒";

    private static string DomainOf(string toolName)
    {
        var index = toolName.IndexOf('.', StringComparison.Ordinal);
        return index > 0 ? toolName[..index] : toolName;
    }

    /// <summary>
    /// 归一化用户输入：去掉首尾空白，并容忍**开头的 BOM**。
    /// </summary>
    /// <param name="input">原始输入。</param>
    /// <returns>归一化文本。</returns>
    /// <remarks>
    /// <c>char.IsWhiteSpace('\uFEFF')</c> 为 <see langword="false"/>，故 <c>Trim()</c> <b>不会</b>去掉 BOM：
    /// 被重定向的脚本文件（<c>dotnet run &lt; script.txt</c>）通常带 BOM，若不容忍，首行会被当成
    /// 普通输入发给模型（表现是"第一条命令莫名不生效"）。
    /// </remarks>
    internal static string NormalizeInput(string input)
        => string.IsNullOrEmpty(input)
            ? string.Empty
            : input.Trim('\uFEFF', ' ', '\t', '\r', '\n');

    private static bool ContainsControlCharacters(string text)
    {
        foreach (var ch in text)
        {
            if (ch == '\u001B' || ch == '\u009B' || (char.IsControl(ch) && ch is not '\t'))
            {
                return true;
            }
        }

        return false;
    }
}
