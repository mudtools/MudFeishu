// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Mud.Feishu.AI.Tools.Tests.Tools;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// R-3 / F-8：Skills 产物的<b>读取闭环</b>与<b>版本一致性</b>守卫。
/// </summary>
/// <remarks>
/// <para>
/// <b>补齐的缺口（F-8 的根因）</b>：<c>skills/**</c> 随 NuGet 包分发、也能导出，但此前<b>仓内零消费者</b>——
/// "包内产物 == 读取内容"从未被任何路径验证；同时产物里没有任何"来自哪个包"的标识，
/// 宿主无法判断手上的 <c>SKILL.md</c> 是否与当前包配套。
/// </para>
/// <para>
/// <b>三条不变量</b>：
/// ① <b>读取 = 导出</b>（<see cref="ISkillDocumentReader"/> 的产物与
/// <see cref="IToolSchemaExporter.Export(ToolSchemaDialect.Skills)"/> 清单逐条同源）；
/// ② 每个 <c>SKILL.md</c> 声明 <c>package_version == 程序集版本</c>；
/// ③ <b>已提交的</b> <c>skills/</c> 树同样满足 ②（bump 包版本却忘记重固化产物 ⇒ 本守卫报红）。
/// </para>
/// <para>
/// <b>自证</b>：<see cref="VersionScanner_ShouldDetectDrift_OtherwiseGuardIsFalseGreen"/> 用合成
/// frontmatter 驱动同一解析器，断言它真的能报出"版本不符"与"字段缺失"。
/// </para>
/// </remarks>
public class SkillsVersionConsistencyContractGuards
{
    private static readonly Regex FrontmatterPattern = new(
        @"^package_version:\s*(?<v>\S+)\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static string AssemblyVersion
    {
        get
        {
            var version = typeof(ISkillDocumentReader).Assembly.GetName().Version;
            return version is null ? "0.0.0" : version.ToString(3);
        }
    }

    [Fact]
    public void ReaderProducts_ShouldMatchExporterManifest_Bidirectionally()
    {
        using var provider = GuardProviderFactory.CreateProvider(_ => { });
        var reader = provider.GetRequiredService<ISkillDocumentReader>();
        var exporter = provider.GetRequiredService<IToolSchemaExporter>();

        var manifest = JsonNode.Parse(exporter.Export(ToolSchemaDialect.Skills))!.AsObject();
        var files = manifest["files"]!.AsArray()
            .Select(static f => (Path: f!["path"]!.GetValue<string>(), Content: f!["content"]!.GetValue<string>()))
            .ToArray();

        files.Should().NotBeEmpty("导出为空说明扫描面坏了（假绿）");
        reader.Paths.Should().BeEquivalentTo(
            files.Select(static f => f.Path),
            "读取接缝的产物集合必须与导出清单一致（同一份编译期单源，不得两条读取路径）");

        var mismatches = files
            .Where(f => !string.Equals(reader.Read(f.Path), f.Content, StringComparison.Ordinal))
            .Select(static f => f.Path)
            .ToArray();

        mismatches.Should().BeEmpty("以下产物的读取内容与导出清单不一致（F-8：读取即所得）：{0}",
            string.Join(", ", mismatches));
    }

    [Fact]
    public void ReaderDomainAccessors_ShouldResolveTheSameContentAsPathAccess()
    {
        using var provider = GuardProviderFactory.CreateProvider(_ => { });
        var reader = provider.GetRequiredService<ISkillDocumentReader>();

        var skillPaths = reader.Paths
            .Where(static p => p.EndsWith("/SKILL.md", StringComparison.Ordinal))
            .ToArray();
        skillPaths.Should().NotBeEmpty("至少应有一个域的 SKILL.md");

        foreach (var path in skillPaths)
        {
            // skills/feishu-{domain}/SKILL.md → domain
            var parts = path.Split('/');
            var domain = parts[1].Substring("feishu-".Length);

            reader.ReadSkill(domain).Should().Be(reader.Read(path),
                $"按域读取（{domain}）与按路径读取必须得到同一份内容（前者不是后者的第二真相源）");
        }

        reader.ReadSkill("no-such-domain").Should().BeNull("不存在的域必须返回 null 而不是抛（宿主按域探测是正常用法）");
    }

    [Fact]
    public void EverySkillFile_ShouldDeclareTheCurrentPackageVersion()
    {
        using var provider = GuardProviderFactory.CreateProvider(_ => { });
        var reader = provider.GetRequiredService<ISkillDocumentReader>();

        reader.PackageVersion.Should().Be(AssemblyVersion, "读取接缝上报的版本必须就是程序集（包）版本");

        var missing = reader.Paths
            .Where(static p => p.EndsWith("/SKILL.md", StringComparison.Ordinal))
            .Where(p => !MatchesVersion(reader.Read(p)!, reader.PackageVersion))
            .ToArray();

        missing.Should().BeEmpty(
            "以下 SKILL.md 未声明 package_version={0}（无版本标识的产物无法被消费者判断是否过期）：{1}",
            reader.PackageVersion, string.Join(", ", missing));
    }

    [Fact]
    public void CommittedSkillsTree_ShouldMatchTheCurrentPackageVersion()
    {
        var root = FindRepositoryRoot();
        var skillsRoot = Path.Combine(root, "skills");
        Directory.Exists(skillsRoot).Should().BeTrue("仓库根必须存在 skills/ 产物树（随包分发的唯一来源）");

        var skillFiles = Directory
            .EnumerateFiles(skillsRoot, "SKILL.md", SearchOption.AllDirectories)
            .OrderBy(static p => p, StringComparer.Ordinal)
            .ToArray();
        skillFiles.Should().NotBeEmpty("skills/ 树为空说明产物未落盘（假绿）");

        var drifted = skillFiles
            .Where(file => !MatchesVersion(File.ReadAllText(file), AssemblyVersion))
            .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
            .ToArray();

        drifted.Should().BeEmpty(
            "以下已提交产物的 package_version 与当前包版本（{0}）不符——"
            + "版本 bump 后必须重固化 skills（$env:FeishuSkillsUpdate='true' 后跑 SkillsExportContractTests）：{1}",
            AssemblyVersion, string.Join(", ", drifted));
    }

    /// <summary>负例自证：解析器必须能报出"版本不符"与"字段缺失"（否则上面几条是假绿）。</summary>
    [Fact]
    public void VersionScanner_ShouldDetectDrift_OtherwiseGuardIsFalseGreen()
    {
        const string matching = "---\nname: feishu-im\nversion: 1.0.0\npackage_version: 3.0.1\n---\n正文\n";
        const string drifted = "---\nname: feishu-im\nversion: 1.0.0\npackage_version: 9.9.9\n---\n正文\n";
        const string missing = "---\nname: feishu-im\nversion: 1.0.0\n---\n正文\n";

        MatchesVersion(matching, "3.0.1").Should().BeTrue("版本相符时必须判为通过");

        MatchesVersion(drifted, "3.0.1").Should().BeFalse("版本不符必须被报出（判据失效即为假绿）");
        MatchesVersion(missing, "3.0.1").Should().BeFalse("字段缺失必须被报出（缺字段比写错更常见）");
    }

    /// <summary>判据（纯函数，便于自证）：frontmatter 的 <c>package_version</c> 是否等于期望值。</summary>
    private static bool MatchesVersion(string skillMarkdown, string expected)
    {
        var match = FrontmatterPattern.Match(skillMarkdown);
        return match.Success && string.Equals(match.Groups["v"].Value, expected, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory) && !File.Exists(Path.Combine(directory, "Mud.Feishu.slnx")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        directory.Should().NotBeNullOrEmpty("测试必须能定位仓库根目录（以 Mud.Feishu.slnx 为锚）");
        return directory!;
    }
}
