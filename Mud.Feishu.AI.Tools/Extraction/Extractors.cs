// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Mud.Feishu.AI.Tools.Extraction;

/// <summary>
/// L1 抽取器：从 Roslyn <see cref="INamedTypeSymbol"/>（接口）提取方法级 <see cref="CapabilityEntry"/>。
/// </summary>
/// <remarks>
/// 两条扫描路径：
/// <para>① 手写 <c>[FeishuTool]</c> 接口（Tier C 复合工具）——保留既有行为</para>
/// <para>② SDK 接口（<c>IFeishu[Tenant|User]V*</c>）——自动派生 Tier R 工具</para>
/// </remarks>
internal static class Extractors
{
    // ────────── 工具名推导 ──────────

    /// <summary>
    /// 从手写 [FeishuTool] 特性取工具名（既有行为，保持兼容）。
    /// </summary>
    public static string? TryGetToolNameFromAttribute(INamedTypeSymbol symbol)
    {
        var attr = symbol.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass is not null
            && a.AttributeClass.Name == "FeishuToolAttribute"
            && a.AttributeClass.ContainingNamespace.ToDisplayString() == "Mud.Feishu.AI.Tools");
        if (attr is null) return null;

        return attr.ConstructorArguments.FirstOrDefault().Value as string;
    }

    /// <summary>
    /// 从 SDK 接口名推导工具名（模块.资源.动作）。
    /// </summary>
    /// <param name="interfaceName">接口名（如 IFeishuTenantV1HireJob）。</param>
    /// <param name="moduleName">模块名（如 Hire）。</param>
    /// <returns>工具名（如 hire.job）或 null（不符合范式）。</returns>
    public static string? TryDeriveToolNameFromSdkInterface(string interfaceName, string moduleName)
    {
        // IFeishuTenantV1HireJob → 模块 Hire → 工具名 hire.job
        // IFeishuUserV2CalendarEvent → 模块 Calendar → 工具名 calendar.event
        // IFeishuTenantV1BitableAppTable → 模块 Bitable → 工具名 bitable.app_table
        var match = SdkInterfaceNameRegex.Match(interfaceName);
        if (!match.Success) return null;

        var domain = match.Groups["domain"].Value;
        var resource = match.Groups["resource"].Value;

        // 模块名转小写作为前缀
        var modulePrefix = ToSnakeCase(moduleName);

        // domain+resource 转下划线
        var resourcePart = string.IsNullOrEmpty(resource)
            ? ToSnakeCase(domain)
            : ToSnakeCase(domain) + "." + ToSnakeCase(resource);

        return $"{modulePrefix}.{resourcePart}";
    }

    /// <summary>
    /// 从方法名推导工具动作（如 ListJobsAsync → list_jobs）。
    /// </summary>
    public static string DeriveActionFromMethodName(string methodName)
    {
        // 去掉 Async 后缀
        var name = methodName;
        if (name.EndsWith("Async"))
        {
            name = name.Substring(0, name.Length - 5);
        }

        return ToSnakeCase(name);
    }

    // ────────── 身份推导 ──────────

    /// <summary>
    /// 从接口名令牌词推导身份维度。
    /// </summary>
    public static ToolIdentity DeriveIdentity(string interfaceName)
    {
        if (interfaceName.StartsWith("IFeishuTenant"))
            return ToolIdentity.Tenant;
        if (interfaceName.StartsWith("IFeishuUser"))
            return ToolIdentity.User;
        if (interfaceName.StartsWith("IFeishuV"))
            return ToolIdentity.Both; // 基接口，不直接产工具
        return ToolIdentity.Tenant; // 默认
    }

    // ────────── 模块名推导 ──────────

    /// <summary>
    /// 从接口所在目录路径推导模块名。
    /// </summary>
    public static string DeriveModuleName(INamedTypeSymbol symbol)
    {
        // 从命名空间取模块名：Mud.Feishu.IFaces.Contact → Contact
        // 或从 ContainingNamespace 的最末段取
        var ns = symbol.ContainingNamespace.ToDisplayString();
        var parts = ns.Split('.');
        // Mud.Feishu.<Module> → 取第三个段
        if (parts.Length >= 3 && parts[0] == "Mud" && parts[1] == "Feishu")
        {
            return parts[2];
        }

        return symbol.ContainingNamespace.Name;
    }

    // ────────── 风险分级 ──────────

    /// <summary>
    /// 根据 HTTP 方法和方法名/路径推导风险分级。
    /// </summary>
    public static ToolRisk DeriveRisk(string httpMethod, string methodName, string routeTemplate)
    {
        var verb = httpMethod.ToUpperInvariant();

        // GET → read
        if (verb == "GET")
            return ToolRisk.Read;

        // 检查危险词表
        var combined = (methodName + " " + routeTemplate).ToLowerInvariant();
        foreach (var dangerWord in DangerWords)
        {
            if (combined.Contains(dangerWord))
                return ToolRisk.HighRiskWrite;
        }

        // 非 GET 且未命中危险词 → write
        return ToolRisk.Write;
    }

    // ────────── XML 文档注释 ──────────

    /// <summary>
    /// 获取方法的 XML summary 文档注释。
    /// </summary>
    public static string? GetDocSummary(IMethodSymbol method)
    {
        var xml = method.GetDocumentationCommentXml();
        if (string.IsNullOrWhiteSpace(xml)) return null;

        return ExtractXmlTag(xml!, "summary");
    }

    /// <summary>
    /// 获取方法的 XML returns 文档注释。
    /// </summary>
    public static string? GetDocReturns(IMethodSymbol method)
    {
        var xml = method.GetDocumentationCommentXml();
        if (string.IsNullOrWhiteSpace(xml)) return null;

        return ExtractXmlTag(xml!, "returns");
    }

    /// <summary>
    /// 获取参数的 XML param 文档注释。
    /// </summary>
    public static string? GetParamDoc(IMethodSymbol method, string paramName)
    {
        var xml = method.GetDocumentationCommentXml();
        if (string.IsNullOrWhiteSpace(xml)) return null;

        // 简易提取：<param name="paramName">描述</param>
        var pattern = $"<param name=\"{paramName}\">(.*?)</param>";
        var match = Regex.Match(xml, pattern, RegexOptions.Singleline);
        if (!match.Success) return null;

        return NormalizeWhitespace(match.Groups[1].Value);
    }

    // ────────── HTTP 方法与路由 ──────────

    /// <summary>
    /// 从方法特性提取 HTTP 方法和路由模板。
    /// </summary>
    public static (string HttpMethod, string RouteTemplate) ExtractHttpInfo(IMethodSymbol method)
    {
        foreach (var attr in method.GetAttributes())
        {
            var attrName = attr.AttributeClass?.Name;
            switch (attrName)
            {
                case "GetAttribute":
                    return ("GET", GetRouteFromAttribute(attr));
                case "PostAttribute":
                    return ("POST", GetRouteFromAttribute(attr));
                case "PutAttribute":
                    return ("PUT", GetRouteFromAttribute(attr));
                case "PatchAttribute":
                    return ("PATCH", GetRouteFromAttribute(attr));
                case "DeleteAttribute":
                    return ("DELETE", GetRouteFromAttribute(attr));
            }
        }

        return ("GET", string.Empty);
    }

    // ────────── 返回类型元数据 ──────────

    /// <summary>
    /// 提取方法返回类型的元数据名（如 FeishuApiResult<JobListResult>）。
    /// </summary>
    public static string ExtractReturnTypeMetadata(IMethodSymbol method)
    {
        return method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    /// <summary>
    /// 解包返回类型：Task<T> → T 的 DisplayString。
    /// </summary>
    public static ITypeSymbol? UnwrapTaskType(ITypeSymbol returnType)
    {
        // Task<T> → T
        if (returnType is INamedTypeSymbol named
            && named.IsGenericType
            && named.OriginalDefinition.ToDisplayString() == "System.Threading.Tasks.Task<T>")
        {
            return named.TypeArguments[0];
        }

        // Task (non-generic) → null
        if (returnType is INamedTypeSymbol taskNamed
            && !taskNamed.IsGenericType
            && taskNamed.ToDisplayString() == "System.Threading.Tasks.Task")
        {
            return null;
        }

        // ValueTask<T> → T
        if (returnType is INamedTypeSymbol valueTaskNamed
            && valueTaskNamed.OriginalDefinition.Name == "ValueTask"
            && valueTaskNamed.TypeArguments.Length == 1)
        {
            return valueTaskNamed.TypeArguments[0];
        }

        return null;
    }

    // ────────── 文件上传/下载检测 ──────────

    /// <summary>
    /// 检测方法是否有文件上传参数。
    /// </summary>
    public static bool HasFileUpload(IMethodSymbol method)
    {
        foreach (var param in method.Parameters)
        {
            if (param.GetAttributes().Any(a => a.AttributeClass?.Name == "FormContentAttribute"))
                return true;

            var typeName = param.Type.ToDisplayString();
            if (typeName == "byte[]" || typeName == "System.Byte[]" || typeName == "System.IO.Stream")
                return true;
        }

        return false;
    }

    /// <summary>
    /// 检测方法是否返回二进制（Task<byte[]?>）。
    /// </summary>
    public static bool ReturnsBinary(IMethodSymbol method)
    {
        var unwrapped = UnwrapTaskType(method.ReturnType);
        if (unwrapped is null) return false;

        var typeName = unwrapped.ToDisplayString();
        return typeName == "byte[]" || typeName == "byte[]?" || typeName == "System.Byte[]" || typeName == "System.Byte[]?";
    }

    // ────────── 私有工具方法 ──────────

    private static readonly Regex SdkInterfaceNameRegex = new(
        @"^IFeishu(?:Tenant|User)?V\d+(?<domain>[A-Z][a-zA-Z0-9]*)(?<resource>[A-Z][a-zA-Z0-9]*)?$",
        RegexOptions.Compiled);

    private static readonly string[] DangerWords =
    [
        "delete", "remove", "batch_delete", "transfer",
        "cancel", "revoke", "resign",
        "permission", "secret", "password", "token",
        "dismiss", "purge", "wipe"
    ];

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        var sb = new StringBuilder(input.Length + 4);
        sb.Append(char.ToLowerInvariant(input[0]));

        for (var i = 1; i < input.Length; i++)
        {
            var c = input[i];
            if (char.IsUpper(c))
            {
                // 检查前一个字符是否也是大写（避免在连续大写中插入下划线）
                if (i > 0 && !char.IsUpper(input[i - 1]) && input[i - 1] != '_')
                {
                    sb.Append('_');
                }
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private static string GetRouteFromAttribute(AttributeData attr)
    {
        if (attr.ConstructorArguments.Length >= 1
            && attr.ConstructorArguments[0].Value is string route)
        {
            return route;
        }

        return string.Empty;
    }

    private static string? ExtractXmlTag(string xml, string tagName)
    {
        var pattern = $"<{tagName}>(.*?)</{tagName}>";
        var match = Regex.Match(xml, pattern, RegexOptions.Singleline);
        if (!match.Success) return null;

        return NormalizeWhitespace(match.Groups[1].Value);
    }

    private static string NormalizeWhitespace(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        // 去除 XML 标记内部的多余空白
        var trimmed = input.Trim();
        // 折叠连续空白
        var sb = new StringBuilder(trimmed.Length);
        var lastWasSpace = false;
        foreach (var c in trimmed)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace)
                {
                    sb.Append(' ');
                    lastWasSpace = true;
                }
            }
            else
            {
                sb.Append(c);
                lastWasSpace = false;
            }
        }

        return sb.ToString();
    }
}
