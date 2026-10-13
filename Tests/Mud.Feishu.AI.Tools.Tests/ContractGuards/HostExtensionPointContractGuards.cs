// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// R-3 / F-11：<b>宿主扩展点的跨消费者契约 e2e</b>。
/// </summary>
/// <remarks>
/// <para>
/// 本仓有四个"宿主按需注册"的扩展点：<see cref="IToolExecutionAuthorizer"/>（给不给）、
/// <see cref="IToolResultShaper"/>（结果怎么整形）、<see cref="IToolExecutionAuditSink"/>（审计去哪）、
/// <see cref="IFeishuAttachmentStager"/>（二进制往哪落）。它们的共同风险是
/// <b>"单点单测全绿、跨点组合失效"</b>——每个实现各自有测试，但没有一条用例证明
/// "在真实执行链里它们会按约定被调用"。
/// </para>
/// <para>
/// 两条不变量：
/// ① <b>链上 e2e</b>：一次真实执行必须同时调用授权器、整形钩子与审计出口（缺一个即报红）；
/// ② <b>覆盖登记</b>：每个扩展点都必须有测试引用 + 有 Demo 消费者（以 Demo 为"第一消费者"），
/// 删掉任一实现或任一测试都会让本守卫报红。
/// </para>
/// </remarks>
public class HostExtensionPointContractGuards
{
    /// <summary>扩展点 → 是否要求 Demo 提供实现（"第一消费者"）。</summary>
    /// <remarks>
    /// <see cref="IToolResultShaper"/> 的 Demo 实现**故意缺席**：SDK 默认即"无实现"，
    /// Demo 若凭空插一个整形器会改变演示结果形态，反而掩盖默认行为。
    /// 它的链上语义由本类的 e2e 用例（测试替身）覆盖。
    /// </remarks>
    private static readonly (string Interface, bool DemoConsumer)[] ExtensionPoints =
    [
        (nameof(IToolExecutionAuthorizer), true),
        (nameof(IToolResultShaper), false),
        (nameof(IToolExecutionAuditSink), true),
        (nameof(IFeishuAttachmentStager), true),
    ];

    [Fact]
    public async Task AllChainExtensionPoints_ShouldBeInvokedInOneExecution()
    {
        var authorizerCalls = 0;
        var authorizer = new Mock<IToolExecutionAuthorizer>();
        authorizer
            .Setup(a => a.AuthorizeAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<bool>(),
                It.IsAny<IReadOnlyDictionary<string, object?>>(),
                It.IsAny<FeishuToolContext>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => authorizerCalls++)
            .ReturnsAsync(AuthorizationResult.Allowed);

        var records = new List<ToolExecutionAuditRecord>();
        var shaperCalls = 0;

        var binding = new FeishuToolBinding(
            new FeishuAppContextScopeFactory(
                new Mock<Mud.HttpUtils.IAppContextHolder>().Object,
                new Mock<Mud.Feishu.Abstractions.IFeishuAppManager>().Object,
                Mock.Of<Mud.HttpUtils.IAppAccessAuthorizer>(a => a.CanSwitchTo(It.IsAny<string>()))),
            Options.Create(new FeishuAgentOptions { Instructions = "test" }),
            authorizer.Object,
            resultShaper: new DelegateShaper(text =>
            {
                shaperCalls++;
                return "shaped:" + text;
            }),
            auditSink: new RecordingSink(records));

        var tool = new FeishuToolDefinition(
            "bitable.query_records",
            "描述",
            ["bitable:app:readonly"],
            IsWrite: false,
            FeishuToolRisk.Read,
            "tenant",
            (_, _, _) => Task.FromResult(ToolResult.FromText("原始结果")));

        var result = await binding.ExecuteAsync(
            tool,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["app_token"] = "bascn" },
            new FeishuToolContext("app-a"),
            _ => Task.FromResult(ToolResult.FromText("原始结果")));

        authorizerCalls.Should().Be(1, "写/读工具都必须经授权门禁（扩展点 ① 被调用）");
        shaperCalls.Should().Be(1, "结果在回填模型前必须过整形钩子（扩展点 ② 被调用）");
        records.Should().ContainSingle("每次执行必须投递审计（扩展点 ③ 被调用）");
        result.ToString().Should().Be("shaped:原始结果", "整形钩子的返回值必须真的进入模型出口");
        records[0].Decision.Should().Be("allowed");
        records[0].Risk.Should().Be(FeishuToolRiskNames.Read, "审计记录带风险字面量（F-13 与之同源）");
    }

    [Fact]
    public void EveryExtensionPoint_ShouldHaveTestCoverage_AndDeclaredDemoConsumer()
    {
        var root = FindRepositoryRoot();
        var testSources = Directory
            .EnumerateFiles(Path.Combine(root, "Tests"), "*.cs", SearchOption.AllDirectories)
            .Where(static p => !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .Select(File.ReadAllText)
            .ToArray();
        var demoSources = Directory
            .EnumerateFiles(Path.Combine(root, "Demos"), "*.cs", SearchOption.AllDirectories)
            .Where(static p => !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .Select(File.ReadAllText)
            .ToArray();

        testSources.Should().NotBeEmpty("Tests/ 扫描面不得为空（否则本守卫恒绿）");
        demoSources.Should().NotBeEmpty("Demos/ 扫描面不得为空（否则'第一消费者'判据恒绿）");

        var violations = new List<string>();
        foreach (var (interfaceName, demoConsumer) in ExtensionPoints)
        {
            if (!testSources.Any(source => source.Contains(interfaceName, StringComparison.Ordinal)))
            {
                violations.Add($"{interfaceName}: 没有任何测试引用它（扩展点无契约覆盖）");
            }

            if (demoConsumer && !demoSources.Any(source => source.Contains(": " + interfaceName, StringComparison.Ordinal)))
            {
                violations.Add($"{interfaceName}: Demo 未提供实现（'第一消费者'缺失，宿主接入无参考实现）");
            }
        }

        violations.Should().BeEmpty("扩展点契约缺口：{0}", string.Join(" | ", violations));
    }

    /// <summary>负例自证：扫描判据必须真的能报出"缺实现"（否则上面那条是假绿）。</summary>
    [Fact]
    public void ConsumerScanner_ShouldDetectMissingImplementation_OtherwiseGuardIsFalseGreen()
    {
        string[] sources = ["internal sealed class X : IToolExecutionAuthorizer", "var unrelated = 1;"];

        sources.Any(source => source.Contains(": IToolExecutionAuditSink", StringComparison.Ordinal))
            .Should().BeFalse("判据必须能报出'某扩展点在 Demo 里没有实现'（报不出即为假绿）");

        sources.Any(source => source.Contains(": IToolExecutionAuthorizer", StringComparison.Ordinal))
            .Should().BeTrue("已有实现时必须判为存在（否则会误伤正确代码）");
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

    /// <summary>结果整形钩子替身（把"是否被调用"与"返回值是否生效"分开可断言）。</summary>
    private sealed class DelegateShaper(Func<string, string> shape) : IToolResultShaper
    {
        public Task<string?> ShapeAsync(string toolName, string projectedResult, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(shape(projectedResult));
    }

    /// <summary>审计出口替身。</summary>
    private sealed class RecordingSink(List<ToolExecutionAuditRecord> records) : IToolExecutionAuditSink
    {
        public Task WriteAsync(ToolExecutionAuditRecord record, CancellationToken cancellationToken = default)
        {
            records.Add(record);
            return Task.CompletedTask;
        }
    }
}
