// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

using Mud.Feishu.AI.CapabilityDemo.Host;

namespace Mud.Feishu.AI.CapabilityDemo.SelfCheck;

/// <summary>组 7：Skills 产物与读取接缝逐字节一致。</summary>
internal static class SkillsParityChecks
{
    public static IEnumerable<CheckResult> Run()
    {
        const string group = "skills-parity";

        using var provider = CompositionRoot.BuildToolsOnly();
        var reader = provider.GetRequiredService<ISkillDocumentReader>();
        var exporter = provider.GetRequiredService<IToolSchemaExporter>();

        using var document = JsonDocument.Parse(exporter.Export(ToolSchemaDialect.Skills));
        var files = document.RootElement.GetProperty("files").EnumerateArray().ToArray();

        yield return new CheckResult(group, "路径数量一致", files.Length == reader.Paths.Count,
            reader.Paths.Count.ToString(CultureInfo.InvariantCulture),
            files.Length.ToString(CultureInfo.InvariantCulture));

        var mismatched = new List<string>();
        foreach (var file in files)
        {
            var path = file.GetProperty("path").GetString()!;
            var exported = file.GetProperty("content").GetString() ?? string.Empty;
            var read = reader.Read(path);

            if (!string.Equals(read, exported, StringComparison.Ordinal))
            {
                mismatched.Add(path);
            }
        }

        yield return new CheckResult(group, "逐字节一致", mismatched.Count == 0,
            "0 处差异", string.Join(",", mismatched.Take(5)));

        var badPaths = reader.Paths
            .Where(static p => !p.StartsWith("skills/feishu-", StringComparison.Ordinal))
            .Take(5)
            .ToArray();
        yield return new CheckResult(group, "路径形态", badPaths.Length == 0,
            "skills/feishu-*/...", string.Join(",", badPaths));
    }
}
