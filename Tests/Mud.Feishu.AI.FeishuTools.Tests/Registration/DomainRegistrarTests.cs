// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.FeishuTools;
using Mud.Feishu.AI.FeishuTools.Tools;
using Mud.Feishu.AI.Tools;

namespace Mud.Feishu.AI.FeishuTools.Tests.Registration;

/// <summary>
/// 子域注册粒度测试（AI-FD-D12 P1D-1c）：单域注册后其余域工具不在注册表、
/// 白名单映射未注册名 fail-fast 报错含域指引、全域入口产物等价。
/// </summary>
public class DomainRegistrarTests
{
    private static IServiceCollection CreateServices(Action<FeishuAgentOptions> configureOptions)
    {
        var options = new FeishuAgentOptions { Instructions = "test" };
        configureOptions(options);
        return new ServiceCollection()
            .AddSingleton(Options.Create(options))
            // R3-5：作用域工厂的核心依赖（与生成的 HTTP 客户端同构），与工具域无关。
            .AddSingleton(new Mock<Mud.HttpUtils.IAppContextHolder>().Object)
            .AddSingleton(new Mock<Mud.Feishu.Abstractions.IFeishuAppManager>().Object)
            .AddSingleton(Mock.Of<Mud.HttpUtils.IAppAccessAuthorizer>(a => a.CanSwitchTo(It.IsAny<string>())))
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1Message>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableAppTable>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableField>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableRecord>().Object);
    }

    [Fact]
    public void AddFeishuBitableTools_ShouldRegisterOnlyBitableTools()
    {
        using var provider = CreateServices(_ => { })
            .AddFeishuBitableTools()
            .BuildServiceProvider();

        var registry = provider.GetRequiredService<FeishuToolRegistry>();
        registry.AllTools.Select(t => t.Name).Should().BeEquivalentTo(
            [FeishuToolNames.BitableListTables, FeishuToolNames.BitableListFields,
             FeishuToolNames.BitableQueryRecords, FeishuToolNames.BitableGetRecordsByIds,

             // R5 / F-11：视图侧是**软依赖**（可空），故视图客户端缺席时这两个工具
             // 仍会注册在案（软缺席 = 执行期报错，而非"工具不存在"）。
             FeishuToolNames.BitableListViews, FeishuToolNames.BitableGetView],
            "单域注册：其余域工具不进注册表（域客户端缺席 → 注册器缺席）");
        registry.EnabledTools.Should().BeEmpty();
    }

    [Fact]
    public void Whitelist_ShouldFailFast_WithDomainGuidance_WhenDomainAbsent()
    {
        var act = () =>
        {
            using var provider = CreateServices(options => options.Tools = [FeishuToolNames.ImGetHistoryMessages])
                .AddFeishuBitableTools()
                .BuildServiceProvider();
            _ = provider.GetRequiredService<FeishuToolRegistry>();
        };

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*未注册工具 'im.get_history_messages'*",
                "错误面从启动崩溃收敛为白名单期明确报错（P1D-1c）");
    }

    [Fact]
    public void AddFeishuTools_ShouldProduceSameRegistry_AsPerDomainExtensions()
    {
        var allDomainOptions = new[]
        {
            FeishuToolNames.BitableListTables, FeishuToolNames.BitableListFields,
            FeishuToolNames.BitableQueryRecords, FeishuToolNames.BitableGetRecordsByIds,
            FeishuToolNames.DocxGetRawContent, FeishuToolNames.DocxGetDocumentBlocks,
            FeishuToolNames.WikiGetNode, FeishuToolNames.WikiListNodes,
            FeishuToolNames.SearchDocWiki,
            FeishuToolNames.ImGetHistoryMessages, FeishuToolNames.ImGetMessageContent,
            FeishuToolNames.DriveListFolderFiles, FeishuToolNames.DriveGetFileMetas,
            FeishuToolNames.KnowledgeSearch,
            FeishuToolNames.SheetsListSheets, FeishuToolNames.SheetsGetRangeValues,
        };

        var mockClients = new Action<IServiceCollection>(services =>
        {
            // R3-5：作用域工厂的核心依赖（与生成的 HTTP 客户端同构），与工具域无关。
            services.AddSingleton(new Mock<Mud.HttpUtils.IAppContextHolder>().Object);
            services.AddSingleton(new Mock<Mud.Feishu.Abstractions.IFeishuAppManager>().Object);
            services.AddSingleton(Mock.Of<Mud.HttpUtils.IAppAccessAuthorizer>(a => a.CanSwitchTo(It.IsAny<string>())));
            services.AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableAppTable>().Object);
            services.AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableField>().Object);
            services.AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableRecord>().Object);
            services.AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1Docx>().Object);
            services.AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1DocxBlocks>().Object);
            services.AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV2WikiNodes>().Object);
            services.AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV2SearchDocWiki>().Object);
            services.AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1Message>().Object);
            services.AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1ChatGroupMember>().Object);
            services.AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV3Spreadsheets>().Object);
            services.AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV3SpreadsheetData>().Object);
            services.AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1DriveFolder>().Object);
            services.AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1DriveFiles>().Object);
            services.AddSingleton(new Mock<Mud.Feishu.AI.Knowledge.IRetriever>().Object);
            services.AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV4Approval>().Object);
            services.AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV4ApprovalQuery>().Object);
            services.AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV4ApprovalTask>().Object);
            services.AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV2Task>().Object);
            services.AddSingleton(new Mock<Mud.Feishu.IFeishuUserV2Task>().Object);
            services.AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV4CalendarEvent>().Object);
            services.AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV4Calendar>().Object);
            services.AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV3User>().Object);
        });

        using var singleEntryProvider = new ServiceCollection()
            .AddSingleton(Options.Create(new FeishuAgentOptions { Instructions = "test", Tools = allDomainOptions }))
            .Apply(mockClients)
            .AddFeishuTools()
            .BuildServiceProvider();

        using var perDomainProvider = new ServiceCollection()
            .AddSingleton(Options.Create(new FeishuAgentOptions { Instructions = "test", Tools = allDomainOptions }))
            .Apply(mockClients)
            .AddFeishuBitableTools()
            .AddFeishuDocxTools()
            .AddFeishuWikiTools()
            .AddFeishuSearchTools()
            .AddFeishuImTools()
            .AddFeishuSheetsTools()
            .AddFeishuDriveTools()
            .AddFeishuContactTools()
            .AddFeishuCalendarTools()
            .AddFeishuTaskTools()
            .AddFeishuKnowledgeTools()

    // R5 / F-11（S-13）：minutes 是新增的独立只读域，"全域 = 逐域联合"的等价性
    // 要求这里也显式调一次——否则 minutes 工具只出现在全域入口，逐域入口缺失。
    .AddFeishuMinutesReadTools()
            // 能力出处元工具是一个独立入口（AT-F12）：它不属于任何业务域，
            // 故"全域 = 逐域联合"的等价性要求这里也显式调一次。
            .AddFeishuCapabilityTools()
            .AddFeishuWriteTools()
            .BuildServiceProvider();

        var single = singleEntryProvider.GetRequiredService<FeishuToolRegistry>();
        var perDomain = perDomainProvider.GetRequiredService<FeishuToolRegistry>();

        single.AllTools.Select(t => t.Name).Should().BeEquivalentTo(
            perDomain.AllTools.Select(t => t.Name), "全域入口与逐域扩展产物一致（兼容等价性）");
        single.EnabledTools.Select(t => t.Name).Should().BeEquivalentTo(perDomain.EnabledTools.Select(t => t.Name));
    }

    /// <summary>
    /// R3-5：作用域工厂必须<b>零业务客户端依赖</b>——按域装配（只注册 Bitable 三个客户端、
    /// 完全没有 IM 客户端）也必须能解析整条工具执行链。
    /// </summary>
    /// <remarks>
    /// 缺陷原始形态：工厂以单例持有 <c>IFeishuTenantV1Message</c>（瞬时注册），既构成
    /// Captive Dependency（TMA-13），又使「只调 <c>AddFeishuBitableTools()</c>」的宿主在解析
    /// <c>FeishuToolBinding</c> 时报「无法解析 IFeishuTenantV1Message」——按域装配这个核心卖点被静默破坏。
    /// </remarks>
    [Fact]
    public void AddFeishuBitableTools_ShouldResolveToolBinding_WithoutImClient()
    {
        using var provider = new ServiceCollection()
            .AddSingleton(Options.Create(new FeishuAgentOptions { Instructions = "test" }))
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableAppTable>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableField>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableRecord>().Object)
            // 工厂的两个单例依赖（生产环境由 AddTokenProvider / 应用管理器提供）与授权器。
            .AddSingleton(new Mock<Mud.HttpUtils.IAppContextHolder>().Object)
            .AddSingleton(new Mock<Mud.Feishu.Abstractions.IFeishuAppManager>().Object)
            .AddSingleton(Mock.Of<Mud.HttpUtils.IAppAccessAuthorizer>(a => a.CanSwitchTo(It.IsAny<string>())))
            .AddFeishuBitableTools()
            .BuildServiceProvider();

        provider.GetRequiredService<FeishuToolBinding>().Should().NotBeNull(
            "按域装配（无 IM 客户端）必须可解析工具绑定——旧工厂直接依赖 IFeishuTenantV1Message 时此处会失败（R3-5）");
    }

    [Fact]
    public void DomainAbsent_ShouldNotThrow_OnRegistryBuild()
    {
        // 只注册 Bitable 域客户端，但注册了全域入口：其余域执行器缺席 → 注册器缺席 → 不抛（软缺席）。
        using var provider = CreateServices(_ => { })
            .AddFeishuTools()
            .BuildServiceProvider();

        var registry = provider.GetRequiredService<FeishuToolRegistry>();
        var act = () => registry.AllTools;
        act.Should().NotThrow("客户端缺席的域静默不注册（错误面收敛到白名单映射期）");
    }
}

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection Apply(this IServiceCollection services, Action<IServiceCollection> configure)
    {
        configure(services);
        return services;
    }
}
