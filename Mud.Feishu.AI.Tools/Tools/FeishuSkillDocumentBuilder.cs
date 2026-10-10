// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mud.Feishu.AI.Tools;

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
/// guidance 是唯一真相源，导出层再"加工"一次就变成第二份真相源。
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

    /// <summary>工具描述在命令清单里的最大长度（超长截断——SKILL.md 要能被人快速扫读）。</summary>
    internal const int DescriptionMaxLength = 240;

    /// <summary>
    /// 构建全部 Skills 文件（按路径 Ordinal 排序）。
    /// </summary>
    internal static IReadOnlyList<SkillDocument> Build()
    {
        var documents = new List<SkillDocument>();
        var writeTools = new HashSet<string>(FeishuToolNames.WriteAll, StringComparer.Ordinal);

        foreach (var domain in FeishuToolGuidance.ByDomain.Keys.OrderBy(static k => k, StringComparer.Ordinal))
        {
            var tools = FeishuToolNames.All
                .Where(name => name.StartsWith(domain + ".", StringComparison.Ordinal))
                .OrderBy(static name => name, StringComparer.Ordinal)
                .ToArray();

            documents.Add(new SkillDocument(
                $"{RootDirectory}/feishu-{domain}/{SkillFileName}",
                BuildSkillMarkdown(domain, tools, writeTools)));

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
        }

        return builder.ToString();
    }

    /// <summary>工具 Schema 里的描述（折叠换行 + 截断；解析失败即省略描述，不抛——纯函数无 IO 无日志）。</summary>
    private static string ToolDescription(string toolName)
    {
        if (!FeishuToolSchemas.SchemaByToolName.TryGetValue(toolName, out var schemaJson))
        {
            return string.Empty;
        }

        string? description;
        try
        {
            description = JsonNode.Parse(schemaJson)?["description"]?.GetValue<string>();
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

        var flattened = Flatten(description);
        return flattened.Length <= DescriptionMaxLength
            ? flattened
            : flattened.Substring(0, DescriptionMaxLength) + "…";
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
}