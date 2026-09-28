// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.FeishuTools.Tools;
using Mud.Feishu.AI.Tools.Generated;

namespace Mud.Feishu.AI.FeishuTools.Internal;

/// <summary>
/// 能力出处元工具执行器（<c>feishu.capability_lookup</c>，AT-F12）。
/// </summary>
/// <remarks>
/// <para>
/// 数据源<b>全部是编译期常量</b>（无下游调用、无网络、无租户依赖，也<b>不需要</b> <c>IToolCatalog</c>）：
/// ① <see cref="FeishuCapabilityCatalog.MethodsByDomain"/>——SDK 的"能力分组 → 方法数"；
/// ② <see cref="FeishuToolSchemas.SchemaByToolName"/>——已策展工具名契约表。
/// </para>
/// <para>
/// <b>为什么用 <c>FeishuToolSchemas</c> 而不是 <c>IToolCatalog</c>（两条理由）</b>：
/// ① 语义更准——本工具回答的是"<b>是否已策展</b>"（编译期契约），而非"本宿主当前是否已接线"；
/// ② 避免 DI 循环——<c>FeishuToolRegistry</c> 的工厂要枚举域注册器，而注册器要构造本执行器，
/// 若本执行器反向依赖由注册表派生的 <c>IToolCatalog</c>，解析链会自我闭环（MS DI 会直接报
/// "circular dependency"）。用静态编译期常量从结构上消除该问题，而不是靠懒解析绕开。
/// </para>
/// <para>
/// <b>"是否已策展"是启发式（如实记录）</b>：分组键形如 <c>{Domain}{Resource}</c>
/// （如 <c>BitableAppTable</c>/<c>CalendarEvent</c>），而工具名前缀是小写模块名
/// （<c>bitable</c>/<c>calendar</c>）。本执行器取分组键的<b>首个 Pascal 段</b>与工具名前缀
/// 做大小写不敏感比对，据此标出 <c>module_curated</c> 并回填该模块下的工具名。
/// 该启发式会在命名跨模块的场景失真（例如 Aily 能力被策展成 <c>knowledge.search</c>，
/// 分组键却是 <c>AilyDataKnowledge</c>，会被判为"未策展"）——故结果中同时回填
/// <c>curated_tools</c> 全集，让模型可以自行核对，而不是只信一个布尔位。
/// </para>
/// </remarks>
internal sealed class CapabilityLookupTools(IOptions<FeishuAgentOptions> options)
{
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;

    /// <summary>feishu.capability_lookup：按关键字检索能力分组。</summary>
    [FeishuToolHandler(typeof(IFeishuCapabilityLookupTool))]
    public Task<FeishuToolResult> LookupAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.FeishuCapabilityLookup, _maxResultLength);
        return executor.RunAsync(() =>
        {
            var args = FeishuCapabilityLookupArgs.Unpack(arguments);

            var curatedByModule = BuildCuratedToolIndex();
            var matched = new JsonArray();
            var matchedModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in FeishuCapabilityCatalog.MethodsByDomain)
            {
                if (pair.Key.IndexOf(args.Keyword, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                var leadingSegment = LeadingPascalSegment(pair.Key);
                matchedModules.Add(leadingSegment);
                matched.AddNode(new JsonObject
                {
                    ["group"] = pair.Key,
                    ["methods"] = pair.Value,
                    ["module_curated"] = curatedByModule.ContainsKey(leadingSegment),
                });
            }

            var curatedTools = new JsonArray();
            foreach (var pair in curatedByModule)
            {
                if (matchedModules.Count == 0 || matchedModules.Contains(pair.Key))
                {
                    foreach (var toolName in pair.Value)
                    {
                        curatedTools.AddNode(JsonValue.Create(toolName));
                    }
                }
            }

            var envelope = new JsonObject
            {
                ["sdk_method_count"] = FeishuCapabilityCatalog.SdkMethodCount,
                ["curated_tool_count"] = FeishuCapabilityCatalog.CuratedToolCount,
                ["matched_group_count"] = matched.Count,
                ["matched_groups"] = matched,
                ["curated_tools"] = curatedTools,
                ["note"] = "仅回答能力分组级的存在性：本工具不返回方法名与请求构造（方法名不在编译期产物中）。"
                    + "curated_tools 为空表示该能力尚未被策展为工具，本宿主的工具集里没有它——不要臆造调用。",
            };

            return Task.FromResult(FeishuToolResult.FromText(
                ToolResultText.TruncateJson(envelope.ToJsonString(), _maxResultLength)));
        });
    }

    /// <summary>构造"模块 → 已策展工具名（有序）"索引。</summary>
    private static SortedDictionary<string, List<string>> BuildCuratedToolIndex()
    {
        var index = new SortedDictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var toolName in FeishuToolSchemas.SchemaByToolName.Keys.OrderBy(static k => k, StringComparer.Ordinal))
        {
            var module = ModulePrefix(toolName);
            if (!index.TryGetValue(module, out var tools))
            {
                tools = [];
                index[module] = tools;
            }

            tools.Add(toolName);
        }

        return index;
    }

    /// <summary>取模块（工具名首个 <c>.</c> 之前的部分）。</summary>
    private static string ModulePrefix(string toolName)
    {
        // 注：netstandard2.0 没有 IndexOf(char, StringComparison)，用字符串重载。
        var index = toolName.IndexOf(".", StringComparison.Ordinal);
        return index > 0 ? toolName.Substring(0, index) : toolName;
    }

    /// <summary>取分组键的首个 Pascal 段（<c>BitableAppTable</c> → <c>Bitable</c>；<c>ImMessage</c> → <c>Im</c>）。</summary>
    private static string LeadingPascalSegment(string groupName)
    {
        var end = 1;
        while (end < groupName.Length && char.IsLower(groupName[end]))
        {
            end++;
        }

        return groupName.Substring(0, end);
    }
}
