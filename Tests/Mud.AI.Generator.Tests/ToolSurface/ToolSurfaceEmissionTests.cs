// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------
using System.Reflection;
using Microsoft.CodeAnalysis.Text;
using Mud.AI.ToolSurface;

namespace Mud.AI.Generator.Tests;

/// <summary>
/// 工具面引擎的<b>端到端发射</b>测试（设计文档 §10「全部输出路径均参与」+ golden + 诊断契约 + 引擎增量）。
/// </summary>
/// <remarks>
/// <para>
/// 与 <see cref="ToolSurfaceSourceGeneratorTests"/>（入口门禁语义）互补：本文件把「剖面 + 独立 SDK 程序集 +
/// 策展工具接口 + 执行器 + 消费方手写 helper」放进同一个编译，断言 <b>8 条输出路径</b>全部产出、
/// 产物可编译、诊断 ID 均落在槽位表内，并以 golden / Guidance 的逐字节断言锁定
/// <b>既有契约测试覆盖不到的发射缺陷类别</b>。
/// </para>
/// <para>
/// SDK 类型刻意放在名为 <c>Test.Sdk</c> 的独立程序集（Tier R 能力目录按「程序集名 ==
/// <c>SdkNamespaceRoot</c>」定位 SDK，见 §4.2 第 2 项），使 Tier R 路径真实可达。
/// </para>
/// <para>
/// 消费方手写 helper（<c>ToolArgs</c> / <c>TestToolRegistration</c> / <c>TestToolDomainRegistrars</c> /
/// <c>TestToolBinding</c> / <c>TestToolRegistry</c> / <c>ITestToolDomainRegistrar</c>）是 owner 程序集的责任，
/// 故此处以内联桩提供——它们同时充当「产物形态可被消费方满足」的机械证据。
/// </para>
/// </remarks>
public class ToolSurfaceEmissionTests
{
    private const string OwnerAssembly = "ToolSurfaceE2E";

    private const string SdkAssemblyName = "Test.Sdk";

    private const string GoldenFileName = "TestToolSchemas.golden.txt";

    // ────────── 测试输入 ──────────

    /// <summary>剖面声明（全部槽位齐备；owner 程序集 = 本测试编译的程序集名）。</summary>
    private const string ProfileSource = """
        using Mud.AI;
        using Mud.AI.Attributes;

        [SdkToolProfile("Test",
            ToolAttributeName = "TestTool",
            ToolAttributeNamespace = "Test.Tools",
            ToolHandlerAttributeName = "TestToolHandler",
            ToolHandlerAttributeNamespace = "Test.Tools",
            ParameterAttributeName = "ToolParameter",
            SdkNamespaceRoot = "Test.Sdk",
            InterfaceNameRegex = "^ITest(?:Tenant|User)?V[0-9]+(?<domain>[A-Za-z]+)$",
            TokenKindStrategy = TokenKindDerivationStrategy.NamePrefix,
            TokenKindMarkers = "ITestTenant=Tenant;ITestUser=User",
            ProductPrefix = "TestTool",
            ProductPluralPrefix = "TestTools",
            DiagnosticPrefix = "MUDTT",
            DiagnosticCategory = "Test.AI",
            SchemaExtensionKey = "x-test",
            OwnerAssembly = "ToolSurfaceE2E",
            GeneratedNamespace = "Test.Tools.Generated",
            ContractNamespace = "Test.Tools",
            RegistrationNamespace = "Test.Tools.Registration",
            RiskEnumFullName = "Test.Tools.TestToolRisk",
            ResultTypeName = "TestToolResult",
            BindingTypeName = "TestToolBinding",
            SdkInterfacePrefix = "ITest",
            CapabilityCatalogPropertyName = "TestToolCatalog",
            GoldenFileName = "TestToolSchemas.golden.txt",
            GoldenUpdatePropertyName = "TestToolGoldenUpdate",
            GuidanceDirectory = "/Guidance/",
            AffordanceDirectory = "/Affordance/",
            WriteVerbKeywords = "delete|create")]
        sealed class TestToolProfile : ISdkToolProfile { }
        """;

    /// <summary>SDK 程序集（独立编译后以 MetadataReference 注入；Tier R 目录按程序集名定位它）。</summary>
    private const string SdkAssemblySource = """
        using System.Threading;
        using System.Threading.Tasks;
        using Mud.HttpUtils.Attributes;

        namespace Test.Sdk
        {
            public sealed class TestPayload
            {
                [System.Text.Json.Serialization.JsonPropertyName("id")]
                public string Id { get; set; } = string.Empty;
            }

            public interface ITestTenantV1Bitable
            {
                [Get("/bitable/apps")]
                Task<TestPayload> ListAsync(CancellationToken cancellationToken = default);
            }

            public interface ITestTenantV1BitableRecord
            {
                [Delete("/bitable/records/{recordId}")]
                Task<TestPayload> DeleteRecordAsync([Path] string recordId, CancellationToken cancellationToken = default);
            }
        }
        """;

    /// <summary>消费方手写 helper 桩（owner 程序集侧的真实责任面）。</summary>
    private const string ConsumerSource = """
        using System;
        using System.Collections.Generic;
        using System.Threading;
        using System.Threading.Tasks;

        namespace Test.Tools
        {
            public enum TestToolRisk { Read = 0, Write = 1, HighRiskWrite = 2 }

            public sealed class TestToolResult { }

            public sealed class TestToolBinding { }

            public sealed class TestToolRegistry { }

            public interface ITestToolDomainRegistrar
            {
                void Register(TestToolRegistry registry);
            }

            public static class TestToolRegistration
            {
                public static void RegisterExecution(
                    TestToolRegistry registry,
                    string toolName,
                    TestToolBinding binding,
                    Func<IReadOnlyDictionary<string, object?>, CancellationToken, Task<TestToolResult>> executor)
                {
                    _ = registry;
                    _ = toolName;
                    _ = binding;
                    _ = executor;
                }
            }

            /// <summary>解包 helper（owner 程序集手写；产物只引用其类型与成员名）。</summary>
            public static class ToolArgs
            {
                public static string RequireString(IReadOnlyDictionary<string, object?> args, string name)
                    => args.TryGetValue(name, out var value) ? value as string ?? string.Empty : string.Empty;

                public static string? OptionalString(IReadOnlyDictionary<string, object?> args, string name)
                    => args.TryGetValue(name, out var value) ? value as string : null;

                public static string[] RequireStringArray(IReadOnlyDictionary<string, object?> args, string name)
                    => args.TryGetValue(name, out var value) && value is string[] array ? array : Array.Empty<string>();

                public static string[]? OptionalStringArray(IReadOnlyDictionary<string, object?> args, string name)
                    => args.TryGetValue(name, out var value) ? value as string[] : null;

                public static int RequireInt(IReadOnlyDictionary<string, object?> args, string name)
                    => args.TryGetValue(name, out var value) && value is int number ? number : 0;

                public static int? OptionalInt(IReadOnlyDictionary<string, object?> args, string name)
                    => args.TryGetValue(name, out var value) ? value as int? : null;

                public static bool? OptionalBool(IReadOnlyDictionary<string, object?> args, string name)
                    => args.TryGetValue(name, out var value) ? value as bool? : null;
            }

            [AttributeUsage(AttributeTargets.Interface)]
            public sealed class TestToolAttribute : Attribute
            {
                public TestToolAttribute(string name) => Name = name;

                public string Name { get; }

                public string? Description { get; set; }

                public string? Source { get; set; }

                public bool IsWrite { get; set; }

                public string[]? RequiredScopes { get; set; }

                public string[]? AnyOf { get; set; }
            }

            [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method)]
            public sealed class ToolParameterAttribute : Attribute
            {
                public ToolParameterAttribute(string name) => Name = name;

                public string Name { get; }

                public bool Required { get; set; }
            }

            [AttributeUsage(AttributeTargets.Method)]
            public sealed class TestToolHandlerAttribute : Attribute
            {
                public TestToolHandlerAttribute(Type toolInterface) => ToolInterface = toolInterface;

                public Type ToolInterface { get; }
            }
        }

        /// <summary>
        /// 域注册器收集助手（owner 侧手写）。落点必须是 <c>RegistrationNamespace</c>：
        /// DI 装配产物以全限定名 <c>{RegistrationNamespace}.{P}DomainRegistrars</c> 引用它
        /// （与它收集的域注册器同命名空间）。
        /// </summary>
        namespace Test.Tools.Registration
        {
            public static class TestToolDomainRegistrars
            {
                public static void Add(Microsoft.Extensions.DependencyInjection.IServiceCollection services,
                    Func<IServiceProvider, Test.Tools.ITestToolDomainRegistrar?> factory)
                {
                    _ = services;
                    _ = factory;
                }
            }
        }
        """;

    /// <summary>策展工具接口 + 执行器（读工具：源方法为 GET）。</summary>
    private const string CuratedToolsSource = """
        using System;
        using System.Collections.Generic;
        using System.Threading;
        using System.Threading.Tasks;
        using Test.Tools;

        namespace Test.Tools
        {
            [TestTool("bitable.list", Description = "列出多维表格", Source = "ITestTenantV1Bitable.ListAsync")]
            public interface ITestTenantV1BitableListTool
            {
                /// <summary>列出多维表格。</summary>
                /// <param name="appToken">多维表格 app token。</param>
                string List([ToolParameter("appToken", Required = true)] string appToken);
            }

            public sealed class BitableTools
            {
                public BitableTools(Test.Sdk.ITestTenantV1Bitable sdk) => Sdk = sdk;

                public Test.Sdk.ITestTenantV1Bitable Sdk { get; }

                [TestToolHandler(typeof(ITestTenantV1BitableListTool))]
                public Task<TestToolResult> HandleListAsync(
                    IReadOnlyDictionary<string, object?> args,
                    CancellationToken cancellationToken)
                {
                    _ = args;
                    _ = cancellationToken;
                    return Task.FromResult(new TestToolResult());
                }
            }
        }
        """;

    /// <summary>写工具（声明 <c>IsWrite = true</c>，源方法为 DELETE）——锁定读写分类的单一真相源。</summary>
    private const string WriteToolSource = """
        using System;
        using System.Collections.Generic;
        using System.Threading;
        using System.Threading.Tasks;
        using Test.Tools;

        namespace Test.Tools
        {
            [TestTool("bitable.delete_record", Description = "删除记录", Source = "ITestTenantV1BitableRecord.DeleteRecordAsync", IsWrite = true)]
            public interface ITestTenantV1BitableRecordTool
            {
                /// <summary>删除记录。</summary>
                /// <param name="recordId">记录 ID。</param>
                string Delete([ToolParameter("recordId", Required = true)] string recordId);
            }

            public sealed class RecordTools
            {
                public RecordTools(Test.Sdk.ITestTenantV1BitableRecord sdk) => Sdk = sdk;

                public Test.Sdk.ITestTenantV1BitableRecord Sdk { get; }

                [TestToolHandler(typeof(ITestTenantV1BitableRecordTool))]
                public Task<TestToolResult> HandleDeleteAsync(
                    IReadOnlyDictionary<string, object?> args,
                    CancellationToken cancellationToken)
                {
                    _ = args;
                    _ = cancellationToken;
                    return Task.FromResult(new TestToolResult());
                }
            }
        }
        """;

    /// <summary>
    /// 诊断富集输入：<c>Source</c> 指向不存在的方法（槽位 019）+ 参数无 XML 文档（槽位 006）。
    /// </summary>
    private const string DiagnosticRichSource = """
        using System;
        using System.Collections.Generic;
        using System.Threading;
        using System.Threading.Tasks;
        using Test.Tools;

        namespace Test.Tools
        {
            [TestTool("bitable.probe", Description = "探针工具", Source = "ITestTenantV1Bitable.NotThereAsync")]
            public interface ITestTenantV1BitableProbeTool
            {
                string Probe(string raw);
            }

            public sealed class ProbeTools
            {
                public ProbeTools(Test.Sdk.ITestTenantV1Bitable sdk) => Sdk = sdk;

                public Test.Sdk.ITestTenantV1Bitable Sdk { get; }

                [TestToolHandler(typeof(ITestTenantV1BitableProbeTool))]
                public Task<TestToolResult> HandleProbeAsync(
                    IReadOnlyDictionary<string, object?> args,
                    CancellationToken cancellationToken)
                {
                    _ = args;
                    _ = cancellationToken;
                    return Task.FromResult(new TestToolResult());
                }
            }
        }
        """;

    // ────────── 全链路 ──────────

    [Fact]
    public void FullPipeline_EmitsEveryOutputPath_AndGeneratedCodeCompiles()
    {
        var run = Run(
            [ProfileSource, ConsumerSource, CuratedToolsSource],
            additionalTexts:
            [
                new InMemoryAdditionalText("/proj/Guidance/bitable.md", "# Bitable 域指引"),
            ],
            buildProperties: new Dictionary<string, string> { ["build_property.TestToolCatalog"] = "true" });

        var hintNames = HintNames(run);

        hintNames.Should().Contain(
        [
            "TestToolSchemas.g.cs",
            "TestToolNames.g.cs",
            "TestToolContracts.g.cs",
            "TestToolArgs/BitableListArgs.g.cs",
            "TestToolDomainRegistrars/BitableToolDomainRegistrar.g.cs",
            "TestToolsServiceCollectionCoreExtensions.g.cs",
            "TestToolGuidance.g.cs",
            "TestToolCapabilityCatalog.g.cs",
            "TestToolMethodCatalog.g.cs",
        ], "§5.1 的 8 条输出路径 + 方法级目录必须全部参与（漏任何一条都属「漏搬 Emitter 分支」的 R-2b 缺陷）");

        run.Result.Diagnostics.Should().NotContain(
            d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning,
            "正向用例不得产出 Error/Warning 级工具面诊断");

        AssertGeneratedCodeCompiles(run.Output);

        // 参数解包产物与解包映射表同源（§5.2-1）：参数特性声明的必填性决定读取器形态。
        var args = Normalize(Source(run, "TestToolArgs/BitableListArgs.g.cs"));
        args.Should()
            .Contain("internal const string ToolName = \"bitable.list\"")
            .And.Contain("ToolArgs.RequireString(args, \"appToken\")")
            .And.Contain("public string AppToken { get; }");

        // 描述符信封：厂商扩展键 / 令牌身份 / 返回契约均来自剖面与 SDK 符号（§2 v2.1 / §3）。
        var schemas = Normalize(Source(run, "TestToolSchemas.g.cs"));
        schemas.Should()
            .Contain("\"x-test\":")
            .And.Contain("\"identity\":\"tenant\"")
            .And.Contain("\"required\":[\"appToken\"]")
            .And.Contain("\"output_schema\":");
        schemas.Should().NotContain("app_token",
            "模型可见键 = C# 参数名（渲染产物口径）；参数特性的声明名必须与之一致，否则触发槽位 015");
    }

    [Fact]
    public void OwnerGate_NonOwnerAssembly_EmitsOnlySchemaConstants()
    {
        // owner 门槛（§5.2-2）：Names/Contracts/Args/Registrars/Guidance 只发射进 owner 程序集，
        // 否则其他声明工具特性样例接口的工程会同名类型冲突（CS0433）。
        var run = Run(
            [ProfileSource, ConsumerSource, CuratedToolsSource],
            additionalTexts: [new InMemoryAdditionalText("/proj/Guidance/bitable.md", "# Bitable 域指引")],
            assemblyName: "SomeOtherAssembly");

        HintNames(run).Should().Equal("TestToolSchemas.g.cs");
    }

    [Fact]
    public void CapabilityCatalog_OnlyEmittedWhenProfilePropertyEnabled()
    {
        var enabled = Run(
            [ProfileSource, ConsumerSource, CuratedToolsSource],
            buildProperties: new Dictionary<string, string> { ["build_property.TestToolCatalog"] = "true" });

        HintNames(enabled).Should().Contain("TestToolCapabilityCatalog.g.cs");

        var catalog = Normalize(Source(enabled, "TestToolCapabilityCatalog.g.cs"));
        catalog.Should().Contain("public const int SdkMethodCount = 2",
            "Tier R 目录枚举 SDK 程序集内 I{前缀}* 接口自身声明的方法");
        catalog.Should().Contain("public const int CuratedToolCount = 1");
        catalog.Should().Contain("public const int DomainCount = 2");
        catalog.Should().Contain("[\"Bitable\"] = 1", "分组轴 = 接口名解析出的 domain 段");
        catalog.Should().Contain("[\"BitableRecord\"] = 1");

        // 槽位 018 是聚合单条 Info（§6.1 非零容忍）。
        var coverage = enabled.Result.Diagnostics.Where(d => d.Id == "MUDTT018").ToList();
        coverage.Should().ContainSingle();
        coverage[0].Severity.Should().Be(DiagnosticSeverity.Info);

        var disabled = Run([ProfileSource, ConsumerSource, CuratedToolsSource]);
        HintNames(disabled).Should().NotContain("TestToolCapabilityCatalog.g.cs");
        HintNames(disabled).Should().NotContain("TestToolMethodCatalog.g.cs");
        disabled.Result.Diagnostics.Should().NotContain(d => d.Id == "MUDTT018");
    }

    // ────────── 方法级目录（MethodCatalog）──────────

    /// <summary>
    /// 方法目录逐方法发射结构化事实（接口名 / HTTP / 路由 / 令牌 / 域 / 风险 / 参数位置 / 策展标记），
    /// 是运行时 schema 自省（<c>schema_read</c>）与万能兜底（<c>api_call</c>）的唯一事实源。
    /// </summary>
    [Fact]
    public void MethodCatalog_EmitsEntryPerSdkMethod_WithHttpAndRouteFacts()
    {
        var run = Run(
            [ProfileSource, ConsumerSource, CuratedToolsSource, WriteToolSource],
            buildProperties: new Dictionary<string, string> { ["build_property.TestToolCatalog"] = "true" });

        var catalog = Normalize(Source(run, "TestToolMethodCatalog.g.cs"));

        // SDK 有 2 个接口 × 各 1 方法 = 2 条目（与 CapabilityCatalog 的 SdkMethodCount 同源）。
        catalog.Should().Contain("public const int SdkMethodCount = 2");

        // ITestTenantV1Bitable.ListAsync → GET /bitable/apps，tenant 令牌，Bitable 域，risk=read(0)，已策展。
        catalog.Should().Contain("\"ITestTenantV1Bitable.ListAsync\"")
            .And.Contain("\"GET\"")
            .And.Contain("\"/bitable/apps\"")
            .And.Contain("\"tenant\"")
            .And.Contain("\"Bitable\"");

        // ITestTenantV1BitableRecord.DeleteRecordAsync → DELETE /bitable/records/{recordId}，risk=high-risk-write(2)。
        catalog.Should().Contain("\"ITestTenantV1BitableRecord.DeleteRecordAsync\"")
            .And.Contain("\"DELETE\"")
            .And.Contain("\"/bitable/records/{recordId}\"")
            .And.Contain("\"BitableRecord\"");

        // 路径参数提取：DeleteRecordAsync 的 recordId 是 [Path] 参数。
        catalog.Should().Contain("\"recordId\"");

        // 策展标记：ListAsync 被 bitable.list 工具策展，DeleteRecordAsync 被 bitable.delete_record 策展。
        // 渲染形态：构造器末尾两实参 = (curated, curatedTool)，策展方法为 (true, "tool.name")。
        catalog.Should().Contain("true, \"bitable.list\"")
            .And.Contain("true, \"bitable.delete_record\"");

        // ByQualifiedName 字典必须包含两个键。
        catalog.Should().Contain("ByQualifiedName");
    }

    /// <summary>
    /// 未开启 CapabilityCatalog 开关时方法目录同样不发射（与方法目录共享同一开关）。
    /// </summary>
    [Fact]
    public void MethodCatalog_NotEmitted_WhenCatalogDisabled()
    {
        var run = Run([ProfileSource, ConsumerSource, CuratedToolsSource]);
        HintNames(run).Should().NotContain("TestToolMethodCatalog.g.cs");
    }

    // ────────── golden（槽位 014）──────────

    [Fact]
    public void Golden_Mismatch_ReportsSlot014_AndProducesSchemaConstantsAnyway()
    {
        var run = Run(
            [ProfileSource, ConsumerSource, CuratedToolsSource],
            additionalTexts: [new InMemoryAdditionalText("/proj/" + GoldenFileName, "bitable.list\t{stale}\n")]);

        var drift = run.Result.Diagnostics.Where(d => d.Id == "MUDTT014").ToList();
        drift.Should().ContainSingle("golden 快照不一致必须恰好上报一条槽位 014");
        drift[0].Severity.Should().Be(DiagnosticSeverity.Error);
        drift[0].GetMessage().Should().Contain("TestToolGoldenUpdate", "漂移消息必须给出剖面声明的重固化属性名");

        HintNames(run).Should().Contain("TestToolSchemas.g.cs", "漂移不得阻断产物发射（否则无法用新产物更新快照）");

        // 缺 GoldenUpdatePropertyName 槽（非必填）时不得拼出 "-p:=true" 这种不可执行的提示。
        var withoutHint = Run(
            [WithoutSlot(ProfileSource, "GoldenUpdatePropertyName"), ConsumerSource, CuratedToolsSource],
            additionalTexts: [new InMemoryAdditionalText("/proj/" + GoldenFileName, "bitable.list\t{stale}\n")]);

        var message = withoutHint.Result.Diagnostics.First(d => d.Id == "MUDTT014").GetMessage();
        message.Should().NotContain("-p:=true").And.Contain("GoldenUpdatePropertyName");
    }

    [Fact]
    public void Golden_ExactMatch_ReportsNothing()
    {
        // 先跑一遍拿到引擎自产 golden，再以其为快照回灌 —— 锁定「golden 口径 == 产物口径」。
        var first = Run([ProfileSource, ConsumerSource, CuratedToolsSource]);
        var golden = GoldenText(first);

        golden.Should().NotBeEmpty();

        var second = Run(
            [ProfileSource, ConsumerSource, CuratedToolsSource],
            additionalTexts: [new InMemoryAdditionalText("/proj/" + GoldenFileName, golden)]);

        second.Result.Diagnostics.Should().NotContain(d => d.Id == "MUDTT014", "逐字节一致的 golden 不得报漂移");
    }

    [Fact]
    public void EmptyGoldenFileName_DoesNotSwallowUnrelatedAdditionalFiles()
    {
        // 剖面未声明 GoldenFileName（非必填槽，如 Wechat 初期剖面）时，任何 AdditionalFile 都不得被当作
        // golden 快照——否则消费方常见的 appsettings.json 会伪造出零容忍的槽位 014。
        var run = Run(
            [WithoutSlot(ProfileSource, "GoldenFileName"), ConsumerSource, CuratedToolsSource],
            additionalTexts: [new InMemoryAdditionalText("/proj/appsettings.json", "{ \"a\": 1 }")]);

        run.Result.Diagnostics.Should().NotContain(d => d.Id == "MUDTT014",
            "GoldenFileName 为空时不得把任意 AdditionalFile 当作 golden 快照");
    }

    // ────────── Guidance（L1/L2 资产键）──────────

    [Fact]
    public void Guidance_NestedTopics_UsePathRelativeAssetKey()
    {
        var run = Run(
            [ProfileSource, ConsumerSource, CuratedToolsSource],
            additionalTexts:
            [
                new InMemoryAdditionalText("/proj/Guidance/bitable.md", "# Bitable 域指引"),
                new InMemoryAdditionalText("/proj/Guidance/bitable/faq.md", "## FAQ"),
            ]);

        var guidance = Normalize(Source(run, "TestToolGuidance.g.cs"));

        guidance.Should().Contain("[\"bitable\"] =", "L1 资产键为 {domain}（无子目录）");
        guidance.Should().Contain("[\"bitable/faq\"] =",
            "L2 资产键必须是相对 guidance 目录的路径去扩展名（{domain}/{topic}）");
        guidance.Should().NotContain("[\"bit\"] =",
            "L2 键不得因下标错位而退化为路径前缀片段，更不得混入 L1 的 ByDomain 字典");
        guidance.Should().Contain("ReferenceKeys").And.Contain("\"bitable/faq\"",
            "L2 资产必须同时进入 ReferenceKeys（guidance_read 的可读清单）");
    }

    [Fact]
    public void Guidance_OnlyNestedAssets_StillEmitsReferences()
    {
        // 只有 L2 资产（无 L1 域文件）时，References / ReferenceKeys 仍必须产出——
        // 它们是 guidance_read 元工具的唯一数据源，整体丢弃即静默丢失素材。
        var run = Run(
            [ProfileSource, ConsumerSource, CuratedToolsSource],
            additionalTexts: [new InMemoryAdditionalText("/proj/Guidance/im/replies.md", "## 回复规范")]);

        var guidance = Normalize(Source(run, "TestToolGuidance.g.cs"));
        guidance.Should().Contain("[\"im/replies\"] = \"## 回复规范\"");
        guidance.Should().Contain("ReferenceKeys").And.Contain("\"im/replies\"");
    }

    [Fact]
    public void EmptyGuidanceDirectory_DoesNotCaptureUnrelatedMarkdown()
    {
        // 剖面未声明 GuidanceDirectory（非必填槽）时，任意 .md AdditionalFile 都不得被当作 guidance 资产——
        // 否则「域名键」会被路径前缀片段污染（如 docs/faq.md ⇒ 键 "docs/f"）。
        var run = Run(
            [WithoutSlot(ProfileSource, "GuidanceDirectory"), ConsumerSource, CuratedToolsSource],
            additionalTexts:
            [
                new InMemoryAdditionalText("/proj/bitable.md", "# 域指引"),
                new InMemoryAdditionalText("/proj/docs/faq.md", "## FAQ"),
            ]);

        HintNames(run).Should().NotContain("TestToolGuidance.g.cs",
            "GuidanceDirectory 为空时不得吸收任何 markdown 资产");
    }

    // ────────── per-tool affordance（F-7）──────────

    /// <summary>一份结构合法的 affordance 资产（四节齐备且非空）。</summary>
    private const string AffordanceAsset = """
        ## WHEN
        需要按条件筛选/排序读取多维表格时。

        ## Avoid when
        - 已知 record_id 且只要一条记录时，改用更省 token 的单条读取工具。

        ## Prerequisites
        - 需要目标表的 app_token 与 table_id（先调用列表类工具拿到）。

        ## Tips
        - page_size 不由模型控制（平台默认），翻页只用 page_token。
        """;

    [Fact]
    public void Affordance_ShouldFlowIntoDescriptorExtension_WithFourArrays()
    {
        var run = Run(
            [ProfileSource, ConsumerSource, CuratedToolsSource],
            additionalTexts: [new InMemoryAdditionalText("/proj/Affordance/bitable.list.md", AffordanceAsset)]);

        var schemas = Normalize(Source(run, "TestToolSchemas.g.cs"));

        schemas.Should().Contain("\"affordance\":{\"when\":[",
            "工具级 affordance 必须落在 x-<sdk> 扩展块内（模型可见面与 golden 同源）");
        schemas.Should().Contain("\"avoid_when\":[")
            .And.Contain("\"prerequisites\":[")
            .And.Contain("\"tips\":[")
            .And.Contain("page_size 不由模型控制");

        // 键顺序即契约（golden 逐字节可比）：when → avoid_when → prerequisites → tips。
        schemas.IndexOf("\"avoid_when\"", StringComparison.Ordinal)
            .Should().BeGreaterThan(schemas.IndexOf("\"when\"", StringComparison.Ordinal));
        schemas.IndexOf("\"prerequisites\"", StringComparison.Ordinal)
            .Should().BeGreaterThan(schemas.IndexOf("\"avoid_when\"", StringComparison.Ordinal));
        schemas.IndexOf("\"tips\"", StringComparison.Ordinal)
            .Should().BeGreaterThan(schemas.IndexOf("\"prerequisites\"", StringComparison.Ordinal));

        // 项目符号行各成一条、普通行合并为一条（词法确定性）。
        schemas.Should().Contain("\"when\":[\"需要按条件筛选/排序读取多维表格时。\"]");
        run.Result.Diagnostics.Should().NotContain(d => d.Id.StartsWith("MUDTT02", StringComparison.Ordinal)
            || d.Id.StartsWith("MUDTT03", StringComparison.Ordinal),
            "合法资产不得产出任何 affordance 诊断");
    }

    [Fact]
    public void WithoutAffordanceDirectory_DescriptorIsByteIdenticalToEnabledButAssetless()
    {
        // 回归锚（本仓 FeishuToolSchemas.golden.txt 的同款自证）：
        // 「未声明槽（空串）」与「声明了槽但无任何资产」必须产出逐字节相同的描述符 ——
        // 这是「可空槽 ⇒ 组件升级对既有消费方零影响」的机械证据。
        var disabled = Run([WithoutSlot(ProfileSource, "AffordanceDirectory"), ConsumerSource, CuratedToolsSource]);
        var enabled = Run([ProfileSource, ConsumerSource, CuratedToolsSource]);

        var enabledGolden = GoldenText(enabled);
        enabledGolden.Should().Be(GoldenText(disabled));
        enabledGolden.Should().NotContain("affordance", "无资产时不得发射 affordance 子块");
    }

    [Fact]
    public void EmptyAffordanceDirectory_DoesNotCaptureUnrelatedMarkdown()
    {
        // 空串槽（非必填）时，任意 .md 都不得被当作 affordance 资产——否则描述符会被
        // 以"文件名不是工具名"批量产出零容忍的 029，升级即构建失败。
        var run = Run(
            [WithoutSlot(ProfileSource, "AffordanceDirectory"), ConsumerSource, CuratedToolsSource],
            additionalTexts:
            [
                new InMemoryAdditionalText("/proj/Affordance/bitable.list.md", AffordanceAsset),
                new InMemoryAdditionalText("/proj/readme.md", "# 无关文档"),
            ]);

        GoldenText(run).Should().NotContain("affordance",
            "AffordanceDirectory 为空时不得吸收任何 markdown 资产");
        run.Result.Diagnostics.Should().NotContain(d => d.Id == "MUDTT028"
            || d.Id == "MUDTT029"
            || d.Id == "MUDTT030"
            || d.Id == "MUDTT031");
    }

    [Fact]
    public void Affordance_MissingSectionAndEmptySection_ReportStructureError()
    {
        // 缺 Tips 节 → 028。
        var missing = Run(
            [ProfileSource, ConsumerSource, CuratedToolsSource],
            additionalTexts: [new InMemoryAdditionalText(
                "/proj/Affordance/bitable.list.md",
                "## WHEN\n列出表格时。\n\n## Avoid when\n- 只要一条时改用单条读取。\n\n## Prerequisites\n- 需要 app_token。\n")]);

        var missingDiagnostic = missing.Result.Diagnostics.First(d => d.Id == "MUDTT028");
        missingDiagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
        missingDiagnostic.GetMessage().Should().Contain("Tips", "必须点名缺失的小节");

        // 未知小节 → 028；空小节 → 028。
        var unknown = Run(
            [ProfileSource, ConsumerSource, CuratedToolsSource],
            additionalTexts: [new InMemoryAdditionalText(
                "/proj/Affordance/bitable.list.md",
                "## WHEN\n列出表格时。\n\n## Examples\n- 例子。\n")]);
        unknown.Result.Diagnostics.First(d => d.Id == "MUDTT028").GetMessage().Should().Contain("Examples");

        var empty = Run(
            [ProfileSource, ConsumerSource, CuratedToolsSource],
            additionalTexts: [new InMemoryAdditionalText(
                "/proj/Affordance/bitable.list.md",
                "## WHEN\n\n## Avoid when\n- 只要一条时改用单条读取。\n\n## Prerequisites\n- 需要 app_token。\n\n## Tips\n- 翻页只用 page_token。\n")]);
        empty.Result.Diagnostics.First(d => d.Id == "MUDTT028").GetMessage().Should().Contain("为空");

        // 结构非法 ⇒ 该工具不得发射半成品 affordance（宁缺勿错）。
        GoldenText(missing).Should().NotContain("affordance");
    }

    [Fact]
    public void Affordance_OrphanAsset_ReportsOrphanError()
    {
        var run = Run(
            [ProfileSource, ConsumerSource, CuratedToolsSource],
            additionalTexts:
            [
                new InMemoryAdditionalText("/proj/Affordance/bitable.list.md", AffordanceAsset),
                new InMemoryAdditionalText("/proj/Affordance/bitable.list_records.md", AffordanceAsset),
            ]);

        var orphan = run.Result.Diagnostics.Where(d => d.Id == "MUDTT029").ToList();
        orphan.Should().ContainSingle("文件名不命中工具名契约表的资产必须被点名");
        orphan[0].Severity.Should().Be(DiagnosticSeverity.Error);
        orphan[0].GetMessage().Should().Contain("bitable.list_records");

        GoldenText(run).Should().Contain("\"affordance\"", "合法资产仍须正常发射（单份坏资产不得连坐）");
    }

    [Fact]
    public void Affordance_DanglingToolReference_ReportsReferenceError()
    {
        var run = Run(
            [ProfileSource, ConsumerSource, CuratedToolsSource],
            additionalTexts: [new InMemoryAdditionalText(
                "/proj/Affordance/bitable.list.md",
                "## WHEN\n列出表格时。\n\n## Avoid when\n- 只要一条时改用 `im.not_a_tool`。\n\n## Prerequisites\n- 需要 `bitable.list` 已可用。\n\n## Tips\n- 见 `docs/guide`（非工具名形态，不参与校验）。\n")]);

        var dangling = run.Result.Diagnostics.Where(d => d.Id == "MUDTT030").ToList();
        dangling.Should().ContainSingle("反引号包裹的 '域.工具' 引用不存在时必须报红");
        dangling[0].GetMessage().Should().Contain("im.not_a_tool");
        dangling[0].GetMessage().Should().NotContain("docs/guide", "非 '域.工具' 形态不得参与校验（防假红）");
    }

    [Fact]
    public void Affordance_OverBudget_ReportsSingleAggregatedWarning_AndQuietWhenWithinBudget()
    {
        var longTips = new string('x', 400);
        var overBudget = Run(
            [ProfileSource, ConsumerSource, CuratedToolsSource],
            additionalTexts: [new InMemoryAdditionalText(
                "/proj/Affordance/bitable.list.md",
                $"## WHEN\n列出表格时。\n\n## Avoid when\n- 只要一条时改用单条读取。\n\n## Prerequisites\n- 需要 app_token。\n\n## Tips\n- {longTips}\n")]);

        var budget = overBudget.Result.Diagnostics.Where(d => d.Id == "MUDTT031").ToList();
        budget.Should().ContainSingle("031 必须是聚合单条（逐资产报会随资产数放大日志）");
        budget[0].Severity.Should().Be(DiagnosticSeverity.Warning, "超预算是内容偏胖，不得阻断构建");
        budget[0].GetMessage().Should().Contain("1 份", "消息必须携带份数，供消费方建立语义量基线（同 009-tools 口径）");
        budget[0].GetMessage().Should().Contain("bitable.list(");

        var within = Run(
            [ProfileSource, ConsumerSource, CuratedToolsSource],
            additionalTexts: [new InMemoryAdditionalText("/proj/Affordance/bitable.list.md", AffordanceAsset)]);
        within.Result.Diagnostics.Should().NotContain(d => d.Id == "MUDTT031", "未超界不得报预算");
    }

    // ────────── 读写分类的单一真相源 ──────────

    [Fact]
    public void IsWrite_NamesContractsAndSchemaExtension_AgreeOnTheSameSource()
    {
        var run = Run([ProfileSource, ConsumerSource, CuratedToolsSource, WriteToolSource]);

        var names = Source(run, "TestToolNames.g.cs");
        var contracts = Source(run, "TestToolContracts.g.cs");
        var schemas = Source(run, "TestToolSchemas.g.cs");

        // 写工具（IsWrite=true，源方法 DELETE 命中危险词 "delete" ⇒ risk=high-risk-write）。
        WriteAllOf(names).Should().Contain("BitableDeleteRecord", "声明的写工具必须进 WriteAll");
        ReadonlyAllOf(names).Should().NotContain("BitableDeleteRecord");
        schemas.Should().Contain("\"is_write\":true")
            .And.Contain("\"risk\":\"high-risk-write\"",
                "危险词表命中必须把风险升级为 high-risk-write（§4.2 第 7 项）");

        // 读工具（源方法 GET ⇒ risk=read）。
        ReadonlyAllOf(names).Should().Contain("BitableList");
        WriteAllOf(names).Should().NotContain("BitableList");
        schemas.Should().Contain("\"is_write\":false");

        // 契约表的 IsWrite 必须与 Names 的读写分组同源（否则出现「第二真相源」）。
        ContractsIsWrite(contracts, "TestToolContract BitableList { get; }").Should().BeFalse(
            "只读工具在契约表中必须 IsWrite=false（与 ReadonlyAll 同源）");
        ContractsIsWrite(contracts, "TestToolContract BitableDeleteRecord { get; }").Should().BeTrue(
            "写工具在契约表中必须 IsWrite=true（与 WriteAll 同源）");
    }

    /// <summary>
    /// 声明与 SDK 事实脱钩的写面：<c>IsWrite</c> 未声明，但 <c>Source</c> 指向 DELETE 方法。
    /// 引擎必须①上报槽位 017，②把读写分类统一收敛到 risk（Names / Contracts / Schema 三处一致），
    /// 不得出现「Names 说只读、Contracts 说写」的第二真相源。
    /// </summary>
    [Fact]
    public void IsWrite_DeclaredFlagDivergesFromSdkFact_ReportsSlot017AndClassifiesByRisk()
    {
        const string declaredReadButWriteSource = """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using Test.Tools;

            namespace Test.Tools
            {
                [TestTool("bitable.record_delete", Description = "删除记录（声明只读，SDK 源为写）", Source = "ITestTenantV1BitableRecord.DeleteRecordAsync")]
                public interface ITestTenantV1BitableRecordDeleteTool
                {
                    /// <summary>删除记录。</summary>
                    /// <param name="recordId">记录 ID。</param>
                    string Delete(string recordId);
                }

                public sealed class RecordDeleteTools
                {
                    public RecordDeleteTools(Test.Sdk.ITestTenantV1BitableRecord sdk) => Sdk = sdk;

                    public Test.Sdk.ITestTenantV1BitableRecord Sdk { get; }

                    [TestToolHandler(typeof(ITestTenantV1BitableRecordDeleteTool))]
                    public Task<TestToolResult> HandleDeleteAsync(
                        IReadOnlyDictionary<string, object?> args, CancellationToken cancellationToken)
                    {
                        _ = args;
                        _ = cancellationToken;
                        return Task.FromResult(new TestToolResult());
                    }
                }
            }
            """;

        var run = Run([ProfileSource, ConsumerSource, CuratedToolsSource, declaredReadButWriteSource]);

        run.Result.Diagnostics.Should().Contain(d => d.Id == "MUDTT017",
            "SDK 源为写面却归类为只读必须上报槽位 017（否则写面绕过授权门禁）");

        var names = Source(run, "TestToolNames.g.cs");
        WriteAllOf(names).Should().Contain("BitableRecordDelete", "写面必须进 WriteAll（保守口径）");
        ReadonlyAllOf(names).Should().NotContain("BitableRecordDelete");

        Source(run, "TestToolContracts.g.cs").Should().Contain("IsWrite: true");
        Source(run, "TestToolSchemas.g.cs").Should().Contain("\"is_write\":true");
    }

    // ────────── 诊断契约 ──────────

    [Fact]
    public void EmittedDiagnostics_AllBelongToTheSlotTable()
    {
        var run = Run([ProfileSource, ConsumerSource, CuratedToolsSource, DiagnosticRichSource]);

        var slots = ToolSurfaceDiagnostics.Slots.Select(static s => s.Slot).ToHashSet();
        var emitted = run.Result.Diagnostics.ToList();
        emitted.Should().NotBeEmpty("该用例应至少产出扫描期诊断，否则本守卫失去检出力");
        emitted.Select(static d => d.Id).Should().Contain("MUDTT019")
            .And.Contain("MUDTT006", "诊断富集输入必须真的命中既有上报点");

        foreach (var diagnostic in emitted)
        {
            diagnostic.Id.Should().StartWith("MUDTT", "剖面注入的动态 ID 前缀必须来自 DiagnosticPrefix 槽");
            var slotText = diagnostic.Id.Substring("MUDTT".Length);
            int.TryParse(slotText, out var slot).Should().BeTrue($"诊断 ID {diagnostic.Id} 的槽位段必须为数字");

            slots.Should().Contain(slot,
                $"诊断 {diagnostic.Id} 必须对应 ToolSurfaceDiagnostics.Slots 中的槽位定义（§6.1 槽位表 = 上报点全集）");

            // 消息实参个数必须与槽位模板的占位符个数一致：实参少于占位符时 string.Format 抛
            // FormatException（构建日志/IDE 展示期直接失败），属"参数契约漂移"的真实缺陷形态。
            diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture)
                .Should().NotBeNullOrWhiteSpace($"诊断 {diagnostic.Id} 必须能被格式化出非空消息");
        }
    }

    [Fact]
    public void EmittedDiagnostics_UseTheProfileCategory_ExceptToolingFallbackSlot()
    {
        var run = Run([ProfileSource, ConsumerSource, CuratedToolsSource, DiagnosticRichSource]);

        var emitted = run.Result.Diagnostics.ToList();
        emitted.Should().NotBeEmpty("无诊断时本守卫失去检出力");

        foreach (var diagnostic in emitted)
        {
            var expected = diagnostic.Id == "MUDTT026" ? "Mud.AI.Tooling" : "Test.AI";
            diagnostic.Descriptor.Category.Should().Be(expected);
        }
    }

    [Fact]
    public void DiagnosticDescriptors_SlotTableAndFactory_AreConsistent()
    {
        var profile = ResolveProfile(ProfileSource);
        var factory = ToolSurfaceDiagnostics.For(profile);

        foreach (var definition in ToolSurfaceDiagnostics.Slots)
        {
            var descriptor = factory[definition.Slot];
            descriptor.Id.Should().Be("MUDTT" + definition.Slot.ToString("D3"));
            descriptor.Title.ToString(System.Globalization.CultureInfo.InvariantCulture).TrimEnd('.')
                .Should().Be(definition.Title.TrimEnd('.'));
            descriptor.DefaultSeverity.Should().Be(definition.Severity);
            descriptor.MessageFormat.ToString(System.Globalization.CultureInfo.InvariantCulture)
                .Should().Be(definition.MessageFormat);

            var expectedCategory = definition.Slot == ToolSurfaceDiagnostics.SlotGeneratorInternalError
                ? profile.ToolingDiagnosticCategory
                : profile.DiagnosticCategory;
            descriptor.Category.Should().Be(expectedCategory);
        }
    }

    // ────────── 管线结构（Guard 覆盖）──────────

    [Fact]
    public void EveryRegisterSourceOutput_IsWrappedByGuard()
    {
        var source = File.ReadAllText(
            Path.Combine(TestRepoRoot.PathOf("Mud.AI.Generator"), "ToolSurface", "ToolSurfaceSourceGenerator.cs"));

        var registrationCount = CountOccurrences(source, "context.RegisterSourceOutput(");
        var guardCount = CountOccurrences(source, "Guard.WrapUnchecked<");

        registrationCount.Should().BeGreaterThan(0);
        guardCount.Should().Be(registrationCount,
            "§1.2-6：每条注册路径都必须经 Guard.WrapUnchecked 兜底"
            + "（行为用例覆盖面 = 想到的路径数，结构包装覆盖面 = 实际路径数）");
    }

    // ────────── 引擎增量（§10：Cached/Modified 运行理由断言）──────────

    /// <summary>六条追踪名（<c>WithTrackingName</c>）：无追踪名时增量回归无法定位到具体步骤。</summary>
    private static readonly string[] TrackedStepNames =
    [
        "ToolSurface_Profiles",
        "ToolSurface_ToolScan",
        "ToolSurface_HandlerScan",
        "ToolSurface_ModelsByProfile",
        "ToolSurface_HandlersByProfile",
    ];

    [Fact]
    public void UnrelatedEdit_ShouldCacheProfileScanAndHandlerSteps()
    {
        // 无关内容编辑（在与工具面无关的树里追加一个无关类）：剖面解析、工具扫描、执行器扫描
        // 都必须命中 Cached/Unchanged——否则 IDE 每次击键都会重跑整套工具面扫描。
        var editedConsumer = ConsumerSource + "\nnamespace Test.Tools { public sealed class Unrelated { } }\n";

        var tracked = RunTwiceWithTracking(
            [ProfileSource, ConsumerSource, CuratedToolsSource],
            [ProfileSource, editedConsumer, CuratedToolsSource]);

        foreach (var stepName in TrackedStepNames)
        {
            var step = tracked.TrackedSteps.FirstOrDefault(kvp => kvp.Key == stepName).Value;
            step.Should().NotBeNullOrEmpty($"应存在追踪步骤 {stepName}");

            var reasons = step.SelectMany(static s => s.Outputs).ToList();
            reasons.Should().NotBeEmpty($"{stepName} 应产出元素");
            reasons.Should().OnlyContain(
                static o => o.Reason == IncrementalStepRunReason.Cached || o.Reason == IncrementalStepRunReason.Unchanged,
                $"无关文件编辑不得让 {stepName} 标记为 Modified（实际：[{string.Join(", ", step.SelectMany(static s => s.Outputs).Select(static o => o.Reason))}]）");
        }
    }

    [Fact]
    public void CuratedInterfaceEdit_ShouldMarkToolScanModified()
    {
        // 工具接口声明的编辑（Description 变化）——替换而非新增第二棵树（同名类型会 CS0101）。
        var editedTool = CuratedToolsSource.Replace(
            "Description = \"列出多维表格\"",
            "Description = \"列出多维表格（改）\"",
            StringComparison.Ordinal);
        editedTool.Should().NotBe(CuratedToolsSource, "编辑必须真的落到声明上，否则本用例恒成立");

        var tracked = RunTwiceWithTracking(
            [ProfileSource, ConsumerSource, CuratedToolsSource],
            [ProfileSource, ConsumerSource, editedTool]);

        var scanStep = tracked.TrackedSteps.First(kvp => kvp.Key == "ToolSurface_ToolScan").Value;
        scanStep.SelectMany(static s => s.Outputs)
            .Should().Contain(o => o.Reason == IncrementalStepRunReason.Modified,
                "工具接口声明的编辑必须让工具扫描步骤 Modified，否则描述符产物静默陈旧");

        // 对照：剖面未编辑，独立步骤必须保持 Cached/Unchanged。
        var profileStep = tracked.TrackedSteps.First(kvp => kvp.Key == "ToolSurface_Profiles").Value;
        profileStep.SelectMany(static s => s.Outputs)
            .Should().OnlyContain(
                static o => o.Reason == IncrementalStepRunReason.Cached || o.Reason == IncrementalStepRunReason.Unchanged,
                "仅编辑工具接口不得让剖面解析步骤失效");
    }

    private static GeneratorRunResult RunTwiceWithTracking(string[] firstSources, string[] secondSources)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new[] { new ToolSurfaceSourceGenerator().AsSourceGenerator() },
            additionalTexts: null,
            parseOptions: parseOptions,
            optionsProvider: new TestAnalyzerConfigOptionsProvider(new Dictionary<string, string>()),
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

        driver = driver.RunGenerators(CreateCompilation(firstSources, OwnerAssembly));
        driver = driver.RunGenerators(CreateCompilation(secondSources, OwnerAssembly));
        return driver.GetRunResult().Results[0];
    }

    // ────────── 辅助 ──────────

    /// <summary>从剖面声明中整行去掉某个槽位赋值（用于「非必填槽缺省」用例）。</summary>
    private static string WithoutSlot(string profileSource, string slotName)
    {
        var marker = slotName + " = ";
        var start = profileSource.IndexOf(marker, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"剖面应声明槽位 {slotName}");

        var lineStart = profileSource.LastIndexOf('\n', start) + 1;
        var lineEnd = profileSource.IndexOf('\n', start);
        lineEnd.Should().BeGreaterThan(start, $"槽位 {slotName} 声明必须独占一行");

        return profileSource.Remove(lineStart, lineEnd - lineStart + 1);
    }

    private static int CountOccurrences(string text, string marker)
    {
        var count = 0;
        var index = text.IndexOf(marker, StringComparison.Ordinal);
        while (index >= 0)
        {
            count++;
            index = text.IndexOf(marker, index + marker.Length, StringComparison.Ordinal);
        }

        return count;
    }

    /// <summary>把原始字符串字面量归一为普通形态，便于按键名断言。</summary>
    private static string Normalize(string generatedSource) => generatedSource.Replace("\"\"\"", "\"");

    /// <summary>工具名契约表的 <c>ReadonlyAll</c> 元素（常量名）。</summary>
    private static List<string> ReadonlyAllOf(string namesSource)
        => ArraySection(namesSource, "public static readonly string[] ReadonlyAll");

    /// <summary>工具名契约表的 <c>WriteAll</c> 元素（常量名）。</summary>
    private static List<string> WriteAllOf(string namesSource)
        => ArraySection(namesSource, "public static readonly string[] WriteAll");

    private static List<string> ArraySection(string source, string marker)
    {
        var start = source.IndexOf(marker, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"产物中应存在 {marker}");

        // 注：marker 自身含 `string[]` 的方括号，故必须从 marker 之后开始找集合表达式。
        var open = source.IndexOf('[', start + marker.Length);
        var close = source.IndexOf(']', open);
        return source.Substring(open + 1, close - open - 1)
            .Split(',')
            .Select(static item => item.Trim())
            .Where(static item => item.Length > 0)
            .ToList();
    }

    /// <summary>取契约表中某属性块的 <c>IsWrite</c> 取值。</summary>
    private static bool ContractsIsWrite(string contractsSource, string propertyMarker)
    {
        var start = contractsSource.IndexOf(propertyMarker, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"产物中应存在契约属性 {propertyMarker}");

        var next = contractsSource.IndexOf("public static TestToolContract", start + propertyMarker.Length, StringComparison.Ordinal);
        var block = next < 0 ? contractsSource.Substring(start) : contractsSource.Substring(start, next - start);

        return block.Contains("IsWrite: true", StringComparison.Ordinal);
    }

    /// <summary>从 <c>{P}Schemas.g.cs</c> 反解 golden 文本（工具名 ⇥ 描述符 JSON，一行一工具）。</summary>
    private static string GoldenText((GeneratorDriverRunResult Result, Compilation Output) run)
        => BuildGoldenFromSchemas(Source(run, "TestToolSchemas.g.cs"));

    private static string BuildGoldenFromSchemas(string schemasSource)
    {
        var builder = new StringBuilder();
        foreach (var line in schemasSource.Split('\n'))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("public const string ", StringComparison.Ordinal)
                || trimmed.IndexOf("SchemaJson = ", StringComparison.Ordinal) < 0)
            {
                continue;
            }

            var valueStart = trimmed.IndexOf('=') + 1;
            var value = trimmed.Substring(valueStart).Trim().TrimEnd(';').Trim();
            var content = value.StartsWith("\"\"\"", StringComparison.Ordinal)
                ? value.Substring(3, value.Length - 6)
                : value.Substring(1, value.Length - 2);

            builder.Append(ReadToolName(content)).Append('\t').Append(content).Append('\n');
        }

        return builder.ToString();
    }

    private static string ReadToolName(string descriptorJson)
    {
        const string marker = "\"name\":\"";
        var start = descriptorJson.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = descriptorJson.IndexOf('"', start);
        return descriptorJson.Substring(start, end - start);
    }

    private static void AssertGeneratedCodeCompiles(Compilation output)
    {
        var errors = output.GetDiagnostics()
            .Where(static d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        errors.Should().BeEmpty(
            "生成产物必须可编译（含 owner 侧手写 helper 的装配形态）；错误：\n" +
            string.Join("\n", errors.Select(static e => e.ToString())));
    }

    private static List<string> HintNames((GeneratorDriverRunResult Result, Compilation Output) run)
        => run.Result.Results[0].GeneratedSources
            .Select(static s => s.HintName)
            .OrderBy(static h => h, StringComparer.Ordinal)
            .ToList();

    private static string Source((GeneratorDriverRunResult Result, Compilation Output) run, string hintName)
    {
        var generated = run.Result.Results[0].GeneratedSources.FirstOrDefault(s => s.HintName == hintName);
        generated.HintName.Should().Be(hintName, $"应产出 {hintName}");
        return generated.SourceText.ToString();
    }

    private static SdkToolProfileModel ResolveProfile(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        var compilation = CSharpCompilation.Create(
            OwnerAssembly,
            new[] { tree },
            BasicReferenceAssemblies.GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return ProfileDiscovery.ResolveProfiles(compilation).Single();
    }

    /// <summary>SDK 程序集引用（<c>Test.Sdk</c>）：Tier R 目录按程序集名定位 SDK（§4.2 第 2 项）。</summary>
    private static MetadataReference CreateSdkAssemblyReference()
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        var compilation = CSharpCompilation.Create(
            SdkAssemblyName,
            new[] { CSharpSyntaxTree.ParseText(SdkAssemblySource, parseOptions) },
            BasicReferenceAssemblies.GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var emitResult = compilation.Emit(stream);
        emitResult.Success.Should().BeTrue(
            "SDK 测试程序集必须可编译；错误：\n" +
            string.Join("\n", emitResult.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)));

        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    private static (GeneratorDriverRunResult Result, Compilation Output) Run(
        string[] sources,
        ImmutableArray<AdditionalText> additionalTexts = default,
        Dictionary<string, string>? buildProperties = null,
        string assemblyName = OwnerAssembly)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        var compilation = CreateCompilation(sources, assemblyName);

        // 输入自身必须可编译，否则后续断言全部失真（把「测试输入写错」与「生成器缺陷」区分开）。
        var inputErrors = compilation.GetDiagnostics()
            .Where(static d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();
        inputErrors.Should().BeEmpty(
            "测试输入必须自身可编译；错误：\n" + string.Join("\n", inputErrors.Select(static e => e.ToString())));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new[] { new ToolSurfaceSourceGenerator().AsSourceGenerator() },
            additionalTexts: additionalTexts.IsDefault ? null : additionalTexts,
            parseOptions: parseOptions,
            optionsProvider: new TestAnalyzerConfigOptionsProvider(buildProperties ?? new Dictionary<string, string>()));

        // 注：GeneratorDriver 不可变——必须持有 RunGeneratorsAndUpdateCompilation 返回的新实例，
        // 否则 GetRunResult() 读到的是「运行前」的空结果。
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        return (driver.GetRunResult(), output);
    }

    private static CSharpCompilation CreateCompilation(string[] sources, string assemblyName)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);

        // DI 装配产物依赖 Microsoft.Extensions.DependencyInjection.Abstractions（IServiceCollection /
        // TryAddSingleton / GetService），BasicReferenceAssemblies 未覆盖，按程序集名补引用。
        var references = BasicReferenceAssemblies.GetReferences();
        references.Add(MetadataReference.CreateFromFile(
            Assembly.Load("Microsoft.Extensions.DependencyInjection.Abstractions").Location));
        references.Add(CreateSdkAssemblyReference());

        return CSharpCompilation.Create(
            assemblyName,
            sources.Select(source => CSharpSyntaxTree.ParseText(source, parseOptions)).ToArray(),
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));
    }

    /// <summary>内存 AdditionalFile（golden 快照 / guidance 资产 / 无关附加文件）。</summary>
    private sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly SourceText _text;

        public InMemoryAdditionalText(string path, string content)
        {
            Path = path;
            _text = SourceText.From(content, Encoding.UTF8);
        }

        public override string Path { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
    }
}