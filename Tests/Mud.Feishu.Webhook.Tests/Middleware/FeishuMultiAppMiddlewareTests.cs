// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Models;
using System.Text;
using System.Text.Json;

namespace Mud.Feishu.Webhook.Tests.Middleware;

/// <summary>
/// FeishuMultiAppMiddleware 单元测试
/// </summary>
public class FeishuMultiAppMiddlewareTests
{
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly Mock<ILogger<FeishuMultiAppMiddleware>> _loggerMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IFeishuWebhookService> _webhookServiceMock;
    private readonly FeishuWebhookHandlerRegistry _handlerRegistry;
    private readonly FeishuWebhookOptions _options;

    public FeishuMultiAppMiddlewareTests()
    {
        _nextMock = new Mock<RequestDelegate>();
        _loggerMock = new Mock<ILogger<FeishuMultiAppMiddleware>>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeMock = new Mock<IServiceScope>();
        _webhookServiceMock = new Mock<IFeishuWebhookService>();
        _handlerRegistry = new FeishuWebhookHandlerRegistry();
        _options = new FeishuWebhookOptions
        {
            GlobalRoutePrefix = "feishu",
            Apps = new Dictionary<string, FeishuAppWebhookOptions>
            {
                ["app1"] = new FeishuAppWebhookOptions
                {
                    AppKey = "app1",
                    VerificationToken = "test_token_1",
                    EncryptKey = "test_encrypt_key_1"
                },
                ["app2"] = new FeishuAppWebhookOptions
                {
                    AppKey = "app2",
                    VerificationToken = "test_token_2",
                    EncryptKey = "test_encrypt_key_2"
                }
            }
        };

        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(_scopeMock.Object);
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(x => x.GetService(typeof(IFeishuWebhookService)))
            .Returns(_webhookServiceMock.Object);
        _scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
    }

    [Fact]
    public async Task InvokeAsync_WithNonMatchingPath_ShouldCallNext()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/api/other", "POST");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithUnknownAppKey_ShouldCallNext()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/feishu/unknown_app", "POST");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithNoHandlers_ShouldContinueProcessingWithGlobalFactory()
    {
        // Arrange - 没有应用专属处理器，但应该继续处理（使用全局工厂）
        var middleware = CreateMiddleware();
        var body = JsonSerializer.Serialize(new { encrypt = "test_encrypted_data" });
        var context = CreateHttpContext("/feishu/app1", "POST", body);

        // 设置 webhookService 模拟
        _webhookServiceMock.Setup(x => x.SetCurrentAppKey(It.IsAny<string>()));
        _webhookServiceMock.Setup(x => x.DecryptEventAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventData { EventType = "test_event", EventId = "test_id" });
        _webhookServiceMock.Setup(x => x.HandleEventAsync(It.IsAny<FeishuWebhookRequest>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _webhookServiceMock.Setup(x => x.HandleEventAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null));

        // Act
        await middleware.InvokeAsync(context);

        // Assert - 不应该调用 _next，应该继续处理请求
        _nextMock.Verify(x => x(context), Times.Never);
        // 验证请求被处理（状态码应该是 200）
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidHttpMethod_ShouldReturn405()
    {
        // Arrange
        _handlerRegistry.Register("app1", typeof(TestHandler));
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/feishu/app1", "GET");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(405);
        _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidContentType_ShouldReturn400()
    {
        // Arrange
        _handlerRegistry.Register("app1", typeof(TestHandler));
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/feishu/app1", "POST");
        context.Request.ContentType = "text/plain";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task InvokeAsync_WithEmptyBody_ShouldReturn400()
    {
        // Arrange
        _handlerRegistry.Register("app1", typeof(TestHandler));
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/feishu/app1", "POST", "");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task InvokeAsync_WithUrlVerificationRequest_ShouldReturnChallenge()
    {
        // Arrange
        _handlerRegistry.Register("app1", typeof(TestHandler));
        var verificationRequest = new EventVerificationRequest
        {
            Type = "url_verification",
            Challenge = "test_challenge_code"
        };
        var requestBody = JsonSerializer.Serialize(verificationRequest);
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/feishu/app1", "POST", requestBody);

        _webhookServiceMock
            .Setup(x => x.VerifyEventSubscriptionAsync(It.IsAny<EventVerificationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventVerificationResponse { Challenge = "test_challenge_code" });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidJson_ShouldReturn400()
    {
        // Arrange
        _handlerRegistry.Register("app1", typeof(TestHandler));
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/feishu/app1", "POST", "invalid json {");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task InvokeAsync_WithMissingEncryptField_ShouldReturn400()
    {
        // Arrange
        _handlerRegistry.Register("app1", typeof(TestHandler));
        var requestBody = JsonSerializer.Serialize(new { type = "event" });
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/feishu/app1", "POST", requestBody);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);
    }

    private FeishuMultiAppMiddleware CreateMiddleware()
    {
        var optionsMonitorMock = new Mock<IOptionsMonitor<FeishuWebhookOptions>>();
        optionsMonitorMock.Setup(x => x.CurrentValue).Returns(_options);
        optionsMonitorMock.Setup(x => x.OnChange(It.IsAny<Action<FeishuWebhookOptions, string?>>()))
            .Returns((IDisposable)null!);

        return new FeishuMultiAppMiddleware(
            _nextMock.Object,
            _scopeFactoryMock.Object,
            _loggerMock.Object,
            optionsMonitorMock.Object,
            _handlerRegistry);
    }

    private static HttpContext CreateHttpContext(string path, string method, string? body = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;
        context.Request.ContentType = "application/json";
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");

        if (body != null)
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            context.Request.Body = new MemoryStream(bytes);
            context.Request.ContentLength = bytes.Length;
        }
        else
        {
            context.Request.Body = new MemoryStream();
        }

        context.Response.Body = new MemoryStream();
        return context;
    }

    private class TestHandler;
}
