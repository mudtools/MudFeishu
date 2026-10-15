// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Tests;

/// <summary>
/// <b>NeutralPublicTypes_ShouldHaveOwningTests</b>（N6 上线，方案 v2 §6.4 / v2.1 Q4）：
/// <see cref="Mud.AI.Agent"/> 的每个公开类型必须「有归属测试」——机械防「迁了类型、忘了迁用例」。
/// </summary>
/// <remarks>
/// <para>
/// <b>判据</b>：类型名在<b>任一</b>测试工程存在命名对应的 <c>&lt;TypeName&gt;Tests</c> 类
/// （首选形态：测试随类型走，P3 上游化时覆盖跟着中立包离开飞书仓）；
/// 或该类型登记在本守卫的 <see cref="OwningEvidence"/> 映射——其行为测试 legitimately
/// 以其他类名落在产品测试工程（通道与产品子类共生、审批投影与产品载荷绑定等），逐条登记并
/// <b>可验证</b>（守卫自证断言映射指向的测试类真实存在，防映射腐烂）。
/// </para>
/// <para>
/// <b>为什么默认报红</b>：新增中立公开类型时，「补一个自有测试类」是最低门槛——
/// 连名字都不取的行为测试等于没有归属；不报红则 Q4 的缺口会无声扩大。
/// </para>
/// <para>
/// 范围：非接口类型（接口由实现方测试覆盖，机械登记属噪声；枚举同理）。
/// <c>Mud.AI.Abstractions</c> 的契约类型按 §6.4「或声明契约的既有归属工程」处理——
/// 由实现方测试覆盖（如 <c>ISessionStore</c> ⇐ <c>MemorySessionStoreTests</c>），不逐条登记。
/// </para>
/// </remarks>
public class NeutralPublicTypesOwningContractGuards
{
    /// <summary>
    /// 行为测试落在其他名称/测试工程的类型 → 归属测试类（守卫自证其存在）。
    /// </summary>
    private static readonly Dictionary<string, string> OwningEvidence = new(StringComparer.Ordinal)
    {
        // 审批闭环：状态机与投影与产品载荷绑定，行为测试在 AI.Tests。
        ["AgentApprovalContinuation"] = "FeishuAgentApprovalContinuationTests",
        ["ApprovalRequestProjector"] = "ConversationalFeishuEventHandlerApprovalTests",
        ["InMemoryPendingApprovalStore"] = "PendingApprovalStoreTests",
        ["PendingApprovalState"] = "PendingApprovalStoreTests",

        // 通道机制与产品子类共生（EditMessage/CardStream 子类各有测试 + 生命周期契约守卫）。
        ["BufferedMessageChannel"] = "ChannelStateLifecycleContractGuards",

        // 回合管线值/选项类型：行为已由管线专属用例承载。
        ["ConversationalTurnOptions"] = "ConversationalTurnPipelineTests",
        ["TurnScopeContext"] = "ConversationalTurnPipelineTests",
        ["TurnOutcome"] = "ConversationalTurnPipelineTests",

        // DI 骨架：装配语义在产品侧守卫锚（会话闸门/存储替换链）。
        ["AgentRuntimeServiceCollectionExtensions"] = "AgentContractGuards",

        // 知识族：选项/估算/索引随 Chunker 与 Retriever 的测试承载。
        ["ChunkingOptions"] = "DocumentChunkerTests",
        ["TokenEstimator"] = "DocumentChunkerTests",
        ["CorpusIndexer"] = "VectorRetrieverTests",

        // 引导族：块/结果值随 Composer 测试承载。
        ["GuidanceBlock"] = "GuidanceComposerTests",
        ["GuidanceResult"] = "GuidanceComposerTests",

        // 工具面机制：守卫与行为测试在 Tools.Tests（P3 上游化时随工具面走）。
        ["ToolApiOutcome"] = "ToolApiFailureTests",
        ["ToolArgumentException"] = "ToolArgsContractGuards",
        ["ToolDryRun"] = "DryRunContractGuards",
        ["ToolError"] = "ToolErrorContractGuards",
        ["ToolErrorFactory"] = "ToolErrorContractGuards",
        ["ToolErrorText"] = "ToolErrorContractGuards",
        ["ToolErrorHintRenderer"] = "ToolErrorContractGuards",
        ["ToolErrorPayloadSerializer"] = "ToolErrorPayloadTests",
        ["ToolErrorSubtype"] = "ToolErrorClassifierTests",
        ["ToolErrorTextFormatter"] = "ToolErrorClassifierTests",
        ["ToolExecutor"] = "ToolExecutorSkeletonGuards",
        ["ToolNamePattern"] = "ToolPolicyContractGuards",
        ["ToolPagination"] = "PaginationContractGuards",
        ["PagedFetchResult"] = "PaginationContractGuards",
        ["ToolPolicyMatcher"] = "ToolPolicyContractGuards",
        ["ToolPolicyRules"] = "ToolPolicyContractGuards",
        ["ToolResult"] = "ToolResultContentSafetyTests",
        ["ToolResultJson"] = "ToolResultSanitizerTests",
        ["ToolResultJsons"] = "ToolResultSanitizerTests",
        ["ToolResultPipeline"] = "ToolResultContentSafetyTests",
        ["ToolResultText"] = "ToolResultSanitizerTests",

        // 阶段 A-1：B 组迁入的 3 个公开类型无同名测试类——形态认知（ToolArgumentNormalizer /
        // ToolArgsDigester）的行为测试以等价性矩阵承载（该矩阵引用产品 ToolArgs 门面做基准，故留在
        // 产品测试工程）；安全基元（SecurityTextPrimitives）由出站净化用例承载。
        ["ToolArgumentNormalizer"] = "ToolArgumentFormEquivalenceTests",
        ["ToolArgsDigester"] = "ToolArgumentFormEquivalenceTests",
        ["SecurityTextPrimitives"] = "ToolResultSanitizerTests",
    };

    /// <summary>扫描范围：Mud.AI.Agent 的公开类型声明（非接口）。</summary>
    [Fact]
    public void NeutralPublicTypes_ShouldHaveOwningTests()
    {
        var root = FindRepositoryRoot();
        var testClassNames = CollectTestClassNames(root);
        var neutralTypes = CollectPublicTypes(Path.Combine(root, "Mud.AI.Agent"));

        neutralTypes.Should().NotBeEmpty("扫描器必须能读到 Mud.AI.Agent 的公开类型声明（读空 = 假绿）");

        var unowned = neutralTypes
            .Where(type => !testClassNames.Contains(type + "Tests") && !OwningEvidence.ContainsKey(type))
            .OrderBy(static type => type, StringComparer.Ordinal)
            .ToList();

        unowned.Should().BeEmpty(
            "以下中立公开类型没有归属测试：既不存在 <Type>Tests 类，也未在 OwningEvidence 登记"
            + "「行为测试落在哪个类」——补一个自有测试类（首选，测试随类型走）或登记证据：\n{0}",
            string.Join("\n", unowned));
    }

    /// <summary>守卫自证：证据映射指向的测试类必须真实存在（映射腐烂 = 报红）。</summary>
    [Fact]
    public void OwningEvidence_ShouldPointToExistingTestClasses()
    {
        var testClassNames = CollectTestClassNames(FindRepositoryRoot());

        var stale = OwningEvidence.Values
            .Where(testClass => !testClassNames.Contains(testClass))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToList();

        stale.Should().BeEmpty(
            "以下 OwningEvidence 指向的测试类已不存在（改名的测试要同批改映射，否则归属声明失守）：{0}",
            string.Join(", ", stale));

        // 反向：证据键必须真的是 Mud.AI.Agent 的公开类型（复制粘贴错键 = 报红）。
        var neutralTypes = CollectPublicTypes(Path.Combine(FindRepositoryRoot(), "Mud.AI.Agent"));
        var phantom = OwningEvidence.Keys
            .Where(type => !neutralTypes.Contains(type))
            .OrderBy(static type => type, StringComparer.Ordinal)
            .ToList();

        phantom.Should().BeEmpty("OwningEvidence 含非中立公开类型的键：{0}", string.Join(", ", phantom));
    }

    /// <summary>Mud.AI.Agent 源码的公开类型名（class/record/struct，排除接口与嵌套）。</summary>
    private static HashSet<string> CollectPublicTypes(string projectDirectory)
    {
        var types = new HashSet<string>(StringComparer.Ordinal);
        var declarationPattern = new System.Text.RegularExpressions.Regex(
            @"(?m)^\s*public\s+(?:sealed\s+|abstract\s+|static\s+|partial\s+|readonly\s+)*(?:record\s+struct|record\s+class|record|struct|class)\s+(\w+)",
            System.Text.RegularExpressions.RegexOptions.Compiled);

        foreach (var file in Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                                 && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")))
        {
            var source = File.ReadAllText(file);
            foreach (System.Text.RegularExpressions.Match match in declarationPattern.Matches(source))
            {
                types.Add(match.Groups[1].Value);
            }
        }

        return types;
    }

    /// <summary>全部测试工程源码中的测试类名（class X / X 类声明，取类型名集合）。</summary>
    private static HashSet<string> CollectTestClassNames(string root)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        var declarationPattern = new System.Text.RegularExpressions.Regex(
            @"(?m)^\s*(?:public|internal)\s+(?:sealed\s+|static\s+|abstract\s+|partial\s+)*class\s+(\w+)",
            System.Text.RegularExpressions.RegexOptions.Compiled);

        foreach (var testProject in Directory.EnumerateDirectories(
                     Path.Combine(root, "Tests"), "Mud.*Tests", SearchOption.TopDirectoryOnly))
        {
            foreach (var file in Directory.EnumerateFiles(testProject, "*.cs", SearchOption.AllDirectories)
                         .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")))
            {
                var source = File.ReadAllText(file);
                foreach (System.Text.RegularExpressions.Match match in declarationPattern.Matches(source))
                {
                    names.Add(match.Groups[1].Value);
                }
            }
        }

        return names;
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
