// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

using Mud.Feishu.AI.CapabilityDemo.Common;
using Mud.Feishu.AI.CapabilityDemo.Host;

namespace Mud.Feishu.AI.CapabilityDemo.Acts;

/// <summary>
/// A2：工具面剖面（离线）。计数一律运行期派生，<b>不写死 331</b>。
/// </summary>
internal static class A2SurfaceAct
{
    public static int Run()
    {
        using var provider = CompositionRoot.BuildToolsOnly();
        var catalog = provider.GetRequiredService<IToolCatalog>();
        var registry = provider.GetRequiredService<FeishuToolRegistry>();
        var exporter = provider.GetRequiredService<IToolSchemaExporter>();

        var entries = catalog.Entries;
        var read = entries.Count(static e => !e.IsWrite);
        var highRisk = entries.Count(static e => e.Risk == FeishuToolRisk.HighRiskWrite);

        DemoConsole.Banner("A2 · 331 的剖面（离线，无凭证）", new Dictionary<string, string?>
        {
            ["目录条目"] = entries.Count.ToString(CultureInfo.InvariantCulture),
            ["只读 / 写"] = $"{read} / {entries.Count - read}",
            ["高风险写"] = highRisk.ToString(CultureInfo.InvariantCulture),
            ["tenant / user"] = $"{entries.Count(e => e.Identity == "tenant")} / {entries.Count(e => e.Identity == "user")}",
            ["域数"] = entries.Select(e => e.Name.Split('.')[0]).Distinct(StringComparer.Ordinal).Count()
                .ToString(CultureInfo.InvariantCulture),
            ["口径"] = "目录不含 http/route；调用形状请看 dry_run 的返回值（A5）",
        });

        DemoConsole.Section("域矩阵（按工具数降序）");
        var domains = entries
            .GroupBy(e => e.Name.Split('.')[0], StringComparer.Ordinal)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.Ordinal);

        DemoConsole.Table(
            ["域", "工具", "只读", "普通写", "高风险写", "tenant", "user"],
            domains.Select(g => new[]
            {
                g.Key,
                g.Count().ToString(CultureInfo.InvariantCulture),
                g.Count(e => !e.IsWrite).ToString(CultureInfo.InvariantCulture),
                g.Count(e => e.IsWrite && e.Risk == FeishuToolRisk.Write).ToString(CultureInfo.InvariantCulture),
                g.Count(e => e.Risk == FeishuToolRisk.HighRiskWrite).ToString(CultureInfo.InvariantCulture),
                g.Count(e => e.Identity == "tenant").ToString(CultureInfo.InvariantCulture),
                g.Count(e => e.Identity == "user").ToString(CultureInfo.InvariantCulture),
            }).ToArray());

        var bitable = entries.Where(e => e.Name.StartsWith("bitable.", StringComparison.Ordinal)).ToArray();
        DemoConsole.Section($"bitable 域明细（{bitable.Length} 枚）");
        DemoConsole.Table(
            ["工具", "风险", "写", "dry_run", "idempotency_key", "scopes"],
            bitable.Select(e => new[]
            {
                e.Name,
                FeishuToolRiskNames.ToLiteral(e.Risk),
                e.IsWrite ? "✓" : "-",
                HasParameter(e, "dry_run") ? "✓" : "-",
                HasParameter(e, "idempotency_key") ? "✓" : "-",
                e.RequiredScopes.Count == 0 ? "-" : string.Join(",", e.RequiredScopes),
            }).ToArray());

        DemoConsole.Section("单工具信封（McpEnvelope 的 affordance 段）");
        var envelope = exporter.Export(ToolSchemaDialect.McpEnvelope);
        using var envelopeDocument = JsonDocument.Parse(envelope);
        var sample = envelopeDocument.RootElement.GetProperty("tools").EnumerateArray()
            .FirstOrDefault(t => t.TryGetProperty("title", out var title)
                                 && title.GetString() == "bitable.update_record");

        if (sample.ValueKind == JsonValueKind.Object)
        {
            var pretty = JsonSerializer.Serialize(sample, new JsonSerializerOptions { WriteIndented = true });
            DemoConsole.Line(pretty.Length > 2400 ? pretty[..2400] + "\n…（已截断显示）" : pretty);
        }
        else
        {
            DemoConsole.Notice(NoticeLevel.Warn, "未在 McpEnvelope 中找到 bitable.update_record。");
        }

        DemoConsole.Section("三源一致性（catalog / registry / McpEnvelope）");
        var catalogNames = entries.Select(static e => e.Name).ToHashSet(StringComparer.Ordinal);
        var registryNames = registry.AllTools.Select(static t => t.Name).ToHashSet(StringComparer.Ordinal);
        var exportNames = envelopeDocument.RootElement.GetProperty("tools").EnumerateArray()
            .Select(static t => t.GetProperty("title").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        var catalogVsRegistry = catalogNames.SetEquals(registryNames);

        // 导出面 = 编译期策展面（恒全量）；注册表面 = 宿主可执行面。差额只允许是「宿主插件软缺席」。
        var softAbsent = ToolSets.HostPluginSoftAbsent.ToHashSet(StringComparer.Ordinal);
        var notRegistered = exportNames.Except(catalogNames, StringComparer.Ordinal).ToArray();
        var catalogVsExport = catalogNames.IsSubsetOf(exportNames)
            && notRegistered.Length == softAbsent.Count
            && notRegistered.All(softAbsent.Contains);
        var mcpCount = exportNames.Count;

        DemoConsole.Line($"catalog={catalogNames.Count}  registry={registryNames.Count}  mcp-envelope={mcpCount}");
        DemoConsole.Notice(catalogVsRegistry ? NoticeLevel.Info : NoticeLevel.Error,
            catalogVsRegistry ? "catalog == registry" : "catalog != registry");
        DemoConsole.Notice(catalogVsExport ? NoticeLevel.Info : NoticeLevel.Error,
            catalogVsExport
                ? $"catalog == mcp-envelope − {softAbsent.Count} 枚宿主插件软缺席"
                : "catalog != mcp-envelope（超出软缺席的漂移）");

        if (catalogVsExport && notRegistered.Length > 0)
        {
            DemoConsole.Line($"软缺席（未注入 IFeishuAttachmentStager / IRetriever）：{string.Join("、", notRegistered)}");
        }

        var consistent = catalogVsRegistry && catalogVsExport;
        DemoConsole.Line();
        DemoConsole.Notice(consistent ? NoticeLevel.Info : NoticeLevel.Error,
            consistent ? "自检通过：三源一致。" : "自检失败：工具面存在漂移。");
        return consistent ? 0 : 1;
    }

    private static bool HasParameter(ToolCatalogEntry entry, string parameterName)
    {
        try
        {
            using var document = JsonDocument.Parse(entry.ParameterSchemaJson);
            return document.RootElement.TryGetProperty("properties", out var properties)
                   && properties.ValueKind == JsonValueKind.Object
                   && properties.TryGetProperty(parameterName, out _);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
