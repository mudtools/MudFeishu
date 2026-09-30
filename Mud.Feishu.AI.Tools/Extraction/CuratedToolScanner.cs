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
/// Tier C 扫描器：把 <c>[FeishuTool]</c> 手写接口（策展暴露面的<b>唯一</b>声明处）编译为
/// <see cref="ToolSchemaModel"/>，并就 <c>Source</c> 与 SDK 事实做交叉校验。
/// </summary>
/// <remarks>
/// <para>
/// <b>这是"工具面不脱钩 SDK"的机械保证</b>：工具名/描述/scope 由人声明（就是策展），
/// 而 <b>HTTP 路由、风险分级、返回形状、上传/下载形态</b>全部从 SDK 符号推导——
/// 声明与 SDK 不符即构建失败（<c>MUDFT019</c> / <c>MUDFT017</c>）。
/// </para>
/// <para>
/// 参数 Schema 由 <see cref="ParameterSchemaRenderer"/> 从符号推导（枚举/数组元素/format/复合 DTO）。
/// </para>
/// </remarks>
internal static class CuratedToolScanner
{
    private static readonly char[] NameSeparator = ['.'];

    /// <summary>扫描单个接口符号。</summary>
    /// <param name="symbol">标注 <c>[FeishuTool]</c> 的接口。</param>
    /// <param name="compilation">当前编译（<c>Source</c> 解析与类型推导用）。</param>
    /// <returns>扫描结果（模型 + 诊断）。</returns>
    public static ScannedTool Scan(INamedTypeSymbol symbol, Compilation compilation)
    {
        var attribute = Extractors.GetFeishuToolAttribute(symbol);
        if (attribute is null)
        {
            // 调用方已过滤；防御性兜底，不产诊断（避免把无关接口变成构建错误）。
            return ScannedTool.Faulted(symbol.Name);
        }

        var candidateToolName = attribute.ConstructorArguments.FirstOrDefault().Value as string;
        if (string.IsNullOrWhiteSpace(candidateToolName))
        {
            // MUDFT001 上报点①：缺工具名。
            return ScannedTool.Faulted(symbol.Name, PendingDiagnostic.Create(Diagnostics.MUDFT001, symbol.Name));
        }

        // R2-04：显式窄化到独立的非空局部变量（取代 `toolName = toolName!;` 的自赋值空抑制）——
        // 一次窄化同时消除 CS1717（自赋值）与下游 4 处 CS8604（把 `string?` 传进非空形参）。
        var toolName = candidateToolName!;
        var description = GetNamedString(attribute, "Description") ?? string.Empty;
        var scopes = GetNamedArray(attribute, "RequiredScopes");
        var isWrite = GetNamedBool(attribute, "IsWrite");
        var source = GetNamedString(attribute, "Source");

        var parameters = ParameterSchemaRenderer.RenderParameters(symbol, compilation);
        var diagnostics = new List<PendingDiagnostic>();

        // 源挂钩交叉校验（AT-B02 的落地形态：工具面消费 Mud.Feishu 符号）。
        var httpMethod = string.Empty;
        var routeTemplate = string.Empty;
        var methodName = string.Empty;
        var returnTypeMetadata = string.Empty;
        string? outputSchema = null;
        var hasFileUpload = false;
        var returnsBinary = false;
        var risk = isWrite ? ToolRisk.Write : ToolRisk.Read;
        var identity = ToolIdentity.Tenant;
        IReadOnlyList<string> outputSchemaTruncations = [];

        if (!string.IsNullOrWhiteSpace(source))
        {
            var separatorIndex = source!.LastIndexOf('.');
            var sourceTypeName = separatorIndex > 0 ? source.Substring(0, separatorIndex) : source!;
            var sourceMethodName = separatorIndex > 0 ? source.Substring(separatorIndex + 1) : string.Empty;

            var resolved = Extractors.ResolveSourceMember(compilation, sourceTypeName, sourceMethodName, out var failure);
            if (resolved is null)
            {
                // MUDFT019 上报点：声明的 SDK 源无法解析（工具面与 SDK 脱钩）。
                diagnostics.Add(PendingDiagnostic.Create(Diagnostics.MUDFT019, toolName, source!, failure ?? "未知原因"));
            }
            else
            {
                var (sourceType, method) = resolved.Value;
                (httpMethod, routeTemplate) = Extractors.ExtractHttpInfo(method);
                methodName = method.Name;
                returnTypeMetadata = Extractors.ExtractReturnTypeMetadata(method);
                hasFileUpload = Extractors.HasFileUpload(method);
                returnsBinary = Extractors.ReturnsBinary(method);
                identity = DeriveIdentityFromSource(sourceType.Name, sourceTypeName, diagnostics, toolName);

                if (!Extractors.TryParseSdkInterfaceName(sourceType.Name, out _, out _, out _))
                {
                    // MUDFT002 上报点：源接口命名不符合 SDK 范式（IFeishu[Tenant|User]V{n}{Domain}{Resource}）。
                    // 不符合范式意味着无法推导令牌身份与能力归属——工具与 SDK 的对应关系不可机械校验。
                    diagnostics.Add(PendingDiagnostic.Create(Diagnostics.MUDFT002, sourceType.Name));
                }

                var derivedRisk = Extractors.DeriveRisk(httpMethod, method.Name);
                risk = Max(risk, derivedRisk);

                if (derivedRisk != ToolRisk.Read && !isWrite)
                {
                    // MUDFT017 上报点：SDK 事实为写面，但工具被归类为只读（会绕过授权门禁）。
                    diagnostics.Add(PendingDiagnostic.Create(
                        Diagnostics.MUDFT017, toolName, source!, httpMethod, RiskToString(derivedRisk)));
                }

                ValidateReturnType(symbol.Name, method, compilation, diagnostics, out outputSchema, out outputSchemaTruncations);
                ValidateUploadParameters(symbol.Name, method, diagnostics);
            }
        }

        ValidateParameterExpansion(symbol.Name, parameters, diagnostics);

        var firstMethod = symbol.GetMembers().OfType<IMethodSymbol>().FirstOrDefault();
        var docSummary = description;
        if (string.IsNullOrWhiteSpace(docSummary))
        {
            docSummary = firstMethod is null ? null : Extractors.GetDocSummary(firstMethod);
        }

        // ── MUDFT005/006 上报点（AT-B14 接线）──
        // 这两条反映的是"模型看到的描述质量"：description 为空 → 模型不知工具干什么；
        // 参数无说明 → 模型只能靠参数名猜。二者都是真实且此前**静默**的缺口。
        if (string.IsNullOrWhiteSpace(docSummary))
        {
            diagnostics.Add(PendingDiagnostic.Create(
                Diagnostics.MUDFT005, symbol.Name, firstMethod?.Name ?? symbol.Name));
        }

        foreach (var parameter in parameters)
        {
            if (string.IsNullOrWhiteSpace(parameter.DocDescription))
            {
                diagnostics.Add(PendingDiagnostic.Create(
                    Diagnostics.MUDFT006, symbol.Name, firstMethod?.Name ?? symbol.Name, parameter.Name));
            }
        }

        var entry = new CapabilityEntry(
            interfaceName: symbol.Name,
            toolName: toolName,
            moduleName: DeriveModule(toolName),
            identity: identity,
            httpMethod: httpMethod,
            routeTemplate: routeTemplate,
            methodName: methodName,
            returnTypeMetadataName: returnTypeMetadata,
            parameters: parameters,
            docSummary: docSummary,
            hasFileUpload: hasFileUpload,
            returnsBinary: returnsBinary,
            risk: risk,
            scopes: scopes,
            outputSchemaJson: outputSchema,
            outputSchemaTruncations: outputSchemaTruncations);

        var model = new ToolSchemaModel(entry, BuildConstName(toolName), description, isWrite, source);
        return diagnostics.Count == 0
            ? ScannedTool.Ok(symbol.Name, model)
            : ScannedTool.OkWithDiagnostics(symbol.Name, model, diagnostics.ToArray());
    }

    // ────────── 校验 ──────────

    private static void ValidateReturnType(
        string interfaceName,
        IMethodSymbol method,
        Compilation compilation,
        List<PendingDiagnostic> diagnostics,
        out string? outputSchema,
        out IReadOnlyList<string> truncations)
    {
        outputSchema = null;
        truncations = [];

        var payload = Extractors.UnwrapTaskType(method.ReturnType);
        if (payload is null || payload.Name == "HttpResponseMessage")
        {
            // MUDFT004 上报点：返回类型不可映射（非泛型 Task / 裸 HttpResponseMessage）。
            diagnostics.Add(PendingDiagnostic.Create(
                Diagnostics.MUDFT004, interfaceName, method.Name, method.ReturnType.ToDisplayString()));
            return;
        }

        // 截断样本由解析器记录、随条目流转，最终由生成器聚合为**单条** MUDFT009
        // （AT-B14：逐处上报会被 SDK 中大量深层 DTO 淹没）。
        var resolver = new TypeSchemaResolver(compilation);
        outputSchema = resolver.ResolveOutputSchema(payload);
        truncations = resolver.Truncations;
    }

    private static void ValidateUploadParameters(
        string interfaceName,
        IMethodSymbol method,
        List<PendingDiagnostic> diagnostics)
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
                // MUDFT008 上报点：上传参数声称为文件但类型无法映射为 format:binary。
                diagnostics.Add(PendingDiagnostic.Create(
                    Diagnostics.MUDFT008, interfaceName, method.Name, parameter.Name, typeName));
            }
        }
    }

    private static void ValidateParameterExpansion(
        string interfaceName,
        IReadOnlyList<CapabilityParameter> parameters,
        List<PendingDiagnostic> diagnostics)
    {
        foreach (var parameter in parameters)
        {
            if (parameter.SchemaFragmentJson != "{\"type\":\"string\"}")
            {
                continue;
            }

            // 复合参数被降级为字符串 = 模型无法得知可传字段（MUDFT010 上报点）。
            if (parameter.CsharpType.IndexOf('<') < 0
                && !IsPrimitiveName(parameter.CsharpType)
                && char.IsUpper(parameter.CsharpType.TrimEnd('?')[0]))
            {
                diagnostics.Add(PendingDiagnostic.Create(
                    Diagnostics.MUDFT010, interfaceName, "", parameter.Name, parameter.CsharpType));
            }
        }
    }

    // ────────── 辅助 ──────────

    private static ToolIdentity DeriveIdentityFromSource(
        string sourceSimpleTypeName,
        string sourceTypeName,
        List<PendingDiagnostic> diagnostics,
        string toolName)
    {
        if (sourceSimpleTypeName.StartsWith("IFeishuTenant", StringComparison.Ordinal))
        {
            return ToolIdentity.Tenant;
        }

        if (sourceSimpleTypeName.StartsWith("IFeishuUser", StringComparison.Ordinal))
        {
            return ToolIdentity.User;
        }

        // MUDFT016 上报点：源指向无令牌的抽象基接口——该类接口不能从 DI 解析，执行链必然失败。
        diagnostics.Add(PendingDiagnostic.Create(Diagnostics.MUDFT016, toolName, "Tenant", sourceTypeName));
        return ToolIdentity.Both;
    }

    private static ToolRisk Max(ToolRisk a, ToolRisk b) => (ToolRisk)Math.Max((int)a, (int)b);

    private static string RiskToString(ToolRisk risk) => risk switch
    {
        ToolRisk.Read => "read",
        ToolRisk.Write => "write",
        ToolRisk.HighRiskWrite => "high-risk-write",
        _ => "read",
    };

    private static string DeriveModule(string toolName)
    {
        var separatorIndex = toolName.IndexOfAny(NameSeparator);
        return separatorIndex > 0 ? toolName.Substring(0, separatorIndex) : toolName;
    }

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

    private static IReadOnlyList<string> GetNamedArray(AttributeData attribute, string key)
    {
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
