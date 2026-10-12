// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Nodes;
using Mud.Feishu.AI.Tools;

namespace Mud.Feishu.AI.Tools.Catalog;

/// <summary>
/// 工具目录实现（AI-FD-D12 P1D-4）：包装 <see cref="FeishuToolRegistry.AllTools"/> 为
/// <see cref="IToolCatalog"/> 稳定契约——构建期一次性投影，运行期零开销枚举。
/// </summary>
internal sealed class FeishuToolCatalog : IToolCatalog
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
/// 零反射零成本；方言 = <see cref="ToolSchemaDialect.OpenAiFunctions"/>（OpenAI tools 数组）与
/// <see cref="ToolSchemaDialect.Skills"/>（R7 / C6a：<c>SKILL.md</c> 产物清单）。
/// </summary>
internal sealed class FeishuToolSchemaExporter : IToolSchemaExporter
{
    /// <inheritdoc />
    public string Export(ToolSchemaDialect dialect)
    {
        // R3-19 复核：fail-fast 有意为之——非法方言不得降级为"空列表"（静默产出空 Schema 会让
        // 消费方以为"该方言下无工具"）。既有用例 FeishuToolCatalogTests.Export_ShouldRejectUnsupportedDialect
        // 已断言本异常，非缺陷。
        return dialect switch
        {
            ToolSchemaDialect.OpenAiFunctions => ExportOpenAiFunctions(),
            ToolSchemaDialect.Skills => FeishuSkillDocumentBuilder.BuildManifestJson(),
            ToolSchemaDialect.McpEnvelope => ExportMcpEnvelope(),
            _ => throw new ArgumentOutOfRangeException(
                nameof(dialect),
                $"暂不支持的导出方言: {dialect}（Aily 归后续批次；未实现方言不得降级为空产物）"),
        };
    }

    /// <summary>
    /// R-3 / F-9：导出<b>全部已策展工具</b>的 MCP <c>tools/list</c> 形信封。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>数据源全部是编译期常量</b>（<c>FeishuToolNames.All</c> + <c>FeishuToolSchemas</c> +
    /// <c>FeishuToolContracts</c>），零反射零运行期依赖——AOT 安全，且导出结果与
    /// "工具面真实存在什么"逐条同源（守卫断言条目双向相等）。
    /// </para>
    /// <para>
    /// 与 MCP server 的 <c>tools/list</c> 的<b>关键差异</b>：本导出不受
    /// <c>FeishuAgent:Tools</c> / <c>WriteAllowList</c> 影响（那是"启用"语义，属宿主运行期配置）；
    /// 本方言回的是"能力存在"语义——正因如此它才适合 CI / 桥接器做静态差异比对。
    /// </para>
    /// </remarks>
    private static string ExportMcpEnvelope()
    {
        var tools = new JsonArray();

        foreach (var name in FeishuToolNames.All.OrderBy(static n => n, StringComparer.Ordinal))
        {
            if (!FeishuToolSchemas.SchemaByToolName.TryGetValue(name, out var schemaJson)
                || JsonNode.Parse(schemaJson) is not JsonObject schema
                || !FeishuToolContracts.ByToolName.TryGetValue(name, out var contract))
            {
                // 编译期常量正常时不可达（契约守卫锁死"工具名 ⊆ Schema 常量表 ⊆ 契约表"）；
                // 防御性跳过而不是让整份导出失败——单条缺失不该使下游拿不到其余工具。
                continue;
            }

            var tool = new JsonObject
            {
                ["name"] = ToMcpToolName(name),

                // title 保留契约名：与《工具权限对照表》同名，排查时不必再做"下划线/点"的心算
                //（与 MCP server 的 tools/list 同口径）。
                ["title"] = name,
                ["description"] = schema["description"]?.GetValue<string>(),
                ["inputSchema"] = schema["parameters"] is { } parameters
                    ? JsonNode.Parse(parameters.ToJsonString())
                    : new JsonObject(),
                ["annotations"] = new JsonObject
                {
                    ["readOnlyHint"] = !contract.IsWrite,
                    ["destructiveHint"] = contract.Risk == FeishuToolRisk.HighRiskWrite,
                    ["openWorldHint"] = true,
                },
            };

            // R-3 / F-7：per-tool affordance 结构化透传（仅配了资产的工具有该字段 ⇒ 未配资产的
            // 工具导出结果逐字节不变）。它**不**进 inputSchema（那是纯参数 Schema），而是并列字段：
            // 让宿主 / CI / 桥接器能按 `when` / `avoid_when` 机械消费，而不必读模型侧描述文本。
            if (ToolAffordanceText.TryGetNode(schema) is { } affordance)
            {
                tool["affordance"] = affordance.DeepClone();
            }

            tools.AddNode(tool);
        }

        return new JsonObject
        {
            ["dialect"] = "mcp-envelope",
            ["tools"] = tools,
        }.ToJsonString();
    }

    /// <summary>契约名 → MCP 工具名（<c>.</c> → <c>_</c>）。</summary>
    /// <remarks>
    /// 与 <c>Mud.Feishu.AI.Mcp</c> 的 <c>McpToolNames.ToMcpName</c> 是<b>同一行为的两处实现</b>：
    /// 依赖方向（Mcp → 本包）禁止反向引用，故无法直接复用。
    /// 二者一致性由 <c>McpEnvelopeExportContractGuards</c>（测试工程同时引用两包）机械锁定——
    /// 任一实现漂移即报红，而不是靠注释约定。
    /// </remarks>
    private static string ToMcpToolName(string contractName) => contractName.Replace('.', '_');

    private static string ExportOpenAiFunctions()
    {
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
