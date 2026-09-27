// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.FeishuTools;

/// <summary>
/// <c>FeishuApiResult</c> 解包结果（Phase 1 §3.3.3 通用规则 3：统一解包，错误转可读文本回填模型）。
/// </summary>
/// <typeparam name="T">业务载荷类型。</typeparam>
/// <param name="Ok">是否成功（<c>code == 0</c> 且载荷非空）。</param>
/// <param name="Data">业务载荷（成功时非空）。</param>
/// <param name="ErrorText">失败原因（可读文本，回填模型）。</param>
internal sealed record FeishuApiOutcome<T>(bool Ok, T? Data, string? ErrorText) where T : class
{
    /// <summary>构造成功结果。</summary>
    public static FeishuApiOutcome<T> Success(T data) => new(true, data, null);

    /// <summary>构造失败结果。</summary>
    public static FeishuApiOutcome<T> Fail(string errorText) => new(false, null, errorText);
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
            return FeishuApiOutcome<T>.Fail($"飞书接口返回错误 code={result.Code.ToString(CultureInfo.InvariantCulture)}, msg={result.Msg ?? "(无错误信息)"}");
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

    /// <summary>结果文本是否已被截断（OTel 审计属性消费）。</summary>
    public static bool IsTruncated(string text)
        => text.IndexOf(TruncatedMarker, StringComparison.Ordinal) >= 0;

    private static JsonNode? FromJsonElement(System.Text.Json.JsonElement element) => element.ValueKind switch
    {
        // GetRawText 产出任意合法 JSON 文本（字符串带引号/数字/布尔/数组/对象），JsonNode.Parse 全覆盖。
        System.Text.Json.JsonValueKind.Null => null,
        System.Text.Json.JsonValueKind.Undefined => null,
        _ => JsonNode.Parse(element.GetRawText()),
    };
}
