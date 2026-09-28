// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Mud.Feishu.AI.FeishuTools;
using Mud.Feishu.AI.FeishuTools.Tools;
using Mud.Feishu.AI.Tools;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// 工具目录与 Schema 导出测试（AI-FD-D12 P1D-4）：目录枚举与契约表一致；
/// OpenAI functions JSON 导出可被标准解析（name/parameters 齐全）。
/// </summary>
public class FeishuToolCatalogTests
{
    [Fact]
    public void Catalog_ShouldMirrorRegistry_WithContractNames()
    {
        using var provider = GuardProviderFactory.CreateProvider(_ => { });
        var registry = provider.GetRequiredService<FeishuToolRegistry>();
        var catalog = provider.GetRequiredService<IToolCatalog>();

        catalog.Entries.Select(e => e.Name).Should().BeEquivalentTo(
            registry.AllTools.Select(t => t.Name),
            "目录与注册表一致（含未启用工具——能力语义与白名单启用语义正交）");
        catalog.Entries.Select(e => e.Name).Should().BeEquivalentTo(FeishuToolNames.All);
    }

    [Fact]
    public void Find_ShouldReturnEntry_WithSchemaMetadata()
    {
        using var provider = GuardProviderFactory.CreateProvider(_ => { });
        var catalog = provider.GetRequiredService<IToolCatalog>();

        var entry = catalog.Find(FeishuToolNames.BitableQueryRecords);
        entry.Should().NotBeNull();
        entry!.Name.Should().Be(FeishuToolNames.BitableQueryRecords);
        entry.IsWrite.Should().BeFalse();
        entry.RequiredScopes.Should().Contain("bitable:app:readonly");
        entry.ParameterSchemaJson.Should().NotBeNullOrEmpty();

        catalog.Find("not.a.tool").Should().BeNull();
    }

    [Fact]
    public void Export_ShouldProduceOpenAiCompatibleToolsJson()
    {
        using var provider = GuardProviderFactory.CreateProvider(_ => { });
        var exporter = provider.GetRequiredService<IToolSchemaExporter>();

        var json = exporter.Export(ToolSchemaDialect.OpenAiFunctions);

        using var document = JsonDocument.Parse(json);
        var array = document.RootElement;
        array.ValueKind.Should().Be(JsonValueKind.Array);
        array.GetArrayLength().Should().Be(FeishuToolNames.All.Length, "导出覆盖全部契约工具");

        foreach (var tool in array.EnumerateArray())
        {
            tool.GetProperty("type").GetString().Should().Be("function");
            var function = tool.GetProperty("function");
            function.GetProperty("name").GetString().Should().NotBeNullOrWhiteSpace();
            function.GetProperty("parameters").ValueKind.Should().Be(JsonValueKind.Object);
            function.TryGetProperty("x-feishu", out _).Should().BeFalse("方言投影剥离 SDK 内部扩展");
        }
    }

    [Fact]
    public void Export_ShouldRejectUnsupportedDialect()
    {
        var exporter = new FeishuToolSchemaExporter();
        var act = () => exporter.Export((ToolSchemaDialect)99);
        act.Should().Throw<ArgumentOutOfRangeException>("Skills/Aily/MCP 方言归 Phase 4（接口位预留）");
    }
}

/// <summary>守卫/目录测试共用的最小容器工厂（分域 mock 齐备）。</summary>
internal static class GuardProviderFactory
{
    internal static ServiceProvider CreateProvider(Action<FeishuAgentOptions> configureOptions)
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
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1DriveFolder>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1DriveFiles>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV3User>().Object)
            .AddSingleton(new Mock<Mud.Feishu.AI.Knowledge.IRetriever>().Object)
            .AddFeishuReadonlyTools()
            .BuildServiceProvider();
    }
}
