// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mud.Feishu.AI.Tools.Catalog;

/// <summary>
/// Skills 产物中的一个文件（路径相对仓库/宿主根目录，内容为 UTF-8 文本）。
/// </summary>
/// <param name="Path">相对路径（<c>skills/feishu-{domain}/SKILL.md</c> 等，恒用 <c>/</c> 分隔）。</param>
/// <param name="Content">文件内容（LF 结尾）。</param>
internal sealed record SkillDocument(string Path, string Content);

/// <summary>
/// C6a：把 <c>Guidance/</c> 资产 + 工具面渲染为<b>对标官方 Agent Skills</b> 的产物
/// （<c>skills/feishu-{domain}/SKILL.md</c> + <c>references/*.md</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它（R7 / C6a）</b>：本仓此前只有两种工具描述载体——进程内的 guidance 资产
/// （编译期常量，供 <c>feishu.guidance_read</c> 与 agent 指令面消费）与 OpenAI functions JSON
/// （只对"已经能调用 tools 的模型"有用）。官方生态位是 <b>SKILL.md</b>：外部 Agent（Claude Code / Cursor /
/// 自建 harness）<b>不接本仓的进程</b>，只能读文件。把 guidance 与工具面落成文件，才第一次让
/// "飞书工具面"能被仓外 Agent 加载——这是当前最大的生态差距。
/// </para>
/// <para>
/// <b>纯函数、零文件 IO</b>：数据源全部是编译期常量（<c>FeishuToolGuidance.ByDomain</c> /
/// <c>References</c> / <c>FeishuToolSchemas</c> / <c>FeishuToolNames</c>），本类型只负责拼字符串；
/// 落盘由宿主或 <c>scripts/</c> / 测试（env 开关）完成——库不做 IO 是既有纪律（AOT 与可测试性）。
/// </para>
/// <para>
/// <b>确定性</b>：域、工具、参考文档全部按 Ordinal 排序 ⇒ 同一份契约常量必然导出逐字节相同的产物
/// （否则 golden 式守卫会随机变红）。
/// </para>
/// <para>
/// <b>不重复 guidance 的职责</b>：SKILL.md 正文<b>原样嵌入</b> L1 资产（不做改写/摘要）——
/// guidance 是唯一真相源，导出层再"加工"一次就变成第二份真相源。同理，聚合入口
/// <c>skills/feishu-suite/SKILL.md</c> 只承载<b>由域资产推导</b>的路由表与规模数字，
/// 不复述任何域内安全规则。
/// </para>
/// </remarks>
internal static class FeishuSkillDocumentBuilder
{
    /// <summary>产物根目录（相对仓库根）。</summary>
    internal const string RootDirectory = "skills";

    /// <summary>域入口文件名（官方约定）。</summary>
    internal const string SkillFileName = "SKILL.md";

    /// <summary>参考文档子目录名。</summary>
    internal const string ReferencesDirectory = "references";

    /// <summary>frontmatter 版本号（随方言落地递增；变更即意味着产物形态变化）。</summary>
    internal const string SkillVersion = "1.0.0";

    /// <summary>
    /// R-3 / F-8：产物所属<b>包版本</b>（frontmatter 的 <c>package_version</c>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么不能与 <see cref="SkillVersion"/> 合一</b>：<see cref="SkillVersion"/> 是<b>产物形态</b>版本
    /// （方言结构变了才动，如 1.0.0 → 2.0.0），本值随<b>包</b>变化。二者合一会让"结构没变、包版本变了"
    /// 与"结构变了"在消费者眼里同形。
    /// </para>
    /// <para>
    /// <b>为什么必须写进产物</b>：<c>skills/**</c> 随 NuGet 包分发，宿主拿到的是一个目录树、
    /// 看不到包元数据。没有该字段，消费者无法判断"手上这份 SKILL.md 是不是当前包的"——
    /// 官方 <c>lark-cli</c> 的 <c>skillscheck</c> 正是为这类漂移而存在。
    /// </para>
    /// <para>
    /// 取值来自程序集版本（<c>&lt;Version&gt;</c> ⇒ <c>AssemblyVersion</c>），故"产物版本 == 包版本"
    /// 在生成期即成立；<b>已提交的产物是否与之同步</b>由
    /// <c>SkillsVersionConsistencyContractGuards</c> 机械断言（漂移即报红）。
    /// </para>
    /// </remarks>
    internal static readonly string PackageVersion = ResolvePackageVersion();

    /// <summary>工具描述在命令清单里的最大长度（超长截断——SKILL.md 要能被人快速扫读）。</summary>
    internal const int DescriptionMaxLength = 240;

    /// <summary>聚合入口目录名（不是 guidance 域，故不参与 <see cref="FeishuToolGuidance.ByDomain"/> 遍历）。</summary>
    internal const string SuiteDirectory = "feishu-suite";

    /// <summary>默认身份（租户级）——尾注只标"非默认"的身份要求。</summary>
    private const string TenantIdentity = "tenant";

    /// <summary>截断优先落点（句读 / 分句 / 空白 / 闭合括号——绝不切进标识符或权限串中间）。</summary>
    private static readonly char[] TruncationBoundaries =
        ['。', '！', '？', '；', '，', '、', ' ', ')', '）', '」', '』'];

    /// <summary>
    /// 构建全部 Skills 文件（按路径 Ordinal 排序）。
    /// </summary>
    internal static IReadOnlyList<SkillDocument> Build()
    {
        var documents = new List<SkillDocument>();
        var writeTools = new HashSet<string>(FeishuToolNames.WriteAll, StringComparer.Ordinal);
        var domainRows = new List<string>();
        var toolTotal = 0;
        var writeTotal = 0;

        foreach (var domain in FeishuToolGuidance.ByDomain.Keys.OrderBy(static k => k, StringComparer.Ordinal))
        {
            var tools = FeishuToolNames.All
                .Where(name => name.StartsWith(domain + ".", StringComparison.Ordinal))
                .OrderBy(static name => name, StringComparer.Ordinal)
                .ToArray();

            documents.Add(new SkillDocument(
                $"{RootDirectory}/feishu-{domain}/{SkillFileName}",
                BuildSkillMarkdown(domain, tools, writeTools)));

            domainRows.Add(BuildSuiteRow(domain, tools, writeTools));
            toolTotal += tools.Length;
            writeTotal += tools.Count(writeTools.Contains);

            foreach (var referenceKey in FeishuToolGuidance.References.Keys
                         .Where(key => key.StartsWith(domain + "/", StringComparison.Ordinal))
                         .OrderBy(static k => k, StringComparer.Ordinal))
            {
                var topic = referenceKey.Substring(domain.Length + 1);
                documents.Add(new SkillDocument(
                    $"{RootDirectory}/feishu-{domain}/{ReferencesDirectory}/{topic}.md",
                    FeishuToolGuidance.References[referenceKey].Replace("\r\n", "\n")));
            }
        }

        documents.Add(new SkillDocument(
            $"{RootDirectory}/{SuiteDirectory}/{SkillFileName}",
            BuildSuiteMarkdown(domainRows, toolTotal, writeTotal)));

        return documents;
    }

    /// <summary>
    /// 构建 <b>清单 JSON</b>（<see cref="IToolSchemaExporter"/> 的返回契约是字符串，目录不是字符串）。
    /// </summary>
    /// <remarks>
    /// 落盘由消费方按 <c>path</c> 写文件即可；清单形态让"导出内容"可被逐字节比对（守卫）。
    /// </remarks>
    internal static string BuildManifestJson()
    {
        var files = new JsonArray();
        foreach (var document in Build())
        {
            // ⚠️ 必须显式转成 JsonNode：JsonArray.Add<T>(T) 带 RequiresDynamicCode/RequiresUnreferencedCode
            // （泛型重载会被 C# 优先选中 ⇒ AOT 严格模式直接 IL2026/IL3050 报错）。
            files.Add((JsonNode)new JsonObject
            {
                ["path"] = document.Path,
                ["content"] = document.Content,
            });
        }

        return new JsonObject
        {
            ["dialect"] = "skills",
            ["files"] = files,
        }.ToJsonString();
    }

    private static string BuildSkillMarkdown(
        string domain,
        IReadOnlyList<string> tools,

        // ⚠️ 用 ISet 而非 IReadOnlySet：后者在 netstandard2.0 目标上不存在（多 TFM 构建会 CS0246）。
        ISet<string> writeTools)
    {
        // ⚠️ 不用 Replace(string, string, StringComparison)：该重载在 netstandard2.0 目标上不存在。
        var guidance = FeishuToolGuidance.ByDomain[domain].Replace("\r\n", "\n");
        var readonlyCount = tools.Count(name => !writeTools.Contains(name));

        var builder = new StringBuilder();
        builder.Append("---").Append('\n');
        builder.Append("name: feishu-").Append(domain).Append('\n');
        builder.Append("description: ")
            .Append($"飞书 {domain} 域工具集：{tools.Count} 个工具（只读 {readonlyCount} / 写 {tools.Count - readonlyCount}）。")
            .Append(Flatten(FirstSentence(guidance)))
            .Append('\n');
        builder.Append("version: ").Append(SkillVersion).Append('\n');
        builder.Append("package_version: ").Append(PackageVersion).Append('\n');
        builder.Append("---").Append('\n');
        builder.Append('\n');

        // 正文原样嵌入 L1 资产（唯一真相源，导出层不加工）。
        builder.Append(guidance.TrimEnd()).Append('\n');

        builder.Append('\n').Append("## 命令（工具）清单").Append('\n');
        if (tools.Count == 0)
        {
            builder.Append("- （本域暂无工具）").Append('\n');
        }
        else
        {
            foreach (var tool in tools)
            {
                builder.Append("- `").Append(tool).Append('`')
                    .Append(writeTools.Contains(tool) ? "（写）" : "（只读）")
                    .Append("：").Append(ToolDescription(tool))
                    .Append('\n');
            }
        }

        var references = FeishuToolGuidance.References.Keys
            .Where(key => key.StartsWith(domain + "/", StringComparison.Ordinal))
            .OrderBy(static k => k, StringComparer.Ordinal)
            .ToArray();
        if (references.Length > 0)
        {
            builder.Append('\n').Append("## 参考文档（细则）").Append('\n');
            foreach (var key in references)
            {
                var topic = key.Substring(domain.Length + 1);
                builder.Append("- `").Append(ReferencesDirectory).Append('/').Append(topic).Append(".md`：")
                    .Append($"`{domain}/{topic}`")
                    .Append('\n');
            }

            // 真实 harness 消费记录发现的死链：L1 正文用 `feishu.guidance_read(域, 主题)` 指向细则，
            // 而只读文件的 Agent 没有这个进程内工具。导出层不改写正文（改了就是第二份真相源），
            // 在此给出**解码规则**——指针的「域, 主题」与本包 references 文件名一一对应。
            builder.Append("- 正文里形如 `feishu.guidance_read(").Append(domain).Append(", 主题)` 的指针，")
                .Append("在文件形态下读本包的 `").Append(ReferencesDirectory).Append("/主题.md`")
                .Append("（本包细则就是那份资产，无需进程内工具）").Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>聚合入口的一行：技能包名 + 该包规模 + 域路由首条（与指令面摘要共用同一次推导）。</summary>
    private static string BuildSuiteRow(
        string domain,
        IReadOnlyList<string> tools,
        ISet<string> writeTools)
    {
        var readonlyCount = tools.Count(name => !writeTools.Contains(name));
        return $"- `feishu-{domain}`（{tools.Count} 工具：只读 {readonlyCount} / 写 {tools.Count - readonlyCount}）："
            + GuidanceRoutingSummary.RouteBody(
                domain,
                FeishuToolGuidance.ByDomain[domain].Replace("\r\n", "\n"));
    }

    /// <summary>
    /// 聚合入口正文：<b>只有</b>三层渐进披露的读法与域路由表——安全规则与前置链留在域包内
    /// （在此复述即第二份真相源）。规模数字由 <see cref="Build"/> 逐域累加得到，不是手抄。
    /// </summary>
    private static string BuildSuiteMarkdown(IReadOnlyList<string> domainRows, int toolTotal, int writeTotal)
    {
        var builder = new StringBuilder();
        builder.Append("---").Append('\n');
        builder.Append("name: ").Append(SuiteDirectory).Append('\n');
        builder.Append("description: ")
            .Append(
                $"飞书工具面单入口路由：{domainRows.Count} 个域技能包、{toolTotal} 个工具"
                + $"（只读 {toolTotal - writeTotal} / 写 {writeTotal}）。先按域路由表选定技能包，再进包读工具清单与前置链。")
            .Append('\n');
        builder.Append("version: ").Append(SkillVersion).Append('\n');
        builder.Append("package_version: ").Append(PackageVersion).Append('\n');
        builder.Append("---").Append('\n');
        builder.Append('\n');

        builder.Append("# 飞书工具面 · 单入口路由").Append('\n');
        builder.Append('\n');
        builder.Append("## 读法（三层渐进披露）").Append('\n');
        builder.Append("1. 在本文件的域路由表里按**对象与动作**选定一个域。").Append('\n');
        builder.Append("2. 加载该域技能包：路由优先级、前置链、避坑、安全规则与逐工具清单都在包内。").Append('\n');
        builder.Append("3. 只有需要结构化语法（卡片 DSL、筛选排序、话题与回复判别等）时再读包内 `references/*.md`。").Append('\n');
        builder.Append('\n');
        builder.Append($"## 域路由表（{domainRows.Count} 个域）").Append('\n');
        foreach (var row in domainRows)
        {
            builder.Append(row).Append('\n');
        }

        builder.Append('\n');
        builder.Append("## 尾注约定").Append('\n');
        builder.Append("- 域包清单里被截断的长描述会在行尾补 `〔…〕` 尾注，回填该工具的权限串与非默认身份要求。").Append('\n');
        builder.Append("- 域包正文里的 `feishu.guidance_read(域, 主题)` 指针，在文件形态下就是那个包的 `")
            .Append(ReferencesDirectory).Append("/主题.md`。").Append('\n');
        builder.Append("- 写工具默认不启用，须经宿主授权（`IToolExecutionAuthorizer`）；判据与预演方式在各域包的安全规则小节给出。").Append('\n');

        return builder.ToString();
    }

    /// <summary>
    /// 工具 Schema 里的描述（折叠换行 + 边界截断 + 契约尾注回填；解析失败即省略描述，不抛——纯函数无 IO 无日志）。
    /// </summary>
    private static string ToolDescription(string toolName)
    {
        if (!FeishuToolSchemas.SchemaByToolName.TryGetValue(toolName, out var schemaJson))
        {
            return string.Empty;
        }

        string? description;
        IReadOnlyList<string> requiredScopes;
        string identity;
        try
        {
            var root = JsonNode.Parse(schemaJson) as JsonObject;
            var meta = root?[ToolAffordanceText.ExtensionKey] as JsonObject;
            description = ReadString(root, "description");
            requiredScopes = ReadStringArray(meta, "required_scopes");
            identity = ReadString(meta, "identity") ?? string.Empty;
        }
        catch (JsonException)
        {
            // 编译期常量正常时不可达（golden 门禁已保证逐字节合法）；防御性省略描述即可。
            // 有意静默（守卫白名单）：纯函数（无 logger），且"省略描述"是确定的降级语义——
            // 常量损坏在构建期已由 golden 门禁与 MUDFT014 拦下，运行期记日志无新增信息。
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        return ClampDescription(Flatten(description), requiredScopes, identity);
    }

    /// <summary>
    /// 超长描述的截断形态：<b>句读边界断开 + 契约信息（权限串 / 非默认身份）以行尾尾注回填</b>。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 权限串与身份要求固定在描述**末尾**（策展约定），所以硬切上界正好把它们吞掉：仓外 Agent
    /// 拿到的是"看着完整、其实缺授权判据"的一行——比缺整行更糟。尾注由 Schema 的
    /// <c>x-feishu</c> 元数据构造（同一份契约的投影，不是第二份真相源），且总长仍 ≤
    /// <see cref="DescriptionMaxLength"/>。
    /// </para>
    /// <para>
    /// 尾注一律给出<b>全量</b>契约（身份 + 权限），不判"头里还剩哪些"：头的预算本身要因尾注而再收缩，
    /// 按收缩前的头来决定补什么会补漏（实测 <c>minutes.upload</c> 正是这样把 user 身份漏掉的）。
    /// </para>
    /// </remarks>
    internal static string ClampDescription(
        string flattened,
        IReadOnlyList<string> requiredScopes,
        string identity)
    {
        if (flattened.Length <= DescriptionMaxLength)
        {
            return flattened;
        }

        var note = BuildContractNote(requiredScopes, NonTenantIdentity(identity));
        if (note.Length == 0)
        {
            return TruncateAtBoundary(flattened, DescriptionMaxLength);
        }

        return TruncateAtBoundary(flattened, Math.Max(1, DescriptionMaxLength - note.Length - 1)) + " " + note;
    }

    /// <summary>非默认身份才进尾注（tenant 是所有工具的缺省，写出来只是噪声）。</summary>
    private static string NonTenantIdentity(string identity)
        => identity.Length > 0 && !TenantIdentity.Equals(identity, StringComparison.Ordinal) ? identity : string.Empty;

    private static string BuildContractNote(IReadOnlyList<string> scopes, string identity)
    {
        var parts = new List<string>();
        if (identity.Length > 0)
        {
            parts.Add(identity + " 身份");
        }

        if (scopes.Count > 0)
        {
            parts.Add("需 " + string.Join(" 与 ", scopes));
        }

        // 无契约可回填时必须返回**空串**——返回空壳 `〔〕` 会让 ClampDescription 的
        // "无尾注即纯边界截断"分支永不可达（截断行尾挂一对空括号，是噪声不是判据）。
        return parts.Count == 0
            ? string.Empty
            : "〔" + string.Join("；", parts) + "〕";
    }

    /// <summary>按句读/空白边界截断（切在边界符之前 ⇒ 不产生半个标识符或半个权限串）。</summary>
    private static string TruncateAtBoundary(string text, int maxLength)
    {
        var width = Math.Min(maxLength, text.Length);
        var cut = -1;
        foreach (var boundary in TruncationBoundaries)
        {
            // ⚠️ LastIndexOf(char, startIndex, count) 的 count 是"从 startIndex 往前数几个字符"，
            // 不是窗口长度——写成 (boundary, 0, width) 会直接 ArgumentOutOfRangeException。
            var index = text.LastIndexOf(boundary, width - 1, width);
            if (index > cut)
            {
                cut = index;
            }
        }

        return cut > width / 2
            ? text.Substring(0, cut) + "…"
            : text.Substring(0, Math.Max(0, width - 1)) + "…";
    }

    /// <summary>取字符串字段（形态不符即视作缺失，不抛——与「常量损坏归构建期门禁」一致）。</summary>
    private static string? ReadString(JsonObject? owner, string key)
        => owner?[key] is JsonValue value && value.TryGetValue(out string? text) ? text : null;

    private static IReadOnlyList<string> ReadStringArray(JsonObject? owner, string key)
    {
        if (owner?[key] is not JsonArray array)
        {
            return Array.Empty<string>();
        }

        var values = new List<string>(array.Count);
        foreach (var item in array)
        {
            if (item is JsonValue value && value.TryGetValue(out string? text) && text!.Length > 0)
            {
                values.Add(text);
            }
        }

        return values;
    }

    private static string FirstSentence(string guidance)
    {
        foreach (var line in guidance.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            // 跳过 markdown 标题行（description 是给 Agent 的一句话，不是标题）。
            if (trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            // 剥掉列表符号与强调标记：description 是给 Agent 的一句话，
            // 带 "- " / "**" 进去会让 Agent Skills 的索引与检索质量明显下降。
            var span = 0;
            while (span < trimmed.Length && (trimmed[span] == '-' || trimmed[span] == '*' || trimmed[span] == ' '))
            {
                span++;
            }

            while (span < trimmed.Length && char.IsDigit(trimmed[span]))
            {
                span++;
            }

            if (span < trimmed.Length && (trimmed[span] == '.' || trimmed[span] == '、'))
            {
                span++;
            }

            var body = trimmed.Substring(span).Trim();

            // ⚠️ 不用 Replace(string, string, StringComparison)：该重载在 netstandard2.0 目标上不存在。
            body = body.Replace("**", string.Empty);
            if (body.Length > 0)
            {
                return body;
            }
        }

        return string.Empty;
    }

    private static string Flatten(string value)
        => value.Replace('\r', ' ').Replace('\n', ' ').Trim();

    /// <summary>从程序集版本推导包版本（<c>3.0.1.0</c> ⇒ <c>3.0.1</c>）。</summary>
    /// <remarks>
    /// 用 <c>AssemblyVersion</c>（而非 <c>AssemblyInformationalVersion</c>）：后者在启用 SourceLink 时
    /// 带 <c>+&lt;commit sha&gt;</c> 后缀，会让"同一包版本"在不同构建机器上产出不同产物——
    /// 那会引爆 <c>SkillsExportContractTests</c> 的逐字节比对（假红）。
    /// </remarks>
    private static string ResolvePackageVersion()
    {
        var version = typeof(FeishuSkillDocumentBuilder).Assembly.GetName().Version;
        return version is null ? "0.0.0" : version.ToString(3);
    }
}