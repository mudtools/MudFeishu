// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.ToolSurface.Extraction;

/// <summary>
/// 类型 → JSON Schema 推导器的<b>引擎侧接缝接口</b>。
/// </summary>
/// <remarks>
/// Schema 层实现为 <c>Schema/TypeSchemaResolver.cs</c>（设计文档 §1.2）；Extraction 层只经本接口消费其能力，
/// 由入口生成器在 <c>Initialize</c> 中以 <see cref="ToolSurfaceSchemaResolver"/> 注册工厂（解耦接缝）。
/// 成员形状与上游公开面一一对应。
/// </remarks>
internal interface IToolSurfaceTypeSchemaResolver
{
    /// <summary>推导过程中的截断样本（深度超限 / 循环引用；由调用方聚合为单条槽位 009）。</summary>
    IReadOnlyList<string> Truncations { get; }

    /// <summary>推导返回值 JSON Schema（可空输入返回 <see langword="null"/>）。</summary>
    string? ResolveOutputSchema(ITypeSymbol? returnType);

    /// <summary>推导类型的对象 Schema（输入参数展开用，受限深度）。</summary>
    string ResolveTypeSchema(ITypeSymbol type, int depth, string? path = null);
}

/// <summary>
/// <see cref="IToolSurfaceTypeSchemaResolver"/> 的注册点（Extraction ↔ Schema 层的解耦接缝）。
/// </summary>
/// <remarks>
/// <b>为什么是静态注册而非参数透传</b>：扫描入口签名已由设计文档锁定为
/// <c>Scan(symbol, compilation, profile, factory)</c>，resolver 不占形参位；Schema 层在生成器
/// 入口组装时注册工厂一次即可。未注册时 Extraction 层<b>降级</b>（复合 DTO 渲染为 string、
/// output schema 不推导）——该降级分支只是兜底（注册由 <c>ToolSurfaceSourceGenerator.Initialize</c>
/// 无条件完成），出现降级即意味着入口接线被改动。
/// <para>
/// <b>工厂签名必须携带 profile</b>（R-2b-5 修正）：<c>TypeSchemaResolver</c> 的解包表读
/// <see cref="SdkToolProfileModel.OutputWrapperNamespace"/>/<see cref="SdkToolProfileModel.OutputWrapperTypeNames"/>
/// 两个槽——只接收 <c>Compilation</c> 的工厂在多 profile 扇出下无法区分当前剖面（静态注册点会串味）。
/// profile 经形参传入后，注册点本身与剖面无关，扇出安全。
/// </para>
/// </remarks>
internal static class ToolSurfaceSchemaResolver
{
    /// <summary>Schema 层注册的工厂（<c>(Compilation, profile) → resolver 实例</c>）。</summary>
    public static Func<Compilation, SdkToolProfileModel, IToolSurfaceTypeSchemaResolver>? Factory { get; set; }

    /// <summary>安全创建；未注册或工厂抛异常时返回 <see langword="null"/>（调用方走降级路径）。</summary>
    public static IToolSurfaceTypeSchemaResolver? TryCreate(Compilation compilation, SdkToolProfileModel profile)
    {
        try
        {
            return Factory?.Invoke(compilation, profile);
        }
        catch (Exception ex)
        {
            GeneratorDebugLogger.LogError(nameof(ToolSurfaceSchemaResolver), ex);
            return null;
        }
    }
}

/// <summary>
/// 参数 → JSON Schema 片段的<b>唯一</b>推导点（从 Roslyn 符号，而非"类型名字符串"）。
/// </summary>
/// <remarks>
/// <para>
/// 移植自上游 <c>Mud.Feishu.AI.Tools.Extraction.ParameterSchemaRenderer</c>（设计文档 §2 判定：
/// 纯机制，引擎持有）；泛化点仅为参数特性名（<c>ToolParameterAttribute</c> 硬编码 →
/// 剖面槽 <see cref="SdkToolProfileModel.ParameterAttributeName"/>）与 resolver/JsonText 接缝。
/// </para>
/// <para>
/// 修掉的三条既有 Schema 质量缺陷（上游旧 MapJsonPrimitiveType 实现）：
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
    /// <param name="interfaceSymbol">工具特性接口符号。</param>
    /// <param name="compilation">当前编译（对象展开需要）。</param>
    /// <param name="profile">当前剖面（提供参数特性名槽）。</param>
    /// <returns>参数条目列表（含已推导的 Schema 片段）。</returns>
    public static IReadOnlyList<CapabilityParameter> RenderParameters(
        INamedTypeSymbol interfaceSymbol,
        Compilation compilation,
        SdkToolProfileModel profile)
    {
        try
        {
            var methodLevelAttributes = interfaceSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .SelectMany(static m => m.GetAttributes())
                .Where(a => IsParameterAttribute(a, profile))
                .ToArray();

            var parameters = interfaceSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(static m => m.DeclaredAccessibility == Accessibility.Public && m.MethodKind == MethodKind.Ordinary)
                .SelectMany(static m => m.Parameters)
                .Where(static p => !IsCancellationToken(p.Type))
                .GroupBy(static p => p.Name, StringComparer.Ordinal)
                .Select(static g => g.First())
                .ToArray();

            var resolver = ToolSurfaceSchemaResolver.TryCreate(compilation, profile);
            var result = new List<CapabilityParameter>(parameters.Length);

            foreach (var parameter in parameters)
            {
                var attribute = GetToolParameterAttribute(parameter, methodLevelAttributes, profile);
                var description = GetParameterDescription(parameter, attribute);
                var isRequired = IsRequired(parameter, attribute);
                var isNullable = parameter.NullableAnnotation == NullableAnnotation.Annotated
                    || (parameter.Type is INamedTypeSymbol named
                        && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T);

                var fragment = RenderFragment(parameter.Type, resolver, depth: 0);

                // R5 / F-2：显式声明的取值闭集优先于类型推导。
                // 放在 InsertDescription 之前 ⇒ InsertDescription 仍能把 description 正确插到
                // 闭合花括号之前（插入点不依赖片段内部是否已带 description）。
                var closedSet = GetParameterEnumType(attribute);
                string? enumMembers = null;
                if (closedSet is not null)
                {
                    if (TryRenderClosedSet(closedSet, out var closedSetFragment, out var closedSetMembers))
                    {
                        fragment = closedSetFragment;
                        enumMembers = closedSetMembers;
                    }
                }

                if (!string.IsNullOrWhiteSpace(description))
                {
                    fragment = InsertDescription(fragment, description!);
                }

                result.Add(new CapabilityParameter(
                    name: parameter.Name,
                    csharpType: parameter.Type.ToDisplayString(),
                    docDescription: description,
                    isRequired: isRequired,
                    isNullable: isNullable,
                    schemaFragmentJson: fragment,
                    declaredToolParameterName: GetDeclaredToolParameterName(parameter, attribute),
                    enumTypeFullName: closedSet?.ToDisplayString(),
                    enumMembers: enumMembers));
            }

            return result;
        }
        catch (Exception ex)
        {
            GeneratorDebugLogger.LogError(nameof(ParameterSchemaRenderer), ex);
            return [];
        }
    }

    /// <summary>
    /// 读取参数特性声明的闭集类型（<c>EnumType = typeof(X)</c>；可标注在参数或方法上）。
    /// </summary>
    /// <remarks>
    /// 走<b>符号</b>而非类型名字符串：<c>typeof(X)</c> 的实参是 <see cref="TypedConstant"/>
    /// （<c>Kind == TypedConstantKind.Type</c>），<c>Value</c> 即 <see cref="ITypeSymbol"/>。
    /// </remarks>
    private static INamedTypeSymbol? GetParameterEnumType(AttributeData? attribute)
        => attribute is null ? null : ReadEnumTypeFrom(attribute);

    /// <summary>从参数特性数据读取 <c>EnumType</c> 的类型符号。</summary>
    internal static INamedTypeSymbol? ReadEnumTypeFrom(AttributeData attribute)
    {
        foreach (var named in attribute.NamedArguments)
        {
            if (named.Key != "EnumType")
            {
                continue;
            }

            if (named.Value.Kind == TypedConstantKind.Type
                && named.Value.Value is INamedTypeSymbol typeSymbol)
            {
                return typeSymbol;
            }
        }

        return null;
    }

    /// <summary>该常量字段是否为<b>整型常量</b>（闭集只描述平台整型取值）。</summary>
    /// <remarks>
    /// 排除 <c>const string</c> 哨兵（供 GetName 对未知值返回的占位成员）与浮点/字符常量。
    /// <c>HasConstantValue</c> 为真时 <c>ConstantValue</c> 的运行时类型即常量类型。
    /// </remarks>
    private static bool IsIntegralConstant(IFieldSymbol field)
        => field.ConstantValue is sbyte or byte or short or ushort or int or uint or long or ulong;

    /// <summary>
    /// 渲染取值闭集：<c>{"type":"string","enum":[…]}</c>。
    /// </summary>
    /// <remarks>
    /// <b>两类来源</b>：
    /// <list type="bullet">
    /// <item><b>C# enum</b>：成员取 <c>GetMembers().OfType&lt;IFieldSymbol&gt;()</c>，
    /// 以 <c>HasConstantValue</c> 过滤（滤掉 enum 的 <c>value__</c> 实例字段），
    /// 渲染<b>成员名</b>（模型的自然语言契约）；</item>
    /// <item><b>常量类</b>（<c>static class</c> + <c>const int</c>）：同样过滤
    /// <c>HasConstantValue</c>，渲染<b>常量名</b>而非数值——因为模型的入参是<b>字面量字符串</b>
    /// （解包期按名字映射），渲染数值会让模型传"2"而解包只认"heading1"。</item>
    /// </list>
    /// 返回 <see langword="false"/> 表示"该类型无可用常量成员"⇒ 调用方回退到类型推导片段。
    /// <para>
    /// ⚠️ 必须同时过滤非整型常量：只按 <c>HasConstantValue</c> 过滤会把 <c>const string</c>
    /// 哨兵渲染进 enum 列表，使闭集多出模型无法使用的值（上游 R5 / F-2 实施期实测踩到）。
    /// </para>
    /// </remarks>
    private static bool TryRenderClosedSet(INamedTypeSymbol closedSetType, out string fragment, out string members)
    {
        fragment = string.Empty;
        members = string.Empty;

        var fields = closedSetType.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(static f => f.HasConstantValue
                && f.DeclaredAccessibility == Accessibility.Public
                && IsIntegralConstant(f))
            .ToArray();

        if (fields.Length == 0)
        {
            return false;
        }

        var names = fields.Select(static f => f.Name).ToArray();

        var sb = new StringBuilder("{\"type\":\"string\",\"enum\":[");
        for (var i = 0; i < names.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }

            sb.Append(JsonQuote(names[i]));
        }

        sb.Append("]}");
        fragment = sb.ToString();

        // 成员表（"name=value" 以 ';' 分隔）——供 ToolArgsEmitter（Emit 层）发射"常量名 → 平台整数值"映射。
        var memberPairs = new List<string>(fields.Length);
        foreach (var field in fields)
        {
            if (field.ConstantValue is not null)
            {
                memberPairs.Add(
                    field.Name + "=" + System.Convert.ToString(field.ConstantValue, System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        members = string.Join(";", memberPairs);
        return true;
    }

    // ────────── 类型 → Schema 片段 ──────────

    private static string RenderFragment(ITypeSymbol type, IToolSurfaceTypeSchemaResolver? resolver, int depth)
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

        // 复合 DTO → 对象展开（受限深度；resolver 缺席或无可展开属性时降级为字符串，保持"可传 JSON 文本"语义）
        if (depth < MaxInputDepth
            && resolver is not null
            && (type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Struct))
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

            sb.Append(JsonQuote(members[i]));
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
            JsonQuote(description),
            "}");
    }

    private static bool HasProperties(string objectSchema)
        => objectSchema.IndexOf("\"properties\":{", StringComparison.Ordinal) >= 0
           && objectSchema.IndexOf("\"properties\":{}", StringComparison.Ordinal) < 0;

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

    private static AttributeData? GetToolParameterAttribute(IParameterSymbol parameter, AttributeData[] methodLevelAttributes, SdkToolProfileModel profile)
    {
        var direct = parameter.GetAttributes().FirstOrDefault(a => IsParameterAttribute(a, profile));
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
    /// 参数特性判定：按 <see cref="SdkToolProfileModel.ParameterAttributeName"/> 槽匹配
    /// <c>Attribute</c> 后缀双形态（上游按硬编码简单名单形态判定，泛化后与工具特性同纪律）。
    /// </summary>
    private static bool IsParameterAttribute(AttributeData attribute, SdkToolProfileModel profile)
    {
        var name = attribute.AttributeClass?.Name;
        var bare = profile.ParameterAttributeName;
        return bare.Length > 0
            && name is not null
            && (name == bare || name == bare + "Attribute");
    }

    /// <summary>
    /// 取参数特性声明的参数名（意图源）：仅在<b>直接标注在参数上</b>且声明名与
    /// C# 参数名不同时返回——该漂移由 L4 校验器升级为槽位 015（声明名 ≠ 渲染键 = 模型可见契约漂移）。
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

    /// <summary>
    /// JSON 字符串字面量转义（含首尾引号）。Schema 层已移植，收敛为单一实现——
    /// 委托 <see cref="Schema.JsonText.Quote"/>（与上游逐字节一致，无行为变化）。
    /// </summary>
    private static string JsonQuote(string value) => Schema.JsonText.Quote(value);
}