// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

using Mud.Feishu.AI.Tools;
using Mud.Feishu.AI.Tools.Generated;

namespace Mud.Feishu.AI.Mcp;

/// <summary>
/// <b>M-2：MCP 资源目录</b>——把域 guidance / per-tool affordance / 域 scope / 错误契约
/// 以 <b>MCP <c>resources</c> 能力</b>暴露，使上下文占用从"全量下发"变为"按需读取"（渐进披露）。
/// </summary>
/// <remarks>
/// <para>
/// <b>数据源全部是编译期常量或装配期事实</b>（与 <c>feishu.capability_lookup</c> 同纪律）：
/// <list type="bullet">
/// <item>L1 guidance：生成器发射的 <see cref="FeishuToolGuidance.ByDomain"/>；</item>
/// <item>L2 references：同上的 <see cref="FeishuToolGuidance.References"/>；</item>
/// <item>per-tool affordance：工具桥的<b>模型可见描述</b>（affordance 由执行链在构造期追加，故描述里就是"模型看到的那份"）；</item>
/// <item>域 scope 对照：<see cref="FeishuToolDefinition.RequiredScopes"/>（工具面事实）+ 风险/身份；</item>
/// <item>错误契约：<see cref="ErrorContractSummary"/>（编译期常量，由守卫与代码闭集机械对齐）。</item>
/// </list>
/// 无网络、无租户依赖——<c>resources/read</c> 因此在任何宿主状态下都不会失败。
/// </para>
/// <para>
/// <b>可见性纪律与 instructions 同源</b>：只列<b>已启用工具所属域</b>的资源
/// （白名单之外的域在 <c>resources/list</c> 中不可见）——资源面不得成为"绕过白名单的枚举通道"。
/// </para>
/// <para>
/// <b>为什么不复用 <c>guidance_read</c> 元工具</b>：那是<b>进程内</b> Agent 的读取通道
/// （工具调用需要执行链/授权/审计），而 MCP 客户端读资源不需要经过工具执行链——
/// 两者读取的是<b>同一份常量</b>（<c>FeishuToolGuidance</c>），只是入口形态不同（M-3 的"同源不同形态"）。
/// </para>
/// </remarks>
public sealed class FeishuMcpResourceCatalog
{
    /// <summary>L1/L2 guidance 资源 URI 方案（<c>feishu-guidance://{domain}[/{topic}]</c>）。</summary>
    public const string GuidanceScheme = "feishu-guidance";

    /// <summary>per-tool affordance 资源 URI 方案（<c>feishu-affordance://{tool}</c>）。</summary>
    public const string AffordanceScheme = "feishu-affordance";

    /// <summary>域 scope 对照资源 URI 方案（<c>feishu-scopes://{domain}</c>）。</summary>
    public const string ScopesScheme = "feishu-scopes";

    /// <summary>错误契约资源 URI（<c>feishu-errors://contract</c>）。</summary>
    public const string ErrorContractUri = "feishu-errors://contract";

    /// <summary>affordance 在模型可见描述中的块标题（与执行链追加的标题逐字一致）。</summary>
    private const string AffordanceBlockTitle = "【决策指导】";

    /// <summary>全部资源统一媒体类型（内容均为 Markdown 文本）。</summary>
    private const string ResourceMimeType = "text/markdown";

    /// <summary>
    /// 错误契约摘要（<b>编译期常量</b>；<c>feishu-errors://contract</c> 的内容）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么内联而不读文件</b>：MCP 服务是<b>进程外</b>形态（stdio 独占），
    /// 其工作目录由客户端决定——读仓库里的 <c>.docs/AI/工具错误契约.md</c> 在部署环境中必然找不到。
    /// 故这里放"消费侧最小集"（闭集 + 决策树），完整契约仍以仓库文档为准。
    /// </para>
    /// <para>
    /// <b>防漂移</b>：<c>McpResourceContractGuards</c> 解析本常量里的
    /// <c>## category 闭集</c> / <c>## subtype 闭集</c> 两节，与代码闭集（<c>ToolErrorCategory</c> /
    /// <c>ToolErrorSubtype</c>）机械比对——少写/多写一个字面量即红。
    /// </para>
    /// </remarks>
    public const string ErrorContractSummary =
        """
        # 工具错误契约（消费侧摘要）

        工具失败时，结果文本的**第一行**是一行紧凑 JSON（≤512 字节）：

        {"tool_error":{"category":"…","subtype":"…","retryable":true,"retry_after_seconds":3,"api_code":99991400,"attempts":2,"trace":"a1b2c3","tool":"…","suggestions":["…"],"required_scopes":["…"]}}

        第二行是 `[next]` 恢复提示（一行处置指引），其后是人类可读正文。
        `required_scopes` 是**工具声明的所需权限**（不是"平台缺什么"的结论）。

        ## category 闭集
        - validation
        - policy
        - authorization
        - confirmation
        - retryable
        - api
        - internal
        - content_safety

        ## subtype 闭集
        - invalid_args
        - missing_required
        - shape_mismatch
        - risk_exceeded
        - identity_mismatch
        - tool_not_allowed
        - domain_not_allowed
        - write_not_allowed
        - tool_budget_exceeded
        - authorization_denied
        - user_auth_required
        - user_token_refresh_failed
        - needs_user_confirmation
        - rate_limited
        - transient
        - timeout
        - upstream_error
        - unexpected
        - injected_content_blocked

        ## 消费者决策树
        - validation：按 suggestions 改参数后重试（不要原样重试）。
        - policy：宿主策略禁止（确定性失败）——放弃该工具或改用只读方案，不要重试。
        - authorization：宿主确认权限/授权器；user_auth_required 则转告用户完成授权。
        - confirmation：等待用户确认，不要自行重复发起。
        - retryable：按 retry_after_seconds 等待后重试同一调用。
        - api：命中码表按码表行动；未命中则核对参数与对象可见性，如实报告（附 api_code/trace）。
        - content_safety：放弃该结果，不要改写参数重试。
        - internal：如实报告用户并附追踪号，不要臆造结果。

        完整契约（字段表 / 生产者纪律 / 版本演进规则）见仓库文档 `.docs/AI/工具错误契约.md`。
        """;

    private readonly Dictionary<string, McpResourceInfo> _byUri;
    private readonly Dictionary<string, string> _content;

    private FeishuMcpResourceCatalog(
        Dictionary<string, McpResourceInfo> byUri,
        Dictionary<string, string> content)
    {
        _byUri = byUri;
        _content = content;
    }

    /// <summary>资源清单（<c>resources/list</c> 的内容；按 URI 序数排序，保证确定性）。</summary>
    public IReadOnlyList<McpResourceInfo> Resources { get; private init; } = [];

    /// <summary>
    /// 构建目录。
    /// </summary>
    /// <param name="enabledTools">已启用的工具定义（白名单应用后的注册表条目）。</param>
    /// <param name="toolDescriptions">
    /// 契约工具名 → 模型可见描述（含 affordance 决策块；由工具桥提供，与 <c>tools/list</c> 同源）。
    /// </param>
    /// <returns>资源目录（只含已启用域的资源）。</returns>
    public static FeishuMcpResourceCatalog Build(
        IReadOnlyList<FeishuToolDefinition> enabledTools,
        IReadOnlyDictionary<string, string> toolDescriptions)
    {
        if (enabledTools is null)
        {
            throw new ArgumentNullException(nameof(enabledTools));
        }

        if (toolDescriptions is null)
        {
            throw new ArgumentNullException(nameof(toolDescriptions));
        }

        var byUri = new Dictionary<string, McpResourceInfo>(StringComparer.Ordinal);
        var content = new Dictionary<string, string>(StringComparer.Ordinal);

        // 域集合（已启用工具所属域）与每域的工具集合：资源可见性以它为准。
        var toolsByDomain = new Dictionary<string, List<FeishuToolDefinition>>(StringComparer.Ordinal);
        foreach (var tool in enabledTools)
        {
            var domain = DomainOf(tool.Name);
            if (domain.Length == 0)
            {
                continue;
            }

            if (!toolsByDomain.TryGetValue(domain, out var list))
            {
                list = [];
                toolsByDomain[domain] = list;
            }

            list.Add(tool);
        }

        // 注：netstandard2.0 不支持 KeyValuePair 解构（netstandard2.1 起才有），故用 pair.Key/Value。
        foreach (var pair in toolsByDomain)
        {
            AddGuidance(pair.Key, byUri, content);
            AddScopes(pair.Key, pair.Value, byUri, content);
        }

        // per-tool affordance：只列**真的带决策块**的工具（没配资产的不产出空资源）。
        foreach (var tool in enabledTools)
        {
            if (!toolDescriptions.TryGetValue(tool.Name, out var description)
                || string.IsNullOrEmpty(description))
            {
                continue;
            }

            var block = ExtractAffordanceBlock(description);
            if (block is null)
            {
                continue;
            }

            var uri = $"{AffordanceScheme}://{tool.Name}";
            byUri[uri] = new McpResourceInfo(
                uri,
                $"{tool.Name} 决策指导",
                $"何时该用 / 何时不要用 / 前置条件 / 提示（{tool.Name} 的 per-tool affordance 全文）",
                ResourceMimeType,
                Utf8Length(block));
            content[uri] = block;
        }

        AddErrorContract(byUri, content);

        var resources = byUri.Values
            .OrderBy(static resource => resource.Uri, StringComparer.Ordinal)
            .ToArray();

        return new FeishuMcpResourceCatalog(byUri, content) { Resources = resources };
    }

    /// <summary>
    /// 读取资源内容。
    /// </summary>
    /// <param name="uri">资源 URI。</param>
    /// <param name="text">资源正文（未命中时为空串）。</param>
    /// <returns>命中返回 <see langword="true"/>。</returns>
    public bool TryRead(string? uri, out string text)
    {
        if (uri is not null && _content.TryGetValue(uri, out var found))
        {
            text = found;
            return true;
        }

        text = string.Empty;
        return false;
    }

    /// <summary>资源是否在清单中（<c>resources/read</c> 的前置判定）。</summary>
    /// <param name="uri">资源 URI。</param>
    public bool Contains(string? uri) => uri is not null && _byUri.ContainsKey(uri);

    /// <summary>合法 URI 形态模板（本服务提供的<b>全部</b>资源形态；M-5 守卫据此校验清单不越界）。</summary>
    public static IReadOnlyList<string> UriTemplates { get; } =
    [
        GuidanceScheme + "://{domain}",
        GuidanceScheme + "://{domain}/{topic}",
        AffordanceScheme + "://{tool}",
        ScopesScheme + "://{domain}",
        ErrorContractUri,
    ];

    /// <summary>合法 URI 前缀清单（未知 URI 的可修指引：列"该往哪写"而不是"你没找到"）。</summary>
    public IReadOnlyList<string> UriPrefixes => UriTemplates;

    // ────────── 资源构造 ──────────

    private static void AddGuidance(
        string domain,
        Dictionary<string, McpResourceInfo> byUri,
        Dictionary<string, string> content)
    {
        if (!FeishuToolGuidance.ByDomain.TryGetValue(domain, out var l1) || string.IsNullOrEmpty(l1))
        {
            return;
        }

        var uri = $"{GuidanceScheme}://{domain}";
        byUri[uri] = new McpResourceInfo(
            uri,
            $"{domain} 域引导（L1 全文）",
            $"{domain} 域的路由优先级 / 前置链 / 避坑 / 安全规则 / 示例全文（instructions 里只有一行摘要时读它）",
            ResourceMimeType,
            Utf8Length(l1));
        content[uri] = l1;

        // L2：只列该域的（键形如 "{domain}/{topic}"），避免把别的域的资源暴露给未启用它的宿主。
        var prefix = domain + "/";
        foreach (var key in FeishuToolGuidance.ReferenceKeys)
        {
            if (!key.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            if (!FeishuToolGuidance.TryGetReference(key, out var l2) || string.IsNullOrEmpty(l2))
            {
                continue;
            }

            var topicUri = $"{GuidanceScheme}://{key}";
            byUri[topicUri] = new McpResourceInfo(
                topicUri,
                $"{key} 主题（L2）",
                $"{domain} 域的深度避坑专题（{key.Substring(prefix.Length)}）",
                ResourceMimeType,
                Utf8Length(l2));
            content[topicUri] = l2;
        }
    }

    private static void AddScopes(
        string domain,
        IReadOnlyList<FeishuToolDefinition> tools,
        Dictionary<string, McpResourceInfo> byUri,
        Dictionary<string, string> content)
    {
        var builder = new StringBuilder();
        builder.Append("# ").Append(domain).Append(" 域的权限与风险对照\n\n");
        builder.Append("> 本文件由工具面的**声明**生成（`[FeishuTool] RequiredScopes` / 风险 / 身份）。\n");
        builder.Append("> `required_scopes` 是工具声明需要哪些权限，**不代表**平台已为应用开通——\n");
        builder.Append("> 人读的开放平台权限名与开通指引见仓库文档 `documents/AIAgent/工具权限对照表.md`。\n\n");

        foreach (var tool in tools.OrderBy(static t => t.Name, StringComparer.Ordinal))
        {
            builder.Append("- `").Append(tool.Name).Append("`：");
            builder.Append(tool.IsWrite ? "写" : "只读");
            builder.Append(" / 风险 ").Append(FeishuToolRiskNames.ToLiteral(tool.Risk));
            builder.Append(" / 身份 ").Append(tool.Identity);
            builder.Append(" / scope ");
            builder.Append(tool.RequiredScopes.Count > 0
                ? string.Join("、", tool.RequiredScopes)
                : "（无，纯元工具）");
            builder.Append('\n');
        }

        var uri = $"{ScopesScheme}://{domain}";
        var text = builder.ToString();
        byUri[uri] = new McpResourceInfo(
            uri,
            $"{domain} 域权限对照",
            $"{domain} 域全部已启用工具的 scope / 风险 / 身份（宿主开通权限与合规评审用）",
            ResourceMimeType,
            Utf8Length(text));
        content[uri] = text;
    }

    private static void AddErrorContract(
        Dictionary<string, McpResourceInfo> byUri,
        Dictionary<string, string> content)
    {
        byUri[ErrorContractUri] = new McpResourceInfo(
            ErrorContractUri,
            "工具错误契约",
            "错误载荷字段 + category/subtype 闭集 + 消费者决策树（读它即可机械决策重试/换参/申请权限/放弃）",
            ResourceMimeType,
            Utf8Length(ErrorContractSummary));
        content[ErrorContractUri] = ErrorContractSummary;
    }

    /// <summary>
    /// 从模型可见描述里取 affordance 决策块（从块标题到描述结尾）。
    /// </summary>
    /// <remarks>
    /// <b>为什么从描述取</b>：affordance 由执行链在构造期追加到工具描述上，因此"描述里的那一段"
    /// 就是<b>模型看到的同一份文本</b>（与 <c>tools/list</c> 逐字节一致）——
    /// 不需要在 MCP 侧重新拼装 affordance（那会造出第二份真相源）。未配资产的工具没有该标题，
    /// 自然不产出资源。
    /// </remarks>
    private static string? ExtractAffordanceBlock(string description)
    {
        var index = description.IndexOf(AffordanceBlockTitle, StringComparison.Ordinal);
        if (index < 0)
        {
            return null;
        }

        var block = description.Substring(index).Trim();
        return block.Length > 0 ? block : null;
    }

    private static string DomainOf(string toolName)
    {
        var index = toolName.IndexOf(".", StringComparison.Ordinal);
        return index > 0 ? toolName.Substring(0, index) : toolName;
    }

    private static int Utf8Length(string text) => Encoding.UTF8.GetByteCount(text);
}
