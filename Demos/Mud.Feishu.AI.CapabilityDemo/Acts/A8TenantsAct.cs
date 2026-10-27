// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

using Mud.Feishu.AI.CapabilityDemo.Common;
using Mud.Feishu.AI.CapabilityDemo.Host;

namespace Mud.Feishu.AI.CapabilityDemo.Acts;

/// <summary>
/// A8：同一进程托管两个飞书应用——租户由 <c>FeishuToolContext.AppKey</c> 显式决定。
/// </summary>
/// <remarks>
/// 本幕要求 <c>FeishuApps</c> 至少两个应用；缺上下文时执行链直接拒绝，<b>绝不回落到默认应用</b>。
/// </remarks>
internal static class A8TenantsAct
{
    public static async Task<int> RunAsync(
        DemoConfigurationSources sources, DemoSettings settings, CancellationToken cancellationToken)
    {
        var appKeys = DemoConfig.ReadAppKeys(sources.Configuration);

        DemoConsole.Banner("A8 · 多租户隔离", new Dictionary<string, string?>
        {
            ["FeishuApps"] = appKeys.Count == 0 ? "（未配置）" : string.Join("、", appKeys),
            ["模型"] = settings.Endpoint is null ? settings.ModelId : $"{settings.ModelId} @ {settings.Endpoint}",
        });

        if (appKeys.Count < 2)
        {
            DemoConsole.Notice(NoticeLevel.Warn,
                $"本幕需要两个应用：请在 appsettings.local.json 提供 {DemoSettings.AppSectionName} 节（AppKey 分别为 app-a / app-b）。");
            DemoConsole.Line("示例：");
            DemoConsole.Line("""
            "FeishuApps": [
              { "AppKey": "app-a", "AppId": "cli_***", "AppSecret": "***", "IsDefault": true },
              { "AppKey": "app-b", "AppId": "cli_***", "AppSecret": "***" }
            ]
            """);
            return 2;
        }

        settings.Validate(requireModel: true, requireApp: false, requireData: false);
        var configuration = DemoConfig.EnsureAppSection(sources.Configuration, settings);

        await using var provider = CompositionRoot.Build(
            configuration, settings, ActProfiles.A8(settings));

        var agent = provider.GetRequiredService<FeishuAgent>();
        var accessor = provider.GetRequiredService<IFeishuToolContextAccessor>();
        var sink = provider.GetRequiredService<InMemoryAuditSink>();

        var observed = new List<string?>();

        for (var i = 0; i < 2; i++)
        {
            var appKey = appKeys[i];
            var key = ConversationKeys.Build(appKey, "a8", settings.UserId);
            var session = await agent.GetOrCreateSessionAsync(key, cancellationToken);

            DemoConsole.Section($"轮次 {i + 1}：appKey={appKey}");
            using (accessor.Begin(new FeishuToolContext(appKey, key, UserId: settings.UserId)))
            {
                var response = await agent.RunAsync(
                    "列出我当前租户下的多维表格（只列前 3 个）。", session, cancellationToken: cancellationToken);
                DemoConsole.Agent(response.Text);
            }

            await agent.SaveSessionAsync(key, session, cancellationToken);
            observed.Add(sink.Snapshot().LastOrDefault()?.AppKey);
        }

        DemoConsole.Notice(NoticeLevel.Info, "轮次 A/B 的审计 appKey 应与上下文一致（令牌按 appKey 托管）。");

        // 无上下文轮：即使配置了 IsDefault=true 也不得兜底。
        DemoConsole.Section("负例：不 Begin 上下文");
        var missingKey = ConversationKeys.Build(appKeys[0], "a8-missing", settings.UserId);
        var missingSession = await agent.GetOrCreateSessionAsync(missingKey, cancellationToken);
        await agent.RunAsync("列出我的多维表格。", missingSession, cancellationToken: cancellationToken);

        var lastReason = sink.Snapshot().LastOrDefault()?.Reason ?? "（无审计记录）";
        var missingRejected = lastReason.Contains("上下文缺失", StringComparison.Ordinal);

        DemoConsole.Notice(missingRejected ? NoticeLevel.Info : NoticeLevel.Error,
            $"缺上下文的结果：{lastReason}");

        DemoConsole.Section("进程外形态：每个租户一个 stdio 进程");
        var dll = Path.Combine(AppContext.BaseDirectory, "Mud.Feishu.AI.CapabilityDemo.dll")
            .Replace("\\", "/");
        foreach (var appKey in appKeys.Take(2))
        {
            DemoConsole.Line($$"""
            {
              "mcpServers": {
                "feishu-{{appKey}}": {
                  "command": "dotnet",
                  "args": ["{{dll}}", "--act", "mcp", "--app", "{{appKey}}"]
                }
              }
            }
            """);
        }

        var isolationOk = observed.Count == 2
                          && string.Equals(observed[0], appKeys[0], StringComparison.Ordinal)
                          && string.Equals(observed[1], appKeys[1], StringComparison.Ordinal);

        var pass = isolationOk && missingRejected;
        DemoConsole.Notice(pass ? NoticeLevel.Info : NoticeLevel.Error,
            pass
                ? "自检通过：两次 Begin 不串租户，缺上下文被拒。"
                : $"自检失败：审计 appKey 序列 [{string.Join(",", observed)}]，缺上下文拒绝={missingRejected}");

        return pass ? 0 : 1;
    }
}
