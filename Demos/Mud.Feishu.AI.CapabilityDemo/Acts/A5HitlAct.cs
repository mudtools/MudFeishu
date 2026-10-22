// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.AI.CapabilityDemo.Common;
using Mud.Feishu.AI.CapabilityDemo.Host;

namespace Mud.Feishu.AI.CapabilityDemo.Acts;

/// <summary>
/// A5：写闭环（读 → dry_run → 审批 → 续跑 → 幂等 → 拒绝 → 能力边界）。
/// </summary>
/// <remarks>
/// 命令行是唯一的"人"：<c>/approve</c> 发放一次性 grant 并触发续跑，<c>/deny</c> 以中性结果续跑。
/// 挂起态下拒绝新问题——未应答审批残留在会话历史里会让后续每一轮抛配对校验异常。
/// </remarks>
internal static class A5HitlAct
{
    public static async Task<int> RunAsync(
        DemoConfigurationSources sources,
        DemoSettings settings,
        bool includeDelete,
        CancellationToken cancellationToken)
    {
        settings.Validate(requireModel: true, requireApp: true, requireData: true);
        var configuration = DemoConfig.EnsureAppSection(sources.Configuration, settings);

        var profile = ActProfiles.A5(settings, includeDelete);
        await using var provider = CompositionRoot.Build(configuration, settings, profile);

        var agent = provider.GetRequiredService<FeishuAgent>();
        var accessor = provider.GetRequiredService<IFeishuToolContextAccessor>();
        var gate = provider.GetRequiredService<IConversationGate>();
        var store = provider.GetRequiredService<IConversationStore>();
        var channel = provider.GetRequiredService<IFeishuToolApprovalChannel>();
        var pendingStore = provider.GetRequiredService<IPendingApprovalStore>();
        var board = provider.GetRequiredService<PendingApprovalBoard>();
        var authorizer = provider.GetRequiredService<ConsoleAuthorizerState>();
        var sink = provider.GetRequiredService<InMemoryAuditSink>();
        var registry = provider.GetRequiredService<FeishuToolRegistry>();

        if (includeDelete)
        {
            authorizer.HighRiskTools.Add(ToolSets.BitableDelete);
        }

        var conversationKey = ConversationKeys.Build(settings.AppKey, "a5", settings.UserId);
        var session = await agent.GetOrCreateSessionAsync(conversationKey, cancellationToken);

        DemoConsole.Banner("A5 · 写闭环：审批、续跑、幂等", new Dictionary<string, string?>
        {
            ["应用"] = $"{settings.AppKey}（AppId {DemoSettings.Mask(settings.AppId)}）",
            ["模型"] = settings.Endpoint is null ? settings.ModelId : $"{settings.ModelId} @ {settings.Endpoint}",
            ["目标表"] = $"{settings.BitTableAppToken} / {settings.BitTableTableId}",
            ["只读面"] = $"{ToolSets.BitableRead5.Length} 枚",
            ["写面"] = string.Join("、", profile.WriteTools),
            ["能力边界"] = includeDelete
                ? "已显式启用 bitable.delete_record（strict 下授权器仍会独立拒绝）"
                : "bitable.delete_record 不在名单 ⇒ 对模型不可见（不重试、不绕过）",
            ["策略"] = $"{authorizer.Policy}（/policy strict|ask|readonly 可切换）",
        });

        DemoConsole.Notice(NoticeLevel.Info,
            "建议顺序：① 读出待办 → ② 预演(dry_run) → ③ /approve → ④ 真写 → ⑤ 幂等新增 → ⑥ /deny → ⑦ 要求删除。");
        DemoConsole.Notice(NoticeLevel.Info, "输入 /help 查看命令；/quit 退出。");

        while (!cancellationToken.IsCancellationRequested)
        {
            DemoConsole.Prompt();
            var input = Console.ReadLine();
            if (input is null)
            {
                break;
            }

            input = input.Trim();
            if (input.Length == 0)
            {
                continue;
            }

            if (!input.StartsWith('/'))
            {
                if (board.HasPending)
                {
                    DemoConsole.Notice(NoticeLevel.Warn,
                        "有挂起审批时拒绝新问题（残留会让会话永久毒化）。请先 /approve、/deny 或 /abandon。");
                    continue;
                }

                session = await RunTurnAsync(
                    agent, accessor, gate, session, conversationKey, settings, input, cancellationToken);
                continue;
            }

            var parts = input.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0].ToLowerInvariant();
            var argument = parts.Length > 1 ? parts[1] : null;

            switch (command)
            {
                case "/run":
                    var text = input.Length > 4 ? input[5..].Trim() : string.Empty;
                    if (text.Length == 0)
                    {
                        DemoConsole.Notice(NoticeLevel.Warn, "用法：/run <文本>");
                        break;
                    }

                    if (board.HasPending)
                    {
                        DemoConsole.Notice(NoticeLevel.Warn, "有挂起审批时拒绝新问题（先 /approve、/deny 或 /abandon）。");
                        break;
                    }

                    session = await RunTurnAsync(
                        agent, accessor, gate, session, conversationKey, settings, text, cancellationToken);
                    break;

                case "/help":
                    PrintHelp();
                    break;

                case "/pending":
                    PrintPending(board);
                    break;

                case "/approve":
                    await DecideAsync(
                        agent, accessor, gate, channel, pendingStore, board,
                        argument, approved: true, reason: "控制台人工批准", cancellationToken);
                    session = await agent.GetOrCreateSessionAsync(conversationKey, cancellationToken);
                    break;

                case "/deny":
                    var deniedReason = parts.Length > 2 ? parts[2] : "控制台人工拒绝";
                    await DecideAsync(
                        agent, accessor, gate, channel, pendingStore, board,
                        argument, approved: false, reason: deniedReason, cancellationToken);
                    session = await agent.GetOrCreateSessionAsync(conversationKey, cancellationToken);
                    break;

                case "/abandon":
                    var cleared = board.Clear();
                    await store.DeleteAsync(conversationKey, cancellationToken);
                    session = await agent.GetOrCreateSessionAsync(conversationKey, cancellationToken);
                    DemoConsole.Notice(NoticeLevel.Info, $"已放弃 {cleared} 条挂起项并重建会话（悬空清场）。");
                    break;

                case "/policy":
                    if (argument is null
                        || !new[] { DemoSettings.PolicyStrict, DemoSettings.PolicyAsk, DemoSettings.PolicyReadonly }
                            .Contains(argument, StringComparer.Ordinal))
                    {
                        DemoConsole.Notice(NoticeLevel.Warn, "用法：/policy strict|ask|readonly");
                        break;
                    }

                    authorizer.Policy = argument;
                    DemoConsole.Notice(NoticeLevel.Info, $"策略已切换为 {argument}。");
                    break;

                case "/audit":
                    DemoConsole.AuditTable(sink.Snapshot());
                    var (allowed, denied, error) = sink.CountByDecision();
                    DemoConsole.Notice(NoticeLevel.Info, $"汇总：allowed={allowed} denied={denied} error={error}");
                    break;

                case "/export":
                    var path = sink.ExportJsonl(argument ?? (string.IsNullOrWhiteSpace(settings.AuditExportPath)
                        ? null
                        : settings.AuditExportPath));
                    DemoConsole.Notice(NoticeLevel.Info, $"审计已导出：{path}");
                    break;

                case "/quit":
                case "/exit":
                    if (board.HasPending)
                    {
                        DemoConsole.Notice(NoticeLevel.Warn, "仍有挂起审批：请先 /approve、/deny 或 /abandon。");
                        break;
                    }

                    DemoConsole.Notice(NoticeLevel.Info, $"已启用工具 {registry.EnabledTools.Count} 枚；再见。");
                    return 0;

                default:
                    DemoConsole.Notice(NoticeLevel.Warn, $"未知命令 {command}（/help 查看）。");
                    break;
            }
        }

        return 0;
    }

    private static async Task<AgentSession> RunTurnAsync(
        FeishuAgent agent,
        IFeishuToolContextAccessor accessor,
        IConversationGate gate,
        AgentSession session,
        string conversationKey,
        DemoSettings settings,
        string text,
        CancellationToken cancellationToken)
    {
        using var handle = await gate.AcquireAsync(conversationKey, cancellationToken);

        using (accessor.Begin(new FeishuToolContext(settings.AppKey, conversationKey, UserId: settings.UserId)))
        {
            var response = await agent.RunAsync(text, session, cancellationToken: cancellationToken);
            DemoConsole.Agent(response.Text);
        }

        await agent.SaveSessionAsync(conversationKey, session, cancellationToken);
        return session;
    }

    private static async Task DecideAsync(
        FeishuAgent agent,
        IFeishuToolContextAccessor accessor,
        IConversationGate gate,
        IFeishuToolApprovalChannel channel,
        IPendingApprovalStore pendingStore,
        PendingApprovalBoard board,
        string? correlationId,
        bool approved,
        string reason,
        CancellationToken cancellationToken)
    {
        PendingApproval? pending;
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            pending = board.Snapshot().FirstOrDefault();
            if (pending is null)
            {
                DemoConsole.Notice(NoticeLevel.Warn, "没有挂起审批可处理。");
                return;
            }
        }
        else
        {
            pending = board.Take(correlationId);
            if (pending is null)
            {
                DemoConsole.Notice(NoticeLevel.Warn, $"关联号 {correlationId} 不存在或已过期。");
                return;
            }
        }

        // 关键一步：批准必须转成授权器可消费的一次性凭据，否则续跑轮会被二次拒绝。
        if (approved)
        {
            board.Grant(pending.Framework.ToolName);
        }

        board.Remove(pending.Framework.RequestId);

        DemoConsole.Notice(NoticeLevel.Info,
            approved
                ? $"已批准 {pending.CorrelationId}（{pending.Framework.ToolName}），续跑中…"
                : $"已拒绝 {pending.CorrelationId}（{pending.Framework.ToolName}），以中性结果续跑…");

        var response = await agent.RunApprovalContinuationAsync(
            toolContextAccessor: accessor,
            approval: pending.Framework,
            approved: approved,
            reason: reason,
            conversationGate: gate,
            approvalChannel: channel,
            logger: NullLogger.Instance,
            pendingApprovalStore: pendingStore,
            cancellationToken: cancellationToken);

        DemoConsole.Agent(response.Text);

        if (board.HasPending)
        {
            DemoConsole.Notice(NoticeLevel.Info, $"续跑又产生了 {board.PendingCount} 条新审批（模型可能串了多个写）。");
        }
    }

    private static void PrintPending(PendingApprovalBoard board)
    {
        var pending = board.Snapshot();
        DemoConsole.Section($"挂起审批（{pending.Count}）");
        if (pending.Count == 0)
        {
            DemoConsole.Line("（空）");
            return;
        }

        DemoConsole.Table(
            ["关联号", "工具", "参数摘要", "过期"],
            pending.Select(p => new[]
            {
                p.CorrelationId,
                p.Framework.ToolName,
                p.Framework.ArgumentsDigest ?? "-",
                p.ExpiresAt.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            }).ToArray());
    }

    private static void PrintHelp()
    {
        DemoConsole.Section("命令");
        DemoConsole.Table(
            ["命令", "作用"],
            [
                ["/run <文本>", "等价直接输入文本（有挂起项时被阻断）"],
                ["/pending", "列出挂起审批"],
                ["/approve [关联号]", "发放一次性 grant 并续跑（真执行）"],
                ["/deny [关联号] [理由]", "拒绝并续跑（工具不执行）"],
                ["/abandon", "放弃全部挂起 + 删除会话 + 重建"],
                ["/policy strict|ask|readonly", "切换授权策略"],
                ["/audit", "最近审计"],
                ["/export [路径]", "审计 JSONL 落盘"],
                ["/quit", "退出（有挂起项时需先清场）"],
            ]);
    }
}
