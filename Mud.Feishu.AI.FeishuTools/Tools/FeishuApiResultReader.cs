// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;

namespace Mud.Feishu.AI.FeishuTools;

/// <summary>
/// <c>FeishuApiResult</c> 解包结果（Phase 1 §3.3.3 通用规则 3：统一解包，错误转可读文本回填模型）。
/// </summary>
/// <typeparam name="T">业务载荷类型。</typeparam>
/// <param name="Ok">是否成功（<c>code == 0</c> 且载荷非空）。</param>
/// <param name="Data">业务载荷（成功时非空）。</param>
/// <param name="ErrorText">失败原因（可读文本，回填模型）。</param>
/// <param name="Code">飞书业务 code（错误分类消费；无 code 场景为 null）。</param>
internal sealed record FeishuApiOutcome<T>(bool Ok, T? Data, string? ErrorText, int? Code = null) where T : class
{
    /// <summary>构造成功结果。</summary>
    public static FeishuApiOutcome<T> Success(T data) => new(true, data, null);

    /// <summary>构造失败结果。</summary>
    /// <param name="errorText">可读错误文本（回填模型）。</param>
    /// <param name="code">飞书业务 code（P1D-2b 错误分类消费；无 code 场景为 null）。</param>
    public static FeishuApiOutcome<T> Fail(string errorText, int? code = null) => new(false, null, errorText, code);
}

/// <summary>
/// <see cref="FeishuApiResult{T}"/> 统一解包器（分域执行器共用；<c>code != 0</c> → 可读错误文本，
/// 不吞错误、不抛裸异常）。
/// </summary>
internal static class FeishuApiResultReader
{
    /// <summary>解包标准结果。</summary>
    /// <typeparam name="T">业务载荷类型。</typeparam>
    /// <param name="result">源生成客户端返回结果（可能为 null——网络层已吞异常的情况）。</param>
    /// <returns>解包结果。</returns>
    public static FeishuApiOutcome<T> Read<T>(FeishuApiResult<T>? result) where T : class
    {
        if (result is null)
        {
            return FeishuApiOutcome<T>.Fail("飞书接口无响应（result 为空）");
        }

        if (result.Code != 0)
        {
            return FeishuApiOutcome<T>.Fail(
                $"飞书接口返回错误 code={result.Code.ToString(CultureInfo.InvariantCulture)}, msg={result.Msg ?? "(无错误信息)"}",
                result.Code);
        }

        if (result.Data is null)
        {
            return FeishuApiOutcome<T>.Fail("飞书接口返回空数据");
        }

        return FeishuApiOutcome<T>.Success(result.Data);
    }
}

/// <summary>
/// 工具入参读取器：模型 tool_call 的参数字典 → 强类型标量（值可能为 JsonElement/字符串/数字）。
/// </summary>
internal static class ToolArgs
{
    /// <summary>读取必填字符串参数。</summary>
    /// <exception cref="ArgumentException">缺失或为空（执行链转结构化错误回填模型）。</exception>
    public static string RequireString(IReadOnlyDictionary<string, object?> arguments, string name)
    {
        var value = OptionalString(arguments, name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"缺少必填参数 {name}");
        }

        return value!;
    }

    /// <summary>读取可选字符串参数。</summary>
    public static string? OptionalString(IReadOnlyDictionary<string, object?> arguments, string name)
        => Convert(arguments.TryGetValue(name, out var value) ? value : null);

    /// <summary>读取可选布尔参数（兼容 JSON <c>true/false</c> 与字符串 <c>"true"/"false"</c>）。</summary>
    public static bool? OptionalBool(IReadOnlyDictionary<string, object?> arguments, string name)
    {
        if (!arguments.TryGetValue(name, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            bool b => b,
            System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.True } => true,
            System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.False } => false,
            System.Text.Json.JsonElement e => bool.TryParse(e.GetRawText().Trim('"'), out var parsed) ? parsed : null,
            string s => bool.TryParse(s, out var parsed) ? parsed : null,
            _ => null,
        };
    }

    /// <summary>读取必填字符串数组参数（缺失或为 <b>空数组</b> → <see cref="ArgumentException"/>，与 <see cref="RequireString"/> 同构）。</summary>
    /// <remarks>空数组视同缺失：飞书侧所有批量接口对空列表都无有意义语义（返回空结果或报错），
    /// 在本地拦下比让平台返回语焉不详的 code 更可读。</remarks>
    /// <exception cref="ArgumentException">缺失或为空。</exception>
    public static string[] RequireStringArray(IReadOnlyDictionary<string, object?> arguments, string name)
        => OptionalStringArray(arguments, name) ?? throw new ArgumentException($"缺少必填参数 {name}");

    /// <summary>读取可选整数参数（兼容 JSON <c>Number</c> / 数字字符串 / 已装箱 <see cref="int"/>）。</summary>
    /// <remarks>
    /// <b>与 <see cref="OptionalString"/>/<see cref="OptionalBool"/> 的差异（有意）</b>：参数缺失返回
    /// <see langword="null"/>，但参数<b>存在</b>却无法解析为整数时<b>抛异常</b>——整数参数的「0」与
    /// 「未提供」是两种语义（如 <c>docx.get_raw_content</c> 的 <c>lang</c>），静默降级会把模型的
    /// 传参错误变成一次看似成功的默认行为。
    /// </remarks>
    /// <exception cref="ArgumentException">参数存在但无法解析为整数。</exception>
    public static int? OptionalInt(IReadOnlyDictionary<string, object?> arguments, string name)
    {
        if (!arguments.TryGetValue(name, out var value) || value is null)
        {
            return null;
        }

        switch (value)
        {
            case int number:
                return number;
            case long number when number >= int.MinValue && number <= int.MaxValue:
                return (int)number;
            case System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.Number } element
                when element.TryGetInt32(out var parsed):
                return parsed;
            case System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.String } element
                when int.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed):
                return parsed;
            case string text when int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed):
                return parsed;
            default:
                throw new ArgumentException($"参数 {name} 需为整数，实际: {value}");
        }
    }

    /// <summary>读取可选字符串数组参数。</summary>
    public static string[]? OptionalStringArray(IReadOnlyDictionary<string, object?> arguments, string name)
    {
        if (!arguments.TryGetValue(name, out var value) || value is null)
        {
            return null;
        }

        if (value is System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.Array } element)
        {
            var items = element.EnumerateArray()
                .Where(e => e.ValueKind is System.Text.Json.JsonValueKind.String)
                .Select(e => e.GetString())
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => s!)
                .ToArray();
            return items.Length == 0 ? null : items;
        }

        if (value is IEnumerable<object> enumerable)
        {
            var items = enumerable
                .Select(Convert)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!)
                .ToArray();
            return items.Length == 0 ? null : items;
        }

        var single = Convert(value);
        return string.IsNullOrWhiteSpace(single) ? null : [single!];
    }

    private static string? Convert(object? value) => value switch
    {
        null => null,
        string s => string.IsNullOrWhiteSpace(s) ? null : s,
        System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.String } e => e.GetString(),
        System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.Number } e => e.GetRawText(),
        System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.True } => bool.TrueString,
        System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.False } => bool.FalseString,
        System.Text.Json.JsonElement => null,
        _ => value.ToString(),
    };
}

/// <summary>
/// 工具结果文本工具：白名单投影（JsonNode 构建，AOT 安全——无反射序列化）与超长截断。
/// </summary>
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
    public static string Truncate(string text, int maxLength)
    {
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
    /// JSON 感知截断（AI-FD-D12 P1D-2a 默认行为升级）：对 JSON 文本按首个数组属性
    /// （<c>items</c>/<c>values</c>/<c>metas</c> 等）<b>逐条删除</b>直至长度达标，追加
    /// <c>truncated</c>/<c>hint</c> 标记——截断不落在 JSON 结构中间，模型拿到的是合法 JSON；
    /// 解析失败（纯文本/非法 JSON）或无数组属性时退回字符截断（<see cref="Truncate"/>，既有行为）。
    /// </summary>
    /// <remarks>
    /// 开销为一次 JSON 解析（结果投影路径上，AOT 安全）；
    /// 仅剩 1 条仍超限时保持合法 JSON 返回（不落到字符截断破坏结构，结构完整性优先于硬上限）。
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
        catch (JsonException)
        {
            root = null; // 非 JSON 文本（如 docx.get_raw_content 的纯文本正文）：退回字符截断。
        }

        // 查找第一个可截断的数组属性（items/values/metas 等列表型键）。
        JsonArray? array = null;
        string? arrayKey = null;
        if (root is not null)
        {
            foreach (var pair in root)
            {
                if (pair.Value is JsonArray { Count: > 0 } candidate)
                {
                    array = candidate;
                    arrayKey = pair.Key;
                    break;
                }
            }
        }

        if (array is null || arrayKey is null)
        {
            return Truncate(text, maxLength);
        }

        // 逐条删除尾部条目直至整体长度达标（至少保留 1 条，保证「有内容且可读」）。
        while (array.Count > 1 && root!.ToJsonString().Length > maxLength)
        {
            ((IList<JsonNode?>)array).RemoveAt(array.Count - 1);
        }

        root!["truncated"] = true;
        root!["hint"] = $"结果已按 {arrayKey} 截断——请缩小查询范围或用 page_token 翻页";
        return root!.ToJsonString();
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
