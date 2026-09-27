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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// [FeishuTool] 源生成器（已决策⑤）：扫描标注 <see cref="FeishuToolAttribute"/> 的接口，
/// 编译期产出 OpenAI-compatible 工具 Schema（<c>name/description/parameters/required</c>
/// + <c>required_scopes</c> 权限元数据，已决策⑥）。
/// </summary>
/// <remarks>
/// <para>
/// 产出物：静态类 <c>Mud.Feishu.AI.Tools.Generated.FeishuToolSchemas</c>，每个工具一个
/// <c>public const string {ToolNameCamel}SchemaJson</c> 与全量注册表
/// <c>SchemaByToolName</c>（供白名单注册/审计消费，运行期零反射）。
/// 全部标注接口<b>只发射一次</b>（<c>Collect</c> 聚合后单次输出）：常量与
/// <c>SchemaByToolName</c> 各仅一份，多工具接口并存不会产生重复成员。
/// </para>
/// <para>
/// Schema 定位（已决策⑥）：必要件非卖点——差异化在执行链
/// （授权钩子 + BeginScope 租户切换 + 强类型直连 + 结果裁剪）。
/// </para>
/// </remarks>
[Generator]
public sealed class FeishuToolSchemaGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 投影为纯字符串模型（值相等）再 Collect：增量管线缓存友好，且保证单文件发射。
        var toolSchemas = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is InterfaceDeclarationSyntax iface && iface.AttributeLists.Count > 0,
                static (ctx, _) => ctx.SemanticModel.GetDeclaredSymbol(ctx.Node) as INamedTypeSymbol)
            .Where(static symbol => symbol is not null && HasFeishuToolAttribute(symbol!))
            .Select(static (symbol, _) => BuildModel(symbol!))
            .Collect();

        context.RegisterSourceOutput(toolSchemas, static (spc, models) => EmitAll(spc, models));
    }

    private static bool HasFeishuToolAttribute(INamedTypeSymbol symbol)
        => symbol.GetAttributes().Any(a => IsFeishuToolAttribute(a.AttributeClass));

    private static bool IsFeishuToolAttribute(INamedTypeSymbol? attributeClass)
        => attributeClass is not null
            && attributeClass.Name == "FeishuToolAttribute"
            && attributeClass.ContainingNamespace.ToDisplayString() == "Mud.Feishu.AI.Tools";

    /// <summary>
    /// 单个工具的编译期 Schema 模型（纯字符串承载）。
    /// </summary>
    /// <remarks>
    /// 手写值相等（而非 record）：生成器宿主为 netstandard2.0，record 的 init 访问器需要
    /// <c>IsExternalInit</c> 垫片。值相等供增量管线缓存。
    /// </remarks>
    private sealed class ToolSchemaModel : IEquatable<ToolSchemaModel?>
    {
        public ToolSchemaModel(string toolName, string interfaceName, string constName, string schemaJson)
        {
            ToolName = toolName;
            InterfaceName = interfaceName;
            ConstName = constName;
            SchemaJson = schemaJson;
        }

        public string ToolName { get; }

        public string InterfaceName { get; }

        public string ConstName { get; }

        public string SchemaJson { get; }

        public bool Equals(ToolSchemaModel? other)
            => other is not null
                && string.Equals(ToolName, other.ToolName, StringComparison.Ordinal)
                && string.Equals(InterfaceName, other.InterfaceName, StringComparison.Ordinal)
                && string.Equals(ConstName, other.ConstName, StringComparison.Ordinal)
                && string.Equals(SchemaJson, other.SchemaJson, StringComparison.Ordinal);

        public override bool Equals(object? obj) => Equals(obj as ToolSchemaModel);

        public override int GetHashCode()
        {
            unchecked
            {
                // ns2.0 无 string.GetHashCode(StringComparison) 重载，经 StringComparer.Ordinal。
                var comparer = StringComparer.Ordinal;
                var hash = 17;
                hash = (hash * 31) + comparer.GetHashCode(ToolName);
                hash = (hash * 31) + comparer.GetHashCode(InterfaceName);
                hash = (hash * 31) + comparer.GetHashCode(ConstName);
                hash = (hash * 31) + comparer.GetHashCode(SchemaJson);
                return hash;
            }
        }
    }

    private static ToolSchemaModel BuildModel(INamedTypeSymbol interfaceSymbol)
    {
        var attribute = interfaceSymbol.GetAttributes().First(a => IsFeishuToolAttribute(a.AttributeClass));
        var toolName = attribute.ConstructorArguments.FirstOrDefault().Value as string ?? string.Empty;
        if (string.IsNullOrWhiteSpace(toolName))
        {
            // 无名工具在发射阶段统一报 MUDFT001（诊断须在 RegisterSourceOutput 内上报）。
            return new ToolSchemaModel(string.Empty, interfaceSymbol.Name, string.Empty, string.Empty);
        }

        var description = GetNamedString(attribute, "Description");
        var scopes = GetNamedArray(attribute, "RequiredScopes");
        var isWrite = GetNamedBool(attribute, "IsWrite");

        var schema = BuildSchemaJson(toolName, description, scopes, isWrite, interfaceSymbol);
        return new ToolSchemaModel(toolName, interfaceSymbol.Name, BuildConstName(toolName), schema);
    }

    private static void EmitAll(SourceProductionContext context, System.Collections.Immutable.ImmutableArray<ToolSchemaModel> models)
    {
        if (models.IsEmpty)
        {
            return;
        }

        var diagnostics = models.Where(m => m.ToolName.Length == 0).ToArray();
        foreach (var invalid in diagnostics)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "MUDFT001", "FeishuTool 缺少工具名",
                    "[FeishuTool] 标注的接口 {0} 未提供工具名", "MudFeishu.AI", DiagnosticSeverity.Error, true),
                Location.None, invalid.InterfaceName));
        }

        var valid = models
            .Where(m => m.ToolName.Length > 0)
            .OrderBy(m => m.ToolName, StringComparer.Ordinal)
            .ToArray();

        if (valid.Length == 0)
        {
            return;
        }

        var source = new StringBuilder();
        source.AppendLine("// <auto-generated> 由 FeishuToolSchemaGenerator 编译期产出，禁止手工修改 </auto-generated>");
        source.AppendLine("#nullable enable");
        source.AppendLine("#pragma warning disable CS1591 // 生成代码不逐一补 XML 注释");
        source.AppendLine("namespace Mud.Feishu.AI.Tools.Generated");
        source.AppendLine("{");
        source.AppendLine("    /// <summary>[FeishuTool] 接口编译期产出的工具 Schema（AOT 安全：零运行时反射）。</summary>");
        source.AppendLine("    public static partial class FeishuToolSchemas");
        source.AppendLine("    {");

        foreach (var model in valid)
        {
            source.AppendLine($"        public const string {model.ConstName} = {ToCSharpStringLiteral(model.SchemaJson)};");
            source.AppendLine();
        }

        source.AppendLine("        /// <summary>工具名 → Schema 的注册表快照（供白名单注册与 scope 审计消费）。</summary>");
        source.AppendLine("        public static System.Collections.Generic.IReadOnlyDictionary<string, string> SchemaByToolName { get; } =");
        source.AppendLine("            new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.Ordinal)");
        source.AppendLine("            {");
        foreach (var model in valid)
        {
            source.AppendLine($"                [{ToCSharpStringLiteral(model.ToolName)}] = {model.ConstName},");
        }

        source.AppendLine("            };");
        source.AppendLine("    }");
        source.AppendLine("}");

        context.AddSource("FeishuToolSchemas.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
    }

    private static string BuildSchemaJson(
        string toolName,
        string? description,
        IReadOnlyList<string> scopes,
        bool isWrite,
        INamedTypeSymbol interfaceSymbol)
    {
        var json = new StringBuilder();
        json.Append("{\"name\":").Append(Quote(toolName));
        if (!string.IsNullOrWhiteSpace(description))
        {
            json.Append(",\"description\":").Append(Quote(description!));
        }

        // 方法参数：接口方法的参数即模型可见参数（工具接口单方法约定）；
        // 方法级 [ToolParameter(name,…)] 按 name 匹配兜底（与参数级标注等效）。
        var methodLevelParameters = interfaceSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .SelectMany(m => m.GetAttributes())
            .Where(a => a.AttributeClass?.Name == "ToolParameterAttribute")
            .ToArray();
        var parameters = interfaceSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.DeclaredAccessibility == Accessibility.Public && m.MethodKind == MethodKind.Ordinary)
            .SelectMany(m => m.Parameters)
            .Where(p => !IsCancellationToken(p.Type))
            .GroupBy(p => p.Name, StringComparer.Ordinal)
            .Select(g => g.First())
            .ToArray();

        json.Append(",\"parameters\":{\"type\":\"object\",\"properties\":{");
        var required = new List<string>();
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var (jsonType, _) = MapJsonType(parameter);
            var parameterDescription = GetParameterDescription(parameter, methodLevelParameters);

            if (i > 0)
            {
                json.Append(',');
            }

            json.Append(Quote(parameter.Name)).Append(":{\"type\":").Append(Quote(jsonType));
            if (jsonType == "array")
            {
                json.Append(",\"items\":{\"type\":\"string\"}");
            }

            if (!string.IsNullOrWhiteSpace(parameterDescription))
            {
                json.Append(",\"description\":").Append(Quote(parameterDescription!));
            }

            json.Append('}');

            if (IsRequired(parameter, methodLevelParameters))
            {
                required.Add(parameter.Name);
            }
        }

        json.Append("},\"required\":[");
        for (var i = 0; i < required.Count; i++)
        {
            if (i > 0)
            {
                json.Append(',');
            }

            json.Append(Quote(required[i]));
        }

        json.Append("]}");

        // required_scopes（已决策⑥）：随 Schema 输出供授权钩子与审计消费；SDK 不内建校验。
        json.Append(",\"x-feishu\":{");
        json.Append("\"is_write\":").Append(isWrite ? "true" : "false");
        json.Append(",\"required_scopes\":[");
        for (var i = 0; i < scopes.Count; i++)
        {
            if (i > 0)
            {
                json.Append(',');
            }

            json.Append(Quote(scopes[i]));
        }

        json.Append("]}}");
        return json.ToString();
    }

    private static (string JsonType, bool IsNullable) MapJsonType(IParameterSymbol parameter)
    {
        var type = parameter.Type;
        if (type is INamedTypeSymbol named
            && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return (MapJsonPrimitiveType(named.TypeArguments[0]), true);
        }

        return (MapJsonPrimitiveType(type), parameter.NullableAnnotation == NullableAnnotation.Annotated);
    }

    private static string MapJsonPrimitiveType(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Array)
        {
            return "array";
        }

        switch (type.SpecialType)
        {
            case SpecialType.System_Boolean:
                return "boolean";
            case SpecialType.System_Int16:
            case SpecialType.System_Int32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt16:
            case SpecialType.System_UInt32:
            case SpecialType.System_UInt64:
                return "integer";
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_Decimal:
                return "number";
            default:
                return "string";
        }
    }

    private static bool IsCancellationToken(ITypeSymbol type)
        => type.Name == "CancellationToken";

    private static bool IsRequired(IParameterSymbol parameter, AttributeData[] methodLevelParameters)
    {
        // [ToolParameter(..., Required=true)] 显式必填；可空引用类型不纳入 required；
        // 非空值类型视为必填。
        var attribute = GetToolParameterAttribute(parameter, methodLevelParameters);
        if (attribute is not null
            && attribute.NamedArguments.FirstOrDefault(a => a.Key == "Required").Value.Value is bool explicitRequired)
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

    private static string? GetParameterDescription(IParameterSymbol parameter, AttributeData[] methodLevelParameters)
    {
        var attribute = GetToolParameterAttribute(parameter, methodLevelParameters);
        if (attribute is not null
            && attribute.ConstructorArguments.Length >= 2
            && attribute.ConstructorArguments[1].Value is string description)
        {
            return description;
        }

        return null;
    }

    /// <summary>取参数级 [ToolParameter]；无则按名匹配方法级标注（兼容两种写法）。</summary>
    private static AttributeData? GetToolParameterAttribute(IParameterSymbol parameter, AttributeData[] methodLevelParameters)
    {
        var direct = parameter.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "ToolParameterAttribute");
        if (direct is not null)
        {
            return direct;
        }

        return methodLevelParameters.FirstOrDefault(a =>
            a.ConstructorArguments.Length >= 1
            && a.ConstructorArguments[0].Value is string name
            && string.Equals(name, parameter.Name, StringComparison.Ordinal));
    }

    private static string? GetNamedString(AttributeData attribute, string key)
        => attribute.NamedArguments.FirstOrDefault(a => a.Key == key).Value.Value as string;

    private static bool GetNamedBool(AttributeData attribute, string key)
        => attribute.NamedArguments.FirstOrDefault(a => a.Key == key).Value.Value is bool value && value;

    private static IReadOnlyList<string> GetNamedArray(AttributeData attribute, string key)
    {
        if (attribute.NamedArguments.FirstOrDefault(a => a.Key == key).Value.Kind != TypedConstantKind.Array)
        {
            return [];
        }

        return attribute.NamedArguments
            .First(a => a.Key == key).Value
            .Values
            .Select(v => v.Value as string)
            .Where(v => v is not null)
            .Select(v => v!)
            .ToArray();
    }

    private static string BuildConstName(string toolName)
    {
        var segments = toolName.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
        var name = string.Join("_", segments).Replace('-', '_');
        var sb = new StringBuilder(name.Length + 8);
        foreach (var ch in name)
        {
            sb.Append(char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_');
        }

        return sb.ToString() + "SchemaJson";
    }

    private static string Quote(string value)
    {
        var sb = new StringBuilder(value.Length + 2);
        sb.Append('"');
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    if (ch < ' ')
                    {
                        sb.Append("\\u").Append(((int)ch).ToString("x4"));
                    }
                    else
                    {
                        sb.Append(ch);
                    }

                    break;
            }
        }

        sb.Append('"');
        return sb.ToString();
    }

    private static string ToCSharpStringLiteral(string value)
    {
        // 原始字符串字面量（C# 11）承载 JSON，减少转义层级；内嵌三引号场景用拼接规避。
        if (value.IndexOf("\"\"\"", StringComparison.Ordinal) >= 0)
        {
            return Quote(value);
        }

        return "\"\"\"" + value + "\"\"\"";
    }
}
