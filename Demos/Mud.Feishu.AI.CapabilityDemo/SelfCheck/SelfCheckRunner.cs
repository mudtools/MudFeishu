// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

using Mud.Feishu.AI.CapabilityDemo.Common;

namespace Mud.Feishu.AI.CapabilityDemo.SelfCheck;

/// <summary>
/// <c>--verify</c>：8 组离线自检。<b>不新建测试工程</b>（方案 ADR-07），
/// 断言对象是演示工程自己的契约，复用与真幕相同的 <see cref="ActProfile"/> / 审批板 / 导出接缝。
/// </summary>
internal static class SelfCheckRunner
{
    private static readonly string[] GroupOrder =
    [
        "act-catalog", "whitelist", "config-template", "tool-surface",
        "approval-board", "mcp-protocol", "skills-parity", "tenant-context",
    ];

    public static int Run()
    {
        var results = new List<CheckResult>();
        results.AddRange(ActCatalogChecks.Run());
        results.AddRange(WhitelistChecks.Run());
        results.AddRange(ConfigTemplateChecks.Run(AppContext.BaseDirectory));
        results.AddRange(ToolSurfaceChecks.Run());
        results.AddRange(ApprovalBoardChecks.Run());
        results.AddRange(McpProtocolChecks.Run());
        results.AddRange(SkillsParityChecks.Run());
        results.AddRange(TenantContextChecks.Run());

        DemoConsole.Banner("--verify · 离线自检矩阵", new Dictionary<string, string?>
        {
            ["组数"] = GroupOrder.Length.ToString(CultureInfo.InvariantCulture),
            ["检查项"] = results.Count.ToString(CultureInfo.InvariantCulture),
            ["前提"] = "内存合成应用（占位凭证、零 HTTP）；不需要模型 Key",
        });

        var failed = 0;
        foreach (var group in GroupOrder)
        {
            var groupResults = results.Where(r => string.Equals(r.Group, group, StringComparison.Ordinal)).ToArray();
            var groupFailed = groupResults.Count(static r => !r.Passed);
            failed += groupFailed;

            DemoConsole.Notice(groupFailed == 0 ? NoticeLevel.Info : NoticeLevel.Error,
                $"[{(groupFailed == 0 ? "PASS" : "FAIL")}] {group}（{groupResults.Length - groupFailed}/{groupResults.Length}）");

            foreach (var result in groupResults)
            {
                if (result.Passed)
                {
                    continue;
                }

                DemoConsole.Line(
                    $"       ✗ {result.Name}"
                    + (result.Expected is null ? string.Empty : $"｜期望：{result.Expected}")
                    + (result.Actual is null ? string.Empty : $"｜实际：{result.Actual}"));
            }
        }

        var groupsPassed = GroupOrder.Count(g => results
            .Where(r => string.Equals(r.Group, g, StringComparison.Ordinal))
            .All(static r => r.Passed));

        DemoConsole.Line();
        DemoConsole.Notice(failed == 0 ? NoticeLevel.Info : NoticeLevel.Error,
            failed == 0
                ? $"结果：{groupsPassed}/{GroupOrder.Length} 组通过（退出码 0）"
                : $"结果：{failed} 项失败（退出码 1）");

        return failed == 0 ? 0 : 1;
    }
}
