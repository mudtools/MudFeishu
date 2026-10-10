// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;
using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// R7 / C6a：Skills 产物（<c>skills/feishu-{domain}/SKILL.md</c> + <c>references/*.md</c>）的
/// <b>落盘一致性与双向工具清单守卫</b>。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么产物必须入库并被守卫锁住</b>：Skills 的价值在于"仓外 Agent 直接加载"——即产物本身是<b>交付物</b>，
/// 而不是运行时才生成的中间物。若产物只在测试里现算现比，某天 guidance 改了而产物没重新生成，
/// 守卫会红；但若守卫判据宽松（只比数量），就会变成"数量对得上、内容过期"的假绿。
/// 故本守卫做<b>逐字节</b>比对 + <b>双向</b>工具清单断言。
/// </para>
/// <para>
/// <b>更新方式</b>：环境变量 <c>FeishuSkillsUpdate=true</c> 后运行本类 → 重写 <c>skills/</c> 树
/// （与 <c>FeishuToolGoldenTests</c> 的 golden 重固化同一范式）。
/// </para>
/// </remarks>
public class SkillsExportContractTests
{
    private static readonly Regex ToolLinePattern = new(
        @"^- `(?<name>[a-z][a-z0-9_]*\.[a-z][a-z0-9_.]*)`（(?<kind>只读|写)）：",
        RegexOptions.Multiline,
        TimeSpan.FromSeconds(5));

    /// <summary>导出结果与 <c>skills/</c> 目录树<b>逐字节一致</b>（未登记的旧文件同样被逮住）。</summary>
    [Fact]
    public void SkillsTree_ShouldMatchExporterOutput()
    {
        var expected = FeishuSkillDocumentBuilder.Build();
        expected.Should().NotBeEmpty("导出面为空 ⇒ 扫描面失效（假绿）");

        var root = SkillsDirectory();

        if (IsUpdateRequested())
        {
            Directory.CreateDirectory(root);
            foreach (var stale in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                File.Delete(stale);
            }

            foreach (var document in expected)
            {
                // ⚠️ document.Path 已含 `skills/` 前缀，落盘时必须相对 **root** 再拼一次，
                // 直接 Path.Combine(root, document.Path) 会产出 skills/skills/**（第一版真实踩过）。
                var relative = document.Path.Substring(FeishuSkillDocumentBuilder.RootDirectory.Length + 1);
                var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, document.Content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }
        }

        Directory.Exists(root).Should().BeTrue(
            $"仓库根缺少 {FeishuSkillDocumentBuilder.RootDirectory}/ 目录（重跑更新命令生成产物）");

        var actual = Directory
            .EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => new SkillDocument(
                // ⚠️ 磁盘相对路径必须带 `skills/` 前缀才与导出器产物键同构（漏掉前缀会产出 skills/skills/**，
                // 且守卫会误判"陈旧文件"——这正是本守卫第一次运行就抓到的形态）。
                FeishuSkillDocumentBuilder.RootDirectory + "/" +
                path.Substring(root.Length + 1).Replace(Path.DirectorySeparatorChar, '/'),
                File.ReadAllText(path)))
            .ToDictionary(static d => d.Path, StringComparer.Ordinal);

        var expectedMap = expected.ToDictionary(static d => d.Path, StringComparer.Ordinal);

        actual.Keys.Except(expectedMap.Keys, StringComparer.Ordinal)
            .Should().BeEmpty("skills/ 下存在导出器不会产生的陈旧文件（重跑更新命令清理）");
        expectedMap.Keys.Except(actual.Keys, StringComparer.Ordinal)
            .Should().BeEmpty("导出器产出的文件未落盘（重跑更新命令生成）");

        foreach (var pair in expectedMap)
        {
            if (string.Equals(actual[pair.Key].Content, pair.Value.Content, StringComparison.Ordinal))
            {
                continue;
            }

            var onDisk = actual[pair.Key].Content;
            var generated = pair.Value.Content;
            var index = 0;
            while (index < onDisk.Length && index < generated.Length && onDisk[index] == generated[index])
            {
                index++;
            }

            onDisk.Should().Be(
                generated,
                $"{pair.Key} 内容与导出器不一致（首个差异位于第 {index} 字符；磁盘长度 {onDisk.Length} / 生成长度 {generated.Length}；" +
                $"磁盘侧片段「{Snippet(onDisk, index)}」生成侧片段「{Snippet(generated, index)}」）——需重跑更新命令重新固化产物");
        }
    }

    /// <summary>
    /// <b>双向</b>：SKILL.md 命令清单里的工具名集合 ≡ <see cref="FeishuToolNames.All"/>。
    /// </summary>
    /// <remarks>
    /// 单向检查（"产物里的工具都真实存在"）会漏掉最常见的失效形态：新增工具后产物没更新——
    /// 外部 Agent 看不到新工具，而 SDK 自检全绿。故必须双向。
    /// </remarks>
    [Fact]
    public void SkillToolList_ShouldMatchToolNames_Bidirectionally()
    {
        var documented = new HashSet<string>(StringComparer.Ordinal);
        var unknown = new List<string>();

        foreach (var document in FeishuSkillDocumentBuilder.Build()
                     .Where(static d => d.Path.EndsWith("SKILL.md", StringComparison.Ordinal)))
        {
            var section = CommandSection(document.Content);
            foreach (System.Text.RegularExpressions.Match match in ToolLinePattern.Matches(section))
            {
                var name = match.Groups["name"].Value;
                if (!FeishuToolNames.All.Contains(name))
                {
                    unknown.Add($"{document.Path}: {name}");
                    continue;
                }

                documented.Add(name);
            }
        }

        unknown.Should().BeEmpty("命令清单里出现不存在的工具（会误导外部 Agent 去调用不存在的命令）");
        documented.Except(FeishuToolNames.All, StringComparer.Ordinal).Should().BeEmpty();

        var missing = FeishuToolNames.All.Except(documented, StringComparer.Ordinal).ToArray();
        missing.Should().BeEmpty(
            "以下工具未出现在任何 SKILL.md 中——仓外 Agent 看不到它们：{0}（需重跑更新命令）",
            string.Join(" | ", missing));
    }

    /// <summary>每个域都有 SKILL.md，且每个域都有 L1 资产（产物与 guidance 资产不得脱钩）。</summary>
    [Fact]
    public void EveryGuidanceDomain_ShouldHaveSkillFile()
    {
        var skillDomains = FeishuSkillDocumentBuilder.Build()
            .Where(static d => d.Path.EndsWith("SKILL.md", StringComparison.Ordinal))
            .Select(static d => d.Path)
            .ToArray();

        foreach (var domain in FeishuToolGuidance.ByDomain.Keys.OrderBy(static k => k, StringComparer.Ordinal))
        {
            skillDomains.Should().Contain(
                $"{FeishuSkillDocumentBuilder.RootDirectory}/feishu-{domain}/{FeishuSkillDocumentBuilder.SkillFileName}",
                $"域 {domain} 有 guidance 资产却没导出 SKILL.md（仓外 Agent 拿不到该域的路由与前置链）");
        }
    }

    /// <summary>reference 资产逐字落地（不加工）——导出层一旦改写正文即产生第二份真相源。</summary>
    [Fact]
    public void ReferenceFiles_ShouldMatchGuidanceAssets_Verbatim()
    {
        var documents = FeishuSkillDocumentBuilder.Build().ToDictionary(static d => d.Path, StringComparer.Ordinal);

        foreach (var pair in FeishuToolGuidance.References)
        {
            var slash = pair.Key.IndexOf('/');
            slash.Should().BeGreaterThan(0, $"参考键必须是 '{{domain}}/{{topic}}' 形态：{pair.Key}");
            var domain = pair.Key.Substring(0, slash);
            var topic = pair.Key.Substring(slash + 1);

            var path = $"{FeishuSkillDocumentBuilder.RootDirectory}/feishu-{domain}/" +
                $"{FeishuSkillDocumentBuilder.ReferencesDirectory}/{topic}.md";

            documents.Should().ContainKey(path);
            documents[path].Content.Should().Be(
                Normalize(pair.Value),
                $"{path} 必须与 Guidance/{pair.Key}.md 逐字一致（references 是唯一真相源的镜像）");
        }
    }

    /// <summary>L1 资产正文必须原样嵌入 SKILL.md（guidance 是唯一真相源，导出层不得改写）。</summary>
    [Fact]
    public void SkillFile_ShouldEmbedGuidanceBody_Verbatim()
    {
        foreach (var document in FeishuSkillDocumentBuilder.Build()
                     .Where(static d => d.Path.EndsWith("SKILL.md", StringComparison.Ordinal)))
        {
            var domain = document.Path
                .Substring(FeishuSkillDocumentBuilder.RootDirectory.Length + 1)
                .Split('/')[0]["feishu-".Length..];

            var guidance = Normalize(FeishuToolGuidance.ByDomain[domain]);
            document.Content.Should().Contain(
                guidance.TrimEnd(),
                $"{document.Path} 必须原样嵌入 L1 资产（否则 guidance 改动与产物之间出现第二份真相源）");
        }
    }

    /// <summary>确定性：同一份契约常量导出两次必须逐字节相同（否则守卫会随机变红）。</summary>
    [Fact]
    public void Export_ShouldBeDeterministic()
    {
        FeishuSkillDocumentBuilder.BuildManifestJson()
            .Should().Be(
                new FeishuToolSchemaExporter().Export(ToolSchemaDialect.Skills),
                "导出入口与内部构造器必须产出同一份清单（否则守卫测的不是消费方真正拿到的东西）");
    }

    /// <summary>
    /// 方言投影的既有形状：Skills 方言产出<b>清单对象</b>（不是 OpenAI 数组）。
    /// </summary>
    [Fact]
    public void Export_Skills_ShouldProduceManifestShape()
    {
        var json = new FeishuToolSchemaExporter().Export(ToolSchemaDialect.Skills);

        using var document = System.Text.Json.JsonDocument.Parse(json);
        document.RootElement.GetProperty("dialect").GetString().Should().Be("skills");
        var files = document.RootElement.GetProperty("files");
        files.GetArrayLength().Should().Be(
            FeishuSkillDocumentBuilder.Build().Count,
            "清单文件数必须与实际产物一一对应（不多不少）");

        var first = files[0];
        first.GetProperty("path").GetString().Should().StartWith(FeishuSkillDocumentBuilder.RootDirectory + "/");
        first.GetProperty("content").GetString().Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// frontmatter 形态：对标官方 Agent Skills（<c>name</c>/<c>description</c>/<c>version</c> 三个键）。
    /// </summary>
    [Fact]
    public void SkillFile_ShouldCarryFrontmatter()
    {
        foreach (var document in FeishuSkillDocumentBuilder.Build()
                     .Where(static d => d.Path.EndsWith("SKILL.md", StringComparison.Ordinal)))
        {
            document.Content.Should().StartWith("---\n", $"{document.Path} 必须以 YAML frontmatter 开头");
            // ⚠️ Skip(1)：首行就是分隔线本身，TakeWhile 从第一行开始会立刻停下（得到空数组 ⇒ 假绿/假红）。
            var frontmatter = document.Content
                .Split('\n')
                .Skip(1)
                .TakeWhile(static line => line != "---")
                .ToArray();
            frontmatter.Should().NotBeEmpty("frontmatter 至少要有一行键值（空块等于没有 frontmatter）");
            frontmatter.Should().Contain(static line => line.StartsWith("name: feishu-", StringComparison.Ordinal));
            frontmatter.Should().Contain(static line => line.StartsWith("description: ", StringComparison.Ordinal));
            frontmatter.Should().Contain(static line =>
                line.StartsWith("version: " + FeishuSkillDocumentBuilder.SkillVersion, StringComparison.Ordinal));
        }
    }

    // ────────── 辅助 ──────────

    /// <summary>
    /// <b>打包路径守卫</b>：<c>skills/</c> 必须随 NuGet 包分发。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 库刻意不做文件 IO（<c>Export</c> 返回清单 JSON），因此"只装 NuGet 包的用户能不能拿到技能包"
    /// <b>完全取决于 csproj 里那一条 <c>None/Content + Pack</c> 声明</b>。它被删掉时没有任何编译期信号，
    /// 表现为"生态位只服务从仓库取源码的人"——这种退化极难被发现，故钉住。
    /// </para>
    /// <para>
    /// 同时锁两个踩过的坑：① <c>PackagePath</c> 必须<b>以目录分隔符结尾</b>（否则 NuGet 与递归目录叠加成
    /// <c>skills/feishu-ai/feishu-ai/SKILL.md</c>）；② 必须用<b>递归</b>通配（否则 <c>references/*.md</c> 丢失）。
    /// 拍平写法（<c>skills\%(Filename)%(Extension)</c>）会让 21 个域的同名 <c>SKILL.md</c> 互相覆盖，禁止。
    /// </para>
    /// </remarks>
    [Fact]
    public void Package_ShouldShipSkillsTree()
    {
        var projectPath = Path.Combine(
            Path.GetDirectoryName(SkillsDirectory())!,
            "Mud.Feishu.AI.Tools",
            "Mud.Feishu.AI.Tools.csproj");

        var document = System.Xml.Linq.XDocument.Load(projectPath);
        System.Xml.Linq.XNamespace msbuild = "http://schemas.microsoft.com/developer/msbuild/2003";

        var packing = document
            .Descendants()
            .Where(static element =>
                (element.Name.LocalName is "None" or "Content") &&
                element.Attribute("Pack")?.Value is "true" or "True")
            .ToArray();

        packing.Should().NotBeEmpty("扫描面为空说明判据写坏了（假绿）");

        var skillItems = packing
            .Where(static element => (element.Attribute("Include")?.Value ?? string.Empty)
                .Replace('\\', '/')
                .Contains("/skills/", StringComparison.Ordinal))
            .ToArray();

        skillItems.Should().ContainSingle("skills 产物必须且只能有一条打包声明（重复声明会在包内互相覆盖）");

        var include = skillItems[0].Attribute("Include")!.Value.Replace('\\', '/');
        var packagePath = skillItems[0].Attribute("PackagePath")?.Value ?? string.Empty;

        include.Should().Contain("**", "必须是递归通配：references/*.md 在子目录里，非递归会把它们全丢掉");
        packagePath.Should().StartWith("skills", "包内路径必须落在 skills/ 下（对标官方布局的默认扫描路径）");
        packagePath.Should().EndWith("\\", "PackagePath 必须以目录分隔符结尾——否则 NuGet 会与递归目录叠加（实测产出 skills/feishu-ai/feishu-ai/SKILL.md）");
        packagePath.Should().NotContain("%(Filename)", "拍平写法会让 21 个域的同名 SKILL.md 互相覆盖");
    }

    /// <summary>
    /// 统一换行（仓库内 .md 资产是 CRLF，产物恒为 LF）——比较前必须归一，否则守卫恒红。
    /// </summary>
    private static string Normalize(string value) => value.Replace("\r\n", "\n");

    /// <summary>取差异位置附近的片段（失败信息里要能看到"到底哪里不一样"，否则守卫红了只能靠猜）。</summary>
    private static string Snippet(string value, int index)
    {
        const int radius = 30;
        var start = Math.Max(0, index - radius);
        var length = Math.Min(value.Length - start, radius * 2);
        return length <= 0 ? "<空>" : value.Substring(start, length).Replace("\r", "\\r").Replace("\n", "\\n");
    }

    /// <summary>取「命令（工具）清单」小节正文（避免 guidance 里的反引号工具名被误当清单项）。</summary>
    private static string CommandSection(string content)
    {
        const string header = "## 命令（工具）清单";
        var start = content.IndexOf(header, StringComparison.Ordinal);
        if (start < 0)
        {
            return string.Empty;
        }

        var rest = content[(start + header.Length)..];
        var next = rest.IndexOf("\n## ", StringComparison.Ordinal);
        return next < 0 ? rest : rest[..next];
    }

    private static bool IsUpdateRequested()
        => string.Equals(
            Environment.GetEnvironmentVariable("FeishuSkillsUpdate"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    private static string SkillsDirectory()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory) && !File.Exists(Path.Combine(directory, "Mud.Feishu.slnx")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        directory.Should().NotBeNullOrEmpty("测试必须能定位仓库根目录（以 Mud.Feishu.slnx 为锚）");
        return Path.Combine(directory!, FeishuSkillDocumentBuilder.RootDirectory);
    }
}