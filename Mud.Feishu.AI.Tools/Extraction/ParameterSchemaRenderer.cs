// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Mud.Feishu.AI.Tools.Schema;

namespace Mud.Feishu.AI.Tools.Extraction;

/// <summary>
/// 参数 → JSON Schema 片段的<b>唯一</b>推导点（从 Roslyn 符号，而非"类型名字符串"）。
/// </summary>
/// <remarks>
/// <para>
/// 修掉的三条既有 Schema 质量缺陷（旧 <c>FeishuToolSchemaGenerator.MapJsonPrimitiveType</c> 的实现）：
/// </para>
/// <list type="number">
/// <item>数组参数 <c>items</c> 恒为 <c>{"type":"string"}</c>（<c>string[]</c> 之外的元素类型全错）；</item>
/// <item>复合 DTO 参数一律降级为 <c>type: string</c>（模型不知道可传哪些字段）；</item>
/// <item>C# <c>enum</c> 无 <c>enum:[...]</c> 约束（模型自由发挥取值）。</item>
/// </list>
/// <para>
/// 设计约束：<b>只从符号推导，不做字符串猜测</b>——类型一旦标注（含 <c>List&lt;T&gt;</c>、
/// <c>T[]</c>、可空、枚举、<c>DateTimeOffset</c>）就产出对应的 JSON Schema 关键字。
/// </para>
/// </remarks>
internal static class ParameterSchemaRenderer
{
    /// <summary>输入参数对象展开的最大深度（防止 DTO 图失控）。</summary>
    public const int MaxInputDepth = 2;

    /// <summary>
    /// 取模型可见的参数集合（跨方法去重，保留首个同名参数——与既有行为一致）。
    /// </summary>
    /// <param name="interfaceSymbol">[FeishuTool] 接口符号。</param>
    /// <param name="compilation">当前编译（对象展开需要）。</param>
    /// <returns>参数条目列表（含已推导的 Schema 片段）。</returns>
    public static IReadOnlyList<CapabilityParameter> RenderParameters(
        INamedTypeSymbol interfaceSymbol,
        Compilation compilation)
    {
        var methodLevelAttributes = interfaceSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .SelectMany(static m => m.GetAttributes())
            .Where(static a => a.AttributeClass?.Name == "ToolParameterAttribute")
            .ToArray();

        var parameters = interfaceSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(static m => m.DeclaredAccessibility == Accessibility.Public && m.MethodKind == MethodKind.Ordinary)
            .SelectMany(static m => m.Parameters)
            .Where(static p => !IsCancellationToken(p.Type))
            .GroupBy(static p => p.Name, StringComparer.Ordinal)
            .Select(static g => g.First())
            .ToArray();

        var resolver = new TypeSchemaResolver(compilation);
        var result = new List<CapabilityParameter>(parameters.Length);

        foreach (var parameter in parameters)
        {
            var attribute = GetToolParameterAttribute(parameter, methodLevelAttributes);
            var description = GetParameterDescription(parameter, attribute);
            var isRequired = IsRequired(parameter, attribute);
            var isNullable = parameter.NullableAnnotation == NullableAnnotation.Annotated
                || (parameter.Type is INamedTypeSymbol named
                    && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T);

            var fragment = RenderFragment(parameter.Type, resolver, depth: 0);
            if (!string.IsNullOrWhiteSpace(description))
            {
                fragment = InsertDescription(fragment, description!);
            }

            result.Add(new CapabilityParameter(
                name: parameter.Name,
                csharpType: parameter.Type.ToDisplayString(),
                parameterKind: DeriveParameterKind(parameter),
                docDescription: description,
                isRequired: isRequired,
                isNullable: isNullable,
                schemaFragmentJson: fragment,
                declaredToolParameterName: GetDeclaredToolParameterName(parameter, attribute)));
        }

        return result;
    }

    // ────────── 类型 → Schema 片段 ──────────

    private static string RenderFragment(ITypeSymbol type, TypeSchemaResolver resolver, int depth)
    {
        // Nullable<T> → 展开 T（可空性由 required 表达，不进 JSON 类型）
        if (type is INamedTypeSymbol nullable
            && nullable.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return RenderFragment(nullable.TypeArguments[0], resolver, depth);
        }

        // 标量
        var scalar = RenderScalar(type);
        if (scalar is not null)
        {
            return scalar;
        }

        // 枚举：值域即契约（模型不应猜测字面量）
        if (type.TypeKind == TypeKind.Enum)
        {
            return RenderEnum(type);
        }

        // byte[] → binary
        if (IsByteArray(type))
        {
            return "{\"type\":\"string\",\"format\":\"binary\"}";
        }

        // T[] → array + 真实元素类型
        if (type is IArrayTypeSymbol array)
        {
            return $"{{\"type\":\"array\",\"items\":{RenderFragment(array.ElementType, resolver, depth + 1)}}}";
        }

        // Stream 家族 → binary
        if (type.Name is "Stream" or "FileStream" or "MemoryStream")
        {
            return "{\"type\":\"string\",\"format\":\"binary\"}";
        }

        // List<T> / IReadOnlyList<T> ... → array + 真实元素类型
        if (type is INamedTypeSymbol collection
            && collection.IsGenericType
            && collection.TypeArguments.Length == 1
            && IsCollectionType(collection))
        {
            return $"{{\"type\":\"array\",\"items\":{RenderFragment(collection.TypeArguments[0], resolver, depth + 1)}}}";
        }

        // Dictionary<string, T> → object + additionalProperties
        if (type is INamedTypeSymbol dictionary
            && dictionary.IsGenericType
            && dictionary.TypeArguments.Length == 2
            && IsDictionaryType(dictionary))
        {
            return $"{{\"type\":\"object\",\"additionalProperties\":{RenderFragment(dictionary.TypeArguments[1], resolver, depth + 1)}}}";
        }

        // 复合 DTO → 对象展开（受限深度；无可展开属性时降级为字符串，保持"可传 JSON 文本"语义）
        if (depth < MaxInputDepth && (type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Struct))
        {
            var expanded = resolver.ResolveTypeSchema(type, depth);
            if (HasProperties(expanded))
            {
                return expanded;
            }
        }

        return "{\"type\":\"string\"}";
    }

    private static string? RenderScalar(ITypeSymbol type)
    {
        switch (type.SpecialType)
        {
            case SpecialType.System_Boolean:
                return "{\"type\":\"boolean\"}";
            case SpecialType.System_Int16:
            case SpecialType.System_Int32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt16:
            case SpecialType.System_UInt32:
            case SpecialType.System_UInt64:
                return "{\"type\":\"integer\"}";
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_Decimal:
                return "{\"type\":\"number\"}";
            case SpecialType.System_String:
            case SpecialType.System_Char:
                return "{\"type\":\"string\"}";
        }

        var name = type.ToDisplayString();
        return name switch
        {
            "System.DateTime" or "System.DateTimeOffset" => "{\"type\":\"string\",\"format\":\"date-time\"}",
            "System.TimeSpan" => "{\"type\":\"string\",\"format\":\"duration\"}",
            "System.Guid" => "{\"type\":\"string\",\"format\":\"uuid\"}",
            _ => null,
        };
    }

    private static string RenderEnum(ITypeSymbol enumType)
    {
        var members = enumType.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(static f => f.HasConstantValue)
            .Select(static f => f.Name)
            .ToArray();

        if (members.Length == 0)
        {
            return "{\"type\":\"string\"}";
        }

        var sb = new StringBuilder("{\"type\":\"string\",\"enum\":[");
        for (var i = 0; i < members.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }

            sb.Append(JsonText.Quote(members[i]));
        }

        sb.Append("]}");
        return sb.ToString();
    }

    /// <summary>把描述插入已渲染的片段（作为 <c>description</c> 关键字）。</summary>
    private static string InsertDescription(string fragment, string description)
    {
        // 片段恒为以 '{' 开头、'}' 结尾的对象；插到闭合花括号之前。
        var insertAt = fragment.Length - 1;
        var separator = fragment.Length > 2 ? "," : string.Empty;
        return string.Concat(
            fragment.Substring(0, insertAt),
            separator,
            "\"description\":",
            JsonText.Quote(description),
            "}");
    }

    private static bool HasProperties(string objectSchema)
        => objectSchema.IndexOf("\"properties\":{", StringComparison.Ordinal) >= 0
           && objectSchema.IndexOf("\"properties\":{}", StringComparison.Ordinal) < 0;

    // ────────── 参数元数据 ──────────

    private static string DeriveParameterKind(IParameterSymbol parameter)
    {
        foreach (var attribute in parameter.GetAttributes())
        {
            switch (attribute.AttributeClass?.Name)
            {
                case "PathAttribute":
                    return "Path";
                case "QueryAttribute":
                    return "Query";
                case "BodyAttribute":
                    return "Body";
                case "HeaderAttribute":
                    return "Header";
                case "FormContentAttribute":
                    return "FormContent";
            }
        }

        return "Query";
    }

    private static bool IsRequired(IParameterSymbol parameter, AttributeData? attribute)
    {
        if (attribute is not null
            && attribute.NamedArguments.FirstOrDefault(static a => a.Key == "Required").Value.Value is bool explicitRequired)
        {
            return explicitRequired;
        }

        if (parameter.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return false;
        }

        return parameter.Type.IsValueType
            && !(parameter.Type is INamedTypeSymbol named
                && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T);
    }

    private static string? GetParameterDescription(IParameterSymbol parameter, AttributeData? attribute)
    {
        if (attribute is not null
            && attribute.ConstructorArguments.Length >= 2
            && attribute.ConstructorArguments[1].Value is string description
            && !string.IsNullOrWhiteSpace(description))
        {
            return description;
        }

        // 回退到 XML <param> 文档注释（模型可读性的第二来源）。
        return GetParamDoc(parameter);
    }

    private static AttributeData? GetToolParameterAttribute(IParameterSymbol parameter, AttributeData[] methodLevelAttributes)
    {
        var direct = parameter.GetAttributes()
            .FirstOrDefault(static a => a.AttributeClass?.Name == "ToolParameterAttribute");
        if (direct is not null)
        {
            return direct;
        }

        return methodLevelAttributes.FirstOrDefault(a =>
            a.ConstructorArguments.Length >= 1
            && a.ConstructorArguments[0].Value is string name
            && string.Equals(name, parameter.Name, StringComparison.Ordinal));
    }

    /// <summary>
    /// 取 <c>[ToolParameter]</c> 声明的参数名（意图源）：仅在<b>直接标注在参数上</b>且声明名与
    /// C# 参数名不同时返回——该漂移由 L4 校验器升级为 MUDFT015（声明名 ≠ 渲染键 = 模型可见契约漂移）。
    /// 方法级回退匹配本身就要求声明名等于参数名，故恒无漂移，返回 <see langword="null"/>。
    /// </summary>
    private static string? GetDeclaredToolParameterName(IParameterSymbol parameter, AttributeData? attribute)
    {
        if (attribute is null
            || attribute.ConstructorArguments.Length < 1
            || attribute.ConstructorArguments[0].Value is not string declared
            || string.Equals(declared, parameter.Name, StringComparison.Ordinal))
        {
            return null;
        }

        return declared;
    }

    private static string? GetParamDoc(IParameterSymbol parameter)
        => parameter.ContainingSymbol is IMethodSymbol method
            ? Extractors.GetParamDoc(method, parameter.Name)
            : null;

    private static bool IsCancellationToken(ITypeSymbol type) => type.Name == "CancellationToken";

    private static bool IsByteArray(ITypeSymbol type)
        => type.TypeKind == TypeKind.Array
           && type is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte };

    private static bool IsCollectionType(INamedTypeSymbol type)
    {
        var name = type.OriginalDefinition.ToDisplayString();
        return name == "System.Collections.Generic.List<T>"
            || name == "System.Collections.Generic.IReadOnlyList<T>"
            || name == "System.Collections.Generic.IEnumerable<T>"
            || name == "System.Collections.Generic.IList<T>"
            || name == "System.Collections.Generic.ICollection<T>"
            || name == "System.Collections.Generic.IReadOnlyCollection<T>";
    }

    private static bool IsDictionaryType(INamedTypeSymbol type)
    {
        var name = type.OriginalDefinition.ToDisplayString();
        return name == "System.Collections.Generic.Dictionary<TKey, TValue>"
            || name == "System.Collections.Generic.IDictionary<TKey, TValue>"
            || name == "System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>";
    }
}
