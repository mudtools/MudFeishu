// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

using Mud.Feishu.AI.CapabilityDemo.Common;
using Mud.Feishu.AI.CapabilityDemo.Host;

namespace Mud.Feishu.AI.CapabilityDemo.Acts;

/// <summary>
/// A3：不确定能力时先问元工具（博客「4 个元工具」）。
/// </summary>
internal static class A3MetaToolsAct
{
    public static async Task<int> RunAsync(
        DemoConfigurationSources sources, DemoSettings settings, CancellationToken cancellationToken)
    {
        settings.Validate(requireModel: true, requireApp: true, requireData: false);
        var configuration = DemoConfig.EnsureAppSection(sources.Configuration, settings);

        await using var provider = CompositionRoot.Build(
            configuration, settings, ActProfiles.A3(settings));

        var agent = provider.GetRequiredService<FeishuAgent>();
        var accessor = provider.GetRequiredService<IFeishuToolContextAccessor>();
        var sink = provider.GetRequiredService<InMemoryAuditSink>();
        var registry = provider.GetRequiredService<FeishuToolRegistry>();

        DemoConsole.Banner("A3 · 先问元工具，再动手", new Dictionary<string, string?>
        {
            ["已启用工具"] = string.Join("、", registry.EnabledTools.Select(static t => t.Name)),
            ["提示"] = "元工具零飞书请求，但仍走同一条执行链（需要 appKey 上下文）",
        });

        var conversationKey = ConversationKeys.Build(settings.AppKey, "a3", settings.UserId);
        var session = await agent.GetOrCreateSessionAsync(conversationKey, cancellationToken);

        using (accessor.Begin(new FeishuToolContext(settings.AppKey, conversationKey, UserId: settings.UserId)))
        {
            var response = await agent.RunAsync(
                "我想把多维表格里『待办』的记录同步到云文档，但不确定现在支持哪些工具。先查清楚能力再给方案。",
                session,
                cancellationToken: cancellationToken);
            DemoConsole.Agent(response.Text);
        }

        await agent.SaveSessionAsync(conversationKey, session, cancellationToken);

        var records = sink.Snapshot();
        DemoConsole.AuditTable(records);

        var metaCalls = records.Count(static r => r.ToolName.StartsWith("feishu.", StringComparison.Ordinal));
        var writes = records.Count(static r => r.IsWrite);
        var pass = metaCalls > 0 && writes == 0;

        DemoConsole.Notice(pass ? NoticeLevel.Info : NoticeLevel.Warn,
            pass
                ? $"自检通过：调用元工具 {metaCalls} 次，零写调用。"
                : $"自检未通过：元工具调用 {metaCalls} 次（期望 ≥1），写调用 {writes} 次（期望 0）。");

        return pass ? 0 : 1;
    }
}
