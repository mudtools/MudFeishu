// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Models;
using Mud.Feishu.Webhook;
using System.Net;
using System.Text;

namespace Mud.Feishu.Webhook.Tests.Middleware;

/// <summary>
/// IP 白名单验证测试
/// </summary>
public class IpAddressValidationTests
{
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly Mock<ILogger<FeishuMultiAppMiddleware>> _loggerMock;
    private readonly FeishuWebhookOptions _options;

    public IpAddressValidationTests()
    {
        _nextMock = new Mock<RequestDelegate>();
        _loggerMock = new Mock<ILogger<FeishuMultiAppMiddleware>>();
        _options = new FeishuWebhookOptions
        {
            GlobalRoutePrefix = "feishu",
            Apps = new Dictionary<string, FeishuAppWebhookOptions>
            {
                ["app1"] = new FeishuAppWebhookOptions
                {
                    AppKey = "app1",
                    VerificationToken = "test_token_1",
                    EncryptKey = "test_encrypt_key_1_32_chars_!!!"
                }
            }
        };
    }

    [Fact]
    public void IpAddressHelper_WithExactMatch_ShouldReturnTrue()
    {
        // Arrange
        var allowedIPs = new HashSet<string> { "192.168.1.100" };

        // Act
        var result = IpAddressHelper.IsIpAllowed("192.168.1.100", allowedIPs);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IpAddressHelper_WithNonAllowedIP_ShouldReturnFalse()
    {
        // Arrange
        var allowedIPs = new HashSet<string> { "192.168.1.100" };

        // Act
        var result = IpAddressHelper.IsIpAllowed("192.168.1.101", allowedIPs);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IpAddressHelper_WithCIDRMatch_ShouldReturnTrue()
    {
        // Arrange
        var allowedIPs = new HashSet<string> { "192.168.1.0/24" };

        // Act
        var result = IpAddressHelper.IsIpAllowed("192.168.1.50", allowedIPs);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IpAddressHelper_WithCIDRNoMatch_ShouldReturnFalse()
    {
        // Arrange
        var allowedIPs = new HashSet<string> { "192.168.1.0/24" };

        // Act
        var result = IpAddressHelper.IsIpAllowed("192.168.2.50", allowedIPs);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IpAddressHelper_WithEmptyAllowedIPs_ShouldReturnFalse()
    {
        // Arrange
        var allowedIPs = new HashSet<string>();

        // Act
        var result = IpAddressHelper.IsIpAllowed("192.168.1.100", allowedIPs);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IpAddressHelper_WithNullIP_ShouldReturnFalse()
    {
        // Arrange
        var allowedIPs = new HashSet<string> { "192.168.1.100" };

        // Act
        var result = IpAddressHelper.IsIpAllowed(null, allowedIPs);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IpAddressHelper_WithIPv6CIDR_ShouldReturnTrue()
    {
        // Arrange
        var allowedIPs = new HashSet<string> { "2001:db8::/32" };

        // Act
        var result = IpAddressHelper.IsIpAllowed("2001:db8:85a3::8a2e:370:7334", allowedIPs);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Middleware_WithIPWhitelist_ShouldBlockNonAllowedIP()
    {
        // Arrange
        _options.AllowedSourceIPs = new HashSet<string> { "192.168.1.100" };
        var optionsMonitorMock = new Mock<IOptionsMonitor<FeishuWebhookOptions>>();
        optionsMonitorMock.Setup(x => x.CurrentValue).Returns(_options);
        optionsMonitorMock.Setup(x => x.OnChange(It.IsAny<Action<FeishuWebhookOptions, string?>>()))
            .Returns((IDisposable)null!);

        var handlerRegistry = new FeishuWebhookHandlerRegistry();
        handlerRegistry.Register("app1", typeof(TestHandler));

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var scopeMock = new Mock<IServiceScope>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(x => x.GetService(typeof(IFeishuWebhookService)))
            .Returns(new Mock<IFeishuWebhookService>().Object);
        scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
        scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);

        var middleware = new FeishuMultiAppMiddleware(
            _nextMock.Object,
            scopeFactoryMock.Object,
            _loggerMock.Object,
            optionsMonitorMock.Object,
            handlerRegistry);

        var context = new DefaultHttpContext();
        context.Request.Path = "/feishu/app1";
        context.Request.Method = "POST";
        context.Request.ContentType = "application/json";
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.200"); // 不在白名单中
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"encrypt\":\"test\"}"));
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Middleware_WithIPWhitelist_ShouldAllowWhitelistedIP()
    {
        // Arrange
        _options.AllowedSourceIPs = new HashSet<string> { "192.168.1.100" };
        var optionsMonitorMock = new Mock<IOptionsMonitor<FeishuWebhookOptions>>();
        optionsMonitorMock.Setup(x => x.CurrentValue).Returns(_options);
        optionsMonitorMock.Setup(x => x.OnChange(It.IsAny<Action<FeishuWebhookOptions, string?>>()))
            .Returns((IDisposable)null!);

        var handlerRegistry = new FeishuWebhookHandlerRegistry();
        handlerRegistry.Register("app1", typeof(TestHandler));

        var webhookServiceMock = new Mock<IFeishuWebhookService>();
        webhookServiceMock.Setup(x => x.DecryptEventAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Abstractions.EventData { EventType = "test.event", EventId = "123" });
        webhookServiceMock.Setup(x => x.HandleEventAsync(It.IsAny<FeishuWebhookRequest>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        webhookServiceMock.Setup(x => x.HandleEventAsync(It.IsAny<Abstractions.EventData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null));

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var scopeMock = new Mock<IServiceScope>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(x => x.GetService(typeof(IFeishuWebhookService)))
            .Returns(webhookServiceMock.Object);
        scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
        scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);

        var middleware = new FeishuMultiAppMiddleware(
            _nextMock.Object,
            scopeFactoryMock.Object,
            _loggerMock.Object,
            optionsMonitorMock.Object,
            handlerRegistry);

        var context = new DefaultHttpContext();
        context.Request.Path = "/feishu/app1";
        context.Request.Method = "POST";
        context.Request.ContentType = "application/json";
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.100"); // 在白名单中
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"encrypt\":\"test\"}"));
        context.Response.Body = new MemoryStream();
        context.Request.Headers["X-Lark-Signature"] = "test_signature";
        context.Request.Headers["X-Lark-Request-Nonce"] = "test_nonce";
        context.Request.Headers["X-Lark-Request-Timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        // Act
        await middleware.InvokeAsync(context);

        // Assert - 应该通过 IP 验证，继续处理请求
        // 注意：由于我们没有完整的签名验证环境，这里只验证没有返回 403
        context.Response.StatusCode.Should().NotBe(403);
    }

    private class TestHandler;
}
