// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

using Mud.Feishu.AI.CapabilityDemo.Common;
using Mud.Feishu.AI.CapabilityDemo.Host;

namespace Mud.Feishu.AI.CapabilityDemo.Acts;

/// <summary>
/// A1：两段装配跑通只读问答（博客「五分钟上手」）。
/// </summary>
internal static class A1HelloAgentAct
{
    public static async Task<int> RunAsync(
        DemoConfigurationSources sources, DemoSettings settings, CancellationToken cancellationToken)
    {
        settings.Validate(requireModel: true, requireApp: true, requireData: false);
        var configuration = DemoConfig.EnsureAppSection(sources.Configuration, settings);

        await using var provider = CompositionRoot.Build(
            configuration, settings, ActProfiles.A1(settings));

        var agent = provider.GetRequiredService<FeishuAgent>();
        var accessor = provider.GetRequiredService<IFeishuToolContextAccessor>();
        var registry = provider.GetRequiredService<FeishuToolRegistry>();

        var enabled = registry.EnabledTools;
        DemoConsole.Banner("A1 · 五分钟上手：两段装配", new Dictionary<string, string?>
        {
            ["配置来源"] = sources.Files.Count == 0 ? "（未发现 appsettings*.json）" : string.Join(" + ", sources.Files),
            ["应用"] = $"{settings.AppKey}（AppId {DemoSettings.Mask(settings.AppId)}）",
            ["模型"] = settings.Endpoint is null ? settings.ModelId : $"{settings.ModelId} @ {settings.Endpoint}",
            ["已启用工具"] = $"{enabled.Count} 个（只读 {enabled.Count(t => !t.IsWrite)} / 写 {enabled.Count(t => t.IsWrite)}）",
            ["身份闭集"] = "tenant（默认）",
            ["风险上限"] = "high-risk-write（默认；本幕无写工具）",
            ["授权强制"] = "on（未注册授权器时写工具默认拒绝）",
        });

        var conversationKey = ConversationKeys.Build(settings.AppKey, "a1", settings.UserId);
        var session = await agent.GetOrCreateSessionAsync(conversationKey, cancellationToken);

        using (accessor.Begin(new FeishuToolContext(settings.AppKey, conversationKey, UserId: settings.UserId)))
        {
            DemoConsole.Section("第 1 轮：先看有什么");
            var first = await agent.RunAsync(
                "我有哪些多维表格？先列出来，不要猜 app_token。", session, cancellationToken: cancellationToken);
            DemoConsole.Agent(first.Text);

            DemoConsole.Section("第 2 轮：多轮记忆（不需要重新列应用）");
            var second = await agent.RunAsync(
                "第一张表有多少条记录？不知道 table_id 就先列字段。", session, cancellationToken: cancellationToken);
            DemoConsole.Agent(second.Text);
        }

        await agent.SaveSessionAsync(conversationKey, session, cancellationToken);
        DemoConsole.Notice(NoticeLevel.Info, "会话已落库（MemoryConversationStore）；退出进程即丢失——演示不接 Redis。");
        return 0;
    }
}
