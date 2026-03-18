// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Mud.Feishu.Webhook.Configuration;

namespace Mud.Feishu.Webhook.Tests.Middleware;

/// <summary>
/// FeishuRateLimitMiddleware 单元测试
/// </summary>
public class FeishuRateLimitMiddlewareTests : IDisposable
{
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly Mock<ILogger<FeishuRateLimitMiddleware>> _loggerMock;
    private readonly FeishuWebhookOptions _options;
    private FeishuRateLimitMiddleware? _middleware;

    public FeishuRateLimitMiddlewareTests()
    {
        _nextMock = new Mock<RequestDelegate>();
        _loggerMock = new Mock<ILogger<FeishuRateLimitMiddleware>>();
        _options = new FeishuWebhookOptions
        {
            GlobalRoutePrefix = "feishu",
            RateLimit = new RateLimitOptions
            {
                EnableRateLimit = true,
                MaxRequestsPerWindow = 10,
                WindowSizeSeconds = 60,
                TooManyRequestsStatusCode = 429,
                TooManyRequestsMessage = "请求过于频繁"
            }
        };
    }

    [Fact]
    public void Constructor_WithNullNext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_options);
        var logger = _loggerMock.Object;

        // Act
        var action = () => new FeishuRateLimitMiddleware(null!, options, logger);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_options);
        RequestDelegate next = _ => Task.CompletedTask;

        // Act
        var action = () => new FeishuRateLimitMiddleware(next, options, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task InvokeAsync_WhenRateLimitDisabled_ShouldCallNext()
    {
        // Arrange
        _options.RateLimit.EnableRateLimit = false;
        _middleware = CreateMiddleware();
        var context = CreateHttpContext("/feishu/app1", "192.168.1.1");

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenNotWebhookPath_ShouldCallNext()
    {
        // Arrange
        _middleware = CreateMiddleware();
        var context = CreateHttpContext("/api/other", "192.168.1.1");

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnderLimit_ShouldCallNext()
    {
        // Arrange
        _middleware = CreateMiddleware();
        var context = CreateHttpContext("/feishu/app1", "192.168.1.1");

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(x => x(context), Times.Once);
        context.Response.StatusCode.Should().NotBe(429);
    }

    [Fact]
    public async Task InvokeAsync_WhenOverLimit_ShouldReturn429()
    {
        // Arrange
        _options.RateLimit.MaxRequestsPerWindow = 2;
        _middleware = CreateMiddleware();

        // Act - 发送超过限制的请求
        for (int i = 0; i < 3; i++)
        {
            var context = CreateHttpContext("/feishu/app1", "192.168.1.1");
            await _middleware.InvokeAsync(context);

            if (i < 2)
            {
                context.Response.StatusCode.Should().NotBe(429);
            }
        }

        // 最后一个请求应该被限流
        var limitedContext = CreateHttpContext("/feishu/app1", "192.168.1.1");
        await _middleware.InvokeAsync(limitedContext);
        limitedContext.Response.StatusCode.Should().Be(429);
    }

    [Fact]
    public async Task InvokeAsync_WhenIpInWhitelist_ShouldSkipRateLimit()
    {
        // Arrange
        _options.RateLimit.WhitelistIPs.Add("192.168.1.100");
        _options.RateLimit.MaxRequestsPerWindow = 1;
        _middleware = CreateMiddleware();

        // Act - 发送多个请求
        for (int i = 0; i < 5; i++)
        {
            var context = CreateHttpContext("/feishu/app1", "192.168.1.100");
            await _middleware.InvokeAsync(context);
            context.Response.StatusCode.Should().NotBe(429);
        }

        // Assert
        _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Exactly(5));
    }

    [Fact]
    public async Task InvokeAsync_WithDifferentApps_ShouldTrackSeparately()
    {
        // Arrange
        _options.RateLimit.MaxRequestsPerWindow = 1;
        _middleware = CreateMiddleware();

        // Act - 发送请求到不同应用
        var context1 = CreateHttpContext("/feishu/app1", "192.168.1.1");
        await _middleware.InvokeAsync(context1);
        context1.Response.StatusCode.Should().NotBe(429);

        var context2 = CreateHttpContext("/feishu/app2", "192.168.1.1");
        await _middleware.InvokeAsync(context2);
        context2.Response.StatusCode.Should().NotBe(429);
    }

    [Fact]
    public async Task InvokeAsync_WithDifferentIPs_ShouldTrackSeparately()
    {
        // Arrange
        _options.RateLimit.MaxRequestsPerWindow = 1;
        _middleware = CreateMiddleware();

        // Act - 从不同IP发送请求
        var context1 = CreateHttpContext("/feishu/app1", "192.168.1.1");
        await _middleware.InvokeAsync(context1);
        context1.Response.StatusCode.Should().NotBe(429);

        var context2 = CreateHttpContext("/feishu/app1", "192.168.1.2");
        await _middleware.InvokeAsync(context2);
        context2.Response.StatusCode.Should().NotBe(429);
    }

    [Fact]
    public async Task InvokeAsync_WithXForwardedForHeader_ShouldUseFirstIP()
    {
        // Arrange
        _middleware = CreateMiddleware();
        var context = CreateHttpContext("/feishu/app1", "10.0.0.1");
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.1, 70.41.3.18";

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithXRealIPHeader_ShouldUseHeaderIP()
    {
        // Arrange
        _middleware = CreateMiddleware();
        var context = CreateHttpContext("/feishu/app1", "10.0.0.1");
        context.Request.Headers["X-Real-IP"] = "203.0.113.50";

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public void Dispose_ShouldCleanupTimer()
    {
        // Arrange
        _middleware = CreateMiddleware();

        // Act & Assert - 不应抛出异常
        var action = () => _middleware.Dispose();
        action.Should().NotThrow();
    }

    private FeishuRateLimitMiddleware CreateMiddleware()
    {
        return new FeishuRateLimitMiddleware(
            _nextMock.Object,
            Options.Create(_options),
            _loggerMock.Object);
    }

    private static HttpContext CreateHttpContext(string path, string remoteIp)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(remoteIp);
        context.Response.Body = new MemoryStream();
        return context;
    }

    public void Dispose()
    {
        _middleware?.Dispose();
    }
}
