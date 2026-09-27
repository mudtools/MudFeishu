// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tests.Agents;

/// <summary>
/// <see cref="FeishuAgentOptions"/> 校验边界（Phase 0 §3.2）与配置面约束（R5）。
/// </summary>
public class FeishuAgentOptionsTests
{
    [Fact]
    public void Validate_ShouldPass_WhenDefaultsOverriddenWithValidValues()
    {
        var act = () => new FeishuAgentOptions
        {
            Instructions = "你是飞书助手",
            MaxHistoryMessages = 20,
        }.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldRejectEmptyInstructions()
    {
        var act = () => new FeishuAgentOptions { Instructions = " " }.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*Instructions*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldRejectNonPositiveMaxHistoryMessages(int maxHistory)
    {
        var act = () => new FeishuAgentOptions
        {
            Instructions = "x",
            MaxHistoryMessages = maxHistory,
        }.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*MaxHistoryMessages*");
    }

    /// <summary>
    /// 会话 TTL 阈值归属 <see cref="FeishuConversationOptions"/>（Abstractions——
    /// Memory/Redis 双后端单一阈值源，包间纵向引用治理），不再位于 FeishuAgentOptions。
    /// </summary>
    [Fact]
    public void FeishuConversationOptions_Validate_ShouldRejectNonPositiveSessionTtl()
    {
        var invalid = () => new FeishuConversationOptions { SessionTtl = TimeSpan.Zero }.Validate();
        invalid.Should().Throw<InvalidOperationException>().WithMessage("*SessionTtl*");

        var valid = () => new FeishuConversationOptions().Validate();
        valid.Should().NotThrow();
    }

    /// <summary>
    /// R5：配置 DTO 不得使用 <c>required</c>——源生成配置绑定器经 <c>new T()</c> 构造，
    /// 带 required 会产生 CS9035。反射断言（比源码扫描更硬）。
    /// </summary>
    [Fact]
    public void Options_ShouldNotUseRequiredModifier()
    {
        var optionsTypes = new[] { typeof(FeishuAgentOptions), typeof(FeishuConversationOptions) };
        foreach (var optionsType in optionsTypes)
            foreach (var property in optionsType.GetProperties())
            {
                var isRequired = property.GetCustomAttributesData()
                    .Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.RequiredMemberAttribute")
                    || optionsType.GetCustomAttributesData()
                        .Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.RequiredMemberAttribute");

                isRequired.Should().BeFalse(
                    $"{optionsType.Name}.{property.Name} 不得标记 required（配置绑定源生成器 CS9035）");
            }
    }
}
