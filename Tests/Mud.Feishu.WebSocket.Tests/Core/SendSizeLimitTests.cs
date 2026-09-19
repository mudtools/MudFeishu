// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// P1-4 回归测试：发送侧与接收侧使用同一个"字节上限"解析入口（<c>ResolveMaxTextMessageBytes</c>）。
/// </summary>
/// <remarks>
/// 语义要点（计划文档 §6.2 修正项 M11）：
/// <list type="bullet">
/// <item>默认派生上限 = 3 × <c>MaxTextMessageSize</c>，恰好等于旧实现"字符语义"的字节上界，
/// 因此是<b>放宽</b>而非收紧，现有一切合法消息继续通过。</item>
/// <item>推论：<b>默认配置下</b>"字符校验先失败"是常态，字节校验只在用户显式收紧
/// <c>MaxTextMessageBytes</c>（小于 3× 字符上限）时才可能触发——这是刻意设计，不是死代码。</item>
/// <item>二进制发送此前<b>完全无上限校验</b>，现已补齐（且 <c>MaxBinaryMessageSize</c> 为 long，比较前需提升类型）。</item>
/// </list>
/// </remarks>
public class SendSizeLimitTests
{
    private readonly Mock<ILogger<WebSocketConnectionManager>> _loggerMock = new();
    private readonly Mock<ILoggerFactory> _loggerFactoryMock = new();

    public SendSizeLimitTests()
    {
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(NullLogger.Instance);
    }

    private WebSocketConnectionManager CreateManager(Action<FeishuWebSocketOptions>? configure = null)
    {
        var options = new FeishuWebSocketOptions { EnableLogging = false };
        configure?.Invoke(options);
        return new WebSocketConnectionManager(_loggerMock.Object, options, _loggerFactoryMock.Object);
    }

    #region MessageSizeLimits 解析

    [Fact]
    public void ResolveMaxTextMessageBytes_ShouldDeriveFromChars_WhenNotConfigured()
    {
        var limits = new MessageSizeLimits { MaxTextMessageSize = 1024 * 1024 };

        limits.MaxTextMessageBytes.Should().Be(0, "默认值表示自动推导");
        limits.ResolveMaxTextMessageBytes().Should().Be(1024 * 1024 * 3,
            "UTF-8 对 UTF-16 字符的最坏展开为 3 字节/字符，派生值保证旧字符语义下的合法消息全部继续通过");
    }

    [Fact]
    public void ResolveMaxTextMessageBytes_ShouldPreferExplicitValue_WhenConfigured()
    {
        var limits = new MessageSizeLimits { MaxTextMessageSize = 1024 * 1024, MaxTextMessageBytes = 4096 };

        limits.ResolveMaxTextMessageBytes().Should().Be(4096, "显式配置优先（供需要按字节收紧的场景使用）");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenMaxTextMessageBytesIsNegative()
    {
        var options = new FeishuWebSocketOptions();
        options.MessageSizeLimits.MaxTextMessageBytes = -1;

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*MaxTextMessageBytes*");
    }

    #endregion

    #region 文本发送字节校验

    [Fact]
    public async Task SendMessageAsync_ShouldThrow_WhenExplicitByteLimitExceeded()
    {
        // Arrange：显式把字节上限收得比字符上限更紧（400 个中文字符 = 1200 字节 > 1024 字节）
        var manager = CreateManager(o =>
        {
            o.MessageSizeLimits.MaxTextMessageSize = 1024 * 1024;
            o.MessageSizeLimits.MaxTextMessageBytes = 1024;
        });
        var message = new string('中', 400);

        // Act
        var act = () => manager.SendMessageAsync(message);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*1024 字节*");
    }

    [Fact]
    public async Task SendMessageAsync_ShouldNotThrow_WhenCjkPayloadWithinDerivedLimit()
    {
        // Arrange：默认派生上限（3× 字符）下，字符校验通过 ⇒ 字节校验必然通过（放宽语义）
        var manager = CreateManager(o => o.MessageSizeLimits.MaxTextMessageSize = 1024);
        var message = new string('中', 1024); // 1024 字符 = 3072 字节 = 派生上限，恰好等于上限

        // Act：由于未连接，异常类型应为 InvalidOperationException（说明已通过两层大小校验）
        var act = () => manager.SendMessageAsync(message);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*未连接*");
    }

    #endregion

    #region 二进制发送上限（此前完全缺失）

    [Fact]
    public async Task SendBinaryMessageAsync_ShouldThrow_WhenExceedsMaxBinaryMessageSize()
    {
        // Arrange
        var manager = CreateManager(o => o.MessageSizeLimits.MaxBinaryMessageSize = 1024);
        var payload = new byte[2048];

        // Act
        var act = () => manager.SendBinaryMessageAsync(payload);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*超*");
    }

    [Fact]
    public async Task SendBinaryMessageAsync_ShouldNotThrowSizeError_WhenExactlyAtLimit()
    {
        // Arrange：恰好等于上限不得被拒（边界语义：> 才拒绝）
        var manager = CreateManager(o => o.MessageSizeLimits.MaxBinaryMessageSize = 1024);
        var payload = new byte[1024];

        // Act：未连接 → InvalidOperationException（说明已通过大小校验）
        var act = () => manager.SendBinaryMessageAsync(payload);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion
}
