// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// <b>唯一源级引擎 + 禁兼引</b>契约守卫（v2.3 接棒已退役的 <c>RetiredLocalEngine_ShouldNotReappearAsALocalGenerator</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>背景</b>：工具面生成引擎（ToolSurface 竖井）自组件仓 <c>Mud.HttpUtils.Generator</c> 整批迁入飞书仓
/// <c>Mud.AI.Generator</c>（调优期仓内闭环，P3 上游化）。旧守卫的四条判据防的是「仓内零生成器」，
/// 引擎进仓后该口径必须<b>收窄</b>为「仓内恰一个源级生成器宿主」——收窄而非放开。
/// </para>
/// <para>
/// <b>为什么禁兼引是机械必需</b>：两套 ToolSurface 引擎进入同一编译单元会产生<b>同名 hintName</b> 产物
/// （<c>{ProductPrefix}Schemas.g.cs</c> 等全部由剖面槽派生）⇒ CS8785/CS0101/产物歧义。
/// 包引用形态的分析器不沿 ProjectReference 传递，故「AI 族切仓内引擎 + 同日摘除组件包」才成立。
/// </para>
/// <para>
/// <b>准确语义（不过度承诺）</b>：本仓同时存在两个编译期 Roslyn 引擎在运行——组件包
/// <c>Mud.HttpUtils.Generator</c>（做 HTTP 客户端生成，SDK 4 工程引用）与仓内 <c>Mud.AI.Generator</c>
/// （做工具面）。本守卫保证的是「<b>源码级 <c>[Generator]</c> 宿主恰一个</b>」+「同一编译单元禁兼引两个
/// ToolSurface」，而非「全仓只有一个编译器插件在跑」。
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
    // 断言 ①：源级 [Generator] 宿主恰一个，且为 Mud.AI.Generator（名称 + slnx 双认定）
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 全仓（剔 obj/bin）<c>[Generator]</c> 标注只允许出现在 <see cref="EngineProject"/> 目录内；
    /// 且该工程必须登记进 <c>Mud.Feishu.slnx</c>（未登记 = 不在构建图内 = 双引擎最隐蔽入口）。
    /// </summary>
    [Fact]
    public void SourceLevelGeneratorHost_ShouldBeExactlyMudAiGenerator()
    {
        var root = FindRepositoryRoot();
        var engineDirectory = Path.Combine(root, "Mud.AI.Generator");

        var strayGenerators = SourceFiles(root)
            .Where(path => !path.StartsWith(engineDirectory, StringComparison.OrdinalIgnoreCase))
            .Where(path => !Path.GetFileName(path).Equals(SelfFileName, StringComparison.OrdinalIgnoreCase))
            .Where(path => HasGeneratorAttribute(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(root, path))
            .ToArray();

        strayGenerators.Should().BeEmpty(
            "仓内只允许 Mud.AI.Generator 一个源级 [Generator] 宿主（两套引擎并存会产生同名 hintName 产物冲突）：{0}",
            string.Join(" | ", strayGenerators));

        File.Exists(Path.Combine(engineDirectory, EngineProject)).Should().BeTrue(
            "Mud.AI.Generator 工程必须存在于仓库根目录（ToolSurface 竖井的宿主）");

        var slnx = File.ReadAllText(Path.Combine(root, "Mud.Feishu.slnx"));
        slnx.Should().Contain($"Mud.AI.Generator/{EngineProject}",
            "Mud.AI.Generator 必须登记进 Mud.Feishu.slnx（未登记的工程不在构建图内，也无法被 AOT 严格模式枚举）");
    }

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
            .Where(project => project.Content.Contains("Mud.AI.Generator.csproj", StringComparison.OrdinalIgnoreCase))
            .Where(project => Regex.IsMatch(project.Content,
                $@"PackageReference\s+Include=""{Regex.Escape(ComponentEnginePackage)}"""))
            .Select(static project => project.Name)
            .ToArray();

        offenders.Should().BeEmpty(
            "以下工程兼引了两个 ToolSurface 引擎（仓内 Mud.AI.Generator + 组件 Mud.HttpUtils.Generator）——"
            + "两套引擎向同一编译单元发射同名 hintName 产物（CS8785/CS0101/产物歧义）：{0}",
            string.Join(" | ", offenders));
    }

    /// <summary>检查器自证：合成「兼引双方」必须被点名；单侧引用不得误伤。</summary>
    [Fact]
    public void DualEngineReferenceChecker_ShouldFlagBothSides()
    {
        const string both = "<Project><ItemGroup><ProjectReference Include=\"..\\Mud.AI.Generator\\Mud.AI.Generator.csproj\" /><PackageReference Include=\"Mud.HttpUtils.Generator\" Version=\"3.0.6\" /></ItemGroup></Project>";
        const string onlyInRepo = "<Project><ItemGroup><ProjectReference Include=\"..\\Mud.AI.Generator\\Mud.AI.Generator.csproj\" /></ItemGroup></Project>";
        const string onlyComponent = "<Project><ItemGroup><PackageReference Include=\"Mud.HttpUtils.Generator\" Version=\"3.0.6\" /></ItemGroup></Project>";

        ReferencesBothEngines(both).Should().BeTrue("兼引双方必须被识别");
        ReferencesBothEngines(onlyInRepo).Should().BeFalse("只引仓内引擎是合法切换态");
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
        const string legal = "<Project><ItemGroup><ProjectReference Include=\"..\\Mud.Feishu.Abstractions\\Mud.Feishu.Abstractions.csproj\" /></ItemGroup></Project>";

        HasMudAiProjectReference(offending).Should().BeTrue("核心 SDK 引用 Mud.AI.* 必须被识别");
        HasMudAiProjectReference(legal).Should().BeFalse("核心 SDK 的内部纵向引用不得误伤");
    }

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
        return content.Contains("Mud.AI.Generator.csproj", StringComparison.OrdinalIgnoreCase)
            && Regex.IsMatch(content, $@"PackageReference\s+Include=""{Regex.Escape(ComponentEnginePackage)}""");
    }

    private static bool HasMudAiProjectReference(string csprojContent)
        => Regex.IsMatch(StripComments(csprojContent), @"<ProjectReference\s+Include=""[^""]*Mud\.AI\.[^""]*\.csproj""");
}
