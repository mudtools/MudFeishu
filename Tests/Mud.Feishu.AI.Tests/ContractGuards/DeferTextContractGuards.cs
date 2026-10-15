// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Events;

namespace Mud.Feishu.AI.Tests.ContractGuards;

/// <summary>
/// T10 / D10 流式文本抑制选项守卫。
/// </summary>
public class DeferTextContractGuards
{
    /// <summary>
    /// 守卫 ①：DeferTextUntilTurnComplete 默认为 false（A15 零差异）。
    /// </summary>
    [Fact]
    public void DeferTextUntilTurnComplete_Default_ShouldBeFalse()
    {
        var options = new FeishuAgentOptions
        {
            Instructions = "你是助手",
        };

        options.DeferTextUntilTurnComplete.Should().BeFalse(
            "默认 false 保证既有流式行为逐字节等价（A15 零差异）");
    }

    /// <summary>
    /// 守卫 ②：属性存在且为 bool 类型。
    /// </summary>
    [Fact]
    public void DeferTextUntilTurnComplete_ShouldExist_AsBool()
    {
        var property = typeof(FeishuAgentOptions)
            .GetProperty("DeferTextUntilTurnComplete");

        property.Should().NotBeNull("FeishuAgentOptions.DeferTextUntilTurnComplete 必须定义");
        property!.PropertyType.Should().Be(typeof(bool),
            "DeferTextUntilTurnComplete 是 bool 开关");
    }

    /// <summary>
    /// 守卫 ③：FeishuAgent 内部透传属性存在（事件处理器消费点）。
    /// </summary>
    [Fact]
    public void FeishuAgent_DeferTextUntilTurnComplete_ShouldExist_AsInternalBool()
    {
        var property = typeof(FeishuAgent)
            .GetProperty("DeferTextUntilTurnComplete",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        property.Should().NotBeNull(
            "FeishuAgent 必须透传 DeferTextUntilTurnComplete 供 ConversationalFeishuEventHandler 消费");
        property!.PropertyType.Should().Be(typeof(bool),
            "透传属性类型必须与 FeishuAgentOptions.DeferTextUntilTurnComplete 一致");
    }

    // 守卫 ④（中立回合管线消费 DeferTextUntilTurnComplete）随回合机制迁入 Mud.AI.Agent，由新仓
    // DeferTextContractGuards.ConversationalTurnPipeline_ShouldConsume_DeferTextUntilTurnComplete 持有；
    // 本仓只保留面向产品侧 FeishuAgentOptions / FeishuAgent 的三条断言。
}
