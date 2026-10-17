// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Extensions;
using Mud.Feishu.AI.Tools;
using Mud.Feishu.AI.Tools.Hosting;

namespace Mud.Feishu.Agent.Demo.Tests;

/// <summary>
/// 「全域只读白名单」与<b>域注册面同宽</b>的装配契约（工具冒烟 / 领域事件两个模式共用同一形状）。
/// </summary>
/// <remarks>
/// <para>
/// <b>拦截的真实回归</b>：白名单取 <c>ReadonlyAll</c>（<b>全部域</b>的只读工具），域客户端却只注册了
/// 少数几个域 ⇒ 启动即抛
/// <c>InvalidOperationException: FeishuAgent:Tools 白名单包含未注册工具 'approval.get_instance'</c>。
/// 工具面随域扩张后，这类失配<b>编译期无信号</b>：白名单是生成的常量、注册面是手写清单，两边谁都不知道谁。
/// </para>
/// <para>
/// <b>为什么必须在容器层断言</b>：失配只在 <c>AddFeishuTools</c> 物化注册表（<c>MapWhitelist</c>）时暴露，
/// 单测不装配整条链就永远看不到；本守卫装配与 <c>ToolsDemo.RunAsync</c> 同形的容器并解析注册表，
/// 把"启动即崩"提前到测试期。
/// </para>
/// </remarks>
public class ToolsDemoWiringContractGuards
{
    /// <summary>白名单里的每一枚工具都必须真的注册，且注册面**不多不少**正好等于白名单。</summary>
    [Fact]
    public void ReadonlyWhitelist_ShouldBeFullyRegistered_ByGlobalDomainRegistrars()
    {
        using var provider = BuildProvider();

        var registered = provider.GetRequiredService<FeishuToolRegistry>()
            .EnabledTools
            .Select(static tool => tool.Name)
            .ToHashSet(StringComparer.Ordinal);

        registered.Should().BeEquivalentTo(DemoAgentDefaults.ReadonlyToolWhitelist,
            "注册面必须与白名单等宽：少注册一枚工具，装配期即 fail-fast（多注册则由白名单裁剪，不入注册表）");
    }

    /// <summary>剔除项只有 <c>knowledge.search</c> 一枚（它绑定宿主 RAG 门面，本 Demo 明确不接 Aily 托管知识）。</summary>
    [Fact]
    public void ReadonlyWhitelist_ShouldExcludeOnlyKnowledgeSearch()
    {
        DemoAgentDefaults.ReadonlyToolWhitelist.Should().NotContain(FeishuToolNames.KnowledgeSearch);
        DemoAgentDefaults.ReadonlyToolWhitelist.Length.Should().Be(
            FeishuToolNames.ReadonlyAll.Length - 1,
            "全域只读白名单 = ReadonlyAll − knowledge.search，恰好少一枚");
        FeishuToolNames.ReadonlyAll.Should().Contain(DemoAgentDefaults.ReadonlyToolWhitelist);
    }

    /// <summary>
    /// 与 <c>ToolsDemo.RunAsync</c> 同形的容器（除运行时日志与流式通道外逐项一致）：
    /// 合成单应用 → 全域域客户端 → 全域只读白名单的 Agent 选项 → 工具面。
    /// </summary>
    /// <returns>服务容器。</returns>
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection()
            .AddFeishuApp(
                DemoAppConfig.CreateSingleApp(
                    DemoAgentDefaults.DefaultAppKey,
                    "cli_demo_guard_000001",
                    "secret-demo-guard-0001"),
                DemoAppConfig.SectionName)
            .AddFeishuServices(static builder => builder.AddAllApis())
            .AddDemoAgent(
                new ChatModelSettings
                {
                    ModelId = "guard-model",
                    ApiKey = "sk-guard-not-a-real-key",
                },
                "DemoWiringGuard",
                "guard",
                static options =>
                {
                    options.Tools = [.. DemoAgentDefaults.ReadonlyToolWhitelist];
                    // 只读面含 task.list_my_tasks（identity=user），必须显式放行 user。
                    options.AllowedIdentities = ["tenant", "user"];
                })
            .AddFeishuTools();

        // 与 ToolsDemo.RunAsync 同形：只读白名单含 drive.download_file / drive.export_document，
        // 其执行器注入非可空 IFeishuAttachmentStager——缺席时整域软缺席，白名单装配期即 fail-fast。
        services.AddHttpClient<DemoAttachmentStager>();
        services.AddSingleton<IFeishuAttachmentStager>(static sp => sp.GetRequiredService<DemoAttachmentStager>());

        return services.BuildServiceProvider();
    }
}
