// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Nodes;
using Mud.Feishu.AI.Tools;

namespace Mud.Feishu.AI.FeishuTools.Tools;

/// <summary>
/// 工具目录实现（AI-FD-D12 P1D-4）：包装 <see cref="FeishuToolRegistry.AllTools"/> 为
/// <see cref="IToolCatalog"/> 稳定契约——构建期一次性投影，运行期零开销枚举。
/// </summary>
public sealed class FeishuToolCatalog : IToolCatalog
{
    private readonly IReadOnlyList<ToolCatalogEntry> _entries;
    private readonly Dictionary<string, ToolCatalogEntry> _byName = new(StringComparer.Ordinal);

    /// <summary>
    /// 从工具注册表构建目录（宿主启动期调用一次）。
    /// </summary>
    /// <param name="registry">工具注册表。</param>
    /// <returns>目录。</returns>
    public static FeishuToolCatalog From(FeishuToolRegistry registry)
    {
        if (registry is null)
            throw new ArgumentNullException(nameof(registry));

        var entries = new List<ToolCatalogEntry>();
        foreach (var definition in registry.AllTools)
        {
            var schemaJson = FeishuToolSchemas.SchemaByToolName.TryGetValue(definition.Name, out var schema)
                ? schema
                : string.Empty;

            // SDK 源符号只存在于编译期契约（注册表定义不携带它）——目录补齐 risk/identity/source 三项
            // （R4/WP2 / F-2：契约事实已有，此前缺类型化出口）。
            var sdkSource = FeishuToolContracts.ByToolName.TryGetValue(definition.Name, out var contract)
                ? contract.SdkSource
                : string.Empty;

            var entry = new ToolCatalogEntry(
                definition.Name,
                definition.Description,
                definition.RequiredScopes,
                definition.IsWrite,
                definition.Risk,
                definition.Identity,
                sdkSource,
                ExtractParameterSchema(schemaJson));
            entries.Add(entry);
        }

        return new FeishuToolCatalog(entries);
    }

    private FeishuToolCatalog(IReadOnlyList<ToolCatalogEntry> entries)
    {
        _entries = entries;
        foreach (var entry in entries)
        {
            _byName[entry.Name] = entry;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ToolCatalogEntry> Entries => _entries;

    /// <inheritdoc />
    public ToolCatalogEntry? Find(string toolName)
        => !string.IsNullOrWhiteSpace(toolName) && _byName.TryGetValue(toolName!, out var entry) ? entry : null;

    /// <summary>
    /// 从编译期 Schema 常量提取纯参数 JSON Schema（与 <see cref="FeishuToolAIFunction.JsonSchema"/> 同源，
    /// 见 <see cref="ToolSchemaJson"/>；缺 Schema 时为空对象 Schema）。
    /// </summary>
    private static string ExtractParameterSchema(string schemaJson)
    {
        if (string.IsNullOrEmpty(schemaJson))
        {
            return ToolSchemaJson.EmptyParametersJson;
        }

        try
        {
            return ToolSchemaJson.ExtractParametersJson(schemaJson);
        }
        catch (JsonException)
        {
            // 有意静默（守卫白名单）：本方法是"读取编译期常量"的纯函数（无 logger），
            // 且失败输出确定（空参数 Schema，仍是合法 JSON Schema）。常量损坏由 golden 门禁在构建期拦下。
            return ToolSchemaJson.EmptyParametersJson;
        }
    }
}

/// <summary>
/// 工具 Schema 导出实现（AI-FD-D12 P1D-4）：数据源 = 编译期 <see cref="FeishuToolSchemas"/> 常量，
/// 零反射零成本；首版方言 <see cref="ToolSchemaDialect.OpenAiFunctions"/>。
/// </summary>
public sealed class FeishuToolSchemaExporter : IToolSchemaExporter
{
    /// <inheritdoc />
    public string Export(ToolSchemaDialect dialect)
    {
        if (dialect != ToolSchemaDialect.OpenAiFunctions)
        {
            throw new ArgumentOutOfRangeException(nameof(dialect), $"暂不支持的导出方言: {dialect}（Skills/Aily/MCP 归 Phase 4）");
        }

        var array = new JsonArray();
        foreach (var pair in FeishuToolSchemas.SchemaByToolName.OrderBy(static p => p.Key, StringComparer.Ordinal))
        {
            try
            {
                if (JsonNode.Parse(pair.Value) is not JsonObject schema)
                {
                    continue;
                }

                var function = new JsonObject
                {
                    ["name"] = schema["name"]?.GetValue<string>(),
                    ["description"] = schema["description"]?.GetValue<string>(),
                    ["parameters"] = schema["parameters"] is { } parameters
                        ? JsonNode.Parse(parameters.ToJsonString())
                        : new JsonObject(),
                };
                array.AddNode(new JsonObject
                {
                    ["type"] = "function",
                    ["function"] = function,
                });
            }
            catch (JsonException)
            {
                // 单条 Schema 异常跳过（编译期常量正常时不可达；防御性隔离）。
                // 有意静默（守卫白名单）：纯函数（无 logger）且"跳过单条"是确定的降级语义，
                // 常量损坏在构建期已由 golden 门禁与 MUDFT014 拦下，运行期记日志无新增信息。
            }
        }

        return array.ToJsonString();
    }
}
