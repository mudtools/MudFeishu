// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections;

namespace Mud.Feishu.AI.Tools.Tests.Eval;

/// <summary>
/// 参数层判据（Schema 面）：必填存在性、类型相符、参数名在册、枚举取值在册。
/// </summary>
/// <remarks>
/// 只覆盖 JSON Schema 能表达的部分——枚举闭集、互斥、简化文法（如 bitable 的 filter）在绝大多数
/// 工具上由执行器判定，属评测的 handler 层。<b>为什么两类都要</b>：Schema 面管住"模型能不能把参数名
/// 与类型写对"，handler 面管住"写对了名字但值不合法时是否被结构化拒绝而不是静默下发"。
/// </remarks>
internal static class EvalArgumentValidator
{
    /// <summary>返回错误标记列表（空集 = 通过）。标记形如 <c>missing_argument:text</c>。</summary>
    internal static IReadOnlyList<string> Validate(string tool, IReadOnlyDictionary<string, object?> arguments)
    {
        var parameters = EvalToolSurface.Instance.ParametersOf(tool);
        var properties = parameters.ValueKind == JsonValueKind.Object
            && parameters.TryGetProperty("properties", out var declaredProperties)
            && declaredProperties.ValueKind == JsonValueKind.Object
                ? declaredProperties
                : default(JsonElement);
        var hasProperties = properties.ValueKind == JsonValueKind.Object;
        var required = parameters.TryGetProperty("required", out var requiredNode)
            && requiredNode.ValueKind == JsonValueKind.Array
            ? requiredNode.EnumerateArray().Select(static e => e.GetString()!).ToArray()
            : [];

        var errors = new List<string>();
        foreach (var name in required)
        {
            if (!arguments.TryGetValue(name, out var value) || value is null)
            {
                errors.Add($"missing_argument:{name}");
            }
        }

        foreach (var argument in arguments)
        {
            if (!hasProperties || !properties.TryGetProperty(argument.Key, out var schema))
            {
                errors.Add($"unknown_argument:{argument.Key}");
                continue;
            }

            var declared = schema.TryGetProperty("type", out var typeNode) ? typeNode.GetString() ?? "string" : "string";
            if (!IsCompatible(argument.Value, declared))
            {
                errors.Add($"type_mismatch:{argument.Key}(expect={declared},actual={KindOf(argument.Value)})");
                continue;
            }

            if (schema.TryGetProperty("enum", out var enumNode) && enumNode.ValueKind == JsonValueKind.Array
                && argument.Value is string enumValue
                && !enumNode.EnumerateArray().Any(e => string.Equals(e.GetString(), enumValue, StringComparison.Ordinal)))
            {
                errors.Add($"enum_violation:{argument.Key}");
            }

            if (declared == "array" && schema.TryGetProperty("items", out var items)
                && items.TryGetProperty("type", out var itemType) && itemType.GetString() is { } itemDeclared
                && argument.Value is IEnumerable sequence and not string)
            {
                foreach (var element in sequence)
                {
                    if (!IsCompatible(element, itemDeclared))
                    {
                        errors.Add($"item_type_mismatch:{argument.Key}(expect={itemDeclared},actual={KindOf(element)})");
                        break;
                    }
                }
            }
        }

        return errors;
    }

    private static bool IsCompatible(object? value, string declaredType) => value switch
    {
        null => true,
        string => declaredType is "string",
        bool => declaredType is "boolean",
        int or long or short or byte => declaredType is "integer" or "number",
        double or float or decimal => declaredType is "number",
        IEnumerable => declaredType is "array" or "object" or "string",
        _ => true,
    };

    private static string KindOf(object? value) => value switch
    {
        null => "null",
        string => "string",
        bool => "boolean",
        int or long or short or byte => "integer",
        double or float or decimal => "number",
        IEnumerable => "array",
        _ => value.GetType().Name,
    };
}
