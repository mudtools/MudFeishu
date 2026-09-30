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
/// <para>
/// <b>两条扫描路径的真实分工（AT-B18 修正——原注释声称路径②"自动派生 Tier R 工具"，与实现不符）</b>：
/// </para>
/// <para>
/// ① <b>手写 <c>[FeishuTool]</c> 接口</b>——<b>唯一</b>的"产工具"路径（经
/// <see cref="CuratedToolScanner"/>）；工具名/描述/scope 由人策展，其余事实（HTTP 路由、风险、返回形状）
/// 从 SDK 符号派生。
/// </para>
/// <para>
/// ② <b>SDK 接口（<c>IFeishu[Tenant|User]V*</c>）</b>——<b>仅</b>聚合为能力目录事实
/// （<c>CapabilityCatalogEmitter</c>：方法总数 / 分组分布），<b>不产任何工具</b>。
/// </para>
/// <para>
/// <b>为什么路径②不产工具（请勿按"能力已存在"的错觉重复建设）</b>：执行器承载的是<b>有意的策展</b>
/// ——哪些字段回填模型、JSON 键怎么命名、错误如何归类——这恰恰是护城河（白名单投影），生成器无从得知。
/// 逐方法发射 1155 条工具还会把编译期成本与程序集体积推高一个量级。该结论与上游主方案的
/// <c>AT-F01</c> 撤销决定一致（见本方案 §0.2 定性判断①、§2.2 纠偏表与 §4 D1）。
/// </para>
/// </remarks>
internal static class Extractors
{
    // ────────── 工具名推导 ──────────

    /// <summary>
    /// 取 <c>[FeishuTool]</c> 特性数据（唯一识别点：按「简单名 + 命名空间」双重判定，
    /// 防同名特性误命中）。
    /// </summary>
    public static AttributeData? GetFeishuToolAttribute(INamedTypeSymbol symbol)
        => symbol.GetAttributes().FirstOrDefault(static a =>
            a.AttributeClass is not null
            && a.AttributeClass.Name == "FeishuToolAttribute"
            && a.AttributeClass.ContainingNamespace.ToDisplayString() == "Mud.Feishu.AI.Tools");

    /// <summary>
    /// 从手写 [FeishuTool] 特性取工具名（既有行为，保持兼容）。
    /// </summary>
    public static string? TryGetToolNameFromAttribute(INamedTypeSymbol symbol)
        => GetFeishuToolAttribute(symbol)?.ConstructorArguments.FirstOrDefault().Value as string;

    /// <summary>
    /// 按「接口名.方法名」解析 SDK 成员（源挂钩 <c>[FeishuTool(Source=...)]</c> 的解析器）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 方法查找<b>必须</b>包含 <see cref="INamedTypeSymbol.AllInterfaces"/>：本仓库的令牌派生接口
    /// （<c>IFeishuTenantV*</c> / <c>IFeishuUserV*</c>）是<b>空</b>接口，全部方法声明在基接口
    /// （<c>IFeishuV*</c>）上——只查 <c>GetMembers(name)</c> 会恒返回空（这是本方案评审纠正的
    /// 一处设计硬伤：按"扫描派生接口"实现 Tier R 会产出空条目）。
    /// </para>
    /// <para>重载选择：取参数最多者（canonical 形态）。</para>
    /// </remarks>
    /// <param name="compilation">当前编译。</param>
    /// <param name="typeName">接口名（如 <c>IFeishuTenantV3User</c>）。</param>
    /// <param name="methodName">方法名（如 <c>GetBatchUsersAsync</c>）。</param>
    /// <param name="failure">失败原因（成功时为 <see langword="null"/>）。</param>
    /// <returns>解析到的类型与方法；任一环节失败返回 <see langword="null"/>。</returns>
    public static (INamedTypeSymbol Type, IMethodSymbol Method)? ResolveSourceMember(
        Compilation compilation,
        string typeName,
        string methodName,
        out string? failure)
    {
        failure = null;

        var type = compilation.GetTypeByMetadataName("Mud.Feishu." + typeName)
                   ?? FindTypeByName(compilation.GlobalNamespace, typeName);
        if (type is null)
        {
            failure = $"未找到 SDK 接口 {typeName}";
            return null;
        }

        var method = type.GetMembers(methodName).OfType<IMethodSymbol>()
            .Concat(type.AllInterfaces.SelectMany(i => i.GetMembers(methodName).OfType<IMethodSymbol>()))
            .Where(static m => m.MethodKind == MethodKind.Ordinary)
            .OrderByDescending(static m => m.Parameters.Length)
            .FirstOrDefault();
        if (method is null)
        {
            failure = $"接口 {typeName} 上未找到方法 {methodName}（已含全部基接口）";
            return null;
        }

        return (type, method);
    }

    private static INamedTypeSymbol? FindTypeByName(INamespaceSymbol ns, string typeName)
    {
        foreach (var type in ns.GetTypeMembers(typeName))
        {
            return type;
        }

        foreach (var child in ns.GetNamespaceMembers())
        {
            var found = FindTypeByName(child, typeName);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// 解析 SDK 接口名的令牌结构（<c>IFeishu[Tenant|User]V{n}{Domain}{Resource}</c>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么不从命名空间推导模块名</b>：本仓库 SDK 接口全部落在命名空间 <c>Mud.Feishu</c>
    /// （模块信息只存在于 <c>Interfaces/{Module}/</c> 目录结构里，Roslyn 符号不可见）——
    /// 从命名空间取"第三段"会得到 <c>Feishu</c>/<c>Interfaces</c> 之类错误结果。
    /// 域名（Domain）是符号层唯一可用的能力轴，故工具名与模块统计一律取 Domain。
    /// </para>
    /// </remarks>
    /// <param name="interfaceName">接口名（如 <c>IFeishuTenantV1BitableAppTable</c>）。</param>
    /// <param name="identity">令牌身份维度。</param>
    /// <param name="domain">域名段（如 <c>Bitable</c>）。</param>
    /// <param name="resource">资源段（可为空，如 <c>V3User</c> 只有 Domain）。</param>
    /// <returns>是否符合 SDK 命名范式。</returns>
    public static bool TryParseSdkInterfaceName(
        string interfaceName,
        out ToolIdentity identity,
        out string domain,
        out string resource)
    {
        identity = ToolIdentity.Both;
        domain = string.Empty;
        resource = string.Empty;

        var match = SdkInterfaceNameRegex.Match(interfaceName);
        if (!match.Success)
        {
            return false;
        }

        identity = interfaceName.StartsWith("IFeishuTenant", StringComparison.Ordinal)
            ? ToolIdentity.Tenant
            : interfaceName.StartsWith("IFeishuUser", StringComparison.Ordinal)
                ? ToolIdentity.User
                : ToolIdentity.Both;

        domain = match.Groups["domain"].Value;
        resource = match.Groups["resource"].Value;
        return true;
    }

    // ────────── 风险分级 ──────────

    /// <summary>
    /// 从 SDK 事实推导风险分级（危险词命中 → <c>high-risk-write</c>；变更动词 → <c>write</c>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>只匹配方法名，不匹配路由</b>：飞书路由里普遍带 <c>{app_token}</c>/<c>{table_id}</c> 这类
    /// 占位段，而危险词表含 <c>token</c>/<c>secret</c>——把路由纳入匹配会让几乎全部
    /// Bitable 工具被误判为 <c>high-risk-write</c>（接线后实测 100% 误报）。
    /// </para>
    /// <para>
    /// <b>POST 不视为写</b>：飞书的只读批量/查询 API 大量使用 POST
    /// （<c>batch_get_id</c>/<c>records/search</c>/<c>doc_wiki/search</c>），
    /// 按动词判"写"会把只读工具误判为写面。真正的写面由 <c>[FeishuTool(IsWrite=...)]</c> 声明，
    /// 本方法只负责<b>升级</b>风险（破坏性动词 / 危险词）。
    /// </para>
    /// </remarks>
    /// <param name="httpMethod">HTTP 方法（GET/POST/PUT/PATCH/DELETE）。</param>
    /// <param name="methodName">SDK 方法名。</param>
    /// <returns>推导出的风险分级（<see cref="ToolRisk.Read"/> 表示"无升级信号"）。</returns>
    public static ToolRisk DeriveRisk(string httpMethod, string methodName)
    {
        if (DangerWords.Any(word => ToSnakeCase(methodName).Contains(word)))
        {
            return ToolRisk.HighRiskWrite;
        }

        return httpMethod.ToUpperInvariant() is "PUT" or "PATCH" or "DELETE"
            ? ToolRisk.Write
            : ToolRisk.Read;
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
    /// 获取参数的 XML param 文档注释。
    /// </summary>
    /// <remarks>
    /// R2-10 死代码清理：此处原有 <c>GetDocReturns</c>（提取 <c>&lt;returns&gt;</c>）——
    /// 它<b>从未被调用</b>（调用方恒传 <c>docReturns: null</c>），连同 <c>CapabilityEntry.DocReturns</c>
    /// 字段与 <c>CuratedToolScanner</c> 的实参一并删除。返回值文档若要接线，需先确认
    /// 「53 个 SDK 方法的 <c>&lt;returns&gt;</c> 覆盖度」这一前提（当前 XML 注释大多为空，
    /// 接线会产生一批空描述），故按"删掉而不是留着一个永远为 null 的通道"处置。
    /// </remarks>
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

    // R3-09：已删除 ExtractReturnTypeMetadata——随 ReturnTypeMetadataName 字段一同清理。

    /// <summary>
    /// 解包返回类型：<c>Task&lt;T&gt;</c> / <c>ValueTask&lt;T&gt;</c> → 载荷类型 T。
    /// </summary>
    /// <remarks>
    /// <b>按「名称 + 元数 + 命名空间」判定，不比对 DisplayString</b>：BCL 的泛型参数名是
    /// <c>TResult</c>（<c>System.Threading.Tasks.Task&lt;TResult&gt;</c>），任何
    /// <c>== "…Task&lt;T&gt;"</c> 的字面比对恒为 false——本方法此前正是这样写的，
    /// 属"死代码期从未暴露的潜在缺陷"（接线后立刻表现为 100% 工具报 MUDFT004）。
    /// </remarks>
    public static ITypeSymbol? UnwrapTaskType(ITypeSymbol returnType)
    {
        if (returnType is not INamedTypeSymbol named
            || named.ContainingNamespace?.ToDisplayString() != "System.Threading.Tasks")
        {
            return null;
        }

        // Task<T> / ValueTask<T> → T
        if (named.Arity == 1 && named.Name is "Task" or "ValueTask")
        {
            return named.TypeArguments[0];
        }

        // Task / ValueTask（非泛型）→ 无载荷
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
        "delete", "remove", "transfer",
        "cancel", "revoke", "resign",
        "permission", "reset_secret", "password",
        "dismiss", "purge", "wipe"
    ];

    /// <summary>
    /// PascalCase → snake_case（危险词匹配与工具名派生的公共拼写规则）。
    /// </summary>
    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(input.Length + 8);
        for (var i = 0; i < input.Length; i++)
        {
            var ch = input[i];
            if (char.IsUpper(ch))
            {
                if (i > 0 && (!char.IsUpper(input[i - 1]) || (i + 1 < input.Length && char.IsLower(input[i + 1]))))
                {
                    sb.Append('_');
                }

                sb.Append(char.ToLowerInvariant(ch));
            }
            else
            {
                sb.Append(ch);
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
