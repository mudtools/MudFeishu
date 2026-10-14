// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.ToolSurface;
using Mud.AI.ToolSurface.Extraction;

namespace Mud.AI.Generator.Tests;

/// <summary>
/// 工具面引擎入口（<see cref="ToolSurfaceSourceGenerator"/>）的行为级测试（设计文档 §10 门禁）：
/// 空剖面短路、SDKT002 缺槽守卫、有效剖面零产出。
/// </summary>
/// <remarks>
/// 端到端「策展接口 → 全家族产物」用例依赖 owner 程序集门槛（<c>OwnerAssembly</c>）与执行器绑定
/// 全套机制，属 R-2c 接线阶段的 golden 快照范畴；本文件锁定的是引擎入口的<b>门禁语义</b>：
/// 无剖面时零成本（0 诊断、0 AddSource）、缺槽剖面被拒产并显式报错。
/// </remarks>
public class ToolSurfaceSourceGeneratorTests
{
    /// <summary>合法剖面的最小完整声明（7 个必填槽全齐）。</summary>
    private const string ValidProfile = """
        using Mud.AI;
        using Mud.AI.Attributes;

        [SdkToolProfile("Test",
            ToolAttributeName = "TestTool",
            ToolAttributeNamespace = "Test.Tools",
            SdkNamespaceRoot = "Test.Sdk",
            ProductPrefix = "TestTool",
            DiagnosticPrefix = "MUDTT",
            DiagnosticCategory = "Test.AI")]
        sealed class TestProfile : ISdkToolProfile { }
        """;

    [Fact]
    public void NoProfile_SourceProducesNothing()
    {
        // 空剖面短路（§5.3/§7.2）：纯 HTTP 消费方典型编译——0 工具诊断、0 AddSource。
        const string source = """
            public interface IPlainApi
            {
                string Name { get; }
            }

            public sealed class PlainService
            {
                [System.Obsolete]
                public void DoWork() { }
            }
            """;

        var result = RunToolSurface(source);

        result.Diagnostics.Should().BeEmpty("无 ISdkToolProfile 时引擎必须零诊断（含 SDKT001/002）");
        result.GeneratedSources.Should().BeEmpty("无剖面时不得产出任何工具面文件");
    }

    [Fact]
    public void ValidProfile_NoToolInterfaces_ProducesNothingWithoutErrors()
    {
        // 剖面合法但编译内无任何工具特性接口：扫描流为空 → 各输出路径静默跳过，不报错。
        var result = RunToolSurface(ValidProfile);

        result.Diagnostics.Should().BeEmpty();
        result.GeneratedSources.Should().BeEmpty();
    }

    [Fact]
    public void ProfileMissingRequiredSlots_ReportsSdkT002_AndProducesNothing()
    {
        // 缺 DiagnosticPrefix + DiagnosticCategory（两个必填槽）→ 恰一条 SDKT002，剖面被跳过。
        const string source = """
            using Mud.AI;
            using Mud.AI.Attributes;

            [SdkToolProfile("Broken",
                ToolAttributeName = "TestTool",
                ToolAttributeNamespace = "Test.Tools",
                SdkNamespaceRoot = "Test.Sdk",
                ProductPrefix = "TestTool")]
            sealed class BrokenProfile : ISdkToolProfile { }
            """;

        var result = RunToolSurface(source);

        var sdkT002 = result.Diagnostics.Where(d => d.Id == DiagnosticIds.SdkToolProfileMissingRequiredSlots).ToList();
        sdkT002.Should().HaveCount(1, "缺槽剖面必须恰好上报一条 SDKT002");
        sdkT002[0].Severity.Should().Be(DiagnosticSeverity.Error);
        sdkT002[0].GetMessage().Should().Contain("Broken")
            .And.Contain("DiagnosticPrefix")
            .And.Contain("DiagnosticCategory");

        result.GeneratedSources.Should().BeEmpty("缺槽剖面不得进入任何扇出路径");
    }

    [Fact]
    public void ProfileInterfaceWithoutAttribute_IsIgnoredByGenerator()
    {
        // 成对守卫分工：缺特性半边由 SDKT001（分析器侧）报错，生成器侧静默忽略、不重复报告。
        const string source = """
            using Mud.AI;

            sealed class BareProfile : ISdkToolProfile { }
            """;

        var result = RunToolSurface(source);

        result.Diagnostics.Should().BeEmpty("生成器不消费缺特性半边，也不得自行报 SDKT001/002");
        result.GeneratedSources.Should().BeEmpty();
    }

    private static GeneratorRunResult RunToolSurface(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        var compilation = CSharpCompilation.Create(
            "ToolSurfaceTestAssembly",
            new[] { tree },
            BasicReferenceAssemblies.GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ToolSurfaceSourceGenerator());
        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult().Results.Single();
    }

    // ────────── P2-2：变换/扫描异常折算为 {prefix}026 随流上报（错误可诊断） ──────────

    /// <summary>两个剖面 + 各自的工具接口（P2-2 跨剖面隔离断言的输入面）。</summary>
    private const string TwoProfilesWithTools = """
        using Mud.AI;
        using Mud.AI.Attributes;

        [SdkToolProfile("Test",
            ToolAttributeName = "TestTool",
            ToolAttributeNamespace = "Test.Tools",
            SdkNamespaceRoot = "Test.Sdk",
            ProductPrefix = "TestTool",
            DiagnosticPrefix = "MUDTT",
            DiagnosticCategory = "Test.AI",
            TokenKindMarkers = "ITest=Tenant")]
        sealed class TestProfile : ISdkToolProfile { }

        [SdkToolProfile("Other",
            ToolAttributeName = "OtherTool",
            ToolAttributeNamespace = "Other.Tools",
            SdkNamespaceRoot = "Other.Sdk",
            ProductPrefix = "OtherTool",
            DiagnosticPrefix = "MUDDD",
            DiagnosticCategory = "Other.AI",
            TokenKindMarkers = "IOther=Tenant")]
        sealed class OtherProfile : ISdkToolProfile { }

        namespace Test.Tools
        {
            [System.AttributeUsage(System.AttributeTargets.Interface)]
            public sealed class TestToolAttribute : System.Attribute
            {
                public TestToolAttribute(string name) => Name = name;
                public string Name { get; }
            }

            /// <summary>测试工具。</summary>
            [TestTool("test_echo")]
            public interface ITestToolApi
            {
                /// <summary>回显消息。</summary>
                /// <param name="message">消息。</param>
                string Echo(string message);
            }
        }

        namespace Other.Tools
        {
            [System.AttributeUsage(System.AttributeTargets.Interface)]
            public sealed class OtherToolAttribute : System.Attribute
            {
                public OtherToolAttribute(string name) => Name = name;
                public string Name { get; }
            }

            /// <summary>其他工具。</summary>
            [OtherTool("other_echo")]
            public interface IOtherToolApi
            {
                /// <summary>回显消息。</summary>
                /// <param name="message">消息。</param>
                string Echo(string message);
            }
        }
        """;

    [Fact]
    public void ToolScanFault_ReportsPrefix026_AndKeepsOtherProfileProducts()
    {
        // P2-2 验证方式：注入异常源 → 断言收到 {prefix}026 且其余剖面产物不受影响。
        // 注入只命中 "Test" 剖面的工具扫描；"Other" 剖面必须照常扫描与发射。
        try
        {
            ToolSurfaceScanner.FaultInjectionForTests =
                profileName => profileName == "Test" ? new InvalidOperationException("boom-scan") : null;

            var result = RunToolSurface(TwoProfilesWithTools);

            var fault = result.Diagnostics.Where(static d => d.Id == "MUDTT026").ToList();
            fault.Should().HaveCount(1, "扫描体异常必须折算为恰好一条 {prefix}026，而非静默吞掉");
            fault[0].Severity.Should().Be(DiagnosticSeverity.Error);
            fault[0].GetMessage().Should().Contain("InvalidOperationException")
                .And.Contain("boom-scan", "026 消息必须携带异常类型与消息，否则无法定位生成器缺陷");

            result.Diagnostics.Where(static d => d.Id.StartsWith("MUDDD", StringComparison.Ordinal)).Should().BeEmpty(
                "单剖面异常不得污染其余剖面的诊断");

            var hintNames = result.GeneratedSources.Select(static s => s.HintName).ToList();
            hintNames.Should().Contain("OtherToolSchemas.g.cs", "其余剖面的产物必须照常发射");
            hintNames.Should().NotContain("TestToolSchemas.g.cs", "故障剖面的产物必须缺席（模型为 null）");
        }
        finally
        {
            ToolSurfaceScanner.FaultInjectionForTests = null;
        }
    }

    [Fact]
    public void HandlerScanFault_ReportsPrefix026_AndKeepsToolProducts()
    {
        // 执行器扫描异常：折算为 {prefix}026，且工具 Schema 产物不受执行器扫描异常影响
        // （faulted handler 的 ToolName/Binding 均为 null，对发射路径惰性）。
        const string source = """
            using Mud.AI;
            using Mud.AI.Attributes;

            [SdkToolProfile("Test",
                ToolAttributeName = "TestTool",
                ToolAttributeNamespace = "Test.Tools",
                ToolHandlerAttributeName = "TestToolHandler",
                ToolHandlerAttributeNamespace = "Test.Tools",
                SdkNamespaceRoot = "Test.Sdk",
                ProductPrefix = "TestTool",
                DiagnosticPrefix = "MUDTT",
                DiagnosticCategory = "Test.AI",
                TokenKindMarkers = "ITest=Tenant")]
            sealed class TestProfile : ISdkToolProfile { }

            namespace Test.Tools
            {
                [System.AttributeUsage(System.AttributeTargets.Interface)]
                public sealed class TestToolAttribute : System.Attribute
                {
                    public TestToolAttribute(string name) => Name = name;
                    public string Name { get; }
                }

                [System.AttributeUsage(System.AttributeTargets.Method)]
                public sealed class TestToolHandlerAttribute : System.Attribute
                {
                    public TestToolHandlerAttribute(System.Type toolInterface) => ToolInterface = toolInterface;
                    public System.Type ToolInterface { get; }
                }

                /// <summary>测试工具。</summary>
                [TestTool("test_echo")]
                public interface ITestToolApi
                {
                    /// <summary>回显消息。</summary>
                    /// <param name="message">消息。</param>
                    string Echo(string message);
                }
            }

            public sealed class TestToolResult { }

            public static class TestExecutor
            {
                /// <summary>执行器。</summary>
                [Test.Tools.TestToolHandler(typeof(Test.Tools.ITestToolApi))]
                public System.Threading.Tasks.Task<TestToolResult> RunAsync(
                    System.Collections.Generic.IReadOnlyDictionary<string, object?> args,
                    System.Threading.CancellationToken cancellationToken)
                    => System.Threading.Tasks.Task.FromResult(new TestToolResult());
            }
            """;

        try
        {
            ToolHandlerScanner.FaultInjectionForTests =
                profileName => profileName == "Test" ? new InvalidOperationException("boom-handler") : null;

            var result = RunToolSurface(source);

            var fault = result.Diagnostics.Where(static d => d.Id == "MUDTT026").ToList();
            fault.Should().HaveCount(1, "执行器扫描体异常必须折算为恰好一条 {prefix}026");
            fault[0].Severity.Should().Be(DiagnosticSeverity.Error);
            fault[0].GetMessage().Should().Contain("InvalidOperationException").And.Contain("boom-handler");

            result.GeneratedSources.Select(static s => s.HintName).Should().Contain(
                "TestToolSchemas.g.cs",
                "工具 Schema 产物不受执行器扫描异常影响");
        }
        finally
        {
            ToolHandlerScanner.FaultInjectionForTests = null;
        }
    }
}