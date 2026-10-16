// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Match = System.Text.RegularExpressions.Match;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// <b>仓内无源级引擎 + 禁兼引</b>契约守卫（v2.3 接棒已退役的 <c>RetiredLocalEngine_ShouldNotReappearAsALocalGenerator</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>背景</b>：工具面生成引擎（ToolSurface 竖井）自组件仓 <c>Mud.HttpUtils.Generator</c> 迁入飞书仓
/// <c>Mud.AI.Generator</c>，随后随 <c>Mud.AI</c> 家族独立化迁至独立仓（<c>D:\Repos\MudAIAgent</c>）。
/// 本仓不再持有任何源级生成器宿主，改以 NuGet <b>分析器包</b>形态消费——故旧口径「仓内恰一个源级生成器
/// 宿主」进一步收窄为「<b>仓内零源级生成器宿主</b>」（收窄而非放开）。
/// </para>
/// <para>
/// <b>为什么禁兼引是机械必需</b>：两套 ToolSurface 引擎进入同一编译单元会产生<b>同名 hintName</b> 产物
/// （<c>{ProductPrefix}Schemas.g.cs</c> 等全部由剖面槽派生）⇒ CS8785/CS0101/产物歧义。
/// 分析器形态的引用（包或工程）不沿 ProjectReference 传递，故必须逐工程显式声明。
/// </para>
/// <para>
/// <b>准确语义（不过度承诺）</b>：本仓同时存在两个编译期 Roslyn 引擎在运行——组件包
/// <c>Mud.HttpUtils.Generator</c>（做 HTTP 客户端生成，SDK 工程引用）与 <c>Mud.AI.Generator</c>
/// <b>分析器包</b>（做工具面，跨仓）。本守卫保证的是「<b>本仓无源码级 <c>[Generator]</c> 宿主</b>」+
/// 「同一编译单元禁兼引两个 ToolSurface 引擎」+「<c>Mud.AI.*</c> 包引用钉死非浮动」+
/// 「核心 SDK 不得依赖 <c>Mud.AI.*</c>」，而非「全仓只有一个编译器插件在跑」。
/// </para>
/// </remarks>
public class SingleToolSurfaceEngineContractGuards
{
    /// <summary>仓内唯一源级生成器宿主工程（ToolSurface 竖井）。</summary>
    private const string EngineProject = "Mud.AI.Generator.csproj";

    /// <summary>引擎的测试工程（GeneratorDriver 单测需要直接引用 Roslyn API——驱动生成器不等于托管第二个引擎）。</summary>
    private const string EngineTestProject = "Mud.AI.Generator.Tests.csproj";

    /// <summary>组件侧 ToolSurface 引擎包（已发布；AI 族停用后仅 SDK 族的 HTTP 生成路径消费）。</summary>
    private const string ComponentEnginePackage = "Mud.HttpUtils.Generator";

    /// <summary>核心 SDK 工程（不得反向依赖 <c>Mud.AI.*</c> 包族——依赖方向铁律）。</summary>
    private static readonly string[] CoreSdkProjects =
    [
        "Mud.Feishu.csproj",
        "Mud.Feishu.Abstractions.csproj",
        "Mud.Feishu.DataModels.csproj",
        "Mud.Feishu.EventCallback.csproj",
        "Mud.Feishu.Authentication.csproj",
        "Mud.Feishu.Webhook.csproj",
        "Mud.Feishu.WebSocket.csproj",
        "Mud.Feishu.Redis.csproj",
        "Mud.Feishu.OpenTelemetry.csproj",
    ];

    /// <summary>
    /// 本守卫自身所在文件名：判据 ① 要匹配的字面量（<c>[Generator]</c> …）就写在本文件里，
    /// 必须把自己排除出扫描面，否则第一次跑就命中自己（旧守卫同款自证陷阱）。
    /// </summary>
    private static readonly string SelfFileName = Path.GetFileName(CallerPath());

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string CallerPath([CallerFilePath] string path = "") => path;

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

    private static IEnumerable<string> ProjectFiles(string root)
        => Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.Contains("obj", StringComparison.OrdinalIgnoreCase)
                     && !p.Contains("bin", StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<string> SourceFiles(string root)
        => Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains("obj", StringComparison.OrdinalIgnoreCase)
                     && !p.Contains("bin", StringComparison.OrdinalIgnoreCase));

    /// <summary>剔除 XML 注释后再匹配（注释里的旧引用不是有效声明）。</summary>
    private static string StripComments(string content)
        => Regex.Replace(content, "<!--.*?-->", string.Empty, RegexOptions.Singleline);

    // ────────────────────────────────────────────────────────────────────
    // 断言 ①：本仓无源级 [Generator] 宿主；引擎以 Mud.AI.Generator 分析器包形态消费（跨仓）
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// v2.3 迁移：源级 <c>[Generator]</c> 宿主（ToolSurface 竖井 <c>Mud.AI.Generator</c>）已整批迁出飞书仓
    /// （迁入新仓 <c>MudAIAgent</c>）；本仓不再持有任何源级生成器宿主，工具面引擎改以 NuGet
    /// <b>分析器包</b>形态消费（<c>OutputItemType="Analyzer"</c> + <c>ReferenceOutputAssembly="false"</c>）。
    /// </summary>
    /// <remarks>
    /// <b>为什么必须机械锁定</b>：分析器包引用一旦被摘除或改写，源生成器不再运行——<b>构建仍全绿</b>
    /// 但工具面 Schema/注册表/Args 产物整体消失（宿主在运行期才以"工具不见了"暴露）。「仓内至多一个
    /// 源级引擎宿主」由此处的"全仓无 <c>[Generator]</c> 宿主"承接。
    /// </remarks>
    [Fact]
    public void SourceLevelGeneratorHost_ShouldBeAbsent_EngineConsumedAsPackage()
    {
        var root = FindRepositoryRoot();

        // 迁移后本仓不应再有任何源级 [Generator] 宿主（引擎已迁出并以分析器包形态回归消费）。
        var strayGenerators = SourceFiles(root)
            .Where(path => !Path.GetFileName(path).Equals(SelfFileName, StringComparison.OrdinalIgnoreCase))
            .Where(path => HasGeneratorAttribute(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(root, path))
            .ToArray();

        strayGenerators.Should().BeEmpty(
            "本仓不得再出现源级 [Generator] 宿主（ToolSurface 引擎 Mud.AI.Generator 已迁出飞书仓，"
            + "以 NuGet 分析器包形态消费；两套引擎并存会产生同名 hintName 产物冲突）：{0}",
            string.Join(" | ", strayGenerators));

        // 引擎以分析器包形态被工具面工程消费——这是"生成器仍在运行"的唯一机械证据。
        var consumerPath = Path.Combine(root, "Mud.Feishu.AI.Tools", "Mud.Feishu.AI.Tools.csproj");
        File.Exists(consumerPath).Should().BeTrue(
            "Mud.Feishu.AI.Tools 工程必须存在（工具面引擎的唯一消费方）");

        var consumer = StripComments(File.ReadAllText(consumerPath));
        consumer.Should().Contain(
            "Include=\"Mud.AI.Generator\"",
            "工具面工程必须以 PackageReference 消费 Mud.AI.Generator 分析器包（摘除即 Schema/注册表静默消失）");
        consumer.Should().Contain(
            "OutputItemType=\"Analyzer\"",
            "Mud.AI.Generator 必须以分析器形态引用（OutputItemType=\"Analyzer\"）——否则它会被当运行时程序集");
        consumer.Should().Contain(
            "ReferenceOutputAssembly=\"false\"",
            "分析器包不得作为运行时程序集引用（ReferenceOutputAssembly=\"false\"）");
    }

    // ────────────────────────────────────────────────────────────────────
    // 断言 ①b：Mud.AI.* 包引用必须是钉死的精确版本（R-5 ③：非浮动）
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// R-5 ③：本仓对 <c>Mud.AI.*</c> 的包引用必须<b>钉死精确版本</b>，不得浮动 / 区间 / 空值。
    /// </summary>
    /// <remarks>
    /// <b>为什么必须机械锁定</b>：跨仓消费下浮动版本会在无代码改动时静默改变编译基线——本地 folder
    /// 源只有一个版本时「看起来正常」，新仓一发新版就被带偏（<c>NU1603</c> 仅在解析版本与声明对不上时
    /// 才报，浮动引用不报）。精确版本 + 显式升级是唯一可审计的跨仓节奏。
    /// </remarks>
    [Fact]
    public void MudAiPackageReferences_ShouldUsePinnedVersion()
    {
        var root = FindRepositoryRoot();
        var offenders = new List<string>();

        foreach (var path in ProjectFiles(root))
        {
            var content = StripComments(File.ReadAllText(path));
            foreach (Match match in Regex.Matches(
                         content,
                         @"PackageReference\s+Include=""(?<pkg>Mud\.AI\.[^""]*)""\s+Version=""(?<ver>[^""]+)"""))
            {
                var version = match.Groups["ver"].Value;
                if (!IsPinnedVersion(version))
                {
                    offenders.Add(
                        $"{Path.GetRelativePath(root, path)}: {match.Groups["pkg"].Value} Version=\"{version}\"");
                }
            }
        }

        offenders.Should().BeEmpty(
            "对 Mud.AI.* 的包引用必须钉死精确版本（不得使用 * / 区间）——浮动版本会在跨仓发版时静默漂移：{0}",
            string.Join(" | ", offenders));
    }

    /// <summary>判据：精确版本 = 数字点分（可带预发布标签），不含 <c>*</c> 与区间记号。</summary>
    private static bool IsPinnedVersion(string version)
        => Regex.IsMatch(version, @"^\d+\.\d+\.\d+(?:-[0-9A-Za-z.\-]+)?$");

    /// <summary>判据自证：浮动 / 区间 / 空版本必须被判为「非钉死」。</summary>
    [Theory]
    [InlineData("1.0.0", true)]
    [InlineData("1.0.0-preview.1", true)]
    [InlineData("10.0.12", true)]
    [InlineData("1.0.*", false)]
    [InlineData("[1.0.0,)", false)]
    [InlineData("1.0.0-*", false)]
    [InlineData("", false)]
    public void PinnedVersionPredicate_ShouldRejectFloatingVersions(string version, bool expected)
        => IsPinnedVersion(version).Should().Be(expected);

    /// <summary>检查器自证：合成「仓外第二个 [Generator] 宿主」必须被点名（防过滤器恒空假绿）。</summary>
    [Theory]
    [InlineData("[Generator]", true)]
    [InlineData("[GeneratorWithAttributeSearch]", true)]
    [InlineData("[GeneratorLite]", false)]
    [InlineData("// [Generator] 注释里的不算", false)]
    public void GeneratorAttributeMatcher_ShouldOnlyMatchRealGeneratorAttributes(string text, bool expected)
        => HasGeneratorAttribute(text).Should().Be(expected);

    // ────────────────────────────────────────────────────────────────────
    // 断言 ②：Roslyn API 直接引用只允许引擎本体与其测试工程
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 除 <see cref="EngineProject"/> / <see cref="EngineTestProject"/> 外，任何工程不得直接引用
    /// <c>Microsoft.CodeAnalysis.CSharp</c>——实现 <c>IIncrementalGenerator</c> 绕不开该依赖，
    /// 故它是「本地生成器宿主」的名称无关判据。
    /// </summary>
    /// <remarks>
    /// 测试工程的豁免理由：<c>CSharpGeneratorDriver</c> 单测需要直接实例化 Roslyn API <b>驱动</b>生成器
    /// （输入 fixture 编译、断言产物与诊断）——驱动不等于托管第二个引擎，且它不含任何 <c>[Generator]</c> 实现
    /// （由断言 ① 的源码扫描兜底）。
    /// </remarks>
    [Fact]
    public void RoslynApiReference_ShouldBeConfinedToEngineAndItsTests()
    {
        var root = FindRepositoryRoot();

        var offenders = ProjectFiles(root)
            .Select(path => (Name: Path.GetFileName(path), Content: StripComments(File.ReadAllText(path))))
            .Where(project => Regex.IsMatch(project.Content, @"PackageReference\s+Include=""Microsoft\.CodeAnalysis\.(?:CSharp|VisualBasic)"""))
            .Where(project => !string.Equals(project.Name, EngineProject, StringComparison.OrdinalIgnoreCase)
                              && !string.Equals(project.Name, EngineTestProject, StringComparison.OrdinalIgnoreCase))
            .Select(static project => project.Name)
            .ToArray();

        offenders.Should().BeEmpty(
            "除 Mud.AI.Generator 与其测试工程外，任何工程不得直接引用 Roslyn 编译器 API"
            + "（Microsoft.CodeAnalysis.CSharp 是 IIncrementalGenerator 绕不开的依赖，即「本地引擎宿主」判据）：{0}",
            string.Join(" | ", offenders));
    }

    /// <summary>检查器自证：合成「第三方工程引用 Roslyn」必须被点名。</summary>
    [Fact]
    public void RoslynApiReferenceChecker_ShouldFlagForeignProjects()
    {
        const string withRef = "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"Microsoft.CodeAnalysis.CSharp\" Version=\"4.11.0\" /></ItemGroup></Project>";

        IsRoslynHost(withRef, "Other.csproj").Should().BeTrue("第三方工程的 Roslyn 引用必须被识别为宿主");
        IsRoslynHost(withRef, EngineProject).Should().BeFalse("引擎自身的 Roslyn 引用不得误判");
        IsRoslynHost(withRef, EngineTestProject).Should().BeFalse("引擎测试工程的 Roslyn 引用不得误判（Driver 单测豁免）");
        IsRoslynHost("<Project><!-- PackageReference Include=\"Microsoft.CodeAnalysis.CSharp\" --></Project>", "Other.csproj")
            .Should().BeFalse("注释里的引用不是有效声明");
    }

    // ────────────────────────────────────────────────────────────────────
    // 断言 ③：禁兼引（双向）——任何工程不得同时引用两个 ToolSurface 引擎
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 同一编译单元禁兼引：任何工程不得<b>同时</b>以 ProjectReference 引用仓内 <c>Mud.AI.Generator</c>
    /// （分析器形态）与以 PackageReference 引用组件 <c>Mud.HttpUtils.Generator</c>——同名 hintName 产物冲突。
    /// </summary>
    [Fact]
    public void NoProjectShouldReferenceBothToolSurfaceEngines()
    {
        var root = FindRepositoryRoot();

        var offenders = ProjectFiles(root)
            .Select(path => (Name: Path.GetFileName(path), Content: StripComments(File.ReadAllText(path))))
            .Where(project => ReferencesMudAiEngine(project.Content))
            .Where(project => Regex.IsMatch(project.Content,
                $@"PackageReference\s+Include=""{Regex.Escape(ComponentEnginePackage)}"""))
            .Select(static project => project.Name)
            .ToArray();

        offenders.Should().BeEmpty(
            "以下工程兼引了两个 ToolSurface 引擎（Mud.AI.Generator 分析器包 + 组件 Mud.HttpUtils.Generator）——"
            + "两套引擎向同一编译单元发射同名 hintName 产物（CS8785/CS0101/产物歧义）：{0}",
            string.Join(" | ", offenders));
    }

    /// <summary>检查器自证：合成「兼引双方」必须被点名；单侧引用不得误伤。</summary>
    [Fact]
    public void DualEngineReferenceChecker_ShouldFlagBothSides()
    {
        const string both = "<Project><ItemGroup><ProjectReference Include=\"..\\Mud.AI.Generator\\Mud.AI.Generator.csproj\" /><PackageReference Include=\"Mud.HttpUtils.Generator\" Version=\"3.0.6\" /></ItemGroup></Project>";
        const string bothPackages = "<Project><ItemGroup><PackageReference Include=\"Mud.AI.Generator\" Version=\"1.0.0\" OutputItemType=\"Analyzer\" ReferenceOutputAssembly=\"false\" /><PackageReference Include=\"Mud.HttpUtils.Generator\" Version=\"3.0.6\" /></ItemGroup></Project>";
        const string onlyInRepo = "<Project><ItemGroup><ProjectReference Include=\"..\\Mud.AI.Generator\\Mud.AI.Generator.csproj\" /></ItemGroup></Project>";
        const string onlyComponent = "<Project><ItemGroup><PackageReference Include=\"Mud.HttpUtils.Generator\" Version=\"3.0.6\" /></ItemGroup></Project>";

        ReferencesBothEngines(both).Should().BeTrue("兼引双方必须被识别");
        ReferencesBothEngines(bothPackages).Should().BeTrue(
            "包形态兼引（迁移后的主形态）也必须被识别——否则禁兼引在包形态下静默失守");
        ReferencesBothEngines(onlyInRepo).Should().BeFalse("只引 Mud.AI.Generator 分析器包是合法消费态");
        ReferencesBothEngines(onlyComponent).Should().BeFalse("只引组件包是切换前状态（SDK 族 HTTP 生成路径）");
    }

    // ────────────────────────────────────────────────────────────────────
    // 断言 ④：依赖方向——核心 SDK 不得反向依赖 Mud.AI.* 包族
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 核心 SDK（<see cref="CoreSdkProjects"/>）不得 ProjectReference 任何 <c>Mud.AI.*</c> 工程——
    /// 把「<c>ApiPageListResult</c> 等核心契约不得迁入 AI 包族」这一关键决定机械锁定，防未来重蹈
    /// （迁入即形成「核心 SDK 反向依赖 AI」的依赖倒置）。
    /// </summary>
    /// <remarks>
    /// 飞书 AI 适配层（<c>Mud.Feishu.AI</c> / <c>Mud.Feishu.AI.Tools</c> / <c>Mud.Feishu.AI.Mcp</c>）
    /// 是 <c>Mud.AI.*</c> 的<b>下游消费方</b>（P1-D 起合法依赖），不受本断言约束。
    /// </remarks>
    [Fact]
    public void CoreSdkProjects_ShouldNotDependOnMudAiFamily()
    {
        var root = FindRepositoryRoot();

        var offenders = new List<string>();
        foreach (var project in CoreSdkProjects)
        {
            var path = Path.Combine(root, Path.GetFileNameWithoutExtension(project), project);
            if (!File.Exists(path))
            {
                continue;
            }

            var content = StripComments(File.ReadAllText(path));
            // 显式限定类型：本工程全局 using 了 Moq，其 Moq.Match 与正则的 Match 同名。
            foreach (System.Text.RegularExpressions.Match match in Regex.Matches(content, @"<ProjectReference\s+Include=""(?<inc>[^""]+)"""))
            {
                var include = match.Groups["inc"].Value.Replace('\\', '/');
                if (include.Contains("Mud.AI.", StringComparison.OrdinalIgnoreCase))
                {
                    offenders.Add($"{project} → {include}");
                }
            }

            // 迁移后 Mud.AI.* 以 NuGet 包形态消费——核心 SDK 同样不得 PackageReference 它们
            // （依赖方向铁律：AI 适配层 → Mud.AI.*，反向即倒置）。
            foreach (System.Text.RegularExpressions.Match match in Regex.Matches(content,
                @"<PackageReference\s+Include=""(?<pkg>Mud\.AI\.[^""]*)"""))
            {
                offenders.Add($"{project} → {match.Groups["pkg"].Value}（包）");
            }
        }

        offenders.Should().BeEmpty(
            "核心 SDK 不得反向依赖 Mud.AI.* 包族（依赖方向：AI 适配层 → Mud.AI.*；反向即倒置）：{0}",
            string.Join(" | ", offenders));
    }

    /// <summary>检查器自证：合成「核心 SDK → Mud.AI.*」必须被点名。</summary>
    [Fact]
    public void CoreSdkDependencyChecker_ShouldFlagMudAiReferences()
    {
        const string offending = "<Project><ItemGroup><ProjectReference Include=\"..\\Mud.AI.Agent\\Mud.AI.Agent.csproj\" /></ItemGroup></Project>";
        const string offendingPackage = "<Project><ItemGroup><PackageReference Include=\"Mud.AI.Agent\" Version=\"1.0.0\" /></ItemGroup></Project>";
        const string legal = "<Project><ItemGroup><ProjectReference Include=\"..\\Mud.Feishu.Abstractions\\Mud.Feishu.Abstractions.csproj\" /></ItemGroup></Project>";

        HasMudAiProjectReference(offending).Should().BeTrue("核心 SDK 引用 Mud.AI.* 必须被识别");
        HasMudAiProjectReference(offendingPackage).Should().BeTrue(
            "核心 SDK 以包形态引用 Mud.AI.* 同样必须被识别（迁移后主形态）");
        HasMudAiProjectReference(legal).Should().BeFalse("核心 SDK 的内部纵向引用不得误伤");
    }

    // ────────────────────────────────────────────────────────────────────
    // 断言 ⑤（中立性——Mud.AI.* 反向不得依赖任何飞书工程）已随实现迁出飞书仓：
    // 迁移后 Mud.AI.* 的 csproj 只存在于新仓，本仓已无该工程可扫（留在本仓会对着不存在的工程空扫 = 假绿）。
    // 由新仓 AgentPackageContractGuards.MudAiFamily_ShouldNotDependOnFeishuProjects 持有。
    // ────────────────────────────────────────────────────────────────────

    // ────────── 判据原语（[Theory] 自证直接驱动，勿内联正则字面量） ──────────

    /// <summary>
    /// 匹配真实的生成器标注。逐行剔除 <c>//</c> 行注释后再匹配——注释/文档里复述
    /// <c>[Generator]</c> 是记录而非声明，计入会假红（本守卫自身的 <c>[InlineData]</c> 即如此）。
    /// </summary>
    private static bool HasGeneratorAttribute(string text)
        => text.Split('\n').Any(static line =>
        {
            var commentIndex = line.IndexOf("//", StringComparison.Ordinal);
            var code = commentIndex >= 0 ? line[..commentIndex] : line;
            return Regex.IsMatch(code, @"\[(?:Generator|GeneratorWithAttributeSearch)\s*[\]\(]");
        });

    private static bool IsRoslynHost(string csprojContent, string projectFileName)
        => Regex.IsMatch(StripComments(csprojContent), @"PackageReference\s+Include=""Microsoft\.CodeAnalysis\.(?:CSharp|VisualBasic)""")
           && !string.Equals(projectFileName, EngineProject, StringComparison.OrdinalIgnoreCase)
           && !string.Equals(projectFileName, EngineTestProject, StringComparison.OrdinalIgnoreCase);

    private static bool ReferencesBothEngines(string csprojContent)
    {
        var content = StripComments(csprojContent);
        return ReferencesMudAiEngine(content)
            && Regex.IsMatch(content, $@"PackageReference\s+Include=""{Regex.Escape(ComponentEnginePackage)}""");
    }

    /// <summary>
    /// 是否引用了飞书侧 ToolSurface 引擎 <c>Mud.AI.Generator</c>。迁移后该引擎以 <b>NuGet 分析器包</b>形态
    /// 消费（<c>PackageReference</c>），迁移前为仓内 <c>ProjectReference</c>——两种形态都必须被认作
    /// 「引用了该引擎」，否则「禁兼引」会在包形态下静默失守（构建仍全绿，同名 hintName 冲突在编译期爆发）。
    /// </summary>
    private static bool ReferencesMudAiEngine(string csprojContent)
        => Regex.IsMatch(
            StripComments(csprojContent),
            @"(?:<ProjectReference\s+Include=""[^""]*Mud\.AI\.Generator\.csproj""|<PackageReference\s+Include=""Mud\.AI\.Generator"")",
            RegexOptions.IgnoreCase);

    private static bool HasMudAiProjectReference(string csprojContent)
    {
        var content = StripComments(csprojContent);
        return Regex.IsMatch(content, @"<ProjectReference\s+Include=""[^""]*Mud\.AI\.[^""]*\.csproj""")
            || Regex.IsMatch(content, @"<PackageReference\s+Include=""Mud\.AI\.[^""]*""");
    }
}
