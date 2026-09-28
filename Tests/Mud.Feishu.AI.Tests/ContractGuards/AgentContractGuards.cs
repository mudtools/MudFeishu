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
    /// MAF 只允许进入 Mud.Feishu.AI（总体设计 §3.1：AI → 核心单向，核心不被 Agent 运行时污染）。
    /// 其他项目经 Mud.Feishu.AI 的<b>项目引用</b>传递获得（如 Mud.Feishu.Redis），不违反本守卫。
    /// </summary>
    [Fact]
    public void AgentsAi_PackageReferences_ShouldBeDeclaredOnlyInMudFeishuAI()
    {
        var csprojFiles = Directory.GetFiles(GetSolutionRoot(), "*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.Contains("obj") && !p.Contains("bin"))
            .ToList();

        var offenders = new List<string>();

        foreach (var csproj in csprojFiles)
        {
            var content = Regex.Replace(File.ReadAllText(csproj), "<!--.*?-->", string.Empty, RegexOptions.Singleline);
            if (!Regex.IsMatch(content, @"Include=""Microsoft\.Agents\.AI[^""]*""", RegexOptions.IgnoreCase))
                continue;

            var projectName = Path.GetFileName(csproj);
            if (!string.Equals(projectName, "Mud.Feishu.AI.csproj", StringComparison.OrdinalIgnoreCase))
                offenders.Add(projectName);
        }

        offenders.Should().BeEmpty("Microsoft.Agents.AI* 的直接 PackageReference 只允许出现在 Mud.Feishu.AI");
    }

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
    /// <see cref="FeishuAgentOptions.Validate"/> 必须被 <see cref="FeishuAgent"/> 构造调用（fail-fast）。
    /// </summary>
    [Fact]
    public void FeishuAgentOptions_Validate_ShouldBeInvokedByAgentConstructor()
    {
        var agentSource = Path.Combine(GetSolutionRoot(), "Mud.Feishu.AI", "Agents", "FeishuAgent.cs");

        File.Exists(agentSource).Should().BeTrue();
        File.ReadAllText(agentSource).Should().Contain(
            "options.Validate()",
            "Agent 构造必须 fail-fast 校验配置（Phase 0 §8）");
    }

    /// <summary>
    /// Phase 2 新增配置属性（<c>SummaryThreshold</c>）必须在 Mud.Feishu.AI 源码中有真实消费点
    /// （R5 规则 2；<c>WriteAllowList</c>/<c>MaxStreamChunkLength</c> 消费点在 FeishuTools 包，
    /// 由 FeishuToolContractGuards 扫描）。
    /// </summary>
    [Fact]
    public void FeishuAgentOptions_Phase2Properties_ShouldHaveRealConsumptionPoints()
    {
        var summarizerSource = Path.Combine(GetSolutionRoot(), "Mud.Feishu.AI", "Conversations", "ConversationSummarizer.cs");

        File.Exists(summarizerSource).Should().BeTrue();
        File.ReadAllText(summarizerSource).Should().Contain(
            "SummaryThreshold",
            "FeishuAgentOptions.SummaryThreshold 必须在 ConversationSummarizer 中被消费（渐进式摘要阈值，Phase 2 §3.2）");
    }

    /// <summary>
    /// AI-FD-D12 批次 A/B 新增配置属性必须有真实消费点（R5 规则 2）：
    /// <c>MaxHistoryTokens</c>（ConversationSummarizer token 维度判定）、
    /// <c>RequireMentionInGroup</c>/<c>AllowP2pConversation</c>（ImMessageConversationalEventHandler 过滤）。
    /// </summary>
    [Fact]
    public void FeishuAgentOptions_Phase12Properties_ShouldHaveRealConsumptionPoints()
    {
        var summarizerSource = Path.Combine(GetSolutionRoot(), "Mud.Feishu.AI", "Conversations", "ConversationSummarizer.cs");
        var imHandlerSource = Path.Combine(
            GetSolutionRoot(), "Mud.Feishu.AI.FeishuTools", "Events", "ImMessageConversationalEventHandler.cs");

        File.Exists(summarizerSource).Should().BeTrue();
        File.ReadAllText(summarizerSource).Should().Contain(
            "MaxHistoryTokens",
            "FeishuAgentOptions.MaxHistoryTokens 必须在 ConversationSummarizer 中被消费（token 维度判定，P2D-3a）");

        File.Exists(imHandlerSource).Should().BeTrue();
        var handlerContent = File.ReadAllText(imHandlerSource);
        handlerContent.Should().Contain(
            "RequireMentionInGroup",
            "ImConversationOptions.RequireMentionInGroup 必须在事件处理器中被消费（群聊 @ 过滤，P2D-5a）");
        handlerContent.Should().Contain(
            "AllowP2pConversation",
            "ImConversationOptions.AllowP2pConversation 必须在事件处理器中被消费（单聊会话开关，P2D-5a）");
    }

    // ────────────────────────────────────────────────────────────────────
    // 守卫 4：包间纵向引用治理（不允许横向引用）
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Mud.Feishu.AI / Mud.Feishu.Redis 等「实现包」只允许引用 Mud.Feishu.Abstractions
    /// （纵向），禁止实现包互相引用（横向）。共享契约（如会话存储
    /// IConversationStore/FeishuConversationOptions）必须下沉 Abstractions。
    /// </summary>
    [Fact]
    public void ImplementationPackages_ShouldOnlyReferenceAbstractions_VerticalDependencyOnly()
    {
        var implementationProjects = new[]
        {
            "Mud.Feishu.AI.csproj",
            "Mud.Feishu.Redis.csproj",
        };

        var offenders = new List<string>();
        foreach (var csproj in Directory.GetFiles(GetSolutionRoot(), "*.csproj", SearchOption.AllDirectories)
                     .Where(p => !p.Contains("obj") && !p.Contains("bin")))
        {
            var projectName = Path.GetFileName(csproj);
            if (!implementationProjects.Contains(projectName, StringComparer.OrdinalIgnoreCase))
                continue;

            var content = Regex.Replace(File.ReadAllText(csproj), "<!--.*?-->", string.Empty, RegexOptions.Singleline);
            foreach (Match match in Regex.Matches(content, @"<ProjectReference\s[^>]*/>", RegexOptions.IgnoreCase))
            {
                var element = match.Value;
                // OutputItemType="Analyzer"（ReferenceOutputAssembly=false）是编译期分析器形态，
                // 不产生运行时程序集依赖，不属于引用治理范畴（如 [FeishuTool] 源生成器）。
                if (element.Contains("OutputItemType", StringComparison.OrdinalIgnoreCase)
                    || element.Contains("ReferenceOutputAssembly=\"false\"", StringComparison.OrdinalIgnoreCase))
                    continue;

                var include = Regex.Match(element, @"Include=""([^""]+)""", RegexOptions.IgnoreCase);
                if (!include.Success)
                    continue;

                var referenced = GetFileNameCrossPlatform(include.Groups[1].Value);
                if (!string.Equals(referenced, "Mud.Feishu.Abstractions.csproj", StringComparison.OrdinalIgnoreCase))
                    offenders.Add($"{projectName} → {referenced}");
            }
        }

        offenders.Should().BeEmpty(
            "实现包之间不允许横向引用，只允许纵向引用（→ Mud.Feishu.Abstractions）；" +
            "共享契约必须迁移至 Abstractions。违例: " + string.Join("; ", offenders));
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

    private static string ReadAiSource(params string[] relativeParts)
    {
        var parts = new string[relativeParts.Length + 2];
        parts[0] = GetSolutionRoot();
        parts[1] = "Mud.Feishu.AI";
        relativeParts.CopyTo(parts, 2);
        return File.ReadAllText(Path.Combine(parts));
    }

    /// <summary>
    /// 工具执行上下文 <c>Begin</c> 必须前置于上下文装配（P0-1）。
    /// </summary>
    /// <remarks>
    /// 顺序类缺陷最易在重构中复活，且症状是「静默零注入 + 仅一条 Warning」——
    /// 断言相对顺序（不是绝对行号），行号漂移不会失效。
    /// </remarks>
    [Fact]
    public void ToolContext_Begin_ShouldPrecedeContextAssembly()
    {
        var source = ReadAiSource("Events", "ConversationalFeishuEventHandler.cs");

        var beginIndex = source.IndexOf("ToolContextAccessor?.Begin", StringComparison.Ordinal);
        var assembleCallIndex = source.IndexOf(
            "await AssembleUserMessageAsync(request, cancellationToken)", StringComparison.Ordinal);

        beginIndex.Should().BeGreaterThan(-1, "必须先建立工具执行上下文");
        assembleCallIndex.Should().BeGreaterThan(-1, "管线必须装配用户消息");
        beginIndex.Should().BeLessThan(assembleCallIndex,
            "工具上下文必须先于上下文装配生效——否则知识/引用类装配器读不到 appKey，RAG 注入模式永久静默失效");
    }

    /// <summary>
    /// <c>FeishuAgent.Name</c> 必须 <c>override</c>，禁止用 <c>new</c> 遮蔽（P2-1）。
    /// </summary>
    [Fact]
    public void FeishuAgent_Name_ShouldBeOverrideNotHide()
    {
        var source = ReadAiSource("Agents", "FeishuAgent.cs");

        source.Should().Contain("override string? Name",
            "AIAgent.Name 是 virtual：override 才能让经 AIAgent 引用（含 MAF 内部诊断）取到真实名字");
        source.Should().NotContain("new string Name", "new 遮蔽会让基类属性恒为 null");
    }

    /// <summary>
    /// <c>FeishuAgent.GetService</c> 必须**先判自身**再退内层（P2-2）。
    /// </summary>
    [Fact]
    public void FeishuAgent_GetService_ShouldCheckSelfFirst()
    {
        var source = ReadAiSource("Agents", "FeishuAgent.cs");

        var declaration = source.IndexOf("object? GetService(", StringComparison.Ordinal);
        declaration.Should().BeGreaterThan(-1);

        var body = source[declaration..];
        var selfCheck = body.IndexOf("IsInstanceOfType(this)", StringComparison.Ordinal);
        var innerCall = body.IndexOf("_innerAgent.GetService(", StringComparison.Ordinal);

        selfCheck.Should().BeGreaterThan(-1, "必须先判自身");
        innerCall.Should().BeGreaterThan(-1);
        selfCheck.Should().BeLessThan(innerCall,
            "否则 GetService(typeof(AIAgent)) 返回内部 ChatClientAgent，宿主将绕过飞书遥测/摘要");
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
        var source = ReadAiSource("Agents", "FeishuAgent.cs");

        var keys = Regex.Matches(source, @"const string (\w*StateKey) = ""([^""]+)""")
            .Select(m => m.Groups[2].Value)
            .ToList();

        keys.Should().NotBeEmpty("ChatHistoryProvider 的状态键必须显式命名（默认键名随类型名漂移）");
        keys.Should().OnlyHaveUniqueItems("MAF 要求会话状态键全局唯一（重复即运行期异常）");
        keys.Should().Contain("feishu.agent.history", "历史状态键是会话摘要与历史读写的同源锚点");
        source.Should().Contain("StateKey = ChatHistoryStateKey", "状态键必须被 ChatClientAgentOptions 真实消费");
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
}
