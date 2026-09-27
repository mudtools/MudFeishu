// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.FeishuTools.Tools;

namespace Mud.Feishu.AI.FeishuTools.Tests.ContractGuards;

/// <summary>
/// 工具名契约表守卫（Phase 1 §7 + Phase 2 §3.3）：Schema 注册表恰为契约名全集
/// （Phase 1 十个只读 + Phase 2 三个写类），防增删/改名漂移；读写白名单分离语义锁定。
/// </summary>
public class FeishuToolContractGuards
{
    [Fact]
    public void SchemaRegistry_ShouldContainExactlyTheThirteenContractTools()
    {
        SchemaByToolName.Keys.Should().BeEquivalentTo(FeishuToolNames.All,
            "工具清单为 Phase 1 十个只读（§3.3.2）+ Phase 2 三个写类（§3.3），增删/改名必须同批更新契约表与守卫");
    }

    [Fact]
    public void ReadonlyTools_ShouldBeReadOnly_WithScopes()
    {
        foreach (var name in FeishuToolNames.ReadonlyAll)
        {
            using var document = JsonDocument.Parse(SchemaByToolName[name]);
            var root = document.RootElement;

            root.GetProperty("x-feishu").GetProperty("is_write").GetBoolean().Should()
                .BeFalse($"{name} 为 Phase 1 只读工具");

            var scopes = root.GetProperty("x-feishu").GetProperty("required_scopes")
                .EnumerateArray().Select(e => e.GetString()!).ToArray();
            scopes.Should().NotBeEmpty($"{name} 必须声明 required_scopes（已决策⑥：scope 随 Schema 供授权钩子与审计消费）");
        }
    }

    [Fact]
    public void WriteTools_ShouldBeFlaggedAsWrite_WithScopes()
    {
        foreach (var name in FeishuToolNames.WriteAll)
        {
            using var document = JsonDocument.Parse(SchemaByToolName[name]);
            var root = document.RootElement;

            root.GetProperty("x-feishu").GetProperty("is_write").GetBoolean().Should()
                .BeTrue($"{name} 为 Phase 2 写类工具——执行链据此强制授权门禁（安全默认）");

            var scopes = root.GetProperty("x-feishu").GetProperty("required_scopes")
                .EnumerateArray().Select(e => e.GetString()!).ToArray();
            scopes.Should().NotBeEmpty($"{name} 必须声明 required_scopes（已决策⑥）");
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
        // R5 规则 2：新增配置属性必须有真实消费点（FeishuToolBinding / AddFeishuTools / EditMessageChannel）。
        var sources = GetFeishuToolsSources();
        sources.Should().NotBeEmpty();

        sources.Any(path => File.ReadAllText(path).Contains("MaxToolResultLength", StringComparison.Ordinal))
            .Should().BeTrue("FeishuAgentOptions.MaxToolResultLength 必须在 FeishuTools 包中被消费（结果截断）");

        sources.Any(path => File.ReadAllText(path).Contains("EnforceToolAuthorization", StringComparison.Ordinal))
            .Should().BeTrue("FeishuAgentOptions.EnforceToolAuthorization 必须在 FeishuTools 包中被消费（授权门禁）");

        sources.Any(path => File.ReadAllText(path).Contains(".MapTool(", StringComparison.Ordinal))
            .Should().BeTrue("FeishuAgentOptions.Tools 白名单必须在 FeishuTools 包中被消费（MapTool 等价物）");

        sources.Any(path => File.ReadAllText(path).Contains("WriteAllowList", StringComparison.Ordinal))
            .Should().BeTrue("FeishuAgentOptions.WriteAllowList 必须在 FeishuTools 包中被消费（写工具白名单单独键控，Phase 2 §4）");

        sources.Any(path => File.ReadAllText(path).Contains("MaxStreamChunkLength", StringComparison.Ordinal))
            .Should().BeTrue("FeishuAgentOptions.MaxStreamChunkLength 必须在 FeishuTools 包中被消费（流式分片编辑阈值，Phase 2 §3.1）");
    }

    [Fact]
    public void AddFeishuReadonlyTools_ShouldRegisterExactlyThirteenTools_NoneEnabledByDefault()
    {
        using var provider = CreateProvider(_ => { });

        var registry = provider.GetRequiredService<FeishuToolRegistry>();
        registry.AllTools.Select(t => t.Name).Should().BeEquivalentTo(FeishuToolNames.All);
        registry.EnabledTools.Should().BeEmpty("工具默认收进注册表不启用，需白名单显式 MapTool（安全默认）");
        registry.AllTools.Should().OnlyContain(
            t => FeishuToolNames.IsWriteTool(t.Name) == t.IsWrite,
            "注册表 IsWrite 元数据与契约表逐一一致");
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
    public void Whitelists_ShouldEnforceReadWriteSeparation()
    {
        // 写工具不得经只读白名单（Tools）启用——写工具必须单独键控且过授权门禁（Phase 2 §4 安全默认）。
        var writeInReadonlyList = () =>
        {
            using var p = CreateProvider(options => options.Tools = [FeishuToolNames.ImSendMessage]);
            _ = p.GetRequiredService<FeishuToolRegistry>();
        };
        writeInReadonlyList.Should().Throw<InvalidOperationException>().WithMessage("*WriteAllowList*");

        // 只读工具不得经写白名单（WriteAllowList）启用。
        var readonlyInWriteList = () =>
        {
            using var p = CreateProvider(options => options.WriteAllowList = [FeishuToolNames.BitableListTables]);
            _ = p.GetRequiredService<FeishuToolRegistry>();
        };
        readonlyInWriteList.Should().Throw<InvalidOperationException>().WithMessage("*只读*");

        // 写白名单可正常启用写工具。
        using var provider = CreateProvider(options => options.WriteAllowList = [FeishuToolNames.ImSendMessage]);
        var registry = provider.GetRequiredService<FeishuToolRegistry>();
        registry.EnabledTools.Select(t => t.Name).Should().BeEquivalentTo(
            [FeishuToolNames.ImSendMessage], "写工具经 FeishuAgent:WriteAllowList 单独键控启用");
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
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV4Approval>().Object)
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
