// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.ToolSurface.Extraction;
using Mud.AI.ToolSurface.Schema;

namespace Mud.AI.ToolSurface.Emit;

/// <summary>
/// 方法级目录发射期中间数据（纯值，不参与增量管线，无需值相等）。
/// </summary>
/// <remarks>
/// 由 <see cref="CapabilityCatalogEmitter"/> 在遍历 SDK 接口时逐方法产出，
/// 由 <see cref="MethodCatalogEmitter"/> 渲染为 <c>{P}MethodCatalog</c> 编译期常量。
/// </remarks>
internal sealed class MethodCatalogEntryData
{
    public MethodCatalogEntryData(
        string qualifiedName,
        string interfaceName,
        string methodName,
        string httpMethod,
        string routeTemplate,
        string tokenKind,
        string module,
        int risk,
        IReadOnlyList<string> pathParams,
        IReadOnlyList<string> queryParams,
        string? bodyType,
        string? summary,
        string? curatedTool)
    {
        QualifiedName = qualifiedName;
        InterfaceName = interfaceName;
        MethodName = methodName;
        HttpMethod = httpMethod;
        RouteTemplate = routeTemplate;
        TokenKind = tokenKind;
        Module = module;
        Risk = risk;
        PathParams = pathParams;
        QueryParams = queryParams;
        BodyType = bodyType;
        Summary = summary;
        CuratedTool = curatedTool;
    }

    /// <summary>方法限定名（<c>{接口名}.{方法名}</c>，如 <c>IFeishuV2OkrObjective.GetObjectiveAsync</c>）。</summary>
    public string QualifiedName { get; }

    /// <summary>声明该方法的 SDK 接口名。</summary>
    public string InterfaceName { get; }

    /// <summary>SDK 方法名。</summary>
    public string MethodName { get; }

    /// <summary>HTTP 方法（GET/POST/PUT/PATCH/DELETE）。</summary>
    public string HttpMethod { get; }

    /// <summary>路由模板（来自 <c>[Get]/[Post]/…</c> 特性的声明，含 <c>{path}</c> 占位）。</summary>
    public string RouteTemplate { get; }

    /// <summary>令牌身份字面量（tenant/user/both/unspecified，经 <see cref="ToolSurfaceTokenKindContract"/> 渲染）。</summary>
    public string TokenKind { get; }

    /// <summary>所属能力域（接口名解析出的 domain 段；解析失败为 <c>Unparsed</c>）。</summary>
    public string Module { get; }

    /// <summary>风险分级（0=read / 1=write / 2=high-risk-write，与消费方风险枚举对齐）。</summary>
    public int Risk { get; }

    /// <summary>路径参数名（<c>[Path]</c>）。</summary>
    public IReadOnlyList<string> PathParams { get; }

    /// <summary>查询参数名（<c>[Query("name")]</c>，取声明名，缺省回退参数名）。</summary>
    public IReadOnlyList<string> QueryParams { get; }

    /// <summary>请求体参数类型显示名（<c>[Body]</c>；无请求体时为 <see langword="null"/>）。</summary>
    public string? BodyType { get; }

    /// <summary>方法 XML summary（可空）。</summary>
    public string? Summary { get; }

    /// <summary>覆盖该方法的策展工具名（可空 = 未策展）。</summary>
    public string? CuratedTool { get; }

    /// <summary>是否已被策展为工具（<see cref="CuratedTool"/> 非空）。</summary>
    public bool Curated => CuratedTool is not null;
}

/// <summary>
/// 方法级目录发射器：把 <see cref="MethodCatalogEntryData"/> 渲染为 <c>{P}MethodCatalog</c> 编译期常量。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么是"轻量元数据"而非完整 Schema</b>（与 <see cref="CapabilityCatalogEmitter"/> 的粒度纪律一致）：
/// 逐方法发射完整 JSON Schema 会把编译期成本与产物体积推高一个量级，而运行时 schema 自省
/// （<c>feishu.schema_read</c>）与万能兜底（<c>feishu.api_call</c>）真正需要的只是「方法名 + HTTP + 路由 +
/// 参数位置与类型名 + 令牌身份 + 域 + 风险」这些<b>符号层可直接提取</b>的事实——不需要类型 Schema 解析。
/// </para>
/// <para>
/// <b>与 {@link CapabilityCatalogEmitter} 的分工</b>：后者产出<b>聚合</b>事实（方法数 / 分组分布 / 覆盖率），
/// 本发射器产出<b>单元</b>事实（逐方法签名）。二者共享同一开关
/// （<c>build_property.{CapabilityCatalogPropertyName}</c>）与同一遍历，只增一条渲染路径。
/// </para>
/// <para>
/// <b>方法归属纪律</b>：沿用能力目录的「自身声明」粒度——方法在哪个接口声明就归属哪个接口，
/// 派生接口的继承方法不重复发射（否则同一方法被每个派生接口重复计数，破坏"目录与方法一一对应"）。
/// </para>
/// </remarks>
internal static class MethodCatalogEmitter
{
    /// <summary>从 SDK 接口方法符号提取方法级事实（参数位置 + HTTP 路由 + 风险 + 文档摘要）。</summary>
    public static MethodCatalogEntryData CreateEntry(
        INamedTypeSymbol interfaceType,
        IMethodSymbol method,
        string module,
        ToolSurfaceTokenKind tokenKind,
        SdkToolProfileModel profile,
        IReadOnlyDictionary<string, string> curatedToolByMethod)
    {
        var (httpMethod, routeTemplate) = Extractors.ExtractHttpInfo(method);
        var risk = (int)Extractors.DeriveRisk(httpMethod, method.Name, profile);
        var qualifiedName = interfaceType.Name + "." + method.Name;
        var tokenKindLiteral = ToolSurfaceTokenKindContract.ToLiteral(tokenKind);

        var pathParams = new List<string>();
        var queryParams = new List<string>();
        string? bodyType = null;

        foreach (var parameter in method.Parameters)
        {
            var classification = ClassifyParameter(parameter);
            switch (classification.Kind)
            {
                case ParameterKind.Path:
                    pathParams.Add(parameter.Name);
                    break;
                case ParameterKind.Query:
                    queryParams.Add(classification.Annotation ?? parameter.Name);
                    break;
                case ParameterKind.Body:
                    bodyType = parameter.Type.ToDisplayString();
                    break;
            }
        }

        var summary = Extractors.GetDocSummary(method);
        curatedToolByMethod.TryGetValue(method.Name, out var curatedTool);

        return new MethodCatalogEntryData(
            qualifiedName,
            interfaceType.Name,
            method.Name,
            httpMethod,
            routeTemplate,
            tokenKindLiteral,
            module,
            risk,
            pathParams,
            queryParams,
            bodyType,
            summary,
            curatedTool);
    }

    /// <summary>发射 <c>{P}MethodCatalog</c> 源码；空条目集合时静默跳过（不产空目录）。</summary>
    public static void Emit(
        SourceProductionContext context,
        IReadOnlyList<MethodCatalogEntryData> entries,
        SdkToolProfileModel profile)
    {
        if (entries.Count == 0)
        {
            return;
        }

        var source = new StringBuilder();
        source.Line($"// <auto-generated> 由 {GeneratedCodeMarker.GeneratorName(profile)} 编译期产出，禁止手工修改 </auto-generated>");
        source.Line("#nullable enable");
        source.Line("#pragma warning disable CS1591 // 生成代码不逐一补 XML 注释");
        source.Line($"namespace {profile.GeneratedNamespace}");
        source.Line("{");
        source.Line("    /// <summary>SDK 方法级目录快照（运行时 schema 自省与万能兜底的唯一事实源）。</summary>");
        source.Line($"    {GeneratedCodeMarker.Attribute(profile)}");
        source.Line($"    internal static class {profile.ProductPrefix}MethodCatalog");
        source.Line("    {");
        source.Line("        /// <summary>单个 SDK 方法的结构化事实。</summary>");
        source.Line("        public sealed class Entry");
        source.Line("        {");
        source.Line("            public Entry(string interfaceName, string method, string http, string route, string tokenKind, string module, string[] pathParams, string[] queryParams, string? bodyType, int risk, string? summary, bool curated, string? curatedTool)");
        source.Line("            {");
        source.Line("                InterfaceName = interfaceName;");
        source.Line("                Method = method;");
        source.Line("                Http = http;");
        source.Line("                Route = route;");
        source.Line("                TokenKind = tokenKind;");
        source.Line("                Module = module;");
        source.Line("                PathParams = pathParams;");
        source.Line("                QueryParams = queryParams;");
        source.Line("                BodyType = bodyType;");
        source.Line("                Risk = risk;");
        source.Line("                Summary = summary;");
        source.Line("                Curated = curated;");
        source.Line("                CuratedTool = curatedTool;");
        source.Line("            }");
        source.Line();
        source.Line("            public string InterfaceName { get; }");
        source.Line("            public string Method { get; }");
        source.Line("            public string Http { get; }");
        source.Line("            public string Route { get; }");
        source.Line("            public string TokenKind { get; }");
        source.Line("            public string Module { get; }");
        source.Line("            public string[] PathParams { get; }");
        source.Line("            public string[] QueryParams { get; }");
        source.Line("            public string? BodyType { get; }");
        source.Line("            public int Risk { get; }");
        source.Line("            public string? Summary { get; }");
        source.Line("            public bool Curated { get; }");
        source.Line("            public string? CuratedTool { get; }");
        source.Line("        }");
        source.Line();
        source.Line("        /// <summary>SDK 接口声明的方法总数（与能力目录的 SdkMethodCount 同源）。</summary>");
        source.Line($"        public const int SdkMethodCount = {entries.Count};");
        source.Line();
        source.Line("        /// <summary>按方法限定名（{接口名}.{方法名}）索引的方法目录。</summary>");
        source.Line($"        {GeneratedCodeMarker.Attribute(profile)}");
        source.Line("        public static System.Collections.Generic.IReadOnlyDictionary<string, Entry> ByQualifiedName { get; } =");
        source.Line("            new System.Collections.Generic.Dictionary<string, Entry>(System.StringComparer.Ordinal)");
        source.Line("            {");
        foreach (var entry in entries)
        {
            source.Line($"                [{JsonText.ToCSharpLiteral(entry.QualifiedName)}] = {RenderEntry(entry)},");
        }

        source.Line("            };");
        source.Line("    }");
        source.Line("}");

        TransitiveCodeGenerator.AddSourceValidated(context, $"{profile.ProductPrefix}MethodCatalog.g.cs", source.ToString());
    }

    private static string RenderEntry(MethodCatalogEntryData entry)
    {
        return "new Entry("
            + JsonText.ToCSharpLiteral(entry.InterfaceName) + ", "
            + JsonText.ToCSharpLiteral(entry.MethodName) + ", "
            + JsonText.ToCSharpLiteral(entry.HttpMethod) + ", "
            + JsonText.ToCSharpLiteral(entry.RouteTemplate) + ", "
            + JsonText.ToCSharpLiteral(entry.TokenKind) + ", "
            + JsonText.ToCSharpLiteral(entry.Module) + ", "
            + RenderStringArray(entry.PathParams) + ", "
            + RenderStringArray(entry.QueryParams) + ", "
            + (entry.BodyType is null ? "null" : JsonText.ToCSharpLiteral(entry.BodyType)) + ", "
            + entry.Risk.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", "
            + (entry.Summary is null ? "null" : JsonText.ToCSharpLiteral(entry.Summary)) + ", "
            + (entry.Curated ? "true" : "false") + ", "
            + (entry.CuratedTool is null ? "null" : JsonText.ToCSharpLiteral(entry.CuratedTool))
            + ")";
    }

    private static string RenderStringArray(IReadOnlyList<string> items)
    {
        if (items.Count == 0)
        {
            return "System.Array.Empty<string>()";
        }

        var builder = new StringBuilder("new string[] { ");
        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(JsonText.ToCSharpLiteral(items[i]));
        }

        builder.Append(" }");
        return builder.ToString();
    }

    private static ParameterClassification ClassifyParameter(IParameterSymbol parameter)
    {
        foreach (var attribute in parameter.GetAttributes())
        {
            switch (attribute.AttributeClass?.Name)
            {
                case "PathAttribute":
                    return new ParameterClassification(ParameterKind.Path, null);
                case "QueryAttribute":
                    var annotation = attribute.ConstructorArguments.Length > 0
                        && attribute.ConstructorArguments[0].Value is string name
                        ? name
                        : null;
                    return new ParameterClassification(ParameterKind.Query, annotation);
                case "BodyAttribute":
                    return new ParameterClassification(ParameterKind.Body, null);
            }
        }

        return new ParameterClassification(ParameterKind.None, null);
    }

    private enum ParameterKind
    {
        None = 0,
        Path = 1,
        Query = 2,
        Body = 3,
    }

    private readonly struct ParameterClassification
    {
        public ParameterClassification(ParameterKind kind, string? annotation)
        {
            Kind = kind;
            Annotation = annotation;
        }

        public ParameterKind Kind { get; }

        public string? Annotation { get; }
    }
}