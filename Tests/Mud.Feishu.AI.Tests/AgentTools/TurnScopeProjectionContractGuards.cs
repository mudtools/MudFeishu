// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.Agent.Events;

namespace Mud.Feishu.AI.Tests.AgentTools;

/// <summary>
/// 回归出处：2026-10-28 审查（.docs/AI模块功能缺陷与完整性分析报告-2026-10-28.md §2.2，P0-A）——
/// 审批续跑轮的 <c>FeishuTurnScopeFactory</c> 漏投影 <c>ThreadId</c>，导致话题内批准续跑后
/// <c>reply_in_thread</c> 行为与事件轮不一致。本守卫锁定：两个 <c>ITurnScopeFactory</c> 实现
/// 对同一 <c>TurnScopeContext</c> 的投影<b>逐字段一致</b>（行为守卫，不比对源码文本）。
/// </summary>
public class TurnScopeProjectionContractGuards
{
    private static TurnScopeContext SampleContext() => new(
        TenantKey: "app-a",
        ConversationKey: "conv-1",
        ChatId: "oc_chat",
        UserId: "ou_user",
        ThreadId: "om_thread");

    [Fact]
    public void BothScopeFactories_ShouldProjectIdenticalFeishuToolContext()
    {
        var accessor = new FeishuToolContextAccessor();
        var eventTurnFactory = (ITurnScopeFactory)new FeishuToolContextScopeFactory(accessor);
        var continuationTurnFactory = (ITurnScopeFactory)new FeishuTurnScopeFactory(accessor);

        using (eventTurnFactory.BeginToolScope(SampleContext()))
        {
            var eventContext = accessor.Current!;

            using (continuationTurnFactory.BeginToolScope(SampleContext()))
            {
                var continuationContext = accessor.Current!;

                continuationContext.AppKey.Should().Be(eventContext.AppKey);
                continuationContext.ConversationKey.Should().Be(eventContext.ConversationKey);
                continuationContext.ChatId.Should().Be(eventContext.ChatId);
                continuationContext.UserId.Should().Be(eventContext.UserId);
                continuationContext.ThreadId.Should().Be(eventContext.ThreadId,
                    "续跑轮（审批回灌）与事件轮必须投影同一会话维度，否则话题内批准续跑后 reply_in_thread 行为分叉（P0-A 回归）");
            }
        }
    }

    [Fact]
    public void ContinuationTurnFactory_ShouldCarryThreadId_FromTurnScopeContext()
    {
        var accessor = new FeishuToolContextAccessor();
        var factory = new FeishuTurnScopeFactory(accessor);

        using (factory.BeginToolScope(SampleContext()))
        {
            accessor.Current!.ThreadId.Should().Be("om_thread",
                "ThreadId 必须随续跑轮进入工具上下文（FeishuToolContext.ThreadId 的存在理由）");
        }
    }
}
