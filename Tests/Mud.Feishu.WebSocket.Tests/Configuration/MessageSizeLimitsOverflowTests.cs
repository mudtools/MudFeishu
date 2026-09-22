// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;

namespace Mud.Feishu.WebSocket.Tests.Configuration;

/// <summary>
/// <see cref="MessageSizeLimits"/> 派生字节上限的溢出边界测试（R2 / WS2-04 / P2-1）。
/// </summary>
/// <remarks>
/// 修复前：<c>ResolveMaxTextMessageBytes() => MaxTextMessageSize * 3</c> 是裸 <see cref="int"/> 乘法，
/// 当 <see cref="MessageSizeLimits.MaxTextMessageSize"/> 超过 <c>int.MaxValue / 3</c>（715,827,882）时
/// 溢出为负数 ⇒ <c>buffer.Length &gt; 负数</c> 恒真 ⇒ <b>文本发送全失败、分片文本全丢弃</b>。
/// <para>
/// 配置面现已补上界（<c>Validate()</c> 拒绝大于 10MB），故该溢出路径在启动期即被拦截；
/// 本类验证的是"即使被绕过校验，派生值也不会变成负数"这一纵深防御。
/// </para>
/// <para>默认值/常规赋值的断言见 <c>MessageSizeLimitsTests</c>（既有类）。</para>
/// </remarks>
public class MessageSizeLimitsOverflowTests
{
    [Fact]
    public void ResolveMaxTextMessageBytes_ShouldNotOverflow_WhenMaxTextMessageSizeIsHuge()
    {
        // Arrange：int.MaxValue 字符 → 3 倍必然溢出 int 范围
        var limits = new MessageSizeLimits { MaxTextMessageSize = int.MaxValue };

        // Act
        var resolved = limits.ResolveMaxTextMessageBytes();

        // Assert
        resolved.Should().BePositive("溢出为负会让大小校验恒真（全部消息被拒/被丢弃）");
        resolved.Should().Be(int.MaxValue, "超出 int 表示域时饱和到 int.MaxValue（返回类型保持 int 以免破坏公共签名）");
    }

    [Fact]
    public void ResolveMaxTextMessageBytes_ShouldSaturateExactly_AtBoundary()
    {
        // int.MaxValue / 3 = 715,827,882 → 该值正好不溢出；+1 即溢出
        var atBoundary = new MessageSizeLimits { MaxTextMessageSize = 715_827_882 };
        var overBoundary = new MessageSizeLimits { MaxTextMessageSize = 715_827_883 };

        atBoundary.ResolveMaxTextMessageBytes().Should().Be(2_147_483_646, "边界内应精确计算");
        overBoundary.ResolveMaxTextMessageBytes().Should().Be(int.MaxValue, "越过边界后必须饱和而非回绕为负");
    }

    [Fact]
    public void ResolveMaxTextMessageBytes_ShouldPreferExplicitValue_WhenConfigured()
    {
        var limits = new MessageSizeLimits { MaxTextMessageSize = int.MaxValue, MaxTextMessageBytes = 4096 };

        limits.ResolveMaxTextMessageBytes().Should().Be(4096, "显式配置优先，且不受派生路径溢出影响");
    }

    [Fact]
    public void ResolveMaxTextMessageBytes_ShouldDeriveThreeTimesChars_WhenDefault()
    {
        new MessageSizeLimits { MaxTextMessageSize = 1024 * 1024 }
            .ResolveMaxTextMessageBytes().Should().Be(3 * 1024 * 1024);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenMaxTextMessageSizeExceedsUpperBound()
    {
        // WS2-04 / I16：新增上界（10MB）
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();
        options.MessageSizeLimits.MaxTextMessageSize = Mud.Feishu.WebSocket.FeishuWebSocketOptions.MaxTextMessageSizeUpperBound + 1;

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*MaxTextMessageSize*");
    }

    [Fact]
    public void Validate_ShouldAcceptDefaultMaxTextMessageSize()
    {
        var act = () => new Mud.Feishu.WebSocket.FeishuWebSocketOptions().Validate();

        act.Should().NotThrow("默认值 1MB 必须在上界之内（上界收紧不得影响默认配置）");
    }
}
