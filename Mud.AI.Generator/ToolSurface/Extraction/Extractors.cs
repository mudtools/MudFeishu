// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Mud.AI.ToolSurface.Extraction;

/// <summary>
/// L1 抽取器：从 Roslyn <see cref="INamedTypeSymbol"/>（接口）提取方法级 <see cref="CapabilityEntry"/>
/// 所需的平台无关事实。
/// </summary>
/// <remarks>
/// <para>
/// 移植自上游 <c>Mud.Feishu.AI.Tools.Extraction.Extractors</c>，泛化点（设计文档 §2 剖面判定）：
/// <list type="bullet">
/// <item>工具特性识别：硬编码 <c>"AgentToolAttribute"</c> + <c>"Mud.Feishu.AI.Tools"</c>
/// → 剖面槽 <see cref="SdkToolProfileModel.ToolAttributeName"/> /
/// <see cref="SdkToolProfileModel.ToolAttributeNamespace"/>（含 <c>Attribute</c> 后缀剥离双形态匹配）；</item>
/// <item>源符号解析（<c>"Mud.Feishu." + typeName</c>）→ 拆至 <see cref="SourceResolver"/>
/// （经 <see cref="SdkToolProfileModel.SdkNamespaceRoot"/>）；</item>
/// <item>接口命名正则（<c>^IFeishu…$</c>）→ 拆至 <see cref="InterfaceIdentityParser"/>
/// （经 <see cref="SdkToolProfileModel.InterfaceNameRegex"/> + 令牌标记表）；</item>
/// <item>危险词表（<c>DangerWords</c>）→ 剖面槽 <see cref="SdkToolProfileModel.WriteVerbKeywords"/>。</item>
/// </list>
/// </para>
/// <para>
/// <b>两条扫描路径的真实分工（上游 AT-B18 结论保留）</b>：
/// ① 手写工具特性接口——<b>唯一</b>的"产工具"路径（经 <see cref="ToolSurfaceScanner"/>）；
/// 工具名/描述/scope 由人策展，其余事实（HTTP 路由、风险、返回形状）从 SDK 符号派生。
/// ② SDK 接口——<b>仅</b>聚合为能力目录事实，<b>不产任何工具</b>：执行器承载的是<b>有意的策展</b>
/// （哪些字段回填模型、JSON 键怎么命名、错误如何归类），生成器无从得知。
/// </para>
/// </remarks>
internal static class Extractors
{
    // ────────── 工具名推导 ──────────

    /// <summary>
    /// 取工具特性数据（唯一识别点：按「简单名 + 命名空间」双重判定，防同名特性误命中）。
    /// </summary>
    /// <remarks>
    /// 简单名匹配<b>双形态</b>：<c>{ToolAttributeName}</c>（C# 源码书写名）与
    /// <c>{ToolAttributeName}Attribute</c>（特性类 CLR 名）——消费方既可写
    /// <c>[AgentTool]</c> 也可写 <c>[AgentToolAttribute]</c>，两种形态都必须命中
    /// （上游只认 <c>AgentToolAttribute</c> 一种，泛化后按 <c>Attribute</c> 后缀剥离补齐双形态）。
    /// </remarks>
    public static AttributeData? GetToolAttribute(INamedTypeSymbol symbol, SdkToolProfileModel profile)
    {
        try
        {
            var bare = profile.ToolAttributeName;
            if (bare.Length == 0)
            {
                return null;
            }

            var full = bare + "Attribute";
            foreach (var attribute in symbol.GetAttributes())
            {
                var attributeClass = attribute.AttributeClass;
                if (attributeClass is not null
                    && (attributeClass.Name == bare || attributeClass.Name == full)
                    && string.Equals(attributeClass.ContainingNamespace?.ToDisplayString(), profile.ToolAttributeNamespace, StringComparison.Ordinal))
                {
                    return attribute;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            GeneratorDebugLogger.LogError(nameof(Extractors), ex);
            return null;
        }
    }

    /// <summary>从手写工具特性取工具名（特性构造参数 0）。</summary>
    public static string? TryGetToolNameFromAttribute(INamedTypeSymbol symbol, SdkToolProfileModel profile)
        => GetToolAttribute(symbol, profile)?.ConstructorArguments.FirstOrDefault().Value as string;

    // ────────── 风险分级 ──────────

    /// <summary>
    /// 从 SDK 事实推导风险分级（危险词命中 → <c>high-risk-write</c>；变更动词 → <c>write</c>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 危险词表来自剖面槽 <see cref="SdkToolProfileModel.WriteVerbKeywords"/>（设计文档 §4.2 第 7 项）。
    /// </para>
    /// <para>
    /// <b>只匹配方法名，不匹配路由</b>（上游结论保留）：SDK 路由里普遍带 <c>{app_token}</c> 这类
    /// 占位段，而危险词表含 <c>token</c>/<c>secret</c>——把路由纳入匹配会让大量只读工具被误判为
    /// <c>high-risk-write</c>（接线后实测 100% 误报）。
    /// </para>
    /// <para>
    /// <b>POST 不视为写</b>（上游结论保留）：只读批量/查询 API 大量使用 POST，按动词判"写"会把
    /// 只读工具误判为写面。真正的写面由工具特性的 <c>IsWrite</c> 声明，本方法只负责<b>升级</b>风险。
    /// </para>
    /// </remarks>
    /// <param name="httpMethod">HTTP 方法（GET/POST/PUT/PATCH/DELETE）。</param>
    /// <param name="methodName">SDK 方法名。</param>
    /// <param name="profile">当前剖面（提供危险词表）。</param>
    /// <returns>推导出的风险分级（<see cref="ToolSurfaceRisk.Read"/> 表示"无升级信号"）。</returns>
    public static ToolSurfaceRisk DeriveRisk(string httpMethod, string methodName, SdkToolProfileModel profile)
    {
        try
        {
            var snake = ToSnakeCase(methodName);
            foreach (var word in profile.WriteVerbKeywords)
            {
                if (word.Length > 0 && snake.IndexOf(word, StringComparison.Ordinal) >= 0)
                {
                    return ToolSurfaceRisk.HighRiskWrite;
                }
            }
        }
        catch (Exception ex)
        {
            GeneratorDebugLogger.LogError(nameof(Extractors), ex);
        }

        return httpMethod.ToUpperInvariant() is "PUT" or "PATCH" or "DELETE"
            ? ToolSurfaceRisk.Write
            : ToolSurfaceRisk.Read;
    }

    // ────────── XML 文档注释 ──────────

    /// <summary>获取方法的 XML summary 文档注释。</summary>
    public static string? GetDocSummary(IMethodSymbol method)
    {
        try
        {
            var xml = GetDocXmlWithSyntaxFallback(method);
            if (string.IsNullOrWhiteSpace(xml)) return null;

            return ExtractXmlTag(xml!, "summary");
        }
        catch (Exception ex)
        {
            GeneratorDebugLogger.LogError(nameof(Extractors), ex);
            return null;
        }
    }

    /// <summary>获取参数的 XML param 文档注释。</summary>
    public static string? GetParamDoc(IMethodSymbol method, string paramName)
    {
        try
        {
            var xml = GetDocXmlWithSyntaxFallback(method);
            if (string.IsNullOrWhiteSpace(xml)) return null;

            // 简易提取：<param name="paramName">描述</param>
            var pattern = $"<param name=\"{Regex.Escape(paramName)}\">(.*?)</param>";
            var match = Regex.Match(xml, pattern, RegexOptions.Singleline);
            if (!match.Success) return null;

            return NormalizeWhitespace(match.Groups[1].Value);
        }
        catch (Exception ex)
        {
            GeneratorDebugLogger.LogError(nameof(Extractors), ex);
            return null;
        }
    }

    /// <summary>
    /// 取方法的文档注释文本：<b>语义途径优先</b>（<see cref="ISymbol.GetDocumentationCommentXml"/>，
    /// 支持 <c>inheritdoc</c> 展开与正式 XML 转义），语义结果为空时<b>回退语法树</b>。
    /// </summary>
    /// <remarks>
    /// <b>为什么需要回退</b>：csc 在未开启 <c>/doc</c>（消费方 <c>GenerateDocumentationFile=false</c>，
    /// 如本仓库 Demos）时把 <c>CSharpCompilationOptions.DocumentationMode</c> 置为
    /// <c>DocumentationMode.None</c>——语义模型不解析文档注释，<c>GetDocumentationCommentXml()</c>
    /// 恒返回空。而文档注释作为普通注释 trivia 仍完整保留在语法树里，不回退会让
    /// 槽位 005（summary 缺失）/006（param 缺失）在此类消费方编译中全部误报。
    /// 回退产物是<b>伪 XML 文本</b>（原始注释体），上层的标签提取本就是正则匹配，两途共用。
    /// </remarks>
    private static string? GetDocXmlWithSyntaxFallback(IMethodSymbol method)
    {
        var xml = method.GetDocumentationCommentXml();
        if (!string.IsNullOrWhiteSpace(xml))
        {
            return xml;
        }

        foreach (var reference in method.DeclaringSyntaxReferences)
        {
            var text = ExtractRawDocCommentText(reference.GetSyntax());
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return null;
    }

    /// <summary>从声明节点的先行 trivia 收集 <c>///</c> 单行与 <c>/** */</c> 块状文档注释（逐行拼为伪 XML 文本）。</summary>
    /// <remarks>
    /// 文档注释无论 <see cref="DocumentationMode"/> 如何都附着在声明<b>首 token</b> 的先行 trivia 上
    /// （注释与方法之间隔着特性列表时同样成立——trivia 附着于 <c>[</c>，它是节点首 token）。
    /// </remarks>
    private static string? ExtractRawDocCommentText(SyntaxNode node)
    {
        var lines = new List<string>();
        foreach (var trivia in node.GetLeadingTrivia())
        {
            var text = trivia.ToString();
            if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                && text.StartsWith("///", StringComparison.Ordinal))
            {
                lines.Add(text.Length > 3 && text[3] == ' ' ? text.Substring(4) : text.Substring(3));
            }
            else if (trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
                && text.StartsWith("/**", StringComparison.Ordinal)
                && text.EndsWith("*/", StringComparison.Ordinal))
            {
                lines.Add(text.Substring(3, text.Length - 5).Trim());
            }
        }

        return lines.Count == 0 ? null : string.Join("\n", lines);
    }

    // ────────── HTTP 方法与路由 ──────────

    /// <summary>
    /// 从方法特性提取 HTTP 方法和路由模板（Mud.AI 声明式 HTTP 特性族，平台无关机制）。
    /// </summary>
    public static (string HttpMethod, string RouteTemplate) ExtractHttpInfo(IMethodSymbol method)
    {
        try
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
        }
        catch (Exception ex)
        {
            GeneratorDebugLogger.LogError(nameof(Extractors), ex);
        }

        return ("GET", string.Empty);
    }

    // ────────── 返回类型元数据 ──────────

    /// <summary>
    /// 解包返回类型：<c>Task&lt;T&gt;</c> / <c>ValueTask&lt;T&gt;</c> → 载荷类型 T。
    /// </summary>
    /// <remarks>
    /// <b>按「名称 + 元数 + 命名空间」判定，不比对 DisplayString</b>（上游教训保留）：
    /// BCL 的泛型参数名是 <c>TResult</c>（<c>System.Threading.Tasks.Task&lt;TResult&gt;</c>），任何
    /// <c>== "…Task&lt;T&gt;"</c> 的字面比对恒为 false——属"死代码期从未暴露的潜在缺陷"。
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

    /// <summary>检测方法是否有文件上传参数。</summary>
    public static bool HasFileUpload(IMethodSymbol method)
    {
        try
        {
            foreach (var param in method.Parameters)
            {
                if (param.GetAttributes().Any(static a => a.AttributeClass?.Name == "FormContentAttribute"))
                    return true;

                var typeName = param.Type.ToDisplayString();
                if (typeName == "byte[]" || typeName == "System.Byte[]" || typeName == "System.IO.Stream")
                    return true;
            }
        }
        catch (Exception ex)
        {
            GeneratorDebugLogger.LogError(nameof(Extractors), ex);
        }

        return false;
    }

    // ────────── 私有工具方法 ──────────

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