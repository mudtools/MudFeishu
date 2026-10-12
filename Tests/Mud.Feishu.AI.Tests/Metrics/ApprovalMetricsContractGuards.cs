// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;
using System.Diagnostics.Metrics;
using Mud.Feishu.Abstractions.Metrics;

namespace Mud.Feishu.AI.Tests.Metrics;

/// <summary>
/// T8 / D8 审批生命周期指标守卫。
/// </summary>
public class ApprovalMetricsContractGuards
{
    /// <summary>
    /// 守卫 ①：AgentApprovalCount 计数器必须存在且名称正确。
    /// </summary>
    [Fact]
    public void AgentApprovalCount_ShouldExist()
    {
        FeishuMetrics.AgentApprovalCount.Should().NotBeNull();
        FeishuMetrics.AgentApprovalCount.Name.Should().Be("feishu.agent.approval");
    }

    /// <summary>
    /// 守卫 ②：ApprovalOutcomes 枚举常量全部有定义且非空（防高基数）。
    /// </summary>
    [Fact]
    public void ApprovalOutcomes_AllConstants_ShouldBeDefined()
    {
        var fields = typeof(FeishuMetrics.ApprovalOutcomes)
            .GetFields(BindingFlags.Public | BindingFlags.Static);

        fields.Should().HaveCount(6, "审批生命周期六态闭集");

        var expectedNames = new[]
        {
            nameof(FeishuMetrics.ApprovalOutcomes.Requested),
            nameof(FeishuMetrics.ApprovalOutcomes.ChannelMissing),
            nameof(FeishuMetrics.ApprovalOutcomes.Abandoned),
            nameof(FeishuMetrics.ApprovalOutcomes.Replayed),
            nameof(FeishuMetrics.ApprovalOutcomes.Expired),
            nameof(FeishuMetrics.ApprovalOutcomes.Consumed),
        };

        foreach (var name in expectedNames)
        {
            var field = typeof(FeishuMetrics.ApprovalOutcomes)
                .GetField(name, BindingFlags.Public | BindingFlags.Static);
            field.Should().NotBeNull($"ApprovalOutcomes.{name} 必须定义");
            var value = (string?)field!.GetValue(null);
            value.Should().NotBeNullOrEmpty($"ApprovalOutcomes.{name} 不得为空字符串");
        }
    }

    /// <summary>
    /// 守卫 ③：维度受控——ApprovalOutcomes 常量不包含 requestId/userId/conversationKey
    /// （A17 + 原则 8）。
    /// </summary>
    [Fact]
    public void ApprovalOutcomes_ShouldNotContainHighCardinalityFields()
    {
        var fields = typeof(FeishuMetrics.ApprovalOutcomes)
            .GetFields(BindingFlags.Public | BindingFlags.Static);

        foreach (var field in fields)
        {
            var value = (string?)field.GetValue(null);
            value.Should().NotBeNull();
            // 常量值不应包含 requestId/userId/conversationKey 等高基数字段
            value!.Should().NotContain("requestId");
            value.Should().NotContain("userId");
            value.Should().NotContain("conversationKey");
        }
    }
}
