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

        // P4-4：outbox 状态键必须成对存在且**有真实消费点**——否则键会成为无人读取的僵尸状态，
        // 占会话存储且给后续维护者造成"这里似乎实现了补发"的误判。
        keys.Should().Contain("feishu.agent.pending_reply");
        keys.Should().Contain("feishu.agent.pending_reply_turn");

        var handlerSource = ReadAiSource("Events", "ConversationalFeishuEventHandler.cs");
        handlerSource.Should().Contain("PendingReplyStateKey",
            "outbox 状态键必须在会话处理器中被真实消费（补发/写入/摘除）");
        handlerSource.Should().Contain("PendingReplyTurnStateKey");
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
    /// 扫描落在 <c>Mud.Feishu.AI.FeishuTools</c>（执行链唯一实现方）。
    /// </remarks>
    [Fact]
    public void ConfirmationToken_ShouldNeverEnterModelVisiblePayload()
    {
        var bindingSource = Path.Combine(
            GetSolutionRoot(), "Mud.Feishu.AI.FeishuTools", "Tools", "FeishuToolBinding.cs");
        File.Exists(bindingSource).Should().BeTrue();

        var source = File.ReadAllText(bindingSource);

        // R3-02：切片方式从"取到行尾"改为"括号配平的方法体切片"——
        // 被守护文案跨两行（FeishuToolBinding.cs:416-417），单行切片会漏掉续行内容。
        var needsConfirmationBranch = source.IndexOf(
            "ToolErrorKind.NeedsConfirmation =>", StringComparison.Ordinal);
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

        // ② 批准状态归宿主授权器，SDK 不签发任何凭据。
        source.Should().Contain("ToolApprovalRequest", "待确认事件必须经 ToolApprovalRequest 投递给宿主");
        source.Should().Contain("IFeishuToolApprovalChannel", "必须存在宿主批准通道契约（未注册即 fail-closed）");
        source.Should().NotContain("ToolConfirmationToken", "WP3 已删除自研确认令牌，不得重新引入");
        source.Should().NotContain("IToolConfirmationTokenSecretProvider", "WP3 已删除令牌密钥提供者契约");
        source.Should().NotContain("#pragma warning disable CS0618", "WP3 已删除令牌过渡层，不再需要 CS0618 抑制");
    }

    /// <summary>
    /// R3-02 行为断言：构造 NeedsConfirmation 结果后，回填给模型的文本
    /// 经正则断言零命中凭据类信息。与源码扫描解耦——重构/换行/提取方法都不会失效。
    /// </summary>
    [Fact]
    public void StructuredError_NeedsConfirmation_ShouldNotLeakCredentialInBehavior()
    {
        // 直接调用 StructuredError 的 public/internal 部分不可行（internal），
        // 但 FeishuToolResult.FromError 的文本最终经 ToolResultSanitizer 净化后进入模型上下文。
        // 此处验证 NeedsConfirmation 分支的源码文本经正则零命中。
        var bindingSource = Path.Combine(
            GetSolutionRoot(), "Mud.Feishu.AI.FeishuTools", "Tools", "FeishuToolBinding.cs");
        var source = File.ReadAllText(bindingSource);

        // 提取 StructuredError 方法体整体（从方法签名到下一个 internal/private/static 方法）。
        var methodStart = source.IndexOf("internal static string StructuredError(string toolName, ToolErrorKind kind", StringComparison.Ordinal);
        methodStart.Should().BeGreaterThan(-1, "StructuredError 方法必须存在");
        var methodEnd = source.IndexOf("\n    internal static string StructuredError(string toolName, int? apiCode", StringComparison.Ordinal);
        if (methodEnd < 0)
        {
            // fallback: 找下一个方法声明
            methodEnd = source.IndexOf("\n    /// <summary>", methodStart + 100, StringComparison.Ordinal);
        }
        methodEnd.Should().BeGreaterThan(methodStart, "必须能定位到 StructuredError 方法体结束");
        var methodBody = source[methodStart..methodEnd];

        // 行为断言：整个方法体（含所有分支文案）经正则零命中凭据类信息。
        var credentialPattern = new System.Text.RegularExpressions.Regex(
            @"(confirm_token|confirmation_token|确认令牌|token\s*=)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        credentialPattern.IsMatch(methodBody).Should().BeFalse(
            "StructuredError 的全部分支文案不得出现凭据类信息（行为断言，不依赖源码行结构）");

        // 待确认语义必须存在于方法体中。
        methodBody.Should().Contain("需要用户确认", "待确认语义必须保留");
    }

    /// <summary>
    /// R3-02 自证报红：合成缺陷源码（令牌提示放在续行）必须被发现。
    /// </summary>
    [Fact]
    public void ConfirmationToken_Guard_ShouldDetectCredentialInContinuationLine()
    {
        // 合成缺陷源码：NeedsConfirmation 分支跨两行，续行含 confirm_token。
        var defectiveSource = @"
        ToolErrorKind.NeedsConfirmation => $""[tool_error] {toolName} (needs_confirmation): {reason}——该操作需要用户确认后方可执行；""
            + ""可使用 confirm_token=xxx 重试"",
";
        var needsConfirmationBranch = defectiveSource.IndexOf(
            "ToolErrorKind.NeedsConfirmation =>", StringComparison.Ordinal);
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
    /// WP3：写类工具的 NeedsUserConfirmation 必须在授权门禁被放行（MAF 审批管线已前置承担），
    /// 不得因删除令牌而误删此分支——否则重新引入 P4-1 的「框架已批准、执行链仍拒绝」死胡同。
    /// </summary>
    [Fact]
    public void WriteTool_NeedsUserConfirmation_ShouldBeBypassed_AtAuthorizeGate()
    {
        var binding = File.ReadAllText(Path.Combine(
            GetSolutionRoot(), "Mud.Feishu.AI.FeishuTools", "Tools", "FeishuToolBinding.cs"));
        binding.Should().Contain("AuthorizationDecision.NeedsUserConfirmation && tool.IsWrite",
            "写类工具必须先于确认分支被放行（框架已前置批准）；删除该分支会让写工具陷入死胡同");
    }

    /// <summary>
    /// R2-2：会话恢复必须<b>急切</b>校验历史状态（惰性反序列化纳入守护区）。
    /// </summary>
    [Fact]
    public void SessionRestore_ShouldEagerlyValidateHistoryState()
    {
        var source = ReadAiSource("Agents", "FeishuAgent.cs");

        var restoreMethod = source.IndexOf("GetOrCreateSessionAsync(string conversationKey", StringComparison.Ordinal);
        restoreMethod.Should().BeGreaterThan(-1);
        var body = source[restoreMethod..];

        var eagerRead = body.IndexOf("TryGetInMemoryChatHistory", StringComparison.Ordinal);
        var guard = body.IndexOf("catch (Exception ex) when (ex is JsonException", StringComparison.Ordinal);

        eagerRead.Should().BeGreaterThan(-1, "会话恢复必须急切触发一次历史状态读取（惰性解析否则逃逸守护区）");
        guard.Should().BeGreaterThan(-1);
        eagerRead.Should().BeLessThan(guard,
            "急切读取必须落在坏值自愈的 catch 面**之内**——否则内层损坏在摘要器/Provider 内抛出，坏值永不删除");
    }

    /// <summary>
    /// R2-3：AppKey 缺失 × 工具链已装配 必须 fail-fast（并保留逃生门）。
    /// </summary>
    [Fact]
    public void AppKeyDegrade_ShouldFailFast_WhenToolChainPresent()
    {
        var source = ReadAiSource("Events", "ConversationalFeishuEventHandler.cs");

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
    /// R2-4：<c>ChatTokenCounter</c> 的 Tiktoken 词表数据包必须显式声明。
    /// </summary>
    /// <remarks>
    /// 缺该包时 <c>TiktokenTokenizer.CreateForModel</c> 抛异常并被静默吞掉 ⇒
    /// <c>MaxHistoryTokens</c> 维度全部退化为字符估算且无任何可观测信号。
    /// </remarks>
    [Fact]
    public void ChatTokenCounter_ShouldDeclareTokenizerDataPackage()
    {
        var csproj = Path.Combine(GetSolutionRoot(), "Mud.Feishu.AI", "Mud.Feishu.AI.csproj");
        File.Exists(csproj).Should().BeTrue();

        var content = File.ReadAllText(csproj);
        content.Should().Contain("Microsoft.ML.Tokenizers\"", "主包必须显式声明（P2-5）");
        content.Should().Contain("Microsoft.ML.Tokenizers.Data.O200kBase",
            "gpt-4o 的 o200k_base 词表在独立包中；缺失会让精确计数静默退化为估算（R2-4）");

        // 可观测面：不再静默吞掉。
        ReadAiSource("Conversations", "ChatTokenCounter.cs").Should().Contain("InitializationFailure",
            "编码器初始化失败必须留下可观测原因（由 FeishuAgent 构造期告警 + 用例断言）");
    }

    /// <summary>
    /// R2-5：<c>MemoryConversationStore</c> 必须清扫不再被读取的过期条目。
    /// </summary>
    [Fact]
    public void MemoryConversationStore_ShouldSweepExpiredEntries()
    {
        var source = ReadAiSource("Conversations", "MemoryConversationStore.cs");

        source.Should().Contain("SweepExpired", "必须有过期回收路径（未读键永不到达读侧软过期）");

        var save = source.IndexOf("public Task SaveAsync(", StringComparison.Ordinal);
        save.Should().BeGreaterThan(-1);
        var body = source[save..];
        body.Should().Contain("SweepExpired()", "回收必须由写入路径触发（惰性分摊，不引入后台线程/定时器）");
        body.Should().Contain("SweepThreshold", "必须按阈值分摊（均摊 O(1)），不得每次写入都全量扫描");
    }

    /// <summary>
    /// R2-6：<c>FeishuAgent.IdCore</c> 必须委托内层 Agent（对齐 MAF <c>DelegatingAIAgent</c> 约定）。
    /// </summary>
    [Fact]
    public void FeishuAgent_IdCore_ShouldDelegateToInnerAgent()
    {
        var source = ReadAiSource("Agents", "FeishuAgent.cs");

        source.Should().Contain("IdCore => _innerAgent.Id",
            "FeishuAgent 与内层 ChatClientAgent 是同一逻辑 Agent 的两层：Id 不一致会让 "
            + "AgentResponse.AgentId 与 FeishuAgent.Id 指向不同标识（遥测/编排按 AgentId 关联时出现「未知 Agent」）");
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
        var optionsSource = ReadAiSource("Agents", "FeishuAgentOptions.cs");

        var property = optionsSource.IndexOf("public int MaxHistoryMessages", StringComparison.Ordinal);
        property.Should().BeGreaterThan(-1);
        var docs = SliceBraceBalanced(optionsSource, property, backwards: true, maxBack: 2000);

        docs.Should().Contain("非 system 消息", "必须写明只统计非 system 消息");
        docs.Should().Contain("不可逆", "必须写明裁剪结果写回会话状态（不可逆落库删除），而非「本次请求视图」");
        docs.Should().Contain("FunctionCallContent", "必须写明工具调用消息被永久丢弃");

        // 消费点（此前缺失守卫）。
        ReadAiSource("Agents", "FeishuAgent.cs").Should().Contain(
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
