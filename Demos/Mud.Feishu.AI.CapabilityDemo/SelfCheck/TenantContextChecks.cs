// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

using Mud.Feishu.AI.CapabilityDemo.Common;
using Mud.Feishu.AI.CapabilityDemo.Host;

namespace Mud.Feishu.AI.CapabilityDemo.SelfCheck;

/// <summary>组 8：租户上下文（AsyncLocal 不串、缺上下文拒绝、空 appKey 命中 TMA2-20）。</summary>
internal static class TenantContextChecks
{
    private const string ReadTool = "bitable.query_records";

    public static IEnumerable<CheckResult> Run()
    {
        const string group = "tenant-context";

        // ① AsyncLocal 作用域语义（纯内存）。
        var accessor = new FeishuToolContextAccessor();
        yield return new CheckResult(group, "初始无上下文", accessor.Current is null);

        using (accessor.Begin(new FeishuToolContext("app-a")))
        {
            yield return new CheckResult(group, "Begin 后可见 app-a", accessor.Current?.AppKey == "app-a",
                "app-a", accessor.Current?.AppKey);

            using (accessor.Begin(new FeishuToolContext("app-b")))
            {
                yield return new CheckResult(group, "嵌套 Begin 切到 app-b", accessor.Current?.AppKey == "app-b",
                    "app-b", accessor.Current?.AppKey);
            }

            yield return new CheckResult(group, "Dispose 恢复 app-a", accessor.Current?.AppKey == "app-a",
                "app-a", accessor.Current?.AppKey);
        }

        yield return new CheckResult(group, "最外层 Dispose 回到 null", accessor.Current is null);

        // ② 缺上下文（不 Begin）：入口即拒绝，不产生审计记录（审计只覆盖绑定内决策）。
        var missing = RunToolCallAsync(begin: false, appKey: null);
        yield return new CheckResult(group, "缺上下文被拒绝",
            missing.ToolResultText.Contains("上下文缺失", StringComparison.Ordinal), "上下文缺失", missing.ToolResultText);
        yield return new CheckResult(group, "缺上下文不进审计（入口失败）",
            missing.AuditCount == 0, "0", missing.AuditCount.ToString(CultureInfo.InvariantCulture));

        // ③ 空 appKey（Begin 但租户为空）：同样在绑定入口早退（TMA2-20），不发 HTTP、不写审计。
        var empty = RunToolCallAsync(begin: true, appKey: string.Empty);
        yield return new CheckResult(group, "空 appKey 命中 TMA2-20",
            empty.ToolResultText.Contains("TMA2-20", StringComparison.Ordinal), "TMA2-20", empty.ToolResultText);
        yield return new CheckResult(group, "空 appKey 不进审计（早退）",
            empty.AuditCount == 0, "0", empty.AuditCount.ToString(CultureInfo.InvariantCulture));
    }

    private static (string ToolResultText, int AuditCount) RunToolCallAsync(bool begin, string? appKey)
    {
        var settings = new DemoSettings { ModelId = "verify", ApiKey = "verify", AppKey = "cap-verify" };

        var client = new ScriptedChatClient(
            ScriptedChatClient.ToolCall(ReadTool, new Dictionary<string, object?>
            {
                ["app_token"] = "bascn_placeholder",
                ["table_id"] = "tbl_placeholder",
            }),
            "（结束）");

        var profile = ActProfiles.A1(settings) with
        {
            Id = "verify-tenant",
            ReadTools = [ReadTool],
            WriteTools = [],
            ScriptedClient = client,
        };

        using var provider = CompositionRoot.Build(
            DemoConfig.CreateVerifyConfiguration(), settings, profile, includeModel: false, consoleAllowed: false);

        var agent = provider.GetRequiredService<FeishuAgent>();
        var toolAccessor = provider.GetRequiredService<IFeishuToolContextAccessor>();
        var sink = provider.GetRequiredService<InMemoryAuditSink>();

        var key = ConversationKeys.Build(settings.AppKey, "verify", "ou_cap_demo_user");
        var session = agent.GetOrCreateSessionAsync(key).GetAwaiter().GetResult();

        if (begin)
        {
            using (toolAccessor.Begin(new FeishuToolContext(appKey!, key)))
            {
                agent.RunAsync("查询。", session).GetAwaiter().GetResult();
            }
        }
        else
        {
            agent.RunAsync("查询。", session).GetAwaiter().GetResult();
        }

        // 回填文本取「模型看到的」那一份：上下文类失败在绑定入口早退，审计 sink 里本来就没有记录。
        return (client.LastToolResultText ?? "（无工具回填）", sink.Count);
    }
}
