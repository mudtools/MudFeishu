// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Configuration;
using Mud.Feishu.AI.Agents;

namespace Mud.Feishu.AI.Tests.ContractGuards;

/// <summary>
/// T12 / D12 HITL 时间常量不变式守卫。
/// </summary>
/// <remarks>
/// 不变式：PendingApprovalSnapshot.DefaultTtl <= FeishuConversationOptions.DefaultSessionTtl
/// （审批快照必须在会话存活期内有效——否则审批落库后快照先过期，宿主续跑会话时无法找到审批）。
/// </remarks>
public class ApprovalTtlInvariantContractGuards
{
    /// <summary>
    /// 守卫：PendingApprovalSnapshot.DefaultTtl 必须 <= FeishuConversationOptions.DefaultSessionTtl。
    /// </summary>
    [Fact]
    public void PendingApprovalDefaultTtl_ShouldBeLessThanOrEqual_SessionTtl()
    {
        // 不变式：DefaultTtl <= DefaultSessionTtl（审批快照在会话存活期内有效）
        (PendingApprovalSnapshot.DefaultTtl <= FeishuConversationOptions.DefaultSessionTtl)
            .Should().BeTrue(
                "审批快照 DefaultTtl 必须在会话 DefaultSessionTtl 之前过期——"
                + "否则审批落库后快照先过期，宿主续跑会话时无法找到审批");
    }

    /// <summary>
    /// 守卫：确认三个时间常量的值。
    /// </summary>
    [Fact]
    public void TimeConstants_ShouldHaveExpectedValues()
    {
        // ConfirmationLifetime = 10 min（PendingApprovalSnapshot.DefaultTtl）
        PendingApprovalSnapshot.DefaultTtl.Should().Be(
            TimeSpan.FromMinutes(10),
            "ConfirmationLifetime = 10 min");

        // DefaultTtl = 10 min（同 ConfirmationLifetime）
        // DefaultSessionTtl = 24 h
        FeishuConversationOptions.DefaultSessionTtl.Should().Be(
            TimeSpan.FromHours(24),
            "SessionTtl = 24 h");
    }
}
