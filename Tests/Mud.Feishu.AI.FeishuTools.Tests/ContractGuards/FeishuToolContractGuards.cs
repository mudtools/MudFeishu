// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.FeishuTools.Tools;

namespace Mud.Feishu.AI.FeishuTools.Tests.ContractGuards;

/// <summary>
/// 工具名契约表守卫（Phase 1 §7）：Schema 注册表恰为 §3.3.2 的 10 个契约名，防增删/改名漂移。
/// </summary>
public class FeishuToolContractGuards
{
    [Fact]
    public void SchemaRegistry_ShouldContainExactlyTheTenContractTools()
    {
        SchemaByToolName.Keys.Should().BeEquivalentTo(FeishuToolNames.All,
            "Phase 1 工具清单为 6 域 10 个（§3.3.2），增删/改名必须同批更新契约表与守卫");
    }

    [Fact]
    public void AllPhase1Tools_ShouldBeReadOnly_WithScopes()
    {
        foreach (var pair in SchemaByToolName)
        {
            using var document = JsonDocument.Parse(pair.Value);
            var root = document.RootElement;

            root.GetProperty("x-feishu").GetProperty("is_write").GetBoolean().Should()
                .BeFalse($"{pair.Key} 为 Phase 1 只读工具（写工具属 Phase 2）");

            var scopes = root.GetProperty("x-feishu").GetProperty("required_scopes")
                .EnumerateArray().Select(e => e.GetString()!).ToArray();
            scopes.Should().NotBeEmpty($"{pair.Key} 必须声明 required_scopes（已决策⑥：scope 随 Schema 供授权钩子与审计消费）");
        }
    }

    [Fact]
    public void SchemaToolNames_ShouldNeverUseSourceMethodNames_ProtectingContractStability()
    {
        // 防源码方法名污染工具名（§8：bitable.list_fields 的源方法历史上语义错位）。
        SchemaByToolName.Keys.Should().NotContain("bitable.queryfieldspagelist");
        SchemaByToolName.Keys.Should().Contain("bitable.list_fields",
            "字段列表工具强制命名为 bitable.list_fields（工具名契约，不跟随源码方法名）");
    }

    [Fact]
    public void ToolOptions_ShouldHaveRealConsumptionPoints_InFeishuToolsSources()
    {
        // R5 规则 2：新增配置属性必须有真实消费点（FeishuToolBinding / AddFeishuReadonlyTools）。
        var sources = GetFeishuToolsSources();
        sources.Should().NotBeEmpty();

        sources.Any(path => File.ReadAllText(path).Contains("MaxToolResultLength", StringComparison.Ordinal))
            .Should().BeTrue("FeishuAgentOptions.MaxToolResultLength 必须在 FeishuTools 包中被消费（结果截断）");

        sources.Any(path => File.ReadAllText(path).Contains("EnforceToolAuthorization", StringComparison.Ordinal))
            .Should().BeTrue("FeishuAgentOptions.EnforceToolAuthorization 必须在 FeishuTools 包中被消费（授权门禁）");

        sources.Any(path => File.ReadAllText(path).Contains(".MapTool(", StringComparison.Ordinal))
            .Should().BeTrue("FeishuAgentOptions.Tools 白名单必须在 FeishuTools 包中被消费（MapTool 等价物）");
    }

    [Fact]
    public void AddFeishuReadonlyTools_ShouldRegisterExactlyTenTools_NoneEnabledByDefault()
    {
        using var provider = CreateProvider(_ => { });

        var registry = provider.GetRequiredService<FeishuToolRegistry>();
        registry.AllTools.Select(t => t.Name).Should().BeEquivalentTo(FeishuToolNames.All);
        registry.EnabledTools.Should().BeEmpty("工具默认收进注册表不启用，需白名单显式 MapTool（安全默认）");
    }

    [Fact]
    public void ToolsWhitelist_ShouldMapRegisteredTools_AndFailFastOnUnknownName()
    {
        using var provider = CreateProvider(
            options => options.Tools = [FeishuToolNames.BitableListTables, FeishuToolNames.WikiGetNode]);

        var registry = provider.GetRequiredService<FeishuToolRegistry>();
        registry.EnabledTools.Select(t => t.Name).Should().BeEquivalentTo(
            [FeishuToolNames.BitableListTables, FeishuToolNames.WikiGetNode],
            "FeishuAgent:Tools 配置白名单是 MapTool 的配置面等价物");

        var unknownInvocation = () =>
        {
            using var p = CreateProvider(options => options.Tools = ["not.a.tool"]);
            _ = p.GetRequiredService<FeishuToolRegistry>();
        };
        unknownInvocation.Should().Throw<InvalidOperationException>(
            "白名单中的未注册名字必须 fail-fast（防配置漂移）");
    }

    [Fact]
    public void ToolSource_ShouldExposeEnabledToolsOnly_WithGeneratedSchemas()
    {
        using var provider = CreateProvider(
            options => options.Tools = [FeishuToolNames.BitableListTables]);

        var source = provider.GetRequiredService<FeishuAgentToolSource>();
        var tools = source.GetTools(provider);

        tools.Should().ContainSingle("仅白名单启用工具暴露给模型");
        tools[0].Name.Should().Be(FeishuToolNames.BitableListTables);
        tools[0].Description.Should().NotBeNullOrEmpty();

        var schema = tools[0].JsonSchema;
        schema.GetProperty("name").GetString().Should().Be(FeishuToolNames.BitableListTables);
        schema.GetProperty("parameters").GetProperty("properties").GetProperty("app_token").Should().NotBeNull();
    }

    /// <summary>构造带 mock 飞书客户端的容器（分域执行器解析强类型接口）。</summary>
    private static ServiceProvider CreateProvider(Action<FeishuAgentOptions> configureOptions)
    {
        var options = new FeishuAgentOptions { Instructions = "test" };
        configureOptions(options);

        return new ServiceCollection()
            .AddSingleton(Options.Create(options))
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableAppTable>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableField>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableRecord>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1Docx>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV2WikiNodes>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV2SearchDocWiki>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1Message>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV3Spreadsheets>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV3SpreadsheetData>().Object)
            .AddFeishuReadonlyTools()
            .BuildServiceProvider();
    }

    private static IReadOnlyDictionary<string, string> SchemaByToolName => FeishuToolSchemas.SchemaByToolName;

    private static List<string> GetFeishuToolsSources()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir) && !File.Exists(Path.Combine(dir, "Mud.Feishu.slnx")))
        {
            dir = Path.GetDirectoryName(dir);
        }

        return Directory.GetFiles(Path.Combine(dir!, "Mud.Feishu.AI.FeishuTools"), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains("obj") && !p.Contains("bin"))
            .ToList();
    }
}
