// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Mud.Feishu.AI.Tools;
using Mud.Feishu.AI.Tools.Tools;
using Mud.Feishu.AI.Tools;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

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

        // R4/WP2（F-2）：目录条目补齐 risk/identity/source——宿主按政策筛选工具不再需要解析 Schema JSON。
        entry.Risk.Should().Be(FeishuToolRisk.Read);
        entry.Identity.Should().Be("tenant");
        entry.SdkSource.Should().Contain("IFeishu", "SDK 源符号来自编译期契约（宿主据此定位实现）");

        catalog.Find("not.a.tool").Should().BeNull();
    }

    /// <summary>
    /// 目录条目的 <c>risk</c>/<c>identity</c>/<c>scope</c>/<c>isWrite</c>/<c>source</c> 必须与编译期契约逐一相等
    /// （R4/WP2 / F-2：契约是唯一真相源，目录只是它的稳定只读视图）。
    /// </summary>
    [Fact]
    public void CatalogEntries_ShouldMirrorTheCompiledContracts()
    {
        using var provider = GuardProviderFactory.CreateProvider(_ => { });
        var catalog = provider.GetRequiredService<IToolCatalog>();

        foreach (var (toolName, contract) in FeishuToolContracts.ByToolName)
        {
            var entry = catalog.Find(toolName);
            entry.Should().NotBeNull($"目录必须覆盖契约工具 {toolName}");
            entry!.Risk.Should().Be(contract.Risk, $"{toolName} 的 risk 必须来自契约");
            entry.Identity.Should().Be(contract.Identity, $"{toolName} 的 identity 必须来自契约");
            entry.IsWrite.Should().Be(contract.IsWrite);
            entry.RequiredScopes.Should().BeEquivalentTo(contract.RequiredScopes);
            entry.SdkSource.Should().Be(contract.SdkSource, $"{toolName} 的 SDK 源符号必须来自契约");
        }
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
    /// <param name="configureOptions">配置覆盖。</param>
    /// <param name="withAttachmentStager">
    /// 是否注册附件落盘器（WP7）：<see langword="false"/> 用于验证"落盘器缺席 → 上传工具软缺席"。
    /// </param>
    /// <param name="withUserTaskClient">
    /// 是否注册用户令牌任务客户端：<see langword="false"/> 用于验证"<b>可空</b> SDK 客户端缺席
    /// <b>不</b>触发软缺席"（<c>TaskTools</c> 的 <c>IFeishuUserV2Task?</c> 是可选依赖，
    /// 缺席时 <c>task.list_my_tasks</c> 仍在位、由执行期回填结构化错误）。
    /// </param>
    internal static ServiceProvider CreateProvider(
        Action<FeishuAgentOptions> configureOptions,
        bool withAttachmentStager = true,
        bool withUserTaskClient = true)
    {
        var options = new FeishuAgentOptions { Instructions = "test" };
        configureOptions(options);

        var services = new ServiceCollection()
            .AddSingleton(Options.Create(options))
            // R3-5：作用域工厂的核心依赖（与生成的 HTTP 客户端同构：IFeishuAppManager + IAppContextHolder
            // + 授权器），与业务域无关——任何装配组合都必须提供，否则连注册表都建不起来。
            .AddSingleton(new Mock<Mud.HttpUtils.IAppContextHolder>().Object)
            .AddSingleton(new Mock<Mud.Feishu.Abstractions.IFeishuAppManager>().Object)
            .AddSingleton(Mock.Of<Mud.HttpUtils.IAppAccessAuthorizer>(a => a.CanSwitchTo(It.IsAny<string>())))
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableAppTable>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableField>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableRecord>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1Docx>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1DocxBlocks>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV2WikiNodes>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV2SearchDocWiki>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1Message>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1ChatGroupMember>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV3Spreadsheets>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV3SpreadsheetData>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV4Approval>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV4ApprovalQuery>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV4ApprovalTask>().Object)
            // R5 / F-11：minutes 域执行器（MinutesReadTools）需要该客户端才能被 DI 装配进目录。
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1MinutesMinute>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1DriveFolder>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1DriveFiles>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV3User>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV4CalendarEvent>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV4Calendar>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV2Task>().Object)
            .AddSingleton(new Mock<Mud.Feishu.AI.Knowledge.IRetriever>().Object);

        if (withUserTaskClient)
        {
            services.AddSingleton(new Mock<Mud.Feishu.IFeishuUserV2Task>().Object);
        }

        if (withAttachmentStager)
        {
            // WP7：附件落盘器（宿主注入）——未注册时 im.send_image / im.send_file 不注册（软缺席）。
            services.AddSingleton(new Mock<Mud.Feishu.AI.Tools.IFeishuAttachmentStager>().Object);
        }

        return services.AddFeishuReadonlyTools().BuildServiceProvider();
    }
}
