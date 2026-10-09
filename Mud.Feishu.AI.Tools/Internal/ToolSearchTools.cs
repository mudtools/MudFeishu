// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Nodes;
using Mud.Feishu.AI.Tools.Tools;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// feishu.tool_search 元工具执行器（B5：已策展工具检索）。
/// </summary>
/// <remarks>
/// <para>
/// 数据源 = <see cref="IToolCatalog"/>（注册表内部视图） + <see cref="FeishuToolRegistry"/>
/// （启用状态） + <c>FeishuToolSchemas</c>（参数 Schema）——零新真相源。
/// </para>
/// <para>
/// 与 <c>feishu.capability_lookup</c> 的区别：后者回答"SDK 里有没有这个能力"（编译期能力目录），
/// 本工具回答"已策展的工具里哪个能干这事，启用了吗"（注册表/契约表）。
/// </para>
/// </remarks>
internal sealed class ToolSearchTools(
    IOptions<FeishuAgentOptions> options,
    IToolCatalog toolCatalog,
    FeishuToolRegistry toolRegistry)
{
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;
    private readonly IToolCatalog _toolCatalog = toolCatalog ?? throw new ArgumentNullException(nameof(toolCatalog));
    private readonly FeishuToolRegistry _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));

    private const int DefaultLimit = 10;
    private const int MaxLimit = 30;

    /// <summary>feishu.tool_search：在已策展工具中搜索。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantToolSearchTool))]
    public Task<FeishuToolResult> SearchAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.FeishuToolSearch, _maxResultLength);
        return executor.RunAsync(() =>
        {
            var keyword = ToolArgs.OptionalString(arguments, "keyword");
            var domain = ToolArgs.OptionalString(arguments, "domain");
            var writeOnly = ToolArgs.OptionalBool(arguments, "write_only") ?? false;
            var readOnly = ToolArgs.OptionalBool(arguments, "read_only") ?? false;
            var limit = TryReadInt(arguments, "limit") ?? DefaultLimit;

            limit = Math.Min(Math.Max(limit, 1), MaxLimit);

            var results = new JsonArray();
            var matchedCount = 0;

            foreach (var entry in _toolCatalog.Entries)
            {
                // 域过滤
                if (!string.IsNullOrEmpty(domain))
                {
                    var entryDomain = ExtractDomain(entry.Name);
                    if (!string.Equals(entryDomain, domain, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                // 读写过滤
                if (writeOnly && !entry.IsWrite)
                {
                    continue;
                }

                if (readOnly && entry.IsWrite)
                {
                    continue;
                }

                // 关键字过滤
                if (!string.IsNullOrEmpty(keyword))
                {
                    var matchesKeyword =
                        entry.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0
                        || entry.Description.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!matchesKeyword)
                    {
                        continue;
                    }
                }

                matchedCount++;

                // limit 检查（在添加之前，以便 matchedCount 反映全部匹配数）
                if (results.Count >= limit)
                {
                    continue;
                }

                var isEnabled = _toolRegistry.IsEnabled(entry.Name);
                var item = new JsonObject
                {
                    ["tool"] = entry.Name,
                    ["domain"] = ExtractDomain(entry.Name),
                    ["is_write"] = entry.IsWrite,
                    ["risk"] = entry.Risk.ToString().ToLowerInvariant(),
                    ["identity"] = entry.Identity,
                    ["enabled"] = isEnabled,
                };

                // 提取必填参数
                var requiredParams = new JsonArray();
                var schemaParams = TryExtractRequiredParams(entry);
                foreach (var p in schemaParams)
                {
                    requiredParams.AddNode(JsonValue.Create(p));
                }

                item["required_params"] = requiredParams;

                // 未启用的提示
                if (!isEnabled)
                {
                    item["hint"] = entry.IsWrite
                        ? "宿主未启用：需加入 FeishuAgent:WriteAllowList"
                        : "宿主未启用：需加入 FeishuAgent:Tools";
                }

                results.AddNode(item);
            }

            var envelope = new JsonObject
            {
                ["results"] = results,
                ["total_matched"] = matchedCount,
                ["returned"] = results.Count,
                ["truncated"] = matchedCount > results.Count,
                ["note"] = "要查方法级签名（HTTP/路由/参数），用 feishu.schema_read。"
                    + "未启用的工具仍返回（含 enabled=false），宿主可据此判断需要放行哪些工具。",
            };

            // 空结果时给候选域建议而非空数组
            if (matchedCount == 0)
            {
                var candidateDomains = new JsonArray();
                var seenDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in _toolCatalog.Entries)
                {
                    var d = ExtractDomain(entry.Name);
                    if (seenDomains.Add(d))
                    {
                        candidateDomains.AddNode(JsonValue.Create(d));
                    }
                }

                envelope["suggestion"] = $"没有匹配的工具。可用的域：{string.Join(", ", seenDomains.OrderBy(static x => x, StringComparer.Ordinal))}。"
                    + "调整 keyword/domain 重试，或用 feishu.capability_lookup 查 SDK 是否有该能力。";
            }

            return Task.FromResult(FeishuToolResult.FromText(
                ToolResultText.TruncateJson(ToolResultJson.ToText(envelope), _maxResultLength)));
        });
    }

    /// <summary>从工具名提取域（首个 <c>.</c> 之前的部分）。</summary>
    private static string ExtractDomain(string toolName)
    {
        var index = toolName.IndexOf(".", StringComparison.Ordinal);
        return index > 0 ? toolName.Substring(0, index) : toolName;
    }

    /// <summary>尝试从参数 Schema JSON 中提取必填参数名列表。</summary>
    private static IReadOnlyList<string> TryExtractRequiredParams(ToolCatalogEntry entry)
    {
        // 从 ParameterSchemaJson 中提取 required 数组
        try
        {
            using var doc = JsonDocument.Parse(entry.ParameterSchemaJson);
            if (doc.RootElement.TryGetProperty("required", out var requiredArray))
            {
                var result = new List<string>();
                foreach (var item in requiredArray.EnumerateArray())
                {
                    var name = item.GetString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        result.Add(name);
                    }
                }

                return result;
            }
        }
        catch
        {
            // 解析失败时返回空列表（不阻断搜索）
        }

        return [];
    }

 int? TryReadInt(IReadOnlyDictionary<string, object?> arguments, string key)
    {
        if (!arguments.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            int i => i,
            long l => (int)l,
            System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.Number } e
                when e.TryGetInt32(out var i) => i,
            _ => null,
        };
    }
}
