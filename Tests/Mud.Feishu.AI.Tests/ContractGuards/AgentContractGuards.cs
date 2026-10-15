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
    // 守卫 2：仅 Mud.Feishu.AI 可直接引用 MAF 包（依赖方向铁律）
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 核心项目（Mud.Feishu 等）不得直接声明 Microsoft.Agents.AI* 引用——
    /// MAF 只允许进入 <b>白名单</b>{<c>Mud.Feishu.AI</c>（产品适配层）、<c>Mud.AI.Agent</c>（AI 运行期）}
    /// （总体设计 §3.1：AI → 核心单向，核心不被 Agent 运行时污染）。
    /// 其他项目经项目引用<b>传递</b>获得（如 Mud.Feishu.Redis），不违反本守卫。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>P1-H3 的收敛式放宽（白名单从 1 → 2）</b>：MAF 是<b>产品中立</b>的运行时框架，
    /// 而 <c>Mud.AI.Agent</c> 的定位就是"AI 运行期"（会话门面 <c>AgentSessionStore</c> 需要
    /// <c>AIAgent</c> 做序列化归属方）。不放宽则会话管线无法中立化。
    /// </para>
    /// <para>
    /// <b>放宽必须配反向断言，否则是净放松</b>（见 <see cref="MudAiAbstractions_ShouldNotDeclareMafPackages"/>
    /// 与非 AI 的 <c>Mud.Feishu*</c> 工程仍被本条拒绝）：
    /// ① <c>Mud.AI.Abstractions</c> 是编译期契约层，被源生成器剖面与声明契约消费，必须保持零 MAF；
    /// ② 全部核心 SDK 工程（含 Mud.Feishu.WebSocket / Webhook / Redis …）仍禁直接声明；
    /// ③ MAF 版本仍由 <see cref="AgentsAi_PackageReferences_ShouldBeSingleVersion"/> 锁单点。
    /// </para>
    /// </remarks>
    [Fact]
    public void AgentsAi_PackageReferences_ShouldBeDeclaredOnlyInAllowListedProjects()
    {
        // MAF 允许集（收敛式放宽，非放开）：产品适配层 + AI 运行期。新增成员必须同批扩本集合，
        // 并在 MudAiAbstractions_ShouldNotDeclareMafPackages 处论证"为什么它也需要 MAF"。
        string[] allowed = ["Mud.Feishu.AI.csproj", "Mud.AI.Agent.csproj"];

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

    /// <summary>
    /// <b>P1-H3 反向断言</b>：<c>Mud.AI.Abstractions</c> 必须保持<b>零 MAF</b>。
    /// </summary>
    /// <remarks>
    /// 该包是最底层的编译期契约层：源生成器按 <c>Mud.AI.*</c> 硬编码命名空间发现剖面、
    /// 声明契约（<c>[AgentTool]</c>/<c>[ToolParameter]</c>/<c>[ToolHandler]</c>）被 233 个策展接口消费。
    /// 它一旦引入 MAF，生成器被测工程与全部非 AI 工程都会被拖进 Agent 运行时的版本面——
    /// 这正是「MAF 单点直引」原铁律要防的事，放宽白名单不应把它一并放开。
    /// </remarks>
    [Fact]
    public void MudAiAbstractions_ShouldNotDeclareMafPackages()
    {
        var csproj = Path.Combine(GetSolutionRoot(), "Mud.AI.Abstractions", "Mud.AI.Abstractions.csproj");
        File.Exists(csproj).Should().BeTrue();

        StripXmlComments(File.ReadAllText(csproj)).Should().NotContain(
            "Microsoft.Agents.AI",
            "Mud.AI.Abstractions 是编译期契约层（被源生成器与声明契约消费），必须保持零 MAF 依赖");
    }

    // ────────────────────────────────────────────────────────────────────
    // 守卫 2b：独立产品线纪律——Mud.AI.* 不得声明 Mud.HttpUtils 组件
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>独立产品线纪律（2026-10-14）</b>：<c>Mud.AI.*</c> 工程不得声明 <c>Mud.HttpUtils</c> 组件。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么必须机械锁定</b>：这条纪律此前是<b>敞口</b>——既有中立性守卫只断言
    /// <c>Mud.AI.*</c> ↛ <c>Mud.Feishu*</c>（见
    /// <c>SingleToolSurfaceEngineContractGuards.MudAiFamily_ShouldNotDependOnFeishuProjects</c>），
    /// 而 <c>Mud.HttpUtils</c> 作为"全仓单版本 HTTP 组件"被默认当成无害依赖，
    /// <c>Mud.AI.Agent</c> 遂长期直接判定 <c>Mud.HttpUtils.ApiException</c>。后果有三：
    /// ① P3 上游化（迁独立 Mud.AI 仓）时该包依赖被一并带走，独立产品线无法独立发布；
    /// ② 版本被 <c>TokenMultiAppContractGuards</c> 的「同一组件单版本」契约反向牵制；
    /// ③ 中立层认识了产品的具体实现类型——与 <c>ISessionStore</c> 端口同一条纪律相悖。
    /// </para>
    /// <para>
    /// <b>修法</b>（不是删功能）：把"状态码"这一跨产品事实经中立端口
    /// <c>Mud.AI.Abstractions.Tools.Errors.ITransportFault</c> 传入，具体异常类型的识别归产品适配层
    /// （飞书侧 = <c>Mud.Feishu.AI.AgentTools.FeishuTransportFaultAdapter</c>，
    /// 端到端接线由 <c>ToolErrorContractGuards.TransportForbidden_ShouldBeClassifiedAsAuthorization</c> 锁定）。
    /// </para>
    /// <para>
    /// <b>射程</b>：只禁<b>组件本身</b>（<c>Mud.HttpUtils</c>）。子包（<c>.Attributes</c>/<c>.Abstractions</c>）
    /// 可能是声明契约面的需要，若要引入须在此处显式扩射程并补论证（判据 <see cref="DeclaresMudHttpUtilsComponent"/>
    /// 的自证用例已把"子包不算命中"钉死，扩射程必须同时改它）。
    /// </para>
    /// </remarks>
    [Fact]
    public void MudAiFamily_ShouldNotDeclareMudHttpUtilsComponent()
    {
        var offenders = ProjectFiles()
            .Where(static path => Path.GetFileName(path).StartsWith("Mud.AI.", StringComparison.Ordinal)
                && !IsUnderTestsDirectory(path))
            .Where(path => DeclaresMudHttpUtilsComponent(File.ReadAllText(path)))
            .Select(Path.GetFileName)
            .ToArray();

        offenders.Should().BeEmpty(
            "Mud.AI.* 是可脱离 Mud 组件独立发布的 AI 产品线：不得声明 Mud.HttpUtils 组件"
            + "（跨产品事实经中立端口传入，适配实现留在产品层）。违规工程：{0}",
            string.Join(" / ", offenders));
    }

    /// <summary>判据自证：只命中组件本身；注释掉的引用与子包不得命中（否则守卫会假红/假绿）。</summary>
    [Theory]
    [InlineData("<Project><ItemGroup><PackageReference Include=\"Mud.HttpUtils\" Version=\"3.0.6\" /></ItemGroup></Project>", true)]
    [InlineData("<Project><ItemGroup><!-- <PackageReference Include=\"Mud.HttpUtils\" Version=\"3.0.6\" /> --></ItemGroup></Project>", false)]
    [InlineData("<Project><ItemGroup><PackageReference Include=\"Mud.HttpUtils.Attributes\" Version=\"3.0.6\" /></ItemGroup></Project>", false)]
    [InlineData("<Project><ItemGroup><ProjectReference Include=\"..\\Mud.AI.Abstractions\\Mud.AI.Abstractions.csproj\" /></ItemGroup></Project>", false)]
    public void MudHttpUtilsComponentJudge_ShouldFlagOnlyTheExactComponent(string csprojContent, bool expected)
        => DeclaresMudHttpUtilsComponent(csprojContent).Should().Be(expected);

    /// <summary>
    /// 判据：声明了<b>组件本身</b>（<c>Include="Mud.HttpUtils"</c>——模式含闭引号，
    /// 故 <c>Mud.HttpUtils.Attributes</c> 这类子包不会命中）。先剔除 XML 注释：注释不是声明。
    /// </summary>
    private static bool DeclaresMudHttpUtilsComponent(string csprojContent)
        => Regex.IsMatch(StripXmlComments(csprojContent), @"Include=""Mud\.HttpUtils""");

    /// <summary>测试工程（<c>Tests/</c> 下）不受本纪律约束：它们要驱动组件、且不随产品线发布。</summary>
    private static bool IsUnderTestsDirectory(string path)
        => path.Contains(
            $"{Path.DirectorySeparatorChar}Tests{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase);

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
    /// N2-f：摘要在机制中立化后消费点跨了两跳——产品配置 <c>FeishuAgentOptions.SummaryThreshold</c>
    /// → 投影 <c>ToRuntimeOptions()</c> → 中立 <c>AgentRuntimeOptions.SummaryThreshold</c>
    /// → 中立摘要器。双清单（消费点 + 投影点）必须<b>同批</b>断言，否则「投影漏字段」这类
    /// 断链会静默生效（配置写了不生效、且不报红）。
    /// </remarks>
    [Fact]
    public void FeishuAgentOptions_Phase2Properties_ShouldHaveRealConsumptionPoints()
    {
        var summarizerSource = Path.Combine(
            GetSolutionRoot(), "Mud.AI.Agent", "Conversations", "ConversationSummarizer.cs");
        var optionsSource = Path.Combine(GetSolutionRoot(), "Mud.Feishu.AI", "Agents", "FeishuAgentOptions.cs");

        File.Exists(summarizerSource).Should().BeTrue(
            "摘要器必须存在于中立包（N2-f 已中立化）——路径变了要同批改本守卫，否则它会在旧路径上假绿");
        File.ReadAllText(summarizerSource).Should().Contain(
            "SummaryThreshold",
            "AgentRuntimeOptions.SummaryThreshold 必须在会话摘要器中被消费（渐进式摘要阈值，Phase 2 §3.2）");

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
    [Fact]
    public void FeishuAgentOptions_Phase12Properties_ShouldHaveRealConsumptionPoints()
    {
        var summarizerSource = Path.Combine(
            GetSolutionRoot(), "Mud.AI.Agent", "Conversations", "ConversationSummarizer.cs");
        var imHandlerSource = Path.Combine(
            GetSolutionRoot(), "Mud.Feishu.AI", "Events", "ImMessageConversationalEventHandler.cs");

        File.Exists(summarizerSource).Should().BeTrue(
            "摘要器必须存在于中立包（N2-f 已中立化）");
        File.ReadAllText(summarizerSource).Should().Contain(
            "MaxHistoryTokens",
            "AgentRuntimeOptions.MaxHistoryTokens 必须在会话摘要器中被消费（token 维度判定，P2D-3a）");

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
                // v2.3 P1-D：Mud.AI.Agent（执行器骨架 + ToolApiOutcome）——AI → Mud.AI.* 单向。
                "Mud.AI.Agent.csproj",
                // P1-H3：ConversationStoreSessionAdapter 直接实现中立端口 ISessionStore
                // （"直接使用的 API 必须直接声明"，P2-5）。方向不变：AI 适配层 → Mud.AI.*（单向）。
                "Mud.AI.Abstractions.csproj",
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

    /// <summary>中立 AI 运行期工程名（H2 起承载 token 计数 / guidance 装配 / 会话门面）。</summary>
    private const string MudAiAgentProject = "Mud.AI.Agent";

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
    /// 工具执行上下文 <c>Begin</c> 必须前置于上下文装配（P0-1）。
    /// </summary>
    /// <remarks>
    /// 顺序类缺陷最易在重构中复活，且症状是「静默零注入 + 仅一条 Warning」——
    /// 断言相对顺序（不是绝对行号），行号漂移不会失效。
    /// <b>N4 改锚</b>：回合机制已上移中立 <c>ConversationalTurnPipeline</c>——
    /// 断言锚点随实现迁移（旧锚读产品薄壳会变成"断言在跑但读旧锚"的假绿）。
    /// 行为锚（替身 ITurnScopeFactory + 替身 IContextAssembler 记录调用序）由
    /// <c>Tests/Mud.AI.Agent.Tests/Events/ConversationalTurnPipelineTests</c> 承载。
    /// </remarks>
    [Fact]
    public void ToolContext_Begin_ShouldPrecedeContextAssembly()
    {
        var source = ReadSource(MudAiAgentProject, "Events", "ConversationalTurnPipeline.cs");

        var beginIndex = source.IndexOf("BeginToolScope(new TurnScopeContext", StringComparison.Ordinal);
        var assembleCallIndex = source.IndexOf(
            "AssembleUserMessageDefaultAsync(request", StringComparison.Ordinal);

        beginIndex.Should().BeGreaterThan(-1, "必须先建立工具执行上下文");
        assembleCallIndex.Should().BeGreaterThan(-1, "管线必须装配用户消息");
        beginIndex.Should().BeLessThan(assembleCallIndex,
            "工具上下文必须先于上下文装配生效——否则知识/引用类装配器读不到租户键，RAG 注入模式永久静默失效");
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
        source.Should().Contain("StateKey = ChatHistoryStateKey", "状态键必须被 ChatClientAgentOptions 真实消费");

        // P4-4：outbox 状态键必须成对存在且**有真实消费点**——否则键会成为无人读取的僵尸状态，
        // 占会话存储且给后续维护者造成"这里似乎实现了补发"的误判。
        keys.Should().Contain("feishu.agent.pending_reply");
        keys.Should().Contain("feishu.agent.pending_reply_turn");

        var handlerSource = ReadSource(AiProject, "Events", "ConversationalFeishuEventHandler.cs");
        handlerSource.Should().Contain("PendingReplyStateKey",
            "outbox 状态键必须在会话处理器中被真实消费（补发/写入/摘除）");
        handlerSource.Should().Contain("PendingReplyTurnStateKey");

        // P1-H3 / D4：状态键的**字面量**是产品事实（留在上方的 FeishuAgent 常量里），
        // 机制（读取状态袋）在**中立包**的会话门面里——故中立门面必须以参数接收该键，
        // 且绝不能出现任何 `feishu.` 前缀（否则等于把产品命名面焊进包族底层，
        // P3 上游化时它会跟着 Mud.AI.Agent 一起走）。
        var storeSource = ReadSource(MudAiAgentProject, "Sessions", "AgentSessionStore.cs");
        storeSource.Should().Contain("_historyStateKey", "中立门面必须以参数接收历史状态键（D4：来源参数化）");
        storeSource.Should().NotContain("\"feishu.",
            "中立包不得硬编码任何产品状态键前缀——键的字面量归产品侧所有");

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
    /// WP3 删除了自研确认令牌全部过渡层（ToolConfirmationToken / IToolConfirmationTokenSecretProvider）。
    /// HITL 的批准状态所有权归宿主授权器（IToolExecutionAuthorizer），SDK 执行链恒为中性拒绝。
    /// 本守卫锁住"模型可见文案不含任何凭据类信息"这一不变量，防止令牌机制被重新引入。
    /// 扫描落在 <c>Mud.Feishu.AI.Tools</c>（执行链唯一实现方）。
    /// </remarks>
    [Fact]
    public void ConfirmationToken_ShouldNeverEnterModelVisiblePayload()
    {
        // v2.3 P1-D：待确认**文案**随错误契约族中立化上移 Mud.AI.Agent/Tools/ToolErrorText.cs
        // （FeishuToolBinding 只保留转发）；判据 ① 的锚点追到实现所在文件，
        // 判据 ②（宿主批准通道）仍是飞书侧契约，锚点留在 FeishuToolBinding.cs。
        var source = File.ReadAllText(Path.Combine(
            GetSolutionRoot(), "Mud.AI.Agent", "Tools", "ToolErrorText.cs"));

        var bindingSource = Path.Combine(
            GetSolutionRoot(), "Mud.Feishu.AI.Tools", "Tools", "FeishuToolBinding.cs");
        File.Exists(bindingSource).Should().BeTrue();

        // R3-02：切片方式从"取到行尾"改为"括号配平的方法体切片"——
        // 被守护文案跨两行（FeishuToolBinding.cs:416-417），单行切片会漏掉续行内容。
        var needsConfirmationBranch = source.IndexOf(
            "ToolErrorCategory.Confirmation =>", StringComparison.Ordinal);
        needsConfirmationBranch.Should().BeGreaterThan(-1);

        // 括号配平切片：从 `=>` 后的 `$"` 开始，配平到语句结束（分号或下一个 case）。
        var sliceStart = source.IndexOf('"', needsConfirmationBranch);
        sliceStart.Should().BeGreaterThan(-1, "NeedsConfirmation 分支必须有字符串字面量");
        var depth = 0;
        var sliceEnd = sliceStart;
        for (var i = sliceStart; i < source.Length; i++)
        {
            var ch = source[i];
            if (ch == '(') depth++;
            else if (ch == ')') { if (depth == 0) break; depth--; }
            else if (ch == ';' && depth == 0) { sliceEnd = i; break; }
            sliceEnd = i;
        }
        var branch = source[needsConfirmationBranch..(sliceEnd + 1)];

        branch.Should().NotContain("确认令牌", "模型可见文案不得携带/提示确认令牌（R2-1）");
        branch.Should().NotContain("confirm_token", "不得再指示模型以 confirm_token 重试（那等于把批准要素交给模型）");
        branch.Should().NotContain("confirmation_token", "不得以任何变体形式回灌令牌提示");
        branch.Should().Contain("需要用户确认", "待确认语义必须保留（三态文案不得退化）");

        // ② 批准状态归宿主授权器，SDK 不签发任何凭据（锚点：飞书侧绑定 —— v2.3 未随骨架上移）。
        var binding = File.ReadAllText(bindingSource);
        binding.Should().Contain("ToolApprovalRequest", "待确认事件必须经 ToolApprovalRequest 投递给宿主");
        binding.Should().Contain("IFeishuToolApprovalChannel", "必须存在宿主批准通道契约（未注册即 fail-closed）");
        binding.Should().NotContain("ToolConfirmationToken", "WP3 已删除自研确认令牌，不得重新引入");
        binding.Should().NotContain("IToolConfirmationTokenSecretProvider", "WP3 已删除令牌密钥提供者契约");
        binding.Should().NotContain("#pragma warning disable CS0618", "WP3 已删除令牌过渡层，不再需要 CS0618 抑制");
    }

    /// <summary>
    /// R3-02 行为断言：StructuredError 全部分支的<b>字符串字面量</b>经正则断言零命中凭据类信息。
    /// 本测试工程未引用 FeishuTools 程序集（无法反射调用 internal 方法），故退化为
    /// "只扫描字符串字面量、剥离注释"的源码断言——注释中的"确认令牌"（解释为什么删除了令牌提示）
    /// 不得触发假红。R3-10 纪律：文本断言仅限无法行为化的项，此处属该例外。
    /// </summary>
    [Fact]
    public void StructuredError_NeedsConfirmation_ShouldNotLeakCredentialInBehavior()
    {
        // v2.3 P1-D：StructuredError 的实现上移 Mud.AI.Agent/Tools/ToolErrorText.cs（同前一条守卫）。
        var bindingSource = Path.Combine(
            GetSolutionRoot(), "Mud.AI.Agent", "Tools", "ToolErrorText.cs");
        var source = File.ReadAllText(bindingSource);

        // 提取 StructuredError 方法体整体（从方法签名到下一个方法声明）。
        // v2.3 P1-D：visibility-agnostic——骨架里该方法为 public（FeishuToolBinding 侧为 internal 转发），
        // 锚点只匹配 `static string StructuredError(…)`，改名/改可见性都不会让守卫假红。
        var methodStart = source.IndexOf("static string StructuredError(string toolName, ToolErrorCategory category", StringComparison.Ordinal);
        methodStart.Should().BeGreaterThan(-1, "StructuredError 方法必须存在");
        var methodEnd = source.IndexOf("\n    static string StructuredError(string toolName, int? apiCode", StringComparison.Ordinal);
        if (methodEnd < 0)
        {
            methodEnd = source.IndexOf("\n    public static string StructuredError(string toolName, int? apiCode", StringComparison.Ordinal);
        }
        if (methodEnd < 0)
        {
            // fallback: 找下一个方法声明
            methodEnd = source.IndexOf("\n    /// <summary>", methodStart + 100, StringComparison.Ordinal);
        }
        methodEnd.Should().BeGreaterThan(methodStart, "必须能定位到 StructuredError 方法体结束");
        var methodBody = source[methodStart..methodEnd];

        // 剥离注释行（// 开头的行）——注释中解释"为什么删除了确认令牌"不应触发假红。
        var lines = methodBody.Split('\n');
        var codeOnly = string.Join('\n', lines.Where(static l =>
        {
            var trimmed = l.AsSpan().TrimStart();
            return !trimmed.StartsWith("//", StringComparison.Ordinal) && !trimmed.StartsWith("///", StringComparison.Ordinal);
        }));

        // 行为断言：剥离注释后的代码体（含所有分支文案字面量）经正则零命中凭据类信息。
        var credentialPattern = new System.Text.RegularExpressions.Regex(
            @"(confirm_token|confirmation_token|确认令牌|token\s*=)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        credentialPattern.IsMatch(codeOnly).Should().BeFalse(
            "StructuredError 的全部分支文案（字符串字面量）不得出现凭据类信息（已剥离注释）");

        // 待确认语义必须存在于方法体中。
        codeOnly.Should().Contain("需要用户确认", "待确认语义必须保留");
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
    /// R2-2 / R4-7：会话恢复必须<b>急切</b>校验历史状态（惰性反序列化纳入守护区），
    /// 且坏值自愈的 catch 面<b>不得是枚举白名单</b>。
    /// </summary>
    /// <remarks>
    /// R4-7 订正：原判据锁定字面量 <c>catch (Exception ex) when (ex is JsonException</c>——
    /// 它固化了"枚举白名单"这一缺陷形态（白名单不含 FormatException/KeyNotFoundException/
    /// IndexOutOfRangeException/NullReferenceException ⇒ 残余异常类型逃出守护区 ⇒ 坏值永不删除）。
    /// 判据改为「catch 面等价于 <c>when (ex is not OperationCanceledException)</c>」，
    /// 并<b>显式禁止</b>白名单写法复活；急切读取仍必须落在 catch 面之内。
    /// </remarks>
    [Fact]
    public void SessionRestore_ShouldEagerlyValidateHistoryState()
    {
        // P1-H3：实现已搬到中立门面 Mud.AI.Agent/Sessions/AgentSessionStore.cs
        // （FeishuAgent 的两个公开方法退化为一行转发，source 里已无 try/catch 与急切读取）。
        // 锚点必须跟着实现走——留在 FeishuAgent.cs 会让断言读到不含这些字面量的文件，
        // 从"报红"退化为"测了另一件事"。
        var source = ReadSource(MudAiAgentProject, "Sessions", "AgentSessionStore.cs");

        var restoreMethod = source.IndexOf("GetOrCreateAsync(string key", StringComparison.Ordinal);
        restoreMethod.Should().BeGreaterThan(-1);
        var body = source[restoreMethod..];

        var eagerRead = body.IndexOf("TryGetInMemoryChatHistory", StringComparison.Ordinal);
        var guard = body.IndexOf("catch (Exception ex) when (ex is not OperationCanceledException)", StringComparison.Ordinal);

        body.Should().NotContain("ex is JsonException or ArgumentException",
            "坏值自愈的 catch 面必须是收口形态（非枚举白名单）——白名单会漏掉残余异常类型，"
            + "使坏载荷每次重投递都在守护区外再抛（R4-7）");

        eagerRead.Should().BeGreaterThan(-1, "会话恢复必须急切触发一次历史状态读取（惰性解析否则逃逸守护区）");
        guard.Should().BeGreaterThan(-1,
            "catch 面必须收口为 `when (ex is not OperationCanceledException)`（R4-7）");
        eagerRead.Should().BeLessThan(guard,
            "急切读取必须落在坏值自愈的 catch 面**之内**——否则内层损坏在摘要器/Provider 内抛出，坏值永不删除");

        // 清库必须用 CancellationToken.None（自愈动作不得被调用方取消中断，R4-7 第 2 条语义）。
        body.Should().Contain("DeleteAsync(key, CancellationToken.None)",
            "清库不得用调用方 token：调用方一取消就留下坏值，会话在其 TTL 内继续毒化");
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
    /// R2-4 / P1-H2b：<c>ChatTokenCounter</c> 的依赖面必须显式声明在<b>它自己所属的工程</b>里。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 缺词表包时 <c>TiktokenTokenizer.CreateForModel</c> 抛异常并被回退路径吞掉 ⇒
    /// <c>MaxHistoryTokens</c> 维度全部退化为字符估算（仅 <c>InitializationFailure</c> 一处可观测）。
    /// </para>
    /// <para>
    /// <b>H2b 起锚点改到 <c>Mud.AI.Agent.csproj</c></b>：类型搬了，声明也必须跟着搬——
    /// 否则本断言会继续对着已经不再使用 tokenizer 的 <c>Mud.Feishu.AI.csproj</c> 检查，
    /// 而真正会出问题的工程反而无人守（"断言在跑但读旧锚"的典型假绿）。
    /// 同时新增 MEAI 断言：<c>CountMessages(IEnumerable&lt;ChatMessage&gt;)</c> 消费 MEAI 类型，
    /// 按 P2-5「直接使用的 API 必须直接声明」，缺它会让中性包依赖上游传递解析。
    /// </para>
    /// </remarks>
    [Fact]
    public void ChatTokenCounter_ShouldDeclareTokenizerDataPackage()
    {
        var csproj = Path.Combine(GetSolutionRoot(), MudAiAgentProject, "Mud.AI.Agent.csproj");
        File.Exists(csproj).Should().BeTrue();

        var content = File.ReadAllText(csproj);
        content.Should().Contain("Microsoft.ML.Tokenizers\"", "主包必须显式声明（P2-5）");
        content.Should().Contain("Microsoft.ML.Tokenizers.Data.O200kBase",
            "gpt-4o 的 o200k_base 词表在独立包中；缺失会让精确计数静默退化为估算（R2-4）");
        content.Should().Contain("Microsoft.Extensions.AI.Abstractions",
            "ChatTokenCounter.CountMessages 直接消费 MEAI 的 ChatMessage——"
            + "直接使用的 API 必须直接声明，否则中性包会依赖 MAF 的传递解析（上游换实现即静默失效）");

        // 可观测面：不再静默吞掉。
        ReadSource(MudAiAgentProject, "Conversations", "ChatTokenCounter.cs").Should().Contain("InitializationFailure",
            "编码器初始化失败必须留下可观测原因（由宿主构造期告警 + 用例断言）");
    }

    /// <summary>
    /// R2-5：<c>MemoryConversationStore</c> 必须清扫不再被读取的过期条目。
    /// </summary>
    [Fact]
    public void MemoryConversationStore_ShouldSweepExpiredEntries()
    {
        // N2-g：清扫机制已中立化（Mud.AI.Agent.Sessions.MemorySessionStore）——守卫锚点必须跟着
        // 实现走，否则它会在产品壳上对新代码"看不见"而假绿（§6.2 改锚纪律 + Q3 教训）。
        var source = ReadSource(MudAiAgentProject, "Sessions", "MemorySessionStore.cs");

        source.Should().Contain("SweepExpired", "必须有过期回收路径（未读键永不到达读侧软过期）");

        var save = source.IndexOf("public Task SaveAsync(", StringComparison.Ordinal);
        save.Should().BeGreaterThan(-1);
        var body = source[save..];
        body.Should().Contain("SweepExpired()", "回收必须由写入路径触发（惰性分摊，不引入后台线程/定时器）");
        body.Should().Contain("SweepThreshold", "必须按阈值分摊（均摊 O(1)），不得每次写入都全量扫描");

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
    /// 旧注释称「超出窗口的旧消息被折叠」，实测为「不可逆落库删除 + 工具消息剔除 + 仅保留首条 system」；
    /// 文档漂移会让宿主误判「模型忘了刚调过的工具」。本守卫防其再次漂移。
    /// 另补：本属性此前长期<b>没有</b>消费点守卫（既有两条 Phase2/Phase12 守卫均未覆盖）。
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

        // 消费点（此前缺失守卫）。
        ReadSource(AiProject, "Agents", "FeishuAgent.cs").Should().Contain(
            "new MessageCountingChatReducer(options.MaxHistoryMessages)",
            "MaxHistoryMessages 必须在 FeishuAgent 中被真实消费（MessageCountingChatReducer 裁剪窗）");
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
