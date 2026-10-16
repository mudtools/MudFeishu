// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;
using Match = System.Text.RegularExpressions.Match;

namespace Mud.Feishu.AI.Tests.ContractGuards;

/// <summary>
/// AI 底座契约守卫（对齐 TokenMultiAppContractGuards 的守卫风格）。
/// </summary>
public class AgentContractGuards
{
    // ────────────────────────────────────────────────────────────────────
    // 守卫 1：Microsoft.Agents.AI* 包引用必须全仓单版本
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// MAF 相关 PackageReference 声明必须唯一版本。混合版本的 TypeLoadException 类故障
    /// 与 MudHttpUtils 单版本守卫所守护的故障同类（AGENTS.md「同一组件单版本」契约）。
    /// </summary>
    [Fact]
    public void AgentsAi_PackageReferences_ShouldBeSingleVersion()
    {
        var csprojFiles = Directory.GetFiles(GetSolutionRoot(), "*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.Contains("obj") && !p.Contains("bin"))
            .ToList();

        csprojFiles.Should().NotBeEmpty("解决方案中应至少有一个 csproj 文件");

        var versions = new HashSet<string>(StringComparer.Ordinal);

        foreach (var csproj in csprojFiles)
        {
            // 先剔除 XML 注释：注释掉的旧引用不是有效声明，参与比对会产生误报。
            var content = Regex.Replace(File.ReadAllText(csproj), "<!--.*?-->", string.Empty, RegexOptions.Singleline);
            foreach (Match match in Regex.Matches(content,
                         @"Include=""(Microsoft\.Agents\.AI[^""]*)""\s+Version=""([^""]+)""",
                         RegexOptions.IgnoreCase))
            {
                versions.Add(match.Groups[2].Value);
            }
        }

        versions.Should().NotBeEmpty("应至少有一个 Microsoft.Agents.AI* 包引用");
        versions.Should().ContainSingle(
            "全仓 Microsoft.Agents.AI* 必须共用一个版本，混合版本会混入两个程序集（TypeLoadException 类故障）");
    }

    // ────────────────────────────────────────────────────────────────────
    // 守卫 1b：跨仓 MAF 版本契约（本仓 ≥ 新仓 Mud.AI.Agent 的编译基线）
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>跨仓 MAF 版本契约</b>（迁移方案 §6：飞书仓新增）：本仓 <c>Mud.Feishu.AI</c> 编译时所依赖的
    /// <c>Microsoft.Agents.AI</c> 版本，必须 ≥ 独立仓 <c>Mud.AI.Agent</c> 包编译时的版本。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 为什么需要它：<c>Mud.AI.Agent</c> 迁出后，「MAF 单版本」由跨仓两半共同决定——新仓升 MAF 而本仓未跟
    /// （或反之）都会让两边的<b>编译基线</b>分裂。NuGet 的单版本统一会把较高版本带进运行期，构建仍绿，
    /// 于是漂移没有编译期或运行期信号。本断言用两边程序集的<b>引用元数据</b>
    /// （<c>Assembly.GetReferencedAssemblies()</c>）做机械比对——不依赖 nuspec / 全局缓存路径，跨机可行。
    /// </para>
    /// <para>判据方向：本仓 ≥ 新仓（放宽方向只朝一边——适配层可先行升 MAF，中立运行期不得落后）。</para>
    /// </remarks>
    [Fact]
    public void MafVersion_ShouldSatisfyMudAiAgentLowerBound()
    {
        const string Maf = "Microsoft.Agents.AI";

        var feishuMaf = ReferencedAssemblyVersion("Mud.Feishu.AI", Maf);
        var neutralMaf = ReferencedAssemblyVersion("Mud.AI.Agent", Maf);

        feishuMaf.Should().NotBeNull("Mud.Feishu.AI 必须编译期引用 Microsoft.Agents.AI（MAF 白名单单点）");
        neutralMaf.Should().NotBeNull("Mud.AI.Agent 包必须编译期引用 Microsoft.Agents.AI（否则包契约已变）");

        (feishuMaf! >= neutralMaf!).Should().BeTrue(
            "Mud.Feishu.AI 的 MAF 版本（{0}）必须 ≥ 独立仓 Mud.AI.Agent 的版本（{1}）——跨仓漂移无编译期信号",
            feishuMaf,
            neutralMaf);
    }

    /// <summary>取程序集 <paramref name="assemblyName"/> 编译期对 <paramref name="referencedName"/> 的引用版本。</summary>
    private static Version? ReferencedAssemblyVersion(string assemblyName, string referencedName)
        => System.Reflection.Assembly.Load(assemblyName)
            .GetReferencedAssemblies()
            .FirstOrDefault(reference => string.Equals(reference.Name, referencedName, StringComparison.OrdinalIgnoreCase))
            ?.Version;

    // ────────────────────────────────────────────────────────────────────
    // 守卫 2：仅 Mud.Feishu.AI 可直接引用 MAF 包（依赖方向铁律）
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 核心项目（Mud.Feishu 等）不得直接声明 Microsoft.Agents.AI* 引用——
    /// MAF 只允许进入 <b>白名单</b>{<c>Mud.Feishu.AI</c>（产品适配层）}
    /// （总体设计 §3.1：AI → 核心单向，核心不被 Agent 运行时污染）。
    /// 其他项目经项目引用<b>传递</b>获得（如 Mud.Feishu.Redis），不违反本守卫。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>迁移收窄（Mud.AI 系列包独立化）</b>：<c>Mud.AI.Agent</c> / <c>Mud.AI.Abstractions</c> /
    /// <c>Mud.AI.Generator</c> 已迁出本仓（独立 <c>Mud.AI</c> 仓）——白名单随之由
    /// {<c>Mud.Feishu.AI</c>、<c>Mud.AI.Agent</c>} 收窄回本仓内的 <c>Mud.Feishu.AI</c> 单点
    /// （非放开：Mud.AI.Agent 在本仓已不存在，保留旧成员只会是一条指向空工程的死名单）。
    /// </para>
    /// <para>
    /// <b>迁移前放宽所配的反向断言，其载体随实现转移到新仓</b>：
    /// ① <c>Mud.AI.Abstractions</c> 保持零 MAF——现由新仓
    /// <c>AgentPackageContractGuards.MudAiAbstractions_ShouldNotDeclareMafPackages</c> 持有；
    /// ② 全部核心 SDK 工程（含 Mud.Feishu.WebSocket / Webhook / Redis …）仍禁直接声明；
    /// ③ MAF 版本仍由 <see cref="AgentsAi_PackageReferences_ShouldBeSingleVersion"/> 锁单点。
    /// </para>
    /// </remarks>
    [Fact]
    public void AgentsAi_PackageReferences_ShouldBeDeclaredOnlyInAllowListedProjects()
    {
        // MAF 允许集：本仓内仅产品适配层（Mud.AI.Agent 已迁独立仓，其 MAF 单点由新仓持有）。
        string[] allowed = ["Mud.Feishu.AI.csproj"];

        var offenders = new List<string>();
        foreach (var csproj in ProjectFiles())
        {
            var content = StripXmlComments(File.ReadAllText(csproj));
            if (!Regex.IsMatch(content, @"Include=""Microsoft\.Agents\.AI[^""]*""", RegexOptions.IgnoreCase))
                continue;

            var projectName = Path.GetFileName(csproj);
            if (!allowed.Contains(projectName, StringComparer.OrdinalIgnoreCase))
                offenders.Add(projectName);
        }

        offenders.Should().BeEmpty(
            "Microsoft.Agents.AI* 的直接 PackageReference 只允许出现在 {0}（其余工程经项目引用传递获得）",
            string.Join(" / ", allowed));
    }

    private static IEnumerable<string> ProjectFiles()
        => Directory.GetFiles(GetSolutionRoot(), "*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.Contains("obj") && !p.Contains("bin"));

    private static string StripXmlComments(string content)
        => Regex.Replace(content, "<!--.*?-->", string.Empty, RegexOptions.Singleline);

    // ────────────────────────────────────────────────────────────────────
    // 守卫 3：配置面（R5）——SectionName 必须真的被 GetSection 使用
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="FeishuAgentOptions.SectionName"/> 必须在生产源码中被 <c>GetSection</c> 使用
    /// （X1 类「不可绑定 Options」回归锁）。
    /// </summary>
    [Fact]
    public void FeishuAgentOptions_SectionName_ShouldBeUsedByGetSection()
    {
        var aiSources = Directory
            .GetFiles(Path.Combine(GetSolutionRoot(), "Mud.Feishu.AI"), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains("obj") && !p.Contains("bin"))
            .ToList();

        aiSources.Should().NotBeEmpty();

        var consumed = aiSources.Any(path =>
        {
            var content = File.ReadAllText(path);
            return content.Contains("FeishuAgentOptions.SectionName", StringComparison.Ordinal)
                && content.Contains("GetSection", StringComparison.Ordinal);
        });

        consumed.Should().BeTrue("FeishuAgentOptions.SectionName 必须被 GetSection(...) 真正使用（R5/X1）");
    }

    /// <summary>
    /// R3-10：行为断言取代文本 Contains（消假绿——Contains 对注释/字符串同样成立）。
    /// 构造非法 FeishuAgentOptions（Instructions 为空），FeishuAgent 构造必须抛 InvalidOperationException。
    /// </summary>
    [Fact]
    public void FeishuAgentOptions_Validate_ShouldBeInvokedByAgentConstructor()
    {
        // R3-10 行为断言：不依赖源码中 "options.Validate()" 字面量是否存在。
        // 构造一个非法 options（Instructions 为空），Agent 构造必须 fail-fast。
        var act = () =>
        {
            var options = new FeishuAgentOptions
            {
                Instructions = "", // 非法：指令不能为空
            };
            // 构造即校验——如果 Validate() 被注释掉/挪走，此处不会抛。
            // chatClient 传 null 会先抛 ArgumentNullException，故用 Mock stub。
            _ = new FeishuAgent(
                new Mock<Microsoft.Extensions.AI.IChatClient>().Object,
                options);
        };

        act.Should().Throw<InvalidOperationException>(
            "Agent 构造必须 fail-fast 校验配置（Phase 0 §8）——Validate() 被调用则非法 options 必抛");
    }

    /// <summary>
    /// Phase 2 新增配置属性（<c>SummaryThreshold</c>）必须有<b>真实端到端</b>消费点（R5 规则 2）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// N2-f：摘要在机制中立化后消费点跨了两跳——产品配置 <c>FeishuAgentOptions.SummaryThreshold</c>
    /// → 投影 <c>ToRuntimeOptions()</c> → 中立 <c>AgentRuntimeOptions.SummaryThreshold</c>
    /// → 中立摘要器。双清单（消费点 + 投影点）必须<b>同批</b>断言，否则「投影漏字段」这类
    /// 断链会静默生效（配置写了不生效、且不报红）。
    /// </para>
    /// <para>
    /// <b>迁移拆分</b>：跨仓双清单拆成两半——本仓保留<b>投影点</b>（产品配置 →
    /// <c>ToRuntimeOptions</c>）正向锚；<b>消费点</b>（中立 <c>ConversationSummarizer</c> 消费
    /// <c>SummaryThreshold</c>）随实现迁出，由新仓
    /// <c>NeutralSourceContractGuards.FeishuAgentOptions_Phase2Properties_ShouldHaveRealConsumptionPoints</c> 持有。
    /// </para>
    /// </remarks>
    [Fact]
    public void FeishuAgentOptions_Phase2Properties_ShouldHaveRealConsumptionPoints()
    {
        var optionsSource = Path.Combine(GetSolutionRoot(), "Mud.Feishu.AI", "Agents", "FeishuAgentOptions.cs");

        File.Exists(optionsSource).Should().BeTrue();
        File.ReadAllText(optionsSource).Should().Contain(
            "ToRuntimeOptions",
            "FeishuAgentOptions 必须把摘要阈值投影进中立运行期配置（产品配置 → 中立旋钮的唯一通道）");
    }

    /// <summary>
    /// AI-FD-D12 批次 A/B 新增配置属性必须有真实消费点（R5 规则 2）：
    /// <c>MaxHistoryTokens</c>（ConversationSummarizer token 维度判定）、
    /// <c>RequireMentionInGroup</c>/<c>AllowP2pConversation</c>/<c>BotName</c>
    /// （ImMessageConversationalEventHandler 过滤）。
    /// </summary>
    /// <remarks>
    /// <b>迁移拆分</b>：<c>MaxHistoryTokens</c> 的<b>消费点</b>（中立 <c>ConversationSummarizer</c>）
    /// 随实现迁出，由新仓
    /// <c>NeutralSourceContractGuards.FeishuAgentOptions_Phase12Properties_ShouldHaveRealConsumptionPoints</c> 持有；
    /// 本仓保留产品侧三条 <c>ImConversationOptions</c> 消费点正向锚。
    /// </remarks>
    [Fact]
    public void FeishuAgentOptions_Phase12Properties_ShouldHaveRealConsumptionPoints()
    {
        var imHandlerSource = Path.Combine(
            GetSolutionRoot(), "Mud.Feishu.AI", "Events", "ImMessageConversationalEventHandler.cs");

        File.Exists(imHandlerSource).Should().BeTrue();
        var handlerContent = File.ReadAllText(imHandlerSource);
        handlerContent.Should().Contain(
            "RequireMentionInGroup",
            "ImConversationOptions.RequireMentionInGroup 必须在事件处理器中被消费（群聊 @ 过滤，P2D-5a）");
        handlerContent.Should().Contain(
            "AllowP2pConversation",
            "ImConversationOptions.AllowP2pConversation 必须在事件处理器中被消费（单聊会话开关，P2D-5a）");
        handlerContent.Should().Contain(
            "BotName",
            "ImConversationOptions.BotName 必须在事件处理器中被消费（群聊「@ 到 Bot 本人」判定，R3-3）");
    }

    // ────────────────────────────────────────────────────────────────────
    // 守卫 4：包间纵向引用治理（不允许横向引用）
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 包间纵向引用治理：**白名单形态**（R-9 修订 C7 纪律）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>原判据（R-9 前）</b>："Mud.Feishu.AI / Mud.Feishu.Redis 只允许引用 Abstractions"。
    /// R-9 把飞书 Agent **集成面**（Channels / Events / Knowledge）并入 <c>Mud.Feishu.AI</c>，
    /// 该包因此合法地需要 core（<c>IFeishu*</c> 强类型客户端）、DataModels（出/入 DTO）与
    /// EventCallback（事件 DTO）——这是决策的**显式代价**，不是漂移。
    /// </para>
    /// <para>
    /// <b>修订后的判据（按能力而非工程名）</b>：
    /// <list type="bullet">
    /// <item><c>Mud.Feishu.AI</c> 允许引用 {Abstractions, Mud.Feishu, DataModels, EventCallback}——即
    /// "纵向依赖 + 其集成面所需的 SDK 数据面"；</item>
    /// <item><c>Mud.Feishu.Redis</c> **维持**"仅 Abstractions"（不放松：它没有集成面职责）；</item>
    /// <item>横向/反向引用（AI.Tools / AI.Mcp / Webhook / WebSocket …）对两者一律禁止。</item>
    /// </list>
    /// </para>
    /// <para>
    /// 负向不变量单独成条（见 <see cref="AiPackage_ShouldNotReferenceToolSurfaceOrMcp"/> 与
    /// <see cref="ToolPackage_ShouldNotReferenceEventCallback"/>），使"防环"与"防回流"两道题的
    /// 失败原因在测试报告里可区分。
    /// </para>
    /// </remarks>
    [Fact]
    public void ImplementationPackages_ShouldOnlyReferenceAllowedSet_VerticalDependencyOnly()
    {
        // 白名单：包名 → 允许引用的工程集合（分析器形态由引用扫描本身排除，见下）。
        var allowed = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Mud.Feishu.AI.csproj"] =
            [
                "Mud.Feishu.Abstractions.csproj",
                "Mud.Feishu.csproj",
                "Mud.Feishu.DataModels.csproj",
                "Mud.Feishu.EventCallback.csproj",
                // 迁移收窄：Mud.AI.Agent / Mud.AI.Abstractions 已迁出本仓，飞书 AI 适配层现以
                // PackageReference 形态消费其程序集（不再出现在本仓 ProjectReference 白名单内，
                // 其内部包锁由新仓 AgentPackageContractGuards 持有）。
            ],
            ["Mud.Feishu.Redis.csproj"] = ["Mud.Feishu.Abstractions.csproj"],
        };

        var offenders = new List<string>();
        foreach (var csproj in Directory.GetFiles(GetSolutionRoot(), "*.csproj", SearchOption.AllDirectories)
                     .Where(p => !p.Contains("obj") && !p.Contains("bin")))
        {
            var projectName = Path.GetFileName(csproj);
            if (!allowed.TryGetValue(projectName, out var permitted))
                continue;

            var content = Regex.Replace(File.ReadAllText(csproj), "<!--.*?-->", string.Empty, RegexOptions.Singleline);
            foreach (Match match in Regex.Matches(content, @"<ProjectReference\s[^>]*/>", RegexOptions.IgnoreCase))
            {
                var element = match.Value;
                // OutputItemType="Analyzer"（ReferenceOutputAssembly=false）是编译期分析器形态，
                // 不产生运行时程序集依赖，不属于引用治理范畴（如 [AgentTool] 源生成器）。
                if (element.Contains("OutputItemType", StringComparison.OrdinalIgnoreCase)
                    || element.Contains("ReferenceOutputAssembly=\"false\"", StringComparison.OrdinalIgnoreCase))
                    continue;

                var include = Regex.Match(element, @"Include=""([^""]+)""", RegexOptions.IgnoreCase);
                if (!include.Success)
                    continue;

                var referenced = GetFileNameCrossPlatform(include.Groups[1].Value);
                if (!permitted.Contains(referenced, StringComparer.OrdinalIgnoreCase))
                    offenders.Add($"{projectName} → {referenced}");
            }
        }

        offenders.Should().BeEmpty(
            "实现包只允许其白名单内的纵向引用（Mud.Feishu.AI = {{Abstractions, core, DataModels, EventCallback}}；"
            + "Mud.Feishu.Redis = {{Abstractions}}）；横向/反向引用（AI.Tools / AI.Mcp / Webhook / WebSocket …）"
            + "会破坏「工具面依赖集成面」的单向性。违例: " + string.Join("; ", offenders));
    }

    /// <summary>
    /// <b>R-9 负向不变量 ①</b>：<c>Mud.Feishu.AI</c> <b>不得</b>引用
    /// <c>Mud.Feishu.AI.Tools</c> / <c>Mud.Feishu.AI.Mcp</c>（防环）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么单独成条而不是只靠白名单</b>：白名单失败只报"引用了不在集合里的工程"，
    /// 维护者第一反应是"把那个工程加进白名单"——而这里的正确答案恰好相反（加进去就成环）。
    /// 本条的失败信息直接给出"防环"结论，杜绝把守卫放宽当作修复。
    /// </para>
    /// <para>
    /// 环的形态：AI.Tools → AI（工具面消费集成面）；若 AI → AI.Tools 同时成立，
    /// 则编译期循环、运行期装配顺序不可判定（DI 会在解析注册表时死锁或得到半构造对象）。
    /// </para>
    /// </remarks>
    [Fact]
    public void AiPackage_ShouldNotReferenceToolSurfaceOrMcp()
    {
        var csproj = Path.Combine(GetSolutionRoot(), "Mud.Feishu.AI", "Mud.Feishu.AI.csproj");
        var content = Regex.Replace(File.ReadAllText(csproj), "<!--.*?-->", string.Empty, RegexOptions.Singleline);

        content.Should().NotContain(
            "Mud.Feishu.AI.Tools.csproj",
            "R-9 不变量：AI.Tools 依赖 AI（单向）——AI 反向引用工具面即成环，编译期直接拒绝而非放宽守卫");
        content.Should().NotContain(
            "Mud.Feishu.AI.Mcp.csproj",
            "R-9 不变量：MCP 是 AI 的下游消费者（AI.Mcp → AI）；反向引用成环");
    }

    /// <summary>
    /// <b>R-9 负向不变量 ②</b>：<c>Mud.Feishu.AI.Tools</c> <b>不得</b>再引用
    /// <c>Mud.Feishu.EventCallback</c>（防回流）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// R-9 的收益之一就是"只接工具面"的宿主不再被动背上事件 DTO 面（EventCallback 的 880 个源文件）。
    /// 该收益**只由一个事实维持**：工具包里没有任何消费事件 DTO 的类型。若日后有人把某个事件处理器
    /// （或一条 `using Mud.Feishu.EventCallback;`）搬回工具包，收益会静默消失（构建照常通过），
    /// 故在此机械锁定。
    /// </para>
    /// </remarks>
    [Fact]
    public void ToolPackage_ShouldNotReferenceEventCallback()
    {
        var csproj = Path.Combine(GetSolutionRoot(), "Mud.Feishu.AI.Tools", "Mud.Feishu.AI.Tools.csproj");
        var content = Regex.Replace(File.ReadAllText(csproj), "<!--.*?-->", string.Empty, RegexOptions.Singleline);

        content.Should().NotContain(
            "Mud.Feishu.EventCallback.csproj",
            "R-9 防回流：集成面（含事件 DTO 消费）已并入 Mud.Feishu.AI——工具包重新引用 EventCallback "
            + "会把事件 DTO 面拉回工具包，静默抹掉 R-9 的依赖面收益");
    }

    /// <summary>
    /// 回归锁：<c>ProjectReference Include</c> 用 Windows 反斜杠书写时，守卫取文件名必须与平台无关
    /// （Linux CI 上 <see cref="Path.GetFileName(string)"/> 会把整条相对路径当成文件名，
    /// 使守卫把合法的纵向引用误报为横向引用；本地 Windows 无法复现该缺陷）。
    /// </summary>
    [Theory]
    [InlineData(@"..\Mud.Feishu.Abstractions\Mud.Feishu.Abstractions.csproj")]
    [InlineData("../Mud.Feishu.Abstractions/Mud.Feishu.Abstractions.csproj")]
    [InlineData("Mud.Feishu.Abstractions.csproj")]
    public void GetFileNameCrossPlatform_ShouldHandleBothSeparatorStyles(string include)
    {
        GetFileNameCrossPlatform(include).Should().Be("Mud.Feishu.Abstractions.csproj");
    }

    // ────────────────────────────────────────────────────────────────────
    // 守卫 5：会话存储配置面（R5）——SectionName 必须真的被 GetSection 使用
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="FeishuConversationOptions.SectionName"/> 必须在生产源码中被
    /// <c>GetSection</c> 使用（配置节绑定真实存在，R5/X1 回归锁）。
    /// </summary>
    [Fact]
    public void FeishuConversationOptions_SectionName_ShouldBeUsedByGetSection()
    {
        var aiSources = Directory
            .GetFiles(Path.Combine(GetSolutionRoot(), "Mud.Feishu.AI"), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains("obj") && !p.Contains("bin"))
            .ToList();

        aiSources.Should().NotBeEmpty();

        aiSources.Any(path =>
            {
                var content = File.ReadAllText(path);
                return content.Contains("FeishuConversationOptions.SectionName", StringComparison.Ordinal)
                    && content.Contains("GetSection", StringComparison.Ordinal);
            })
            .Should().BeTrue("FeishuConversationOptions.SectionName 必须被 GetSection(...) 真正使用（R5/X1）");
    }

    // ────────────────────────────────────────────────────────────────────
    // 守卫 6：顺序类缺陷回归锁（P0-1 / P2-1 / P2-2）
    // ────────────────────────────────────────────────────────────────────

    /// <summary>飞书 AI 适配层工程名（产品语义所在）。</summary>
    private const string AiProject = "Mud.Feishu.AI";

    /// <summary>
    /// 按<b>工程名 + 相对路径</b>读取生产源码。
    /// </summary>
    /// <remarks>
    /// <b>为什么必须显式给工程名（H4′）</b>：P1-H2/H3 把可复用机制搬出 <c>Mud.Feishu.AI</c> 后，
    /// 原实现（硬编码 <c>Mud.Feishu.AI</c>）会让断言去读<b>已经不存在的文件</b>——
    /// 要么直接抛（响亮），要么更隐蔽地读到空壳。显式工程名把"这条断言锚在谁的实现上"
    /// 变成源码上可见的事实，搬迁时无法漏改。
    /// </remarks>
    private static string ReadSource(string projectName, params string[] relativeParts)
    {
        var parts = new string[relativeParts.Length + 2];
        parts[0] = GetSolutionRoot();
        parts[1] = projectName;
        relativeParts.CopyTo(parts, 2);
        return File.ReadAllText(Path.Combine(parts));
    }

    /// <summary>
    /// <c>FeishuAgent.Name</c> 必须 <c>override</c>，禁止用 <c>new</c> 遮蔽（P2-1）。
    /// </summary>
    [Fact]
    public void FeishuAgent_Name_ShouldBeOverrideNotHide()
    {
        var source = ReadSource(AiProject, "Agents", "FeishuAgent.cs");

        source.Should().Contain("override string? Name",
            "AIAgent.Name 是 virtual：override 才能让经 AIAgent 引用（含 MAF 内部诊断）取到真实名字");
        source.Should().NotContain("new string Name", "new 遮蔽会让基类属性恒为 null");
    }

    /// <summary>
    /// <c>FeishuAgent.GetService</c> 必须**先判自身**再退内层（P2-2）。
    /// </summary>
    /// <remarks>
    /// <b>H1 起改为行为断言（锁语义，不锁文本）</b>：委托成员已由上游
    /// <see cref="DelegatingAIAgent"/> 提供，本类不再手写 <c>GetService</c>——
    /// 原源码文本判据（<c>IsInstanceOfType(this)</c> 早于 <c>_innerAgent.GetService(</c>）
    /// 在删除覆写后必然失真。行为判据等价且抗重构：
    /// <c>GetService(typeof(AIAgent))</c> 必须返回<b>外层实例</b>（命中内层即绕过飞书遥测/摘要）。
    /// </remarks>
    [Fact]
    public void FeishuAgent_GetService_ShouldReturnOuterInstance()
    {
        var options = new FeishuAgentOptions
        {
            Name = "guard-agent",
            Instructions = "x",
        };
        var agent = new FeishuAgent(new Mock<Microsoft.Extensions.AI.IChatClient>().Object, options);

        agent.GetService(typeof(AIAgent)).Should().BeSameAs(agent,
            "GetService(typeof(AIAgent)) 必须返回外层 FeishuAgent；命中内层 ChatClientAgent 会让宿主"
            + "绕过飞书遥测/摘要（上游 DelegatingAIAgent 的「先判自身」次序是本不变式的前提）");

        agent.GetService(typeof(FeishuAgent)).Should().BeSameAs(agent, "自身类型必须命中自身");
        agent.GetService(typeof(AIAgent), "some-key").Should().NotBeSameAs(agent, "带键查询必须退内层（键语义未知）");
    }

    /// <summary>
    /// MAF 状态键必须唯一且被真实消费（防 ChatHistoryProvider 与 AIContextProvider 状态键冲突）。
    /// </summary>
    /// <remarks>
    /// MAF <c>ChatClientAgent.ValidateAndCollectStateKeys</c> 在运行期对重复状态键直接抛异常；
    /// 本守卫把该失败提前到测试期。<c>CompactionProvider</c> 已评估后不采用（B3.1 否决），
    /// 故当前只应存在历史键一个状态键常量。
    /// </remarks>
    [Fact]
    public void AgentStateKeys_ShouldBeUniqueAndConsumed()
    {
        var source = ReadSource(AiProject, "Agents", "FeishuAgent.cs");

        var keys = Regex.Matches(source, @"const string (\w*StateKey) = ""([^""]+)""")
            .Select(m => m.Groups[2].Value)
            .ToList();

        keys.Should().NotBeEmpty("ChatHistoryProvider 的状态键必须显式命名（默认键名随类型名漂移）");
        keys.Should().OnlyHaveUniqueItems("MAF 要求会话状态键全局唯一（重复即运行期异常）");
        keys.Should().Contain("feishu.agent.history", "历史状态键是会话摘要与历史读写的同源锚点");

        // A1（v3）：消费点从组装语句升级为「两条链各自可查」——中立工厂把键以参数装入 Provider
        // （不得硬编码字面量），产品侧必须经该工厂装配（否则 Provider 写的状态袋与门面读的不是同一格）。
        // 中立半边的断言（StateKey = historyStateKey / 中立 Agents 目录零产品字面量）随实现迁出，
        // 由新仓 NeutralSourceContractGuards.AgentStateKeys_ShouldBeUniqueAndConsumed 持有。
        source.Should().Contain("AgentAssemblyFactory.Create(",
            "飞书侧必须经中立装配工厂构造内层 Agent（否则历史状态键的注入面又回到产品侧）");

        // P4-4：outbox 状态键必须成对存在且**有真实消费点**——否则键会成为无人读取的僵尸状态，
        // 占会话存储且给后续维护者造成"这里似乎实现了补发"的误判。
        keys.Should().Contain("feishu.agent.pending_reply");
        keys.Should().Contain("feishu.agent.pending_reply_turn");

        var handlerSource = ReadSource(AiProject, "Events", "ConversationalFeishuEventHandler.cs");
        handlerSource.Should().Contain("PendingReplyStateKey",
            "outbox 状态键必须在会话处理器中被真实消费（补发/写入/摘除）");
        handlerSource.Should().Contain("PendingReplyTurnStateKey");

        // P1-H3 / D4：状态键的**字面量**是产品事实（留在上方的 FeishuAgent 常量里），
        // 机制（读取状态袋）在**中立包**的会话门面里——中立门面以参数接收该键、且不得出现
        // `feishu.` 前缀的断言随实现迁出（见新仓 NeutralSourceContractGuards）。

        // 飞书侧必须真的把键**注入**门面（否则参数化只是签名装饰）。
        source.Should().Contain("ChatHistoryStateKey",
            "飞书侧必须把历史状态键注入中立门面，否则门面读的状态袋与 Provider 写的不是同一格");
    }

    // ────────────────────────────────────────────────────────────────────
    // 守卫 7：R2 批次缺陷回归锁（R2-1 ~ R2-6 / R2-12）
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// R2-1（P0）/ WP3：SDK 不签发/不校验任何确认凭据——模型可见载荷中
    /// <b>绝不</b>出现 confirm_token、令牌明文或自批复指示。
    /// </summary>
    /// <remarks>
    /// <para>
    /// WP3 删除了自研确认令牌全部过渡层（ToolConfirmationToken / IToolConfirmationTokenSecretProvider）。
    /// HITL 的批准状态所有权归宿主授权器（IToolExecutionAuthorizer），SDK 执行链恒为中性拒绝。
    /// </para>
    /// <para>
    /// <b>迁移拆分</b>：判据 ①（模型可见文案的凭据零命中）的锚点原文案已随错误契约族中立化上移
    /// <c>Mud.AI.Agent/Tools/ToolErrorText.cs</c>，该半边的括号配平切片断言随实现迁出，由新仓
    /// <c>NeutralSourceContractGuards.ConfirmationToken_ShouldNeverEnterModelVisiblePayload</c> 持有；
    /// 本仓保留判据 ②——飞书侧绑定（宿主批准通道）契约正向锚。
    /// </para>
    /// </remarks>
    [Fact]
    public void ConfirmationToken_ShouldNeverEnterModelVisiblePayload()
    {
        var bindingSource = Path.Combine(
            GetSolutionRoot(), "Mud.Feishu.AI.Tools", "Tools", "FeishuToolBinding.cs");
        File.Exists(bindingSource).Should().BeTrue();

        // ② 批准状态归宿主授权器，SDK 不签发任何凭据（锚点：飞书侧绑定 —— v2.3 未随骨架上移）。
        var binding = File.ReadAllText(bindingSource);
        binding.Should().Contain("ToolApprovalRequest", "待确认事件必须经 ToolApprovalRequest 投递给宿主");
        binding.Should().Contain("IFeishuToolApprovalChannel", "必须存在宿主批准通道契约（未注册即 fail-closed）");
        binding.Should().NotContain("ToolConfirmationToken", "WP3 已删除自研确认令牌，不得重新引入");
        binding.Should().NotContain("IToolConfirmationTokenSecretProvider", "WP3 已删除令牌密钥提供者契约");
        binding.Should().NotContain("#pragma warning disable CS0618", "WP3 已删除令牌过渡层，不再需要 CS0618 抑制");
    }

    /// <summary>
    /// R3-02 自证报红：合成缺陷源码（令牌提示放在续行）必须被发现。
    /// </summary>
    [Fact]
    public void ConfirmationToken_Guard_ShouldDetectCredentialInContinuationLine()
    {
        // 合成缺陷源码：NeedsConfirmation 分支跨两行，续行含 confirm_token。
        var defectiveSource = @"
        ToolErrorCategory.Confirmation => $""[tool_error] {toolName} (needs_confirmation): {reason}——该操作需要用户确认后方可执行；""
            + ""可使用 confirm_token=xxx 重试"",
";
        var needsConfirmationBranch = defectiveSource.IndexOf(
            "ToolErrorCategory.Confirmation =>", StringComparison.Ordinal);
        needsConfirmationBranch.Should().BeGreaterThan(-1);

        // 用与主守卫相同的括号配平切片逻辑。
        var sliceStart = defectiveSource.IndexOf('"', needsConfirmationBranch);
        var depth = 0;
        var sliceEnd = sliceStart;
        for (var i = sliceStart; i < defectiveSource.Length; i++)
        {
            var ch = defectiveSource[i];
            if (ch == '(') depth++;
            else if (ch == ')') { if (depth == 0) break; depth--; }
            else if (ch == ';' && depth == 0) { sliceEnd = i; break; }
            sliceEnd = i;
        }
        var branch = defectiveSource[needsConfirmationBranch..(sliceEnd + 1)];

        branch.Should().Contain("confirm_token",
            "合成缺陷源码的续行含 confirm_token——切片必须覆盖跨行内容（自证报红）");
    }

    /// <summary>
    /// R4-1：写类工具的 NeedsUserConfirmation <b>不得</b>在授权门禁被放行。
    /// </summary>
    /// <remarks>
    /// WP3 的 <c>Pass()</c> 特例（"MAF 已前置批准 ⇒ 走到这里即人已批准"）在运行期无任何校验：
    /// <c>ApprovalRequiredAIFunction</c> 是 MEAI <b>纯标记类型</b>，拦截只在 <c>FunctionInvokingChatClient</c>
    /// 内生效——宿主直接 <c>InvokeAsync</c> 或绕开该管线时，写工具会被静默放行。
    /// 批准状态的唯一所有者是宿主 <c>IToolExecutionAuthorizer</c>。
    /// </remarks>
    [Fact]
    public void WriteTool_NeedsUserConfirmation_ShouldBeDenied_AtAuthorizeGate()
    {
        var binding = File.ReadAllText(Path.Combine(
            GetSolutionRoot(), "Mud.Feishu.AI.Tools", "Tools", "FeishuToolBinding.cs"));

        binding.Should().NotContain("AuthorizationDecision.NeedsUserConfirmation && tool.IsWrite",
            "写类工具的待确认不得据此放行（R4-1 fail-closed）——该特例必须删除，否则直调路径静默放行写操作");

        binding.Should().Contain("AuthorizationDecision.NeedsUserConfirmation =>",
            "NeedsUserConfirmation 必须统一走下面的挂起解析分支（读写工具同一条路径）");
        binding.Should().Contain(
            "ResolveNeedsConfirmationAsync(tool, arguments, context, result.Reason, cancellationToken)",
            "挂起解析调用点必须保留——宿主授权器是批准状态的唯一所有者");
    }

    /// <summary>
    /// R4-4：两处 Readme 不得再把「自研确认令牌」描述为现存机制（WP3 已整条删除），
    /// 也不得再声明「写工具放行」（R4-1 已删除该特例）。
    /// </summary>
    /// <remarks>
    /// 只锁<b>文档与实现的一致性</b>：允许「已删除 / 不再 / 不含」等否定语境
    /// （文档必须解释"为什么没有了"，那不是漂移），命中令牌词却<b>无</b>否定词才算漂移。
    /// 两条文档（底座 + 工具面）都扫描——R4-4 的缺陷正是底座 Readme 与工具 Readme 各自
    /// 残留了一套过期 HITL 描述（只改其一仍会误导宿主）。
    /// </remarks>
    [Fact]
    public void Readmes_ShouldNotDescribeConfirmToken()
    {
        var root = GetSolutionRoot();
        var readmes = new[]
        {
            Path.Combine(root, "Mud.Feishu.AI", "Readme.md"),
            Path.Combine(root, "Mud.Feishu.AI.Tools", "Readme.md"),
        };

        // 否定语境标记：命中其一即视为"文档在说明该机制已不存在"，不算漂移。
        string[] negationMarkers =
        [
            "已删除", "已移除", "整条删除", "删除", "移除", "不再", "废弃", "从未", "不含", "没有", "不得", "禁止",
        ];
        // 令牌词：出现即要求同行有否定语境（防止把已删除机制重新描述为可用）。
        string[] credentialMarkers = ["confirm_token", "ToolConfirmationToken", "确认令牌"];
        // R4-1：写工具放行特例已删除，文档不得再声明"必须保留"这一放行旁路。
        const string passMarker = "Pass()";

        var offenders = new List<string>();

        foreach (var readme in readmes)
        {
            File.Exists(readme).Should().BeTrue($"文档守卫必须能读到 {readme}（宁可失败，也不要假绿）");
            var lines = File.ReadAllLines(readme);

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var location = $"{Path.GetFileName(readme)}:{i + 1}";

                if (credentialMarkers.Any(m => line.Contains(m, StringComparison.Ordinal)))
                {
                    var negated = negationMarkers.Any(m => line.Contains(m, StringComparison.Ordinal));
                    if (!negated)
                        offenders.Add($"{location} 描述了令牌机制但无否定语境：{line.Trim()}");
                }

                if (line.Contains(passMarker, StringComparison.Ordinal)
                    && line.Contains("必须保留", StringComparison.Ordinal))
                {
                    offenders.Add($"{location} 仍声明写工具放行旁路「必须保留 {passMarker}」（R4-1 已删除）：{line.Trim()}");
                }
            }
        }

        offenders.Should().BeEmpty(
            "R4-4：两处 Readme 不得再把自研确认令牌描述为现存机制，也不得再声明写工具放行特例。"
            + " 违例: " + string.Join("; ", offenders));
    }

    /// <summary>
    /// R2-3：AppKey 缺失 × 工具链已装配 必须 fail-fast（并保留逃生门）。
    /// </summary>
    [Fact]
    public void AppKeyDegrade_ShouldFailFast_WhenToolChainPresent()
    {
        var source = ReadSource(AiProject, "Events", "ConversationalFeishuEventHandler.cs");

        var degradeBranch = source.IndexOf("if (string.IsNullOrWhiteSpace(request.AppKey))", StringComparison.Ordinal);
        degradeBranch.Should().BeGreaterThan(-1);
        // R3-10：括号配平切片取代固定字符窗口 +2600（消假红——代码增长即红）。
        var branch = SliceBraceBalanced(source, degradeBranch);

        branch.Should().Contain("ToolContextAccessor is not null", "必须以「工具链已装配」为判据");
        branch.Should().Contain("AllowToolsWithoutAppKey", "必须提供显式逃生门（默认 fail-closed）");

        source.Should().Contain("protected virtual bool AllowToolsWithoutAppKey => false",
            "默认必须 fail-closed：工具面依赖非空 appKey，降级形态下工具/知识必然 100% 失败");
    }

    /// <summary>
    /// R2-5：<c>MemoryConversationStore</c> 必须清扫不再被读取的过期条目。
    /// </summary>
    /// <remarks>
    /// <b>迁移拆分</b>：清扫机制（<c>SweepExpired</c> / <c>SweepThreshold</c>）随实现迁入中立包
    /// <c>Mud.AI.Agent.Sessions.MemorySessionStore</c>，其回收路径断言由新仓
    /// <c>NeutralSourceContractGuards.MemorySessionStore_ShouldSweepExpiredEntries</c> 持有；
    /// 本仓保留产品壳的「桥接而非重抄机制」正/负向锚。
    /// </remarks>
    [Fact]
    public void MemoryConversationStore_ShouldSweepExpiredEntries()
    {
        // 产品壳只做「冻结契约 → 中立实现 + 产品 TTL」的适配，不得把机制抄回来（两份实现必然漂移）。
        var shell = ReadSource(AiProject, "Conversations", "MemoryConversationStore.cs");
        shell.Should().Contain("MemorySessionStore", "产品壳必须桥接到中立实现（机制唯一）");
        shell.Should().NotContain("SweepExpired", "清扫逻辑不得回流产品壳");
    }

    /// <summary>
    /// R2-6：<c>FeishuAgent.Id</c> 必须与内层 Agent 同源（对齐 MAF <c>DelegatingAIAgent</c> 约定）。
    /// </summary>
    /// <remarks>
    /// <b>H1 起改为反射断言（锁语义，不锁文本）</b>：委托成员已由上游
    /// <see cref="DelegatingAIAgent"/> 提供（其 <c>IdCore =&gt; InnerAgent.Id</c>），
    /// 本类不再手写 <c>IdCore</c>——原源码文本判据 <c>"IdCore =&gt; _innerAgent.Id"</c>
    /// 在删除覆写后必然失真且会误导维护者"把委托成员写回来"。
    /// 不变式的真实形态是「本类是 <see cref="DelegatingAIAgent"/> 的派生」：只要成立，
    /// <c>Id</c> 就恒等于内层 <c>ChatClientAgent.Id</c>（否则 <c>AgentResponse.AgentId</c>
    /// 与 <c>FeishuAgent.Id</c> 指向不同标识，遥测/编排按 AgentId 关联时出现「未知 Agent」）。
    /// 行为侧由 <c>FeishuAgentTests.Id_ShouldMatchInnerAgent_SoResponseAgentIdIsTraceable</c> 覆盖。
    /// </remarks>
    [Fact]
    public void FeishuAgent_ShouldDelegateIdentityToInnerAgent()
    {
        typeof(DelegatingAIAgent).IsAssignableFrom(typeof(FeishuAgent)).Should().BeTrue(
            "FeishuAgent 必须派生自上游 DelegatingAIAgent（委托语义的类型级对齐）；"
            + "一旦退回 AIAgent + 手写转发，Id/Name/GetService/会话三钩子/Run 两钩子的"
            + "一致性就退化为「注释级约定」，无人机械保证");

        typeof(FeishuAgent).GetProperty(nameof(AIAgent.Name))!.DeclaringType.Should().Be(
            typeof(FeishuAgent),
            "Name 仍必须由本类覆写（产品名字的所有者是本类，不是内层）");

        // 判据自证（防过滤器恒真假绿）：一个**不**派生自 DelegatingAIAgent 的类型必须判 false。
        typeof(DelegatingAIAgent).IsAssignableFrom(typeof(AgentContractGuards)).Should().BeFalse(
            "反例必须被拒——否则上面那条断言只是恒真的装饰");
    }

    /// <summary>
    /// R2-12：<c>MaxHistoryMessages</c> 的<b>真实语义</b>必须被文档化。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 旧注释称「超出窗口的旧消息被折叠」，实测为「不可逆落库删除 + 工具消息剔除 + 仅保留首条 system」；
    /// 文档漂移会让宿主误判「模型忘了刚调过的工具」。本守卫防其再次漂移。
    /// </para>
    /// <para>
    /// <b>迁移拆分</b>：中立工厂的消费点（<c>MessageCountingChatReducer</c> 裁剪窗）随实现迁出，
    /// 由新仓 <c>NeutralSourceContractGuards.HistoryReducerSemantics_ShouldBeDocumented</c> 持有；
    /// 本仓保留「文档语义 + 产品侧注入」两半正向锚。
    /// </para>
    /// </remarks>
    [Fact]
    public void HistoryReducerSemantics_ShouldBeDocumented()
    {
        var optionsSource = ReadSource(AiProject, "Agents", "FeishuAgentOptions.cs");

        var property = optionsSource.IndexOf("public int MaxHistoryMessages", StringComparison.Ordinal);
        property.Should().BeGreaterThan(-1);
        var docs = SliceBraceBalanced(optionsSource, property, backwards: true, maxBack: 2000);

        docs.Should().Contain("非 system 消息", "必须写明只统计非 system 消息");
        docs.Should().Contain("不可逆", "必须写明裁剪结果写回会话状态（不可逆落库删除），而非「本次请求视图」");
        docs.Should().Contain("FunctionCallContent", "必须写明工具调用消息被永久丢弃");

        // 消费点（此前缺失守卫；A1（v3）后落点在中立工厂，产品侧只注入该上限）。
        ReadSource(AiProject, "Agents", "FeishuAgent.cs").Should().Contain(
            "options.MaxHistoryMessages",
            "产品侧必须把 MaxHistoryMessages 注入中立装配工厂（否则配置上限成为无人消费的装饰）");
    }

    /// <summary>
    /// 跨平台取文件名。csproj 里的 <c>ProjectReference Include</c> 普遍写成 Windows 反斜杠相对路径
    /// （如 <c>..\Mud.Feishu.Abstractions\Mud.Feishu.Abstractions.csproj</c>），而
    /// <see cref="Path.GetFileName(string)"/> 只按 <see cref="Path.DirectorySeparatorChar"/> 切分：
    /// Linux（CI）上 '/' 才是分隔符、'\' 是合法文件名字符，整条相对路径会被当作文件名返回，
    /// 使「只允许纵向引用」守卫把合法的 → Abstractions 引用误报为横向引用（本地 Windows 无法复现）。
    /// 因此这里不依赖平台分隔符，显式按两种分隔符取最后一段。
    /// </summary>
    private static string GetFileNameCrossPlatform(string path)
    {
        var lastSeparator = path.LastIndexOfAny(PathSeparators);
        return lastSeparator < 0 ? path : path[(lastSeparator + 1)..];
    }

    private static readonly char[] PathSeparators = new[] { '\\', '/' };

    private static string GetSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir) && !File.Exists(Path.Combine(dir, "Mud.Feishu.slnx")))
        {
            dir = Path.GetDirectoryName(dir);
        }

        dir.Should().NotBeNull("找不到解决方案根目录时本守卫无法工作（宁可失败，也不要假绿）");
        return dir!;
    }

    /// <summary>
    /// R3-10：括号配平切片——从 <paramref name="start"/> 开始（或向前），
    /// 做大括号深度扫描取方法体/文档块，取代固定字符窗口。
    /// </summary>
    /// <param name="source">源码文本。</param>
    /// <param name="start">起始索引。</param>
    /// <param name="backwards">是否向前扫描（取 start 之前的内容）。</param>
    /// <param name="maxBack">向前扫描的最大字符数。</param>
    private static string SliceBraceBalanced(string source, int start, bool backwards = false, int maxBack = 0)
    {
        if (backwards)
        {
            var backStart = Math.Max(0, start - maxBack);
            return source[backStart..start];
        }

        var depth = 0;
        var end = start;
        for (var i = start; i < source.Length; i++)
        {
            var ch = source[i];
            if (ch == '{') depth++;
            else if (ch == '}') { depth--; if (depth == 0) { end = i + 1; break; } }
            end = i + 1;
        }
        return source[start..end];
    }
}
