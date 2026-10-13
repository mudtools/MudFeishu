// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Generated;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// 能力出处元工具执行器（<c>feishu.capability_lookup</c>，AT-F12）。
/// </summary>
/// <remarks>
/// <para>
/// 数据源<b>全部是编译期常量</b>（无下游调用、无网络、无租户依赖，也<b>不需要</b> <c>IToolCatalog</c>）：
/// ① <see cref="FeishuToolCapabilityCatalog.MethodsByDomain"/>——SDK 的"能力分组 → 方法数"；
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
/// <b>"是否已策展"的判据（BUG-3 修复后）</b>：分组键形如 <c>{Domain}{Resource}</c>
/// （如 <c>BitableAppTable</c>/<c>CalendarEvent</c>），而工具名前缀是小写模块名
/// （<c>bitable</c>/<c>calendar</c>）。本执行器取分组键的<b>首个 Pascal 段</b>，
/// 先经<b>模块别名表</b> <see cref="CapabilityModuleAliases"/> 解析为"该能力实际落在哪些模块"
/// （如 <c>AilyDataKnowledge</c> → <c>knowledge</c>、<c>ChatGroupAnnouncement</c> → <c>chatgroup</c>/<c>docx</c>），
/// 再与工具名前缀做大小写不敏感比对，据此标出 <c>module_curated</c> 并回填
/// <c>curated_modules</c>（解析结果）与 <c>curated_tools</c>（该模块下的工具名全集）。
/// </para>
/// <para>
/// <b>为什么需要别名表</b>：纯"首段 vs 模块前缀"的启发式在<b>跨命名</b>场景必然失真——
/// Aily 的能力已被策展为 <c>knowledge.search</c>，分组键却是 <c>AilyDataKnowledge</c>，
/// 模型会被误导为"该能力未策展/不可用"（而事实上可用）。
/// 别名表是编译期常量并由 <c>CapabilityAliasContractGuards</c> 锁定，不是运行期猜测。
/// </para>
/// </remarks>
internal sealed class CapabilityLookupTools(IOptions<FeishuAgentOptions> options)
{
    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    /// <summary>feishu.capability_lookup：按关键字检索能力分组。</summary>
    [ToolHandler(typeof(IFeishuTenantCapabilityLookupTool))]
    public Task<ToolResult> LookupAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.FeishuCapabilityLookup, _maxResultLength);
        return executor.RunAsync(() =>
        {
            var args = FeishuCapabilityLookupArgs.Unpack(arguments);

            var curatedByModule = BuildCuratedToolIndex();
            var matched = new JsonArray();
            var matchedModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in FeishuToolCapabilityCatalog.MethodsByDomain)
            {
                if (pair.Key.IndexOf(args.Keyword, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                var leadingSegment = LeadingPascalSegment(pair.Key);

                // BUG-3：跨命名分组经别名表解析到真实模块（如 AilyDataKnowledge → knowledge），
                // 否则 module_curated 会因"首段 ≠ 模块前缀"而误报"未策展"，让模型以为能力不可用。
                var modules = CapabilityModuleAliases.ResolveModules(leadingSegment);

                // 别名命中的模块也计入 matched：curated_tools 回填必须覆盖"该分组实际对应的工具"。
                foreach (var module in modules)
                {
                    matchedModules.Add(module);
                }

                matched.AddNode(new JsonObject
                {
                    ["group"] = pair.Key,
                    ["methods"] = pair.Value,
                    ["module_curated"] = modules.Any(curatedByModule.ContainsKey),
                    ["curated_modules"] = new JsonArray([.. modules.Select(static m => (JsonNode?)JsonValue.Create(m))]),
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
                ["sdk_method_count"] = FeishuToolCapabilityCatalog.SdkMethodCount,
                ["curated_tool_count"] = FeishuToolCapabilityCatalog.CuratedToolCount,
                ["matched_group_count"] = matched.Count,
                ["matched_groups"] = matched,
                ["curated_tools"] = curatedTools,
                ["actionable"] = curatedTools.Count > 0,
                ["note"] = "回答能力分组级的存在性。要查方法级签名（HTTP/路由/参数），用 feishu.schema_read。"
                    + "curated_tools 为空表示该能力尚未被策展为工具，本宿主的工具集里没有它——不要臆造调用。"
                    + "actionable=true 表示有对应工具可直接调用；actionable=false 表示能力存在但未策展为工具，"
                    + "**本工具集没有调用通道**：请如实告知用户该能力暂不可用（可建议宿主策展）。",
            };

            // R-1：出站唯一出口（B-1 一类——此前 TruncateJson 的截断对模型不可见）。
            return Task.FromResult(ToolResultPipeline.OkJson(envelope, _maxResultLength));
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

    /// <summary>
    /// feishu.guidance_read：按需读取 L2 references（R5 / F-9）。
    /// </summary>
    /// <remarks>
    /// <b>键不存在时必须给候选清单</b>：只回"未找到"会让模型反复试错；
    /// 把可用的 <c>{domain}/{topic}</c> 全列出来，模型一次就能选对（对齐 F-8 的可恢复性要求）。
    /// </remarks>
    [ToolHandler(typeof(IFeishuTenantGuidanceReadTool))]
    public Task<ToolResult> GuidanceReadAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.FeishuGuidanceRead, _maxResultLength);
        return executor.RunAsync(() =>
        {
            var args = FeishuGuidanceReadArgs.Unpack(arguments);
            var key = $"{args.Domain.Trim()}/{args.Topic.Trim()}";

            if (Mud.Feishu.AI.Tools.Generated.FeishuToolGuidance.TryGetReference(key, out var text))
            {
                // R-1：出站唯一出口（B-1 一类）。
                return Task.FromResult(ToolResultPipeline.Ok(text, _maxResultLength));
            }

            var available = Mud.Feishu.AI.Tools.Generated.FeishuToolGuidance.ReferenceKeys;
            throw new ArgumentException(
                $"没有 L2 guidance '{key}'。可用的键：{string.Join(" / ", available)}。"
                + "（键 = {域}/{主题}，域即工具名前缀；请从上列清单里选，不要自行拼造）");
        });
    }
}