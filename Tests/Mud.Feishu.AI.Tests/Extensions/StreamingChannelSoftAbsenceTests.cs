// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;

namespace Mud.Feishu.AI.Tests.Extensions;

/// <summary>
/// 回归出处：2026-10-28 审查（H，.docs/AI模块功能缺陷与完整性分析报告-2026-10-28.md §2.6）——
/// <c>AddFeishuStreamingChannel</c> 在 CardKit 客户端缺席时曾向容器注册「实例为 null」的
/// Singleton 描述符（<c>null!</c>），依赖容器对 null 实例的宽容。修复后缺席 = 服务缺席，
/// 消费侧 <c>GetService</c> 语义不变，<c>GetRequiredService</c> 得到标准失败而非静默 null。
/// </summary>
public class StreamingChannelSoftAbsenceTests
{
    [Fact]
    public void AddFeishuStreamingChannel_ShouldNotRegisterNullInstance_WhenCardKitClientsAbsent()
    {
        var services = new ServiceCollection()
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1Message>().Object)
            .AddSingleton<Mud.Feishu.AI.AgentTools.IFeishuAppContextScopeFactory>(
                new Mock<Mud.Feishu.AI.AgentTools.IFeishuAppContextScopeFactory>().Object)
            .AddSingleton(Options.Create(new FeishuAgentOptions { Instructions = "x" }));
        services.AddFeishuStreamingChannel();

        using var provider = services.BuildServiceProvider();

        // 软缺席 = 服务不在容器里（标准 DI 缺席语义），而不是「注册了但实例为 null」。
        var act = () => provider.GetRequiredService<Mud.Feishu.AI.Channels.CardKitStreamMessageChannel>();
        act.Should().Throw<InvalidOperationException>(
            "CardKit 客户端缺席时通道必须是服务缺席（GetRequiredService 抛错），不得注册 null 实例（H 回归）");

        // 消费链不受影响：IMessageChannel 仍可解析（链内仅编辑通道）。
        provider.GetService<IMessageChannel>().Should().NotBeNull(
            "CardKit 缺席时流式链降级为仅编辑通道，行为与修复前一致");
    }
}
