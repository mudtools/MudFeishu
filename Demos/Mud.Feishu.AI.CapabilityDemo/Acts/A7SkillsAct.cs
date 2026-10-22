// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

using Mud.AI.Agent.Guidance;
using Mud.Feishu.AI.CapabilityDemo.Common;
using Mud.Feishu.AI.CapabilityDemo.Host;

namespace Mud.Feishu.AI.CapabilityDemo.Acts;

/// <summary>
/// A7：Skills 产物消费（离线）——读得到、能进指令面、与导出逐字节一致。
/// </summary>
internal static class A7SkillsAct
{
    public static int Run(string domain)
    {
        using var provider = CompositionRoot.BuildToolsOnly();
        var reader = provider.GetRequiredService<ISkillDocumentReader>();
        var exporter = provider.GetRequiredService<IToolSchemaExporter>();

        var domains = reader.Paths
            .Where(static p => p.EndsWith("/SKILL.md", StringComparison.Ordinal))
            .Select(static p => p.Split('/')[1].Replace("feishu-", string.Empty, StringComparison.Ordinal))
            .OrderBy(static d => d, StringComparer.Ordinal)
            .ToArray();

        DemoConsole.Banner("A7 · 进程外：Skills 产物（离线）", new Dictionary<string, string?>
        {
            ["package_version"] = reader.PackageVersion,
            ["产物条目"] = reader.Paths.Count.ToString(CultureInfo.InvariantCulture),
            ["域数"] = domains.Length.ToString(CultureInfo.InvariantCulture),
            ["默认域"] = domain,
        });

        DemoConsole.Section("按域读取 SKILL.md");
        var skill = reader.ReadSkill(domain);
        if (skill is null)
        {
            DemoConsole.Notice(NoticeLevel.Error, $"域 '{domain}' 没有 SKILL.md；可用域：{string.Join("、", domains)}");
            return 2;
        }

        DemoConsole.Line($"= {domain} / SKILL.md（{skill.Length} 字符）=");
        DemoConsole.Line(Head(skill, 900));

        var reference = reader.Paths
            .FirstOrDefault(p => p.StartsWith($"skills/feishu-{domain}/references/", StringComparison.Ordinal)
                                 && p.EndsWith(".md", StringComparison.Ordinal));

        DemoConsole.Section("按域 + 主题读取 references");
        if (reference is null)
        {
            DemoConsole.Line("（该域没有 references/*.md）");
        }
        else
        {
            var topic = reference.Split('/')[^1].Replace(".md", string.Empty, StringComparison.Ordinal);
            var content = reader.ReadReference(domain, topic);
            DemoConsole.Line($"{reference}（{content?.Length ?? 0} 字符）");
            DemoConsole.Line(Head(content, 500));
        }

        DemoConsole.Section("闭环：产物 → guidance 注入");
        var composed = GuidanceComposer.Compose(
            "（宿主指令占位）",
            [new GuidanceBlock(domain, skill)],
            FeishuGuidanceBudget.MaxGuidanceLength);
        DemoConsole.Line(
            $"域 {domain} 注入 {composed.Instructions.Length} 字符（预算 {FeishuGuidanceBudget.MaxGuidanceLength}；截断={composed.Truncated}）");

        DemoConsole.Section("同源自检：Export(Skills) ≡ reader");
        using var document = JsonDocument.Parse(exporter.Export(ToolSchemaDialect.Skills));
        var files = document.RootElement.GetProperty("files").EnumerateArray().ToArray();

        var mismatches = new List<string>();
        foreach (var file in files)
        {
            var path = file.GetProperty("path").GetString()!;
            var exported = file.GetProperty("content").GetString() ?? string.Empty;
            var read = reader.Read(path);

            if (read is null)
            {
                mismatches.Add($"{path}（reader 读不到）");
            }
            else if (!string.Equals(read, exported, StringComparison.Ordinal))
            {
                mismatches.Add($"{path}（长度 {read.Length} vs {exported.Length}）");
            }
        }

        var pathSetOk = reader.Paths.Count == files.Length;
        var pass = mismatches.Count == 0 && pathSetOk;

        DemoConsole.Line($"导出 {files.Length} 个文件；reader {reader.Paths.Count} 个路径");
        if (mismatches.Count > 0)
        {
            foreach (var mismatch in mismatches.Take(10))
            {
                DemoConsole.Notice(NoticeLevel.Error, mismatch);
            }
        }

        DemoConsole.Notice(pass ? NoticeLevel.Info : NoticeLevel.Error,
            pass ? "自检通过：导出与读取逐字节一致。" : "自检失败：产物与读取接缝分叉。");

        return pass ? 0 : 1;
    }

    private static string Head(string? text, int max)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "（空）";
        }

        return text.Length <= max ? text : text[..max] + "\n…（已截断显示）";
    }
}
