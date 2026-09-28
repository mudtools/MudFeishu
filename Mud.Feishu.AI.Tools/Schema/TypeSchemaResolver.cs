// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Mud.Feishu.AI.Tools.Extraction;

namespace Mud.Feishu.AI.Tools.Schema;

/// <summary>
/// 类型 → JSON Schema 递归推导器（替换现状 <c>MapJsonPrimitiveType</c> 的"复合类型一律 string"降级策略）。
/// </summary>
/// <remarks>
/// <para>支持返回类型解包链（<see cref="FeishuApiResult{T}"/> 派生族）和 DataModels 递归推导。</para>
/// <para>四条收敛规则（防 schema 爆炸）：</para>
/// <para>1. 只发射带 [JsonPropertyName] 的属性</para>
/// <para>2. 深度上限 <see cref="MaxOutputDepth"/> = 4</para>
/// <para>3. 循环引用检测（以 OriginalDefinition 的完整元数据名入 HashSet 递归栈）</para>
/// <para>4. required 不伪造（非可空引用 / 非 Nullable&lt;T&gt; 值类型 → required）</para>
/// </remarks>
internal sealed class TypeSchemaResolver
{
    /// <summary>输出 Schema 最大递归深度。</summary>
    public const int MaxOutputDepth = 4;

    private readonly Compilation _compilation;
    private readonly HashSet<string> _recursionStack = new();

    // 信封类型名（用 OriginalDefinition.ToDisplayString() 比对）
    private const string FeishuApiResultGeneric = "Mud.Feishu.DataModels.FeishuApiResult<T>";
    private const string FeishuApiPageListResultGeneric = "Mud.Feishu.DataModels.FeishuApiPageListResult<T>";
    private const string FeishuApiListResultGeneric = "Mud.Feishu.DataModels.FeishuApiListResult<T>";
    private const string FeishuApiPageListTotalResultGeneric = "Mud.Feishu.DataModels.FeishuApiPageListTotalResult<T>";
    private const string FeishuNullDataApiResultName = "FeishuNullDataApiResult";

    public TypeSchemaResolver(Compilation compilation)
    {
        _compilation = compilation;
    }

    /// <summary>
    /// 解包返回类型并推导 OutputSchema JSON 片段。
    /// </summary>
    /// <param name="returnType">方法的 <see cref="ITypeSymbol"/>（如 <c>Task&lt;FeishuApiResult&lt;X&gt;&gt;</c> 的内层类型）。</param>
    /// <returns>JSON Schema 片段（如 <c>{"type":"object","properties":{...}}</c>），或 <c>"{}"</c> 表示空对象。</returns>
    public string ResolveOutputSchema(ITypeSymbol? returnType)
    {
        if (returnType is null)
            return "{}";

        var payload = UnwrapEnvelope(returnType);
        if (payload is null)
            return "{}";

        return ResolveTypeSchema(payload, depth: 0);
    }

    /// <summary>
    /// 解包信封类型：FeishuApiResult&lt;T&gt; → T；FeishuApiPageListResult&lt;T&gt; → ApiPageListResult&lt;T&gt;（需上溯基类链）。
    /// </summary>
    public ITypeSymbol? UnwrapEnvelope(ITypeSymbol type)
    {
        // 直接泛型参数检查
        if (type is INamedTypeSymbol named)
        {
            // FeishuNullDataApiResult → 无 payload
            if (named.Name == FeishuNullDataApiResultName)
                return null;

            // FeishuApiResult<T> → T
            var originalDef = named.OriginalDefinition.ToDisplayString();
            if (originalDef == FeishuApiResultGeneric && named.TypeArguments.Length == 1)
                return named.TypeArguments[0];

            // FeishuApiListResult<T> → ApiListResult<T> (自身)
            if (originalDef == FeishuApiListResultGeneric && named.TypeArguments.Length == 1)
                return named;

            // FeishuApiPageListResult<T> → 上溯基类链找 Data
            if (originalDef == FeishuApiPageListResultGeneric && named.TypeArguments.Length == 1)
                return FindDataPropertyType(named);

            // FeishuApiPageListTotalResult<T> → 上溯基类链找 Data
            if (originalDef == FeishuApiPageListTotalResultGeneric && named.TypeArguments.Length == 1)
                return FindDataPropertyType(named);
        }

        // 非 信封类型——直接作为 payload
        return type;
    }

    /// <summary>
    /// 推导任意 C# 类型的 JSON Schema（递归核心）。
    /// </summary>
    public string ResolveTypeSchema(ITypeSymbol type, int depth)
    {
        // 深度上限
        if (depth > MaxOutputDepth)
            return "{}";

        // Nullable<T> / 可空引用 → 展开 T 的 schema，不进 required
        if (type is INamedTypeSymbol nullable
            && nullable.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return ResolveTypeSchema(nullable.TypeArguments[0], depth);
        }

        // 标量类型
        var primitive = MapPrimitiveType(type);
        if (primitive is not null)
            return primitive;

        // 数组类型
        if (type.TypeKind == TypeKind.Array && type is IArrayTypeSymbol array)
        {
            return ResolveArraySchema(array.ElementType, depth);
        }

        // 集合类型 List<T> / IReadOnlyList<T> / IEnumerable<T>
        if (type is INamedTypeSymbol collection
            && collection.IsGenericType
            && collection.TypeArguments.Length == 1
            && IsCollectionType(collection))
        {
            return ResolveArraySchema(collection.TypeArguments[0], depth);
        }

        // Dictionary<string, T>
        if (type is INamedTypeSymbol dict
            && dict.IsGenericType
            && dict.TypeArguments.Length == 2
            && IsDictionaryType(dict))
        {
            var valueSchema = ResolveTypeSchema(dict.TypeArguments[1], depth);
            return $"{{\"type\":\"object\",\"additionalProperties\":{valueSchema}}}";
        }

        // byte[] → binary
        if (type.ToDisplayString() is "byte[]" or "System.Byte[]" or "byte[]?" or "System.Byte[]?")
        {
            return "{\"type\":\"string\",\"format\":\"binary\"}";
        }

        // object / JsonNode / 未识别 → 自由对象
        if (type.SpecialType == SpecialType.System_Object
            || type.Name == "JsonNode"
            || type.Name == "JsonObject"
            || type.Name == "JsonElement"
            || type.Name == "JsonDocument")
        {
            return "{}";
        }

        // 枚举
        if (type.TypeKind == TypeKind.Enum)
        {
            return ResolveEnumSchema(type);
        }

        // DataModels 类 → 递归推导 properties
        if (type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Struct)
        {
            return ResolveObjectSchema(type, depth);
        }

        // 接口 / 抽象类 → 降级为 object
        return "{}";
    }

    // ────────── 私有推导方法 ──────────

    private string? MapPrimitiveType(ITypeSymbol type)
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
                return "{\"type\":\"string\"}";
        }

        // DateTime / DateTimeOffset
        var typeName = type.ToDisplayString();
        if (typeName == "System.DateTime" || typeName == "System.DateTimeOffset")
            return "{\"type\":\"string\",\"format\":\"date-time\"}";
        if (typeName == "System.TimeSpan")
            return "{\"type\":\"string\",\"format\":\"duration\"}";
        if (typeName == "System.Guid")
            return "{\"type\":\"string\",\"format\":\"uuid\"}";

        return null;
    }

    private string ResolveArraySchema(ITypeSymbol elementType, int depth)
    {
        var itemSchema = ResolveTypeSchema(elementType, depth + 1);
        return $"{{\"type\":\"array\",\"items\":{itemSchema}}}";
    }

    private string ResolveEnumSchema(ITypeSymbol enumType)
    {
        var sb = new StringBuilder();
        sb.Append("{\"type\":\"string\",\"enum\":[");
        var members = enumType.GetMembers().OfType<IFieldSymbol>().ToArray();
        for (var i = 0; i < members.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append('"').Append(members[i].Name).Append('"');
        }

        sb.Append("]}");
        return sb.ToString();
    }

    private string ResolveObjectSchema(ITypeSymbol type, int depth)
    {
        var metadataName = type.OriginalDefinition.ToDisplayString();

        // 循环引用检测
        if (_recursionStack.Contains(metadataName))
            return "{}";

        _recursionStack.Add(metadataName);
        try
        {
            var sb = new StringBuilder();
            sb.Append("{\"type\":\"object\",\"properties\":{");

            var required = new List<string>();
            var properties = GetSerializableProperties(type);
            var first = true;

            foreach (var prop in properties)
            {
                if (!first) sb.Append(',');
                first = false;

                var jsonName = GetJsonPropertyName(prop);
                sb.Append('"').Append(jsonName).Append("\":");

                var propSchema = ResolveTypeSchema(prop.Type, depth + 1);
                sb.Append(propSchema);

                // required 判定：非可空引用类型 / 非 Nullable<T> 值类型
                if (IsRequired(prop))
                {
                    required.Add(jsonName);
                }
            }

            sb.Append("}");

            if (required.Count > 0)
            {
                sb.Append(",\"required\":[");
                for (var i = 0; i < required.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append('"').Append(required[i]).Append('"');
                }

                sb.Append("]");
            }

            sb.Append("}");
            return sb.ToString();
        }
        finally
        {
            _recursionStack.Remove(metadataName);
        }
    }

    private IEnumerable<IPropertySymbol> GetSerializableProperties(ITypeSymbol type)
    {
        // 只发射带 [JsonPropertyName] 的属性
        var allMembers = new List<IPropertySymbol>();
        var current = type;
        while (current is not null && current.SpecialType != SpecialType.System_Object)
        {
            allMembers.AddRange(current.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public
                    && p.GetMethod is not null
                    && HasJsonPropertyNameAttribute(p)));
            current = current.BaseType;
        }

        return allMembers;
    }

    private static bool HasJsonPropertyNameAttribute(IPropertySymbol prop)
    {
        return prop.GetAttributes().Any(a =>
            a.AttributeClass?.Name == "JsonPropertyNameAttribute"
            && a.AttributeClass.ContainingNamespace.ToDisplayString() == "System.Text.Json.Serialization");
    }

    private static string GetJsonPropertyName(IPropertySymbol prop)
    {
        var attr = prop.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass?.Name == "JsonPropertyNameAttribute");
        if (attr is not null && attr.ConstructorArguments.Length >= 1
            && attr.ConstructorArguments[0].Value is string name)
        {
            return name;
        }

        return prop.Name;
    }

    private static bool IsRequired(IPropertySymbol prop)
    {
        // 可空引用类型 → 不 required
        if (prop.NullableAnnotation == NullableAnnotation.Annotated)
            return false;

        // Nullable<T> → 不 required
        if (prop.Type is INamedTypeSymbol nullable
            && nullable.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            return false;

        // 值类型 → required
        if (prop.Type.IsValueType)
            return true;

        // 非可空引用类型 → required
        return prop.NullableAnnotation != NullableAnnotation.Annotated;
    }

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

    private static ITypeSymbol? FindDataPropertyType(INamedTypeSymbol type)
    {
        // 上溯基类链查找 Data 属性的类型
        var current = type;
        while (current is not null && current.SpecialType != SpecialType.System_Object)
        {
            var dataProp = current.GetMembers("Data").OfType<IPropertySymbol>().FirstOrDefault();
            if (dataProp is not null)
                return dataProp.Type;

            current = current.BaseType;
        }

        // 如果自身有泛型参数，用泛型参数作为 payload
        if (type.TypeArguments.Length == 1)
            return type.TypeArguments[0];

        return type;
    }
}
