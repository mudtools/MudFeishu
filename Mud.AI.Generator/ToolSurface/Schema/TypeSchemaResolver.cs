// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.ToolSurface.Extraction;

namespace Mud.AI.ToolSurface.Schema;

/// <summary>
/// 类型 → JSON Schema 递归推导器（移植自上游 <c>Mud.Feishu.AI.Tools.Schema.TypeSchemaResolver</c>，
/// 设计文档 §2 判定：纯机制引擎单元持有；§4.2 第 14 项：返回包装解包表改读剖面槽）。
/// </summary>
/// <remarks>
/// <para>
/// <b>接缝注册</b>：本类实现 Extraction 层的 <see cref="IToolSurfaceTypeSchemaResolver"/>，
/// 由<b>入口生成器（R-2b-5 批次）</b>在 <c>Initialize</c> 中注册
/// <c>ToolSurfaceSchemaResolver.Factory = compilation =&gt; new TypeSchemaResolver(compilation, profile)</c>
/// （多 profile 扇出时按当前输出路径的 profile 组装闭包）。Schema 层自身不做任何静态注册。
/// </para>
/// <para>
/// <b>泛化点</b>（上游硬编码 → 剖面槽）：
/// <c>Mud.Feishu.DataModels.FeishuApiResult&lt;T&gt;</c> 等 4 个泛型 + <c>FeishuNullDataApiResult</c>
/// 的解包表 → <see cref="SdkToolProfileModel.OutputWrapperNamespace"/> +
/// <see cref="SdkToolProfileModel.OutputWrapperTypeNames"/>（<c>|</c> 分隔清单，按
/// 「命名空间全等 + 类型简单名全等」匹配）。
/// </para>
/// <para>
/// <b>解包行为统一规则</b>（上游对 4 个泛型有 3 种分支，泛化为一条结构规则）：
/// 清单内的泛型包装类型 → 上溯基类链取 <c>Data</c> 属性类型（无 <c>Data</c> 时回退单泛型实参）；
/// 清单内的非泛型类型（上游 <c>FeishuNullDataApiResult</c> 形态）→ 无载荷（<c>null</c>）。
/// 对上游各类型：FeishuApiResult&lt;X&gt; 的 <c>Data</c> 即 X（与上游取 TypeArguments[0] 同结果）；
/// PageList/PageListTotal 与上游 <c>FindDataPropertyType</c> 路径逐字节一致；
/// NullData → <c>"{}"</c> 与上游一致。唯一已知行为差：<c>FeishuApiListResult&lt;T&gt;</c>
/// 上游返回自身（信封 <c>code/msg/data</c> 进 Schema），统一规则返回 <c>Data</c> 类型
/// （仅 <c>items</c>）——与 PageList 族的信封剥离口径一致，属上游三分支不一致的收敛，
/// R-2c 零漂移比对时需知悉。
/// </para>
/// <para>
/// 四条收敛规则（防 schema 爆炸，机制原样保留）：
/// 1. 只发射带 [JsonPropertyName] 的属性；
/// 2. 深度上限 <see cref="MaxOutputDepth"/> = 4；
/// 3. 循环引用检测（以 OriginalDefinition 的完整元数据名入 HashSet 递归栈）；
/// 4. required 不伪造（非可空引用 / 非 Nullable&lt;T&gt; 值类型 → required）。
/// </para>
/// </remarks>
internal sealed class TypeSchemaResolver : IToolSurfaceTypeSchemaResolver
{
    /// <summary>输出 Schema 最大递归深度。</summary>
    public const int MaxOutputDepth = 4;

    /// <summary>单个工具最多记录的截断样本数（防"一份深度爆炸的返回类型把诊断撑爆"）。</summary>
    public const int MaxRecordedTruncations = 5;

    private readonly HashSet<string> _recursionStack = new();
    private readonly List<string> _truncations = [];
    private readonly string _wrapperNamespace;
    private readonly ImmutableArray<string> _wrapperTypeNames;

    public TypeSchemaResolver(Compilation compilation, SdkToolProfileModel profile)
    {
        _ = compilation;
        profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _wrapperNamespace = profile.OutputWrapperNamespace;
        _wrapperTypeNames = profile.OutputWrapperTypeNames;
    }

    /// <summary>
    /// 本次解析中被截断的位置（槽位 009：深度超限 / 循环引用）——<b>供上层聚合上报</b>。
    /// </summary>
    /// <remarks>
    /// <b>为什么是"实例内收集 + 上层聚合"而不是"逐处上报"</b>：SDK 中层级超过
    /// <see cref="MaxOutputDepth"/> 的 DTO 数量可观，逐处上报会产生成千条警告淹没构建输出
    /// （槽位 018 的覆盖报告已确立"聚合单条"的体例）。
    /// 本解析器只负责<b>如实记录</b>，聚合与上报由生成器在拿到全部模型的阶段一次完成。
    /// </remarks>
    public IReadOnlyList<string> Truncations => _truncations;

    /// <inheritdoc />
    public string? ResolveOutputSchema(ITypeSymbol? returnType)
    {
        try
        {
            if (returnType is null)
            {
                return null;
            }

            var payload = UnwrapEnvelope(returnType);
            if (payload is null)
            {
                return "{}";
            }

            return ResolveTypeSchema(payload, depth: 0);
        }
        catch (Exception ex)
        {
            GeneratorDebugLogger.LogError(nameof(TypeSchemaResolver), ex);
            return "{}";
        }
    }

    /// <summary>
    /// 解包信封类型（按剖面解包表）：包装清单内的泛型 → 上溯基类链取 <c>Data</c> 属性类型；
    /// 清单内的非泛型 → 无载荷；清单外类型直接作为 payload 返回。
    /// </summary>
    internal ITypeSymbol? UnwrapEnvelope(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named
            && _wrapperTypeNames.Length > 0
            && string.Equals(named.ContainingNamespace?.ToDisplayString(), _wrapperNamespace, StringComparison.Ordinal))
        {
            foreach (var wrapperName in _wrapperTypeNames)
            {
                if (!string.Equals(named.Name, wrapperName, StringComparison.Ordinal))
                {
                    continue;
                }

                // 非泛型形态（上游 FeishuNullDataApiResult）→ 无载荷。
                if (!named.IsGenericType || named.TypeArguments.Length == 0)
                {
                    return null;
                }

                if (named.TypeArguments.Length == 1)
                {
                    return FindDataPropertyType(named);
                }

                return named;
            }
        }

        // 非信封类型——直接作为 payload
        return type;
    }

    /// <inheritdoc />
    public string ResolveTypeSchema(ITypeSymbol type, int depth, string? path = null)
    {
        try
        {
            // 深度上限
            if (depth > MaxOutputDepth)
            {
                RecordTruncation(path, "深度超限");
                return "{}";
            }

            // Nullable<T> / 可空引用 → 展开 T 的 schema，不进 required
            if (type is INamedTypeSymbol nullable
                && nullable.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                return ResolveTypeSchema(nullable.TypeArguments[0], depth, path);
            }

            // 标量类型
            var primitive = MapPrimitiveType(type);
            if (primitive is not null)
            {
                return primitive;
            }

            // 数组类型
            if (type.TypeKind == TypeKind.Array && type is IArrayTypeSymbol array)
            {
                return ResolveArraySchema(array.ElementType, depth, path);
            }

            // 集合类型 List<T> / IReadOnlyList<T> / IEnumerable<T>
            if (type is INamedTypeSymbol collection
                && collection.IsGenericType
                && collection.TypeArguments.Length == 1
                && IsCollectionType(collection))
            {
                return ResolveArraySchema(collection.TypeArguments[0], depth, path);
            }

            // Dictionary<string, T>
            if (type is INamedTypeSymbol dict
                && dict.IsGenericType
                && dict.TypeArguments.Length == 2
                && IsDictionaryType(dict))
            {
                var valueSchema = ResolveTypeSchema(dict.TypeArguments[1], depth, path + ".{value}");
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
                return ResolveObjectSchema(type, depth, path);
            }

            // 接口 / 抽象类 → 降级为 object。
            // 注：**不**上报槽位 009——该诊断的语义是"深度截断或循环引用"，而"接口无法推导属性"
            // 是另一类事实（其形状本就由实现类承载），混报会让 009 的计数失去可解释性。
            return "{}";
        }
        catch (Exception ex)
        {
            GeneratorDebugLogger.LogError(nameof(TypeSchemaResolver), ex);
            return "{}";
        }
    }

    /// <summary>记录一处截断（去重 + 限量；供上层聚合为单条槽位 009）。</summary>
    private void RecordTruncation(string? path, string reason)
    {
        if (_truncations.Count >= MaxRecordedTruncations)
        {
            // 已够定位问题；继续累积只会让诊断变长（聚合上报只取样本）。
            if (_truncations.Count == MaxRecordedTruncations)
            {
                _truncations.Add("…（更多同类截断已省略）");
            }

            return;
        }

        _truncations.Add($"{path ?? "$"}（{reason}）");
    }

    // ────────── 私有推导方法 ──────────

    private static string? MapPrimitiveType(ITypeSymbol type)
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

    private string ResolveArraySchema(ITypeSymbol elementType, int depth, string? path)
    {
        var itemSchema = ResolveTypeSchema(elementType, depth + 1, (path ?? "$") + "[]");
        return $"{{\"type\":\"array\",\"items\":{itemSchema}}}";
    }

    private string ResolveEnumSchema(ITypeSymbol enumType)
    {
        var sb = new StringBuilder();
        sb.Append("{\"type\":\"string\",\"enum\":[");

        // ⚠️ R5 / F-2 修正：必须按 HasConstantValue 过滤。
        //   C# enum 隐式含一个名为 `value__` 的**实例**字段（无常量值），
        //   不过滤会把它写进 enum 列表 ⇒ 输出 Schema 里出现伪成员 "value__"。
        //   ParameterSchemaRenderer.RenderEnum 与 RenderClosedSet 早已过滤；此处是输出侧的漏网点，
        //   两个方向的口径必须一致（否则同一 enum 在输入 Schema 与输出 Schema 里成员集不同）。
        var members = enumType.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(static f => f.HasConstantValue)
            .ToArray();

        for (var i = 0; i < members.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append('"').Append(members[i].Name).Append('"');
        }

        sb.Append("]}");
        return sb.ToString();
    }

    private string ResolveObjectSchema(ITypeSymbol type, int depth, string? path)
    {
        var metadataName = type.OriginalDefinition.ToDisplayString();

        // 循环引用检测
        if (_recursionStack.Contains(metadataName))
        {
            RecordTruncation(path, "循环引用");
            return "{}";
        }

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

                var propSchema = ResolveTypeSchema(prop.Type, depth + 1, (path ?? "$") + "." + jsonName);
                sb.Append(propSchema);

                // required 判定：非可空引用类型 / 非 Nullable<T> 值类型
                if (IsRequired(prop))
                {
                    required.Add(jsonName);
                }
            }

            sb.Append('}');

            if (required.Count > 0)
            {
                sb.Append(",\"required\":[");
                for (var i = 0; i < required.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append('"').Append(required[i]).Append('"');
                }

                sb.Append(']');
            }

            sb.Append('}');
            return sb.ToString();
        }
        finally
        {
            _recursionStack.Remove(metadataName);
        }
    }

    private static IEnumerable<IPropertySymbol> GetSerializableProperties(ITypeSymbol type)
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