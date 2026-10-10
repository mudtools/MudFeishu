// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Tools;

namespace Mud.Feishu.AI.Tools;

// R-6（模块边界归位）：本类型原先住在 FeishuApiResultReader.cs 里——文件名描述"API 结果解包"，
// 而本类型是"出站文本截断"，命名与归属不一致（读者按文件名找不到它）。
// ⚠️ R1.3 评审订正：原文称本类型与 ToolResultJson 构成"循环协作"——**不成立**，
// 依赖是单向的（TruncateJson → ToolResultJson.ToText），故本项只是**可读性与归属**整理，
// 不含"消除循环依赖"这一收益。

/// <summary>
/// 工具结果文本工具：白名单投影（JsonNode 构建，AOT 安全——无反射序列化）与超长截断。
/// </summary>
/// <remarks>
/// <b>R-1 起</b>：本类型是<b>底层机制</b>，不经它直接回填模型——所有出站都走
/// <c>ToolResultPipeline</c>（唯一出口，负责预算与截断标记）。直接
/// <c>FeishuToolResult.FromText(ToolResultText.Truncate…)</c> 由
/// <c>TruncationVisibilityContractGuards</c> 机械拦截（B-1 的根因形态）。
/// </remarks>
internal static class ToolResultText
{
    /// <summary>截断标记（回填模型可读）。</summary>
    public const string TruncatedMarker = "…[truncated";

    /// <summary>
    /// AOT 安全的 <see cref="JsonArray"/> 追加：走 <c>IList{JsonNode}</c> 显式接口实现，
    /// 绕过带 <c>RequiresDynamicCode</c> 注解的泛型 <c>Add{T}</c>（T 为 JsonObject 等非原生类型时触发）。
    /// </summary>
    public static void AddNode(this JsonArray array, JsonNode? node)
        => ((System.Collections.Generic.IList<JsonNode?>)array).Add(node);

    /// <summary>把模型 tool_call 侧的任意标量值转为 <see cref="JsonNode"/>（不认识的值降级为字符串）。</summary>
    public static JsonNode? ToJsonNode(object? value) => value switch
    {
        null => null,
        string s => JsonValue.Create(s),
        bool b => JsonValue.Create(b),
        int i => JsonValue.Create(i),
        long l => JsonValue.Create(l),
        double d => JsonValue.Create(d),
        decimal m => JsonValue.Create(m),
        System.Text.Json.JsonElement e => FromJsonElement(e),
        _ => JsonValue.Create(value.ToString()),
    };

    /// <summary>按 <see cref="FeishuAgentOptions.MaxToolResultLength"/> 截断并追加标记。</summary>
    /// <remarks>
    /// <paramref name="text"/> 可空：投影函数常把"该字段本来就没有值"（如无文本块）直传进来，
    /// 若在此抛 <see cref="NullReferenceException"/>，失败会以**未处理异常**的形式冒到工具层，
    /// 而不是一个可读的空值 —— 定位成本远高于容忍一个 null。
    /// </remarks>
    public static string Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        if (maxLength < 1)
        {
            maxLength = 1;
        }

        if (text.Length <= maxLength)
        {
            return text;
        }

        return text.Substring(0, maxLength)
            + $"{TruncatedMarker}: 长度 {text.Length.ToString(CultureInfo.InvariantCulture)} 超过上限 {maxLength.ToString(CultureInfo.InvariantCulture)}，请缩小查询范围或用 page_token 翻页]";
    }

    /// <summary>
    /// JSON 感知截断（AI-FD-D12 P1D-2a 默认行为升级）：对 JSON 文本按数组属性
    /// （<c>items</c>/<c>values</c>/<c>metas</c>/<c>blocks</c> 等）删除尾部条目直至长度达标，追加
    /// <c>truncated</c>/<c>hint</c> 标记——截断不落在 JSON 结构中间，模型拿到的是合法 JSON；
    /// 解析失败（纯文本/非法 JSON）或无数组属性时退回字符截断（<see cref="Truncate"/>，既有行为）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 开销为一次 JSON 解析（结果投影路径上，AOT 安全）；
    /// 仅剩 1 条仍超限时保持合法 JSON 返回（不落到字符截断破坏结构，结构完整性优先于硬上限）。
    /// </para>
    /// <para>
    /// <b>处理<b>全部</b>数组属性而不仅是第一个</b>：有的信封天然带两个同长的大数组
    /// （典型 <c>docx.import_markdown</c> 的 <c>first_level_block_ids</c> + <c>blocks</c>）。
    /// 只剪第一个会留下另一个照样超标 —— 而 <c>truncated</c> 标记还会告诉调用方"已经处理过了"，
    /// 即"看起来守住了上限，实际没有"，比不截断更危险。
    /// </para>
    /// <para>
    /// 删除按<b>成批（每次约一半）</b>进行：逐条删除时每删 1 条都要重新序列化整棵树，
    /// 双大数组信封会退化成 O(n²) 的字符串重排；成批把迭代次数降到 O(log n)。
    /// </para>
    /// </remarks>
    public static string TruncateJson(string text, int maxLength)
    {
        if (maxLength < 1)
        {
            maxLength = 1;
        }

        if (text.Length <= maxLength)
        {
            return text;
        }

        JsonObject? root;
        try
        {
            root = JsonNode.Parse(text) as JsonObject;
        }
        catch (System.Text.Json.JsonException)
        {
            // 有意静默（守卫白名单）：截断器是**纯函数**（无 logger 面），且此处"吞掉"的是
            // "这段文本不是 JSON"这一**确定事实**——它正是回退到字符截断的判据，不是故障。
            // 当成异常上报会把"纯文本正文（docx.get_raw_content）"变成噪声日志。
            root = null; // 非 JSON 文本（如 docx.get_raw_content 的纯文本正文）：退回字符截断。
        }

        // 收集**全部**可截断的数组属性（items/values/blocks/…）。
        //
        // ⚠️ 这里必须收集所有数组，不能只取第一个：有的信封天然带**两个大数组**
        // （典型：docx.import_markdown 的 first_level_block_ids + blocks 同长）。
        // 只剪第一个会让另一个原样留下 ⇒ "已截断"但体积照样超标，而截断标记还告诉
        // 调用方"已经处理过了"，比不截断更危险。
        var arrays = new List<(JsonArray Array, string Key)>();
        if (root is not null)
        {
            foreach (var pair in root)
            {
                if (pair.Value is JsonArray { Count: > 0 } candidate)
                {
                    arrays.Add((candidate, pair.Key));
                }
            }
        }

        if (arrays.Count == 0)
        {
            return Truncate(text, maxLength);
        }

        // 记录"主导数组"（条目最多者）用于提示文案。
        var primaryKey = arrays.OrderByDescending(static a => a.Array.Count).First().Key;

        // 反复从"条目最多且仍可删"的数组尾部**成批**删除，直至整体达标。
        //
        // 成批（每次删一半）而非逐条：逐条删除时每删 1 条都要重新序列化整棵树，
        // 双大数组信封会退化成 O(n²) 的字符串重排。成批把迭代次数降到 O(log n)。
        // 每个数组至少保留 1 条，保证「有内容且可读」。
        while (ToolResultJson.ToText(root!).Length > maxLength)
        {
            var target = arrays
                .Where(static a => a.Array.Count > 1)
                .OrderByDescending(static a => a.Array.Count)
                .FirstOrDefault();

            if (target.Array is null)
            {
                break; // 所有数组都已只剩 1 条：保持合法 JSON 返回（结构完整性优先于硬上限）。
            }

            var removable = target.Array.Count - 1;
            var batch = Math.Max(1, Math.Min(removable, target.Array.Count / 2));
            for (var i = 0; i < batch; i++)
            {
                ((IList<JsonNode?>)target.Array).RemoveAt(target.Array.Count - 1);
            }
        }

        root!["truncated"] = true;
        root!["hint"] = $"结果已按 {primaryKey} 截断——请缩小查询范围或用 page_token 翻页";
        return ToolResultJson.ToText(root!);
    }

    /// <summary>结果文本是否已被截断（OTel 审计属性消费；兼容字符截断与 JSON 感知截断两种标记）。</summary>
    public static bool IsTruncated(string text)
        => text.IndexOf(TruncatedMarker, StringComparison.Ordinal) >= 0
           || text.IndexOf("\"truncated\":true", StringComparison.Ordinal) >= 0;

    private static JsonNode? FromJsonElement(System.Text.Json.JsonElement element) => element.ValueKind switch
    {
        // GetRawText 产出任意合法 JSON 文本（字符串带引号/数字/布尔/数组/对象），JsonNode.Parse 全覆盖。
        System.Text.Json.JsonValueKind.Null => null,
        System.Text.Json.JsonValueKind.Undefined => null,
        _ => JsonNode.Parse(element.GetRawText()),
    };
}
