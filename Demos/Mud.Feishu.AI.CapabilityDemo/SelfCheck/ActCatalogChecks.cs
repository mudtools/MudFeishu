// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

using Mud.Feishu.AI.CapabilityDemo.Common;

namespace Mud.Feishu.AI.CapabilityDemo.SelfCheck;

/// <summary>组 1：幕目录（id/别名唯一、章节齐备、能力声明自洽）。</summary>
internal static class ActCatalogChecks
{
    public static IEnumerable<CheckResult> Run()
    {
        const string group = "act-catalog";
        var acts = ActCatalog.Acts.ToArray();

        yield return Check(group, "八幕齐备", acts.Length == 8, "8", acts.Length.ToString(CultureInfo.InvariantCulture));

        var ids = acts.Select(static a => a.Id).ToArray();
        var aliases = acts.Select(static a => a.Alias).ToArray();

        yield return Check(group, "id 唯一", ids.Distinct(StringComparer.OrdinalIgnoreCase).Count() == ids.Length,
            ids.Length.ToString(CultureInfo.InvariantCulture),
            ids.Distinct(StringComparer.OrdinalIgnoreCase).Count().ToString(CultureInfo.InvariantCulture));

        yield return Check(group, "别名唯一", aliases.Distinct(StringComparer.OrdinalIgnoreCase).Count() == aliases.Length);

        var missingSections = acts.Where(static a => string.IsNullOrWhiteSpace(a.BlogSection)).Select(static a => a.Id).ToArray();
        yield return Check(group, "每幕有博客章节", missingSections.Length == 0, "全部非空", string.Join(",", missingSections));

        var offlineWithModel = acts.Where(static a => a.IsOffline && a.RequiresModel).Select(static a => a.Id).ToArray();
        yield return Check(group, "离线幕不需要模型", offlineWithModel.Length == 0, "无", string.Join(",", offlineWithModel));

        var dataWithoutModel = acts.Where(static a => a.RequiresData && !a.RequiresModel).Select(static a => a.Id).ToArray();
        yield return Check(group, "需要数据的幕也需要模型", dataWithoutModel.Length == 0, "无", string.Join(",", dataWithoutModel));

        var verify = ActCatalog.Resolve("verify");
        yield return Check(group, "verify 可解析", verify is { Id: "verify" });

        yield return Check(group, "a1 可解析（按别名）", ActCatalog.Resolve("hello") is { Id: "a1" });

        yield return Check(group, "未知值不解析", ActCatalog.Resolve("nope") is null);
    }

    private static CheckResult Check(
        string group, string name, bool passed, string? expected = null, string? actual = null)
        => new(group, name, passed, expected, actual);
}
