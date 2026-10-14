// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.ToolSurface.Extraction;

/// <summary>
/// Tier C 扫描器：把手写工具特性接口（策展暴露面的<b>唯一</b>声明处）编译为
/// <see cref="ToolSchemaModel"/>，并就 <c>Source</c> 与 SDK 事实做交叉校验。
/// </summary>
/// <remarks>
/// <para>
/// 移植自上游 <c>Mud.Feishu.AI.Tools.Extraction.CuratedToolScanner</c>，泛化点（设计文档 §2）：
/// <list type="bullet">
/// <item>特性识别 → <see cref="Extractors.GetToolAttribute"/>（<c>ToolAttributeName/Namespace</c> 槽）；</item>
/// <item>源挂钩解析 → <see cref="SourceResolver"/>（<c>SdkNamespaceRoot</c> 槽）；</item>
/// <item>令牌身份推导 → <see cref="InterfaceIdentityParser.DeriveTokenKind"/>
/// （<c>TokenKindStrategy</c> + <c>TokenKindMarkers</c> 标记表，替代 <c>StartsWith("IFeishuTenant")</c>）；</item>
/// <item>接口范式校验 → <see cref="InterfaceIdentityParser.TryParse"/>（<c>InterfaceNameRegex</c> 槽）；</item>
/// <item>危险词表 → <c>WriteVerbKeywords</c> 槽（经 <see cref="Extractors.DeriveRisk"/>）；</item>
/// <item>诊断 → <see cref="ToolSurfaceDiagnostics.Factory"/> 动态槽位（替代静态 MUDFT 常量）。</item>
/// </list>
/// </para>
/// <para>
/// <b>这是"工具面不脱钩 SDK"的机械保证</b>：工具名/描述/scope 由人声明（就是策展），
/// 而 <b>HTTP 路由、风险分级、返回形状、上传/下载形态</b>全部从 SDK 符号推导——
/// 声明与 SDK 不符即构建失败（槽位 019 / 017）。
/// </para>
/// <para>
/// 参数 Schema 由 <see cref="ParameterSchemaRenderer"/> 从符号推导（枚举/数组元素/format/复合 DTO）。
/// </para>
/// </remarks>
internal static class ToolSurfaceScanner
{
    /// <summary>
    /// P2-2 验证钩子：仅供测试程序集（<c>InternalsVisibleTo</c>）注入异常源，驱动 <c>{prefix}026</c>
    /// 兜底路径——实参为剖面名，返回非空异常即在扫描体起点抛出。常态为 <see langword="null"/>，零开销。
    /// </summary>
    internal static Func<string, Exception?>? FaultInjectionForTests;

    /// <summary>扫描单个接口符号。</summary>
    /// <param name="symbol">标注工具特性的接口。</param>
    /// <param name="compilation">当前编译（<c>Source</c> 解析与类型推导用）。</param>
    /// <param name="profile">当前剖面。</param>
    /// <param name="factory">当前剖面的诊断工厂（动态槽位描述符）。</param>
    /// <returns>扫描结果（模型 + 诊断）。</returns>
    public static ScannedTool Scan(
        INamedTypeSymbol symbol,
        Compilation compilation,
        SdkToolProfileModel profile,
        ToolSurfaceDiagnostics.Factory factory)
    {
        try
        {
            if (FaultInjectionForTests?.Invoke(profile.Name) is { } fault)
            {
                throw fault;
            }

            var attribute = Extractors.GetToolAttribute(symbol, profile);
            if (attribute is null)
            {
                // 调用方已过滤；防御性兜底，不产诊断（避免把无关接口变成构建错误）。
                return ScannedTool.Faulted(symbol.Name);
            }

            var candidateToolName = attribute.ConstructorArguments.FirstOrDefault().Value as string;
            if (string.IsNullOrWhiteSpace(candidateToolName))
            {
                // 槽位 001 上报点：缺工具名。
                return ScannedTool.Faulted(symbol.Name, PendingDiagnostic.Create(
                    factory[ToolSurfaceDiagnostics.SlotMissingToolName],
                    profile.ToolAttributeName,
                    symbol.Name));
            }

            // 显式窄化到独立的非空局部变量（上游 R2-04 纪律：一次窄化同时消除 CS1717 与下游 CS8604）。
            var toolName = candidateToolName!;
            var description = GetNamedString(attribute, "Description") ?? string.Empty;
            var scopes = GetNamedArray(attribute, "RequiredScopes");
            var isWrite = GetNamedBool(attribute, "IsWrite");
            var source = GetNamedString(attribute, "Source");

            var parameters = ParameterSchemaRenderer.RenderParameters(symbol, compilation, profile);
            var anyOfGroups = ReadAnyOfGroups(attribute, toolName, parameters, factory, out var anyOfDiagnostics);
            var diagnostics = new List<PendingDiagnostic>(anyOfDiagnostics);

            // 源挂钩交叉校验（工具面消费 SDK 符号）。
            var httpMethod = string.Empty;
            var routeTemplate = string.Empty;
            var methodName = string.Empty;
            string? outputSchema = null;
            var risk = isWrite ? ToolSurfaceRisk.Write : ToolSurfaceRisk.Read;
            var identity = DefaultIdentity;
            IReadOnlyList<string> outputSchemaTruncations = [];

            if (!string.IsNullOrWhiteSpace(source))
            {
                SourceResolver.TryNormalize(source, profile, out var sourceTypeName, out var sourceMethodName);

                var resolved = SourceResolver.Resolve(compilation, sourceTypeName, sourceMethodName, profile, out var failure);
                if (resolved is null)
                {
                    // 槽位 019 上报点：声明的 SDK 源无法解析（工具面与 SDK 脱钩）。
                    diagnostics.Add(PendingDiagnostic.Create(
                        factory[ToolSurfaceDiagnostics.SlotSourceUnresolvable],
                        profile.ToolAttributeName, toolName, source!, failure ?? "未知原因"));
                }
                else
                {
                    var (sourceType, method) = resolved.Value;
                    (httpMethod, routeTemplate) = Extractors.ExtractHttpInfo(method);
                    methodName = method.Name;
                    identity = DeriveIdentityFromSource(sourceType.Name, sourceTypeName, diagnostics, toolName, profile, factory);

                    if (!InterfaceIdentityParser.TryParse(sourceType.Name, profile, out _))
                    {
                        // 槽位 002 上报点：源接口命名不符合剖面声明的 SDK 范式——
                        // 不符合范式意味着无法推导令牌身份与能力归属，工具与 SDK 的对应关系不可机械校验。
                        diagnostics.Add(PendingDiagnostic.Create(
                            factory[ToolSurfaceDiagnostics.SlotInterfaceNamingViolation],
                            sourceType.Name, profile.Name));
                    }

                    var derivedRisk = Extractors.DeriveRisk(httpMethod, method.Name, profile);
                    risk = Max(risk, derivedRisk);

                    if (derivedRisk != ToolSurfaceRisk.Read && !isWrite)
                    {
                        // 槽位 017 上报点：SDK 事实为写面，但工具被归类为只读（会绕过授权门禁）。
                        diagnostics.Add(PendingDiagnostic.Create(
                            factory[ToolSurfaceDiagnostics.SlotReadWriteDecoupling],
                            toolName, source!, httpMethod, RiskToString(derivedRisk)));
                    }

                    ValidateReturnType(symbol.Name, method, compilation, profile, diagnostics, factory, out outputSchema, out outputSchemaTruncations);
                    ValidateUploadParameters(symbol.Name, method, diagnostics, factory);
                }
            }

            ValidateParameterExpansion(symbol.Name, parameters, diagnostics, factory);

            var firstMethod = symbol.GetMembers().OfType<IMethodSymbol>().FirstOrDefault();
            var docSummary = description;
            if (string.IsNullOrWhiteSpace(docSummary))
            {
                docSummary = firstMethod is null ? null : Extractors.GetDocSummary(firstMethod);
            }

            // ── 槽位 005/006 上报点 ──
            // 这两条反映的是"模型看到的描述质量"：description 为空 → 模型不知工具干什么；
            // 参数无说明 → 模型只能靠参数名猜。二者都是真实且此前**静默**的缺口。
            if (string.IsNullOrWhiteSpace(docSummary))
            {
                diagnostics.Add(PendingDiagnostic.Create(
                    factory[ToolSurfaceDiagnostics.SlotMissingXmlSummary],
                    symbol.Name, firstMethod?.Name ?? symbol.Name));
            }

            foreach (var parameter in parameters)
            {
                if (string.IsNullOrWhiteSpace(parameter.DocDescription))
                {
                    diagnostics.Add(PendingDiagnostic.Create(
                        factory[ToolSurfaceDiagnostics.SlotMissingXmlParam],
                        symbol.Name, firstMethod?.Name ?? symbol.Name, parameter.Name));
                }
            }

            var entry = new CapabilityEntry(
                interfaceName: symbol.Name,
                toolName: toolName,
                identity: identity,
                httpMethod: httpMethod,
                routeTemplate: routeTemplate,
                methodName: methodName,
                parameters: parameters,
                docSummary: docSummary,
                risk: risk,
                scopes: scopes,
                outputSchemaJson: outputSchema,
                outputSchemaTruncations: outputSchemaTruncations,
                anyOfGroups: anyOfGroups);

            // 注：isWrite 只参与 risk 推导（见上），不进入模型——读写分类的唯一事实源是 entry.Risk。
            var model = new ToolSchemaModel(entry, BuildConstName(toolName), description, source);
            return diagnostics.Count == 0
                ? ScannedTool.Ok(symbol.Name, model)
                : ScannedTool.OkWithDiagnostics(symbol.Name, model, diagnostics.ToArray());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            GeneratorDebugLogger.LogError(nameof(ToolSurfaceScanner), ex);
            // P2-2：扫描体异常折算为 {prefix}026 随流上报——此前返回无诊断的 Faulted，
            // 产物静默缺席而构建全绿（错误不可诊断）。OCE 是宿主取消语义，必须继续上抛。
            return ScannedTool.Faulted(
                symbol.Name,
                PendingDiagnostic.Create(
                    factory[ToolSurfaceDiagnostics.SlotGeneratorInternalError],
                    profile.Name + ":" + nameof(ToolSurfaceScanner),
                    ex.GetType().Name,
                    ex.Message));
        }
    }

    // ────────── 校验 ──────────

    /// <summary>
    /// 读取并校验工具特性的 <c>AnyOf = ["a|b|c"]</c> 条件必填组。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么在生成器侧校验</b>：组内参数名若拼错，渲染出的 <c>anyOf</c> 会指向不存在的字段，
    /// 约束<b>永久失效且无任何症状</b>（构建通过、Schema 合法、只是约束了空气）。
    /// 这类"静默失效"必须在构建期拦下，故报槽位 011（Error）。
    /// </para>
    /// <para>
    /// <b>三条校验规则</b>：① 组内每个参数名必须存在于签名；② 组内不得含
    /// <c>Required = true</c> 的参数（否则"至少一个"退化为"全部必填"，语义相反）；
    /// ③ 单元素组无意义（等价于该参数必填，应改用 <c>Required</c>）。
    /// </para>
    /// </remarks>
    private static IReadOnlyList<IReadOnlyList<string>> ReadAnyOfGroups(
        AttributeData attribute,
        string toolName,
        IReadOnlyList<CapabilityParameter> parameters,
        ToolSurfaceDiagnostics.Factory factory,
        out List<PendingDiagnostic> diagnostics)
    {
        diagnostics = [];
        var groups = new List<IReadOnlyList<string>>();

        var declarations = GetNamedArray(attribute, "AnyOf");
        if (declarations.Count == 0)
        {
            return groups;
        }

        // netstandard2.0 无 Enumerable.ToHashSet / StringSplitOptions.TrimEntries，手工构建。
        var parameterNames = new HashSet<string>(StringComparer.Ordinal);
        var requiredNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var p in parameters)
        {
            parameterNames.Add(p.Name);
            if (p.IsRequired)
            {
                requiredNames.Add(p.Name);
            }
        }

        var available = string.Join(" / ", parameterNames);

        foreach (var declaration in declarations)
        {
            // 语法："a|b|c"（'|' 分隔）。空白容忍，但空组判错。
            var members = new List<string>();
            foreach (var raw in declaration.Split('|'))
            {
                var member = raw.Trim();
                if (member.Length > 0)
                {
                    members.Add(member);
                }
            }

            if (members.Count == 0)
            {
                diagnostics.Add(PendingDiagnostic.Create(
                    factory[ToolSurfaceDiagnostics.SlotAnyOfUnknownParameter],
                    toolName, declaration, "(空)", available));
                continue;
            }

            // ① 组内每个参数名必须存在于签名，否则 anyOf 约束"空气"。
            var unknown = members.FindAll(name => !parameterNames.Contains(name));
            if (unknown.Count > 0)
            {
                diagnostics.Add(PendingDiagnostic.Create(
                    factory[ToolSurfaceDiagnostics.SlotAnyOfUnknownParameter],
                    toolName, declaration, unknown[0], available));
                continue;
            }

            // ② 与"已必填"混用会让 anyOf 语义反转（"至少一个" → "全部必填"）。
            var conflicting = members.FindAll(requiredNames.Contains);
            if (conflicting.Count > 0)
            {
                diagnostics.Add(PendingDiagnostic.Create(
                    factory[ToolSurfaceDiagnostics.SlotAnyOfUnknownParameter],
                    toolName, declaration, conflicting[0], available));
                continue;
            }

            // ③ 单元素组等价于 Required = true，应改用它。
            if (members.Count == 1)
            {
                diagnostics.Add(PendingDiagnostic.Create(
                    factory[ToolSurfaceDiagnostics.SlotAnyOfUnknownParameter],
                    toolName, declaration, members[0], available));
                continue;
            }

            groups.Add(members);
        }

        return groups;
    }

    private static void ValidateReturnType(
        string interfaceName,
        IMethodSymbol method,
        Compilation compilation,
        SdkToolProfileModel profile,
        List<PendingDiagnostic> diagnostics,
        ToolSurfaceDiagnostics.Factory factory,
        out string? outputSchema,
        out IReadOnlyList<string> truncations)
    {
        outputSchema = null;
        truncations = [];

        var payload = Extractors.UnwrapTaskType(method.ReturnType);
        if (payload is null || payload.Name == "HttpResponseMessage")
        {
            // 槽位 004 上报点：返回类型不可映射（非泛型 Task / 裸 HttpResponseMessage）。
            diagnostics.Add(PendingDiagnostic.Create(
                factory[ToolSurfaceDiagnostics.SlotReturnTypeNotMappable],
                interfaceName, method.Name, method.ReturnType.ToDisplayString()));
            return;
        }

        // 截断样本由解析器记录、随条目流转，最终由生成器聚合为**单条**槽位 009
        // （逐处上报会被 SDK 中大量深层 DTO 淹没）。
        // resolver 由入口生成器注册（Schema 层 <c>TypeSchemaResolver</c>）；缺席时降级为不推导 output schema。
        var resolver = ToolSurfaceSchemaResolver.TryCreate(compilation, profile);
        if (resolver is not null)
        {
            outputSchema = resolver.ResolveOutputSchema(payload);
            truncations = resolver.Truncations;
        }
    }

    private static void ValidateUploadParameters(
        string interfaceName,
        IMethodSymbol method,
        List<PendingDiagnostic> diagnostics,
        ToolSurfaceDiagnostics.Factory factory)
    {
        if (!Extractors.HasFileUpload(method))
        {
            return;
        }

        foreach (var parameter in method.Parameters)
        {
            var typeName = parameter.Type.ToDisplayString();
            if (typeName is "System.Threading.CancellationToken")
            {
                continue;
            }

            var isBinary = typeName is "byte[]" or "System.Byte[]" or "System.IO.Stream"
                or "System.IO.FileStream" or "System.IO.MemoryStream";
            var isFormContent = parameter.GetAttributes().Any(static a => a.AttributeClass?.Name == "FormContentAttribute");
            if (!isBinary && !isFormContent)
            {
                // 槽位 008 上报点：上传参数声称为文件但类型无法映射为 format:binary。
                diagnostics.Add(PendingDiagnostic.Create(
                    factory[ToolSurfaceDiagnostics.SlotBinaryMappingFailure],
                    interfaceName, method.Name, parameter.Name, typeName));
            }
        }
    }

    private static void ValidateParameterExpansion(
        string interfaceName,
        IReadOnlyList<CapabilityParameter> parameters,
        List<PendingDiagnostic> diagnostics,
        ToolSurfaceDiagnostics.Factory factory)
    {
        foreach (var parameter in parameters)
        {
            if (parameter.SchemaFragmentJson != "{\"type\":\"string\"}")
            {
                continue;
            }

            // 复合参数被降级为字符串 = 模型无法得知可传字段（槽位 010 上报点）。
            if (parameter.CsharpType.IndexOf('<') < 0
                && !IsPrimitiveName(parameter.CsharpType)
                && char.IsUpper(parameter.CsharpType.TrimEnd('?')[0]))
            {
                diagnostics.Add(PendingDiagnostic.Create(
                    factory[ToolSurfaceDiagnostics.SlotQueryExpansionFailure],
                    interfaceName, "", parameter.Name, parameter.CsharpType));
            }
        }
    }

    // ────────── 辅助 ──────────

    /// <summary>
    /// 无 <c>Source</c> 声明（或声明未命中）时的缺省令牌身份。
    /// </summary>
    /// <remarks>
    /// 上游缺省为 <c>ToolIdentity.Tenant</c>；剖面没有"缺省身份"槽（设计文档 §4.2 十七组清单无此项），
    /// 为保持行为零漂移沿用同一枚举值（枚举槽本身平台无关，Tenant 语义 = 租户/企业级令牌）。
    /// </remarks>
    private const ToolSurfaceTokenKind DefaultIdentity = ToolSurfaceTokenKind.Tenant;

    /// <summary>
    /// 从源接口的命名标记表推导令牌身份（泛化自上游 <c>StartsWith("IFeishuTenant")</c> 硬编码）。
    /// 未命中任何标记（= 双令牌基接口形态）时报槽位 016：源指向无令牌的抽象基接口——
    /// 该类接口不能从 DI 解析，执行链必然失败。
    /// </summary>
    private static ToolSurfaceTokenKind DeriveIdentityFromSource(
        string sourceSimpleTypeName,
        string sourceTypeName,
        List<PendingDiagnostic> diagnostics,
        string toolName,
        SdkToolProfileModel profile,
        ToolSurfaceDiagnostics.Factory factory)
    {
        var kind = InterfaceIdentityParser.DeriveTokenKind(sourceSimpleTypeName, profile, out var rawMarker);
        if (rawMarker is not null)
        {
            return kind;
        }

        diagnostics.Add(PendingDiagnostic.Create(
            factory[ToolSurfaceDiagnostics.SlotTokenKindMismatch],
            toolName,
            ToolSurfaceTokenKindContract.ToLiteral(DefaultIdentity),
            sourceTypeName));
        return kind;
    }

    private static ToolSurfaceRisk Max(ToolSurfaceRisk a, ToolSurfaceRisk b) => (ToolSurfaceRisk)Math.Max((int)a, (int)b);

    private static string RiskToString(ToolSurfaceRisk risk) => risk switch
    {
        ToolSurfaceRisk.Read => "read",
        ToolSurfaceRisk.Write => "write",
        ToolSurfaceRisk.HighRiskWrite => "high-risk-write",
        _ => "read",
    };

    private static bool IsPrimitiveName(string csharpType)
    {
        var name = csharpType.TrimEnd('?');
        return name is "string" or "bool" or "int" or "long" or "short" or "byte" or "double" or "float"
            or "decimal" or "char" or "object" or "System.String" or "System.Object" or "System.DateTime"
            or "System.DateTimeOffset" or "System.Guid" or "System.TimeSpan";
    }

    private static string BuildConstName(string toolName)
    {
        var sb = new StringBuilder(toolName.Length + 8);
        foreach (var ch in toolName)
        {
            sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');
        }

        return sb.Append("SchemaJson").ToString();
    }

    private static string? GetNamedString(AttributeData attribute, string key)
        => attribute.NamedArguments.FirstOrDefault(a => a.Key == key).Value.Value as string;

    private static bool GetNamedBool(AttributeData attribute, string key)
        => attribute.NamedArguments.FirstOrDefault(a => a.Key == key).Value.Value is bool value && value;

    private static IReadOnlyList<string> GetNamedArray(AttributeData? attribute, string key)
    {
        if (attribute is null)
        {
            return [];
        }

        var constant = attribute.NamedArguments.FirstOrDefault(a => a.Key == key).Value;
        if (constant.Kind != TypedConstantKind.Array)
        {
            return [];
        }

        return constant.Values
            .Select(static v => v.Value as string)
            .Where(static v => !string.IsNullOrEmpty(v))
            .Select(static v => v!)
            .ToArray();
    }
}