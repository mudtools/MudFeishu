// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Net.WebSockets;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// P2-15 回归测试：WebSocket 主机白名单（<see cref="FeishuWebSocketOptions.AllowedHostSuffixes"/>）。
/// </summary>
/// <remarks>
/// 改造前 <c>ConnectAsync</c> 仅校验 scheme——端点 URL 由服务端 API 下发，一旦被篡改
/// （DNS 劫持 / API 响应被篡改 / 配置错误），客户端会向任意主机发起连接（SSRF 面）。
/// <para>
/// 默认白名单为 <c>*.feishu.cn;*.larksuite.com</c>；置空表示不限制（历史行为）。
/// </para>
/// </remarks>
public class HostAllowListTests
{
    private readonly Mock<ILoggerFactory> _loggerFactoryMock = new();

    public HostAllowListTests()
    {
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(NullLogger.Instance);
    }

    private WebSocketConnectionManager CreateManager(string allowedHostSuffixes, int connectionTimeoutMs = 1000)
        => new(NullLogger<WebSocketConnectionManager>.Instance,
            new FeishuWebSocketOptions
            {
                ConnectionTimeoutMs = connectionTimeoutMs,
                AllowedHostSuffixes = allowedHostSuffixes,
                // 本类的用例聚焦"主机白名单"语义，统一放行 ws:// 以免 scheme 校验抢先抛错、遮蔽被测行为
                Certificate = new WebSocketCertificateOptions { AllowInsecureWebSocket = true }
            },
            _loggerFactoryMock.Object);

    private static int GetClosedPort()
    {
        // 无监听者的端口：既保证"通过了主机校验"的用例快速失败（连接拒绝），
        // 又保证"未通过校验"的用例不会意外建立连接
        var probe = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        probe.Start();
        var port = ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    [Fact]
    public async Task ConnectAsync_ShouldRejectHostOutsideDefaultAllowList()
    {
        // Arrange：默认白名单 = *.feishu.cn;*.larksuite.com
        var manager = CreateManager("*.feishu.cn;*.larksuite.com");

        // Act
        var act = () => manager.ConnectAsync("wss://evil.example.com/ws");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*白名单*");
        manager.ConnectionCount.Should().Be(0);
    }

    [Fact]
    public async Task ConnectAsync_ShouldRejectSpoofedSuffixHost()
    {
        // Arrange：evil-feishu.cn 的后缀是 "feishu.cn" 而非 ".feishu.cn"，
        // 不得被 *.feishu.cn 的通配规则误放行（前导点匹配）
        var manager = CreateManager("*.feishu.cn;*.larksuite.com");

        // Act
        var act = () => manager.ConnectAsync("wss://evil-feishu.cn/ws");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*白名单*");
    }

    [Fact]
    public async Task ConnectAsync_ShouldAllowHost_WhenExplicitlyListed()
    {
        // Arrange：精确主机名在白名单内 → 应通过主机校验、进入真实连接阶段（连接被拒）
        var closedPort = GetClosedPort();
        var manager = CreateManager("127.0.0.1");

        // Act
        var act = () => manager.ConnectAsync($"ws://127.0.0.1:{closedPort}/ws");

        // Assert：异常必须是连接类错误，而非主机白名单校验
        var exception = await act.Should().ThrowAsync<Exception>();
        exception.Which.Message.Should().NotContain("白名单", "已列入白名单的主机必须放行主机校验");

        manager.Dispose();
    }

    [Fact]
    public async Task ConnectAsync_ShouldAllowWildcardSubdomain_WhenConfigured()
    {
        // Arrange：sub.127.0.0.1 是 *.127.0.0.1 的子域（DNS 解析会快速失败）
        var manager = CreateManager("*.127.0.0.1", connectionTimeoutMs: 2000);

        // Act
        var act = () => manager.ConnectAsync("ws://sub.127.0.0.1:9/ws");

        // Assert
        var exception = await act.Should().ThrowAsync<Exception>();
        exception.Which.Message.Should().NotContain("白名单", "通配后缀必须放行任意层级子域");

        manager.Dispose();
    }

    [Fact]
    public async Task ConnectAsync_ShouldAllowAnyHost_WhenAllowListEmpty()
    {
        // Arrange：置空 = 不限制（历史行为），本地回环网关可正常发起连接
        var closedPort = GetClosedPort();
        var manager = CreateManager(string.Empty);

        // Act
        var act = () => manager.ConnectAsync($"ws://127.0.0.1:{closedPort}/ws");

        // Assert
        var exception = await act.Should().ThrowAsync<Exception>();
        exception.Which.Message.Should().NotContain("白名单");

        manager.Dispose();
    }

    [Fact]
    public void Validate_ShouldAcceptEmptyAllowList()
    {
        // Arrange：置空语义（不限制）必须是合法配置
        var options = new FeishuWebSocketOptions { AllowedHostSuffixes = "  " };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }
}
