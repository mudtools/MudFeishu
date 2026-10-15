// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// 任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

using Mud.Feishu.Abstractions.Conversations;
using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Conversations;
using Mud.Feishu.AI.Extensions;

namespace Mud.Feishu.Agent.Demo.Tests;

/// <summary>
/// 演示装配路径上的削峰闭环：<c>AddFeishuAgent</c> 是否真的把全局闸门装进容器（R8 延续）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么必须在容器层断言</b>：闸门的削峰能力来自"装饰器被装进 <c>IConversationGate</c> 这一槽位"。
/// 若注册被摘掉，事件入口拿到的仍是内层会话闸——同键串行照旧、全局上限<b>静默消失</b>，
/// 单测（直接 new 壳）与编译期都无任何信号。
/// </para>
/// <para>
/// <b>断言面</b>：① 槽位解析出的实例是壳类型；② 上限真的生效（第二个键被削峰成
/// <see cref="ConversationBusyException"/>）——②比①强，它连"装了但参数没接对"一并拦下。
/// </para>
/// </remarks>
public class AgentGateWiringTests
{
    /// <summary>槽位解析：<c>IConversationGate</c> 必须是削峰壳（而非内层会话闸直连）。</summary>
    [Fact]
    public void ServiceProvider_ShouldResolveConcurrencyGateAsDecorator()
    {
        using var provider = BuildProvider(maxConcurrentTurns: 4, queueTimeoutSeconds: 1);

        provider.GetRequiredService<IConversationGate>()
            .Should().BeOfType<GlobalConcurrencyGate>(
                "AddFeishuAgent 必须装削峰壳——漏装会静默失去全局上限（内层会话闸只剩同键串行）");

        provider.GetRequiredService<KeyedConversationGate>()
            .Should().NotBeNull("内层仍是默认进程内会话闸（多实例部署由宿主替换为分布式实现）");
    }

    /// <summary>行为闭环：装配参数真的接到了引擎上（上限 1 ⇒ 跨键请求也被削峰）。</summary>
    [Fact]
    public async Task ConcurrencyGate_ShouldThrottle_WhenResolvedFromProvider()
    {
        using var provider = BuildProvider(maxConcurrentTurns: 1, queueTimeoutSeconds: 1);
        var gate = provider.GetRequiredService<IConversationGate>();

        var held = await gate.AcquireAsync("demo:app-a:conversation:chat:oc_1");

        // 跨键取值 ⇒ 内层会话闸本可并行放行，此处被拒只可能来自全局上限。
        var act = async () => await gate.AcquireAsync("demo:app-a:conversation:chat:oc_2");

        await act.Should().ThrowAsync<ConversationBusyException>(
            "MaxConcurrentTurns=1 时第二个在途请求必须被削峰（装配参数未接到引擎上即为漏）");

        held.Dispose();
    }

    private static ServiceProvider BuildProvider(int maxConcurrentTurns, int queueTimeoutSeconds)
        => new ServiceCollection()
            .AddFeishuAgent(configure: options =>
            {
                options.ModelServiceKey = "demo-model";
                options.Name = "demo-agent";
                options.Instructions = "demo-instructions";
                options.MaxConcurrentTurns = maxConcurrentTurns;
                options.TurnQueueTimeoutSeconds = queueTimeoutSeconds;
            })
            .BuildServiceProvider();
}
