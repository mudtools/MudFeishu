// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

using Mud.Feishu.AI.CapabilityDemo.Common;
using Mud.Feishu.AI.CapabilityDemo.Host;

namespace Mud.Feishu.AI.CapabilityDemo.SelfCheck;

/// <summary>组 2：白名单（存在性、读写分类、不交叉、危险工具排除）。</summary>
internal static class WhitelistChecks
{
    public static IEnumerable<CheckResult> Run()
    {
        const string group = "whitelist";

        using var provider = CompositionRoot.BuildToolsOnly();
        var catalog = provider.GetRequiredService<IToolCatalog>();
        var names = catalog.Entries.Select(static e => e.Name).ToHashSet(StringComparer.Ordinal);

        var settings = new DemoSettings { ModelId = "verify", ApiKey = "verify" };
        var profiles = new[]
        {
            ActProfiles.A1(settings),
            ActProfiles.A3(settings),
            ActProfiles.A5(settings, includeDelete: false),
            ActProfiles.A8(settings),
            ActProfiles.A6(settings),
        };

        foreach (var profile in profiles)
        {
            var missingRead = profile.ReadTools.Where(n => !names.Contains(n)).ToArray();
            yield return Check(group, $"{profile.Id} 只读名存在", missingRead.Length == 0,
                "全部存在", string.Join(",", missingRead));

            var missingWrite = profile.WriteTools.Where(n => !names.Contains(n)).ToArray();
            yield return Check(group, $"{profile.Id} 写名存在", missingWrite.Length == 0,
                "全部存在", string.Join(",", missingWrite));

            var misclassifiedRead = profile.ReadTools
                .Select(n => catalog.Find(n))
                .Where(e => e is not null && e.IsWrite)
                .Select(static e => e!.Name)
                .ToArray();
            yield return Check(group, $"{profile.Id} 只读名确为只读", misclassifiedRead.Length == 0,
                "无写工具", string.Join(",", misclassifiedRead));

            var misclassifiedWrite = profile.WriteTools
                .Select(n => catalog.Find(n))
                .Where(e => e is not null && !e.IsWrite)
                .Select(static e => e!.Name)
                .ToArray();
            yield return Check(group, $"{profile.Id} 写名确为写", misclassifiedWrite.Length == 0,
                "无只读工具", string.Join(",", misclassifiedWrite));
        }

        var a5 = ActProfiles.A5(settings, includeDelete: false);
        yield return Check(group, "A5 读写不交叉",
            a5.ReadTools.Intersect(a5.WriteTools, StringComparer.Ordinal).Any() is false);

        yield return Check(group, "A5 默认不含删除",
            a5.WriteTools.Contains(ToolSets.BitableDelete, StringComparer.Ordinal) is false,
            $"不含 {ToolSets.BitableDelete}");

        yield return Check(group, "A6 不含写工具",
            ActProfiles.A6(settings).WriteTools.Length == 0, "0", ActProfiles.A6(settings).WriteTools.Length.ToString(CultureInfo.InvariantCulture));

        yield return Check(group, "元工具齐备",
            ToolSets.Meta4.All(names.Contains), "4 枚", string.Join(",", ToolSets.Meta4.Where(n => !names.Contains(n))));
    }

    private static CheckResult Check(
        string group, string name, bool passed, string? expected = null, string? actual = null)
        => new(group, name, passed, expected, actual);
}
