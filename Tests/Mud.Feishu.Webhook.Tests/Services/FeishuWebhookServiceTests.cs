// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Models;

namespace Mud.Feishu.Webhook.Tests.Services;

/// <summary>
/// FeishuWebhookService 单元测试
/// </summary>
public class FeishuWebhookServiceTests
{
    private readonly Mock<IOptionsMonitor<FeishuWebhookOptions>> _optionsMonitorMock;
    private readonly Mock<IFeishuEventValidator> _validatorMock;
    private readonly Mock<IFeishuEventDecryptor> _decryptorMock;
    private readonly Mock<IFeishuEventHandlerFactory> _handlerFactoryMock;
    private readonly Mock<ILogger<FeishuWebhookService>> _loggerMock;
    private readonly FeishuWebhookConcurrencyService _concurrencyService;
    private readonly Mock<IFeishuEventDeduplicator> _deduplicatorMock;
    private readonly Mock<IEncryptKeyProvider> _encryptKeyProviderMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IWebhookAppKeyAccessor> _appKeyAccessorMock;
    private readonly FeishuWebhookOptions _options;

    public FeishuWebhookServiceTests()
    {
        _optionsMonitorMock = new Mock<IOptionsMonitor<FeishuWebhookOptions>>();
        _validatorMock = new Mock<IFeishuEventValidator>();
        _decryptorMock = new Mock<IFeishuEventDecryptor>();
        _handlerFactoryMock = new Mock<IFeishuEventHandlerFactory>();
        _loggerMock = new Mock<ILogger<FeishuWebhookService>>();
        _deduplicatorMock = new Mock<IFeishuEventDeduplicator>();
        _encryptKeyProviderMock = new Mock<IEncryptKeyProvider>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _appKeyAccessorMock = new Mock<IWebhookAppKeyAccessor>();

        _options = new FeishuWebhookOptions
        {
            EnableBodySignatureValidation = false,
            EventHandlingTimeoutMs = 5000,
            MaxConcurrentEvents = 10
        };

        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(_options);

        var concurrencyLoggerMock = new Mock<ILogger<FeishuWebhookConcurrencyService>>();
        _concurrencyService = new FeishuWebhookConcurrencyService(_optionsMonitorMock.Object, concurrencyLoggerMock.Object);

        // 设置 _appKeyAccessorMock 使 SetAppKey 方法能够更新 CurrentAppKey 属性
        string? currentAppKey = null;
        _appKeyAccessorMock
            .Setup(x => x.SetAppKey(It.IsAny<string>()))
            .Callback<string>(appKey => currentAppKey = appKey);
        _appKeyAccessorMock
            .Setup(x => x.CurrentAppKey)
            .Returns(() => currentAppKey);

        _encryptKeyProviderMock
            .Setup(x => x.GetEncryptKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string appKey, CancellationToken _) =>
            {
                if (_options.Apps.TryGetValue(appKey, out var appConfig))
                {
                    return appConfig.EncryptKey;
                }
                return null;
            });

        _encryptKeyProviderMock
            .Setup(x => x.GetVerificationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string appKey, CancellationToken _) =>
            {
                if (_options.Apps.TryGetValue(appKey, out var appConfig))
                {
                    return appConfig.VerificationToken;
                }
                return null;
            });
    }

    [Fact]
    public async Task VerifyEventSubscriptionAsync_WithValidRequest_ShouldReturnChallenge()
    {
        // Arrange
        var request = new EventVerificationRequest
        {
            Type = "url_verification",
            Token = "test_token",
            Challenge = "test_challenge"
        };

        _validatorMock
            .Setup(x => x.ValidateSubscriptionRequestAsync(request, "test_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // 设置当前应用键，这是 VerifyEventSubscriptionAsync 的前置条件
        var appConfig = new FeishuAppWebhookOptions
        {
            AppKey = "test_app",
            VerificationToken = "test_token",
            EncryptKey = "12345678901234567890123456789012" // 32 chars
        };
        _options.Apps["test_app"] = appConfig;

        var service = CreateService();
        service.SetCurrentAppKey("test_app");

        // Act
        var result = await service.VerifyEventSubscriptionAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test_challenge", result.Challenge);
    }

    [Fact]
    public async Task VerifyEventSubscriptionAsync_WithInvalidRequest_ShouldReturnNull()
    {
        // Arrange
        var request = new EventVerificationRequest
        {
            Type = "url_verification",
            Token = "wrong_token",
            Challenge = "test_challenge"
        };

        _validatorMock
            .Setup(x => x.ValidateSubscriptionRequestAsync(request, "test_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = CreateService();

        // Act
        var result = await service.VerifyEventSubscriptionAsync(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task HandleEventAsync_WithEventData_ShouldProcessSuccessfully()
    {
        // Arrange
        var eventData = new EventData
        {
            EventId = "test_event_123",
            EventType = "test.event",
            CreateTime = 1234567890
        };

        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessing(eventData.EventId, It.IsAny<string?>()))
            .Returns(false);

        _handlerFactoryMock
            .Setup(x => x.HandleEventParallelAsync(eventData.EventType, eventData, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.HandleEventAsync(eventData);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorReason);
        _handlerFactoryMock.Verify(x => x.HandleEventParallelAsync(
            eventData.EventType,
            eventData,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleEventAsync_WithDuplicateEvent_ShouldSkipProcessing()
    {
        // Arrange
        var eventData = new EventData
        {
            EventId = "duplicate_event",
            EventType = "test.event"
        };

        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessing(eventData.EventId, It.IsAny<string?>()))
            .Returns(true); // 已处理过

        var service = CreateService();

        // Act
        var result = await service.HandleEventAsync(eventData);

        // Assert
        Assert.True(result.Success); // 幂等性：返回成功
        _handlerFactoryMock.Verify(x => x.HandleEventParallelAsync(
            It.IsAny<string>(),
            It.IsAny<EventData>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DecryptEventAsync_WithValidData_ShouldReturnEventData()
    {
        // Arrange
        var encryptedData = "encrypted_data";
        var expectedEventData = new EventData
        {
            EventId = "test_123",
            EventType = "test.event"
        };
        var encryptKey = "test_key_32_characters_long____";

        _decryptorMock
            .Setup(x => x.DecryptAsync(encryptedData, encryptKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEventData);

        var service = CreateService();
        service.SetCurrentAppKey("test-app");
        _options.Apps["test-app"] = new FeishuAppWebhookOptions
        {
            AppKey = "test-app",
            EncryptKey = encryptKey
        };

        // Act
        var result = await service.DecryptEventAsync(encryptedData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test_123", result.EventId);
        Assert.Equal("test.event", result.EventType);
    }

    [Fact]
    public async Task DecryptEventAsync_WithInvalidData_ShouldReturnNull()
    {
        // Arrange
        var encryptedData = "invalid_data";
        var encryptKey = "test_key_32_characters_long____";

        _decryptorMock
            .Setup(x => x.DecryptAsync(encryptedData, encryptKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventData?)null);

        var service = CreateService();
        service.SetCurrentAppKey("test-app");
        _options.Apps["test-app"] = new FeishuAppWebhookOptions
        {
            AppKey = "test-app",
            EncryptKey = encryptKey
        };

        // Act
        var result = await service.DecryptEventAsync(encryptedData);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task HandleEventAsync_WithFeishuWebhookRequest_ShouldValidateSignatureSuccessfully()
    {
        // Arrange
        var nonce = "test_nonce_12345";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var encryptKey = "test_key_32_characters_long____";
        var body = "{\"encrypt\":\"encrypted_data\"}";

        // 计算正确的签名
        var signString = $"{timestamp}{nonce}{encryptKey}{body}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signString));
        var computedSignature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

        var request = new FeishuWebhookRequest
        {
            Encrypt = "encrypted_data",
            Signature = computedSignature,
            Nonce = nonce,
            Timestamp = timestamp
        };

        var service = CreateService();
        service.SetCurrentAppKey("test-app");
        _options.Apps["test-app"] = new FeishuAppWebhookOptions
        {
            AppKey = "test-app",
            EncryptKey = encryptKey
        };

        // Act
        var result = await service.HandleEventAsync(request, body);

        // Assert
        Assert.True(result);
    }

    private FeishuWebhookService CreateService()
    {
        return new FeishuWebhookService(
            _optionsMonitorMock.Object,
            _validatorMock.Object,
            _decryptorMock.Object,
            _handlerFactoryMock.Object,
            _loggerMock.Object,
            Array.Empty<IFeishuEventInterceptor>(),
            _concurrencyService,
            _deduplicatorMock.Object,
            _encryptKeyProviderMock.Object,
            new FeishuWebhookHandlerRegistry(),
            new FeishuWebhookInterceptorRegistry(),
            _serviceProviderMock.Object,
            _appKeyAccessorMock.Object,
            null,
            null);
    }

    [Fact]
    public async Task HandleEventAsync_WithDifferentAppKeys_ShouldAllowSameEventId()
    {
        // Arrange
        var eventId = "shared_event_123";
        var eventData1 = new EventData
        {
            EventId = eventId,
            EventType = "test.event"
        };
        var eventData2 = new EventData
        {
            EventId = eventId,
            EventType = "test.event"
        };

        var appKey1 = "app-001";
        var appKey2 = "app-002";

        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessing(eventId, appKey1))
            .Returns(false);

        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessing(eventId, appKey2))
            .Returns(false);

        _handlerFactoryMock
            .Setup(x => x.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act - App1 处理事件
        service.SetCurrentAppKey(appKey1);
        var result1 = await service.HandleEventAsync(eventData1);

        // Assert - App1 应该成功
        Assert.True(result1.Success);

        // Act - App2 处理相同事件ID
        service.SetCurrentAppKey(appKey2);
        var result2 = await service.HandleEventAsync(eventData2);

        // Assert - App2 也应该成功（不同应用隔离）
        Assert.True(result2.Success);
    }

    [Fact]
    public async Task HandleEventAsync_WithAppSpecificHandler_ShouldUseAppHandler()
    {
        // Arrange
        var appKey = "app-001";
        var eventData = new EventData
        {
            EventId = "test_event_456",
            EventType = "test.event"
        };

        var handlerRegistry = new FeishuWebhookHandlerRegistry();
        handlerRegistry.Register(appKey, typeof(TestAppHandler));

        var handlerMock = new Mock<IFeishuEventHandler>();
        handlerMock
            .Setup(x => x.HandleAsync(eventData, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(TestAppHandler)))
            .Returns(handlerMock.Object);

        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessing(eventData.EventId, appKey))
            .Returns(false);

        var service = new FeishuWebhookService(
            _optionsMonitorMock.Object,
            _validatorMock.Object,
            _decryptorMock.Object,
            _handlerFactoryMock.Object,
            _loggerMock.Object,
            Array.Empty<IFeishuEventInterceptor>(),
            _concurrencyService,
            _deduplicatorMock.Object,
            _encryptKeyProviderMock.Object,
            handlerRegistry,
            new FeishuWebhookInterceptorRegistry(),
            _serviceProviderMock.Object,
            _appKeyAccessorMock.Object,
            null,
            null);

        // Act
        service.SetCurrentAppKey(appKey);
        var result = await service.HandleEventAsync(eventData);

        // Assert
        Assert.True(result.Success);
        handlerMock.Verify(x => x.HandleAsync(eventData, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleEventAsync_WithAppSpecificInterceptor_ShouldUseAppInterceptor()
    {
        // Arrange
        var appKey = "app-001";
        var eventData = new EventData
        {
            EventId = "test_event_789",
            EventType = "test.event"
        };

        var interceptorRegistry = new FeishuWebhookInterceptorRegistry();
        interceptorRegistry.Register(appKey, typeof(TestAppInterceptor));

        var interceptorMock = new Mock<IFeishuEventInterceptor>();
        interceptorMock
            .Setup(x => x.BeforeHandleAsync(eventData.EventType, eventData, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        interceptorMock
            .Setup(x => x.AfterHandleAsync(eventData.EventType, eventData, It.IsAny<Exception?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(TestAppInterceptor)))
            .Returns(interceptorMock.Object);

        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessing(eventData.EventId, appKey))
            .Returns(false);

        _handlerFactoryMock
            .Setup(x => x.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new FeishuWebhookService(
            _optionsMonitorMock.Object,
            _validatorMock.Object,
            _decryptorMock.Object,
            _handlerFactoryMock.Object,
            _loggerMock.Object,
            Array.Empty<IFeishuEventInterceptor>(),
            _concurrencyService,
            _deduplicatorMock.Object,
            _encryptKeyProviderMock.Object,
            new FeishuWebhookHandlerRegistry(),
            interceptorRegistry,
            _serviceProviderMock.Object,
            _appKeyAccessorMock.Object,
            null,
            null);

        // Act
        service.SetCurrentAppKey(appKey);
        var result = await service.HandleEventAsync(eventData);

        // Assert
        Assert.True(result.Success);
        interceptorMock.Verify(x => x.BeforeHandleAsync(eventData.EventType, eventData, It.IsAny<CancellationToken>()), Times.Once);
        interceptorMock.Verify(x => x.AfterHandleAsync(eventData.EventType, eventData, It.IsAny<Exception?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private class TestAppHandler : IFeishuEventHandler
    {
        public string SupportedEventType => "test.event";
        public Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private class TestAppInterceptor : IFeishuEventInterceptor
    {
        public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
