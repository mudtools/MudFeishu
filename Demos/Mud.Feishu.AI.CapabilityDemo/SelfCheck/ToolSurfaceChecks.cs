// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

using Mud.Feishu.AI.CapabilityDemo.Common;
using Mud.Feishu.AI.CapabilityDemo.Host;

namespace Mud.Feishu.AI.CapabilityDemo.SelfCheck;

/// <summary>组 4：工具面（三源一致、bitable 28、风险/idempotency 事实）。</summary>
internal static class ToolSurfaceChecks
{
    public static IEnumerable<CheckResult> Run()
    {
        const string group = "tool-surface";

        using var provider = CompositionRoot.BuildToolsOnly();
        var catalog = provider.GetRequiredService<IToolCatalog>();
        var registry = provider.GetRequiredService<FeishuToolRegistry>();
        var exporter = provider.GetRequiredService<IToolSchemaExporter>();

        var entries = catalog.Entries;
        var catalogNames = entries.Select(static e => e.Name).ToHashSet(StringComparer.Ordinal);
        var registryNames = registry.AllTools.Select(static t => t.Name).ToHashSet(StringComparer.Ordinal);

        using var document = JsonDocument.Parse(exporter.Export(ToolSchemaDialect.McpEnvelope));
        var exportNames = document.RootElement.GetProperty("tools").EnumerateArray()
            .Select(static t => t.GetProperty("title").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        yield return new CheckResult(
            group, "catalog == registry", catalogNames.SetEquals(registryNames),
            catalogNames.Count.ToString(CultureInfo.InvariantCulture),
            registryNames.Count.ToString(CultureInfo.InvariantCulture));

        // 导出面 = 编译期策展面（恒定全量）；注册表面 = 宿主可执行面（插件缺席即软缺席）。
        // 差额因此不是漂移，而是「本 Demo 未注册 IFeishuAttachmentStager / IRetriever」这一装配事实。
        var notRegistered = exportNames.Except(catalogNames, StringComparer.Ordinal).ToArray();
        var softAbsent = ToolSets.HostPluginSoftAbsent.ToHashSet(StringComparer.Ordinal);
        var unexpectedAbsent = notRegistered.Where(n => !softAbsent.Contains(n)).Take(5).ToArray();
        var unexpectedRegistered = catalogNames.Except(exportNames, StringComparer.Ordinal).Take(5).ToArray();
        yield return new CheckResult(
            group, "catalog == mcp-envelope − 宿主插件软缺席",
            catalogNames.IsSubsetOf(exportNames) && notRegistered.Length == softAbsent.Count
                && notRegistered.All(softAbsent.Contains),
            $"差额恰为 {softAbsent.Count} 枚软缺席",
            $"多注册:{string.Join(",", unexpectedRegistered)} 意外缺席:{string.Join(",", unexpectedAbsent)}");

        var bitable = entries.Where(static e => e.Name.StartsWith("bitable.", StringComparison.Ordinal)).ToArray();
        yield return new CheckResult(
            group, "bitable 28 枚", bitable.Length == 28,
            "28", bitable.Length.ToString(CultureInfo.InvariantCulture));

        var delete = catalog.Find("bitable.delete_record");
        yield return new CheckResult(
            group, "delete_record 为 high-risk-write",
            delete is { Risk: FeishuToolRisk.HighRiskWrite },
            FeishuToolRiskNames.HighRiskWrite,
            delete is null ? "（未注册）" : FeishuToolRiskNames.ToLiteral(delete.Risk));

        var add = catalog.Find("bitable.add_record");
        yield return new CheckResult(
            group, "add_record 有 idempotency_key",
            add is not null && HasParameter(add, "idempotency_key"));

        yield return new CheckResult(
            group, "delete_record 无 idempotency_key（博客口径校正）",
            delete is not null && !HasParameter(delete, "idempotency_key"));

        var withDryRun = entries.Count(static e => e.IsWrite && HasParameter(e, "dry_run"));
        var writes = entries.Count(static e => e.IsWrite);
        yield return new CheckResult(
            group, "写工具全量声明 dry_run", withDryRun == writes,
            writes.ToString(CultureInfo.InvariantCulture), withDryRun.ToString(CultureInfo.InvariantCulture));
    }

    private static bool HasParameter(ToolCatalogEntry entry, string name)
    {
        try
        {
            using var document = JsonDocument.Parse(entry.ParameterSchemaJson);
            return document.RootElement.TryGetProperty("properties", out var properties)
                   && properties.ValueKind == JsonValueKind.Object
                   && properties.TryGetProperty(name, out _);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
