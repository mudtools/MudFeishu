// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.FeishuTools.Tests.Tools;

namespace Mud.Feishu.AI.FeishuTools.Tests.ContractGuards;

/// <summary>
/// 域注册器 / DI 装配产物契约守卫（<c>ToolRegistrarEmitter</c> →
/// <c>FeishuToolDomainRegistrars.g.cs</c> + <c>FeishuToolsServiceCollectionCoreExtensions.g.cs</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么只测这两件事</b>：注册表的<b>完整性</b>与<b>逐域等价性</b>已由既有守卫覆盖
/// （<c>FeishuToolContractGuards.AddFeishuTools_ShouldRegisterExactlyTheContractTools_NoneEnabledByDefault</c>、
/// <c>DomainRegistrarTests.AddFeishuTools_ShouldProduceSameRegistry_AsPerDomainExtensions</c>），
/// 附件落盘器软缺席由 <c>AttachmentToolsChainTests.WithoutAttachmentStager_*</c> 覆盖。
/// 生成化之后<b>新增</b>的回归面只有两处——都源于「软缺席改由构造器签名推导」：
/// ① 可空 SDK 客户端（<c>TaskTools</c> 的 user 客户端）<b>不得</b>被误判为软缺席；
/// ② 仅依赖 <c>IOptions&lt;T&gt;</c> 的执行器（能力出处元工具）在<b>零客户端</b>下仍须注册。
/// </para>
/// <para>
/// 另两条是<b>防样板回潮</b>的源码扫描（与本目录既有体例一致）：手写侧不得再出现注册器实现与
/// 逐域 DI 核心方法——它们现在是生成器产物。
/// </para>
/// </remarks>
public class ToolRegistrarContractGuards
{
    /// <summary>
    /// 可空 SDK 客户端缺席<b>不得</b>触发软缺席：<c>TaskTools(IFeishuTenantV2Task, IFeishuUserV2Task?, …)</c>
    /// 中 user 客户端缺席时执行器仍须构造（<c>list_my_tasks</c> 的存在性不变，缺令牌由执行期回填结构化错误）。
    /// </summary>
    [Fact]
    public void NullableSdkClientAbsent_ShouldNotSoftAbsentExecutor()
    {
        using var provider = GuardProviderFactory.CreateProvider(_ => { }, withUserTaskClient: false);

        var names = provider.GetRequiredService<FeishuToolRegistry>().AllTools.Select(static t => t.Name).ToArray();

        names.Should().Contain(FeishuToolNames.TaskCreateTask);
        names.Should().Contain(
            FeishuToolNames.TaskListMyTasks,
            "可空 SDK 客户端缺席不得触发软缺席——软缺席会让工具从注册表消失（白名单期 fail-fast），"
            + "而真实语义是「工具在位、执行期报缺客户端」");
    }

    /// <summary>
    /// 仅依赖 <c>IOptions&lt;T&gt;</c> 的执行器（无任何软缺席候选）在<b>无任何域客户端</b>下仍须注册。
    /// </summary>
    /// <remarks>
    /// 唯一注册的消息客户端不属于任何<b>工具</b>域——它是执行链基础设施
    /// （<c>FeishuAppContextScopeFactory</c> 的构造依赖，缺它连注册表都建不起来，与本次生成化无关）。
    /// </remarks>
    [Fact]
    public void OptionsOnlyExecutor_ShouldBeRegisteredWithoutAnyDomainClient()
    {
        var services = new ServiceCollection()
            .AddSingleton(Options.Create(new FeishuAgentOptions { Instructions = "test" }))
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1Message>().Object);

        using var provider = services.AddFeishuCapabilityTools().BuildServiceProvider();

        // 用 BeEquivalentTo + 显式数组：Equal(params string[]) 会把理由文本当成期望元素。
        provider.GetRequiredService<FeishuToolRegistry>().AllTools.Select(static t => t.Name)
            .Should().BeEquivalentTo(
                new[] { FeishuToolNames.FeishuCapabilityLookup },
                "能力出处元工具的数据源是编译期常量，装配它不需要任何飞书域客户端（不随域缺席而软缺席）");
    }

    /// <summary>
    /// 手写侧不得再声明域注册器实现——实现类由 <c>ToolRegistrarEmitter</c> 编译期产出
    /// （防止"新增域时顺手写一个手写注册器"让两套机制长期并存）。
    /// </summary>
    [Fact]
    public void DomainRegistrarImplementations_ShouldBeGeneratedOnly()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "Mud.Feishu.AI.FeishuTools",
            "Registration",
            "FeishuToolDomainRegistrars.cs"));

        source.Should().NotContain(
            ": IFeishuToolDomainRegistrar",
            "手写侧只允许保留契约（IFeishuToolDomainRegistrar）与登记助手；实现类必须由生成器产出"
            + "（BitableToolDomainRegistrar 等已由 FeishuToolDomainRegistrars.g.cs 生成）");
    }

    /// <summary>
    /// 手写 DI 入口不得再声明逐域核心方法——它们由生成器按执行器类产出
    /// （<c>AddFeishu{Tools}Core</c>），手写侧只保留公开入口的编排。
    /// </summary>
    [Fact]
    public void PerDomainDiCoreMethods_ShouldBeGeneratedOnly()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "Mud.Feishu.AI.FeishuTools",
            "Extensions",
            "FeishuToolsServiceCollectionExtensions.cs"));

        // 注意用正则而非纯字符串：`AddFeishuToolInfrastructure`（共享基础设施）是**有意保留**手写的。
        System.Text.RegularExpressions.Regex
            .Matches(source, @"private static IServiceCollection AddFeishu\w+ToolsCore\b")
            .Should().BeEmpty(
                "逐域 DI 核心方法（执行器工厂 + 注册器登记）必须由生成器产出："
                + "软缺席判定按执行器构造器签名推导，手写副本会让两套语义长期漂移");
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
