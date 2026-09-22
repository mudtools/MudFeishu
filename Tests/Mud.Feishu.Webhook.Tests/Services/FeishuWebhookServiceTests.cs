// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
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
            EventHandlingTimeoutMs = 5000,
            MaxConcurrentEvents = 10
        };

        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(_options);
        // 既有测试聚焦分发/状态机路径；WHF-09 门控（默认开启）在专属用例中覆盖
        _options.IgnoreUnknownEventTypes = false;

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
            .Setup(x => x.TryMarkAsProcessingAsync(eventData.EventId, It.IsAny<string?>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = false, WasProcessing = false });

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
            .Setup(x => x.TryMarkAsProcessingAsync(eventData.EventId, It.IsAny<string?>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = true, WasProcessing = false });

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

        // 设置验证器 mock 返回 true（委托给验证器进行签名验证）
        // WHF-R2/B3：FeishuWebhookService 调用带 CancellationToken 的重载
        _validatorMock
            .Setup(x => x.ValidateHeaderSignatureAsync(
                timestamp, nonce, body, computedSignature, encryptKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

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
        _validatorMock.Verify(x => x.ValidateHeaderSignatureAsync(
            timestamp, nonce, body, computedSignature, encryptKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    private FeishuWebhookService CreateService(
        IFailedEventStore? failedEventStore = null,
        IFeishuEventInterceptor[]? interceptors = null,
        Action<FeishuWebhookOptions>? configureOptions = null)
    {
        // 在**共享的** _options 实例上追加配置（不得替换 CurrentValue——既有用例会在
        // 调用 CreateService 前直接改写 _options.Apps / IgnoreUnknownEventTypes 等字段）。
        configureOptions?.Invoke(_options);
        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(_options);

        return new FeishuWebhookService(
            _optionsMonitorMock.Object,
            _validatorMock.Object,
            _decryptorMock.Object,
            _handlerFactoryMock.Object,
            _loggerMock.Object,
            interceptors ?? Array.Empty<IFeishuEventInterceptor>(),
            _concurrencyService,
            _deduplicatorMock.Object,
            _encryptKeyProviderMock.Object,
            new FeishuWebhookHandlerRegistry(),
            new FeishuWebhookInterceptorRegistry(),
            _serviceProviderMock.Object,
            _appKeyAccessorMock.Object,
            failedEventStore);
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
            .Setup(x => x.TryMarkAsProcessingAsync(eventId, appKey1, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = false, WasProcessing = false });

        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessingAsync(eventId, appKey2, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = false, WasProcessing = false });

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
            .Setup(x => x.TryMarkAsProcessingAsync(eventData.EventId, appKey, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = false, WasProcessing = false });

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
            _appKeyAccessorMock.Object);

        // Act
        service.SetCurrentAppKey(appKey);
        var result = await service.HandleEventAsync(eventData);

        // Assert
        Assert.True(result.Success);
        handlerMock.Verify(x => x.HandleAsync(eventData, It.IsAny<CancellationToken>()), Times.Once);
    }

    // P1-2（R2）：应用专属处理器必须按 SupportedEventType 过滤（空声明 = 处理全部）
    [Fact]
    public async Task HandleEventAsync_WithAppSpecificHandlersOfDifferentTypes_ShouldDispatchOnlyMatchingHandler()
    {
        // Arrange
        var appKey = "app-001";
        var eventData = new EventData
        {
            EventId = "test_event_p12_a",
            EventType = "event.a"
        };

        var handlerRegistry = new FeishuWebhookHandlerRegistry();
        handlerRegistry.Register(appKey, typeof(TestAppHandler));
        handlerRegistry.Register(appKey, typeof(TestAppHandlerB));

        var handlerAMock = new Mock<IFeishuEventHandler>();
        handlerAMock.SetupGet(x => x.SupportedEventType).Returns("event.a");
        var handlerBMock = new Mock<IFeishuEventHandler>();
        handlerBMock.SetupGet(x => x.SupportedEventType).Returns("event.b");

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(TestAppHandler)))
            .Returns(handlerAMock.Object);
        _serviceProviderMock
            .Setup(x => x.GetService(typeof(TestAppHandlerB)))
            .Returns(handlerBMock.Object);

        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessingAsync(eventData.EventId, appKey, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = false, WasProcessing = false });

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
            _appKeyAccessorMock.Object);

        // Act
        service.SetCurrentAppKey(appKey);
        var result = await service.HandleEventAsync(eventData);

        // Assert：声明 event.a 的处理器恰好 1 次，event.b 处理器未被解析（0 次）
        Assert.True(result.Success);
        handlerAMock.Verify(x => x.HandleAsync(eventData, It.IsAny<CancellationToken>()), Times.Once);
        handlerBMock.Verify(x => x.HandleAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleEventAsync_WithAppSpecificHandlerDeclaredEmpty_ShouldReceiveAllEventTypes()
    {
        // Arrange
        var appKey = "app-001";
        var handlerRegistry = new FeishuWebhookHandlerRegistry();
        handlerRegistry.Register(appKey, typeof(TestAppHandler));

        var handlerMock = new Mock<IFeishuEventHandler>();
        handlerMock.SetupGet(x => x.SupportedEventType).Returns(string.Empty);

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(TestAppHandler)))
            .Returns(handlerMock.Object);

        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessingAsync(It.IsAny<string>(), appKey, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = false, WasProcessing = false });

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
            _appKeyAccessorMock.Object);

        service.SetCurrentAppKey(appKey);

        // Act：两种不同 eventType 均应投递到空声明处理器
        var eventA = new EventData { EventId = "test_event_p12_e1", EventType = "event.a" };
        var eventB = new EventData { EventId = "test_event_p12_e2", EventType = "event.b" };
        var resultA = await service.HandleEventAsync(eventA);
        var resultB = await service.HandleEventAsync(eventB);

        // Assert（向后兼容契约锁定）
        Assert.True(resultA.Success);
        Assert.True(resultB.Success);
        handlerMock.Verify(x => x.HandleAsync(eventA, It.IsAny<CancellationToken>()), Times.Once);
        handlerMock.Verify(x => x.HandleAsync(eventB, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleEventAsync_WhenAllAppHandlersFilteredOut_ShouldSkipDispatch_WithoutRollback()
    {
        // Arrange：应用注册了处理器但声明类型与事件不匹配（过滤后 tasks 为空）
        var appKey = "app-001";
        var eventData = new EventData
        {
            EventId = "test_event_p12_c",
            EventType = "event.a"
        };

        var handlerRegistry = new FeishuWebhookHandlerRegistry();
        handlerRegistry.Register(appKey, typeof(TestAppHandler));

        var handlerMock = new Mock<IFeishuEventHandler>();
        handlerMock.SetupGet(x => x.SupportedEventType).Returns("event.b");

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(TestAppHandler)))
            .Returns(handlerMock.Object);

        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessingAsync(eventData.EventId, appKey, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = false, WasProcessing = false });

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
            _appKeyAccessorMock.Object);

        // Act
        service.SetCurrentAppKey(appKey);
        var result = await service.HandleEventAsync(eventData);

        // Assert：WhenAll 空集正常完成 → Mark completed → 成功；
        // 不落回滚/失败存储路径，也不回落全局工厂（评审决策：静默成功，与 WHF-09 unhandled 口径一致）
        Assert.True(result.Success);
        handlerMock.Verify(x => x.HandleAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()), Times.Never);
        _handlerFactoryMock.Verify(
            x => x.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _deduplicatorMock.Verify(
            x => x.MarkAsCompletedAsync(eventData.EventId, appKey, It.IsAny<CancellationToken>()), Times.Once);
        _deduplicatorMock.Verify(
            x => x.RollbackProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
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
            .Setup(x => x.TryMarkAsProcessingAsync(eventData.EventId, appKey, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = false, WasProcessing = false });

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
            _appKeyAccessorMock.Object);

        // Act
        service.SetCurrentAppKey(appKey);
        var result = await service.HandleEventAsync(eventData);

        // Assert
        Assert.True(result.Success);
        interceptorMock.Verify(x => x.BeforeHandleAsync(eventData.EventType, eventData, It.IsAny<CancellationToken>()), Times.Once);
        interceptorMock.Verify(x => x.AfterHandleAsync(eventData.EventType, eventData, It.IsAny<Exception?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleEventAsync_WithHandlerException_ShouldRollbackDeduplication()
    {
        // Arrange - 事件处理异常时应回滚去重状态
        var eventData = new EventData
        {
            EventId = "rollback_test_event",
            EventType = "test.event"
        };

        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessingAsync(eventData.EventId, It.IsAny<string?>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = false, WasProcessing = false });

        _handlerFactoryMock
            .Setup(x => x.HandleEventParallelAsync(eventData.EventType, eventData, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Handler failed"));

        var service = CreateService();

        // Act
        var result = await service.HandleEventAsync(eventData);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Internal server error", result.ErrorReason ?? "");
        // 验证去重状态被回滚
        _deduplicatorMock.Verify(x => x.RollbackProcessingAsync(eventData.EventId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        // 验证未调用 MarkAsCompletedAsync
        _deduplicatorMock.Verify(x => x.MarkAsCompletedAsync(eventData.EventId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleEventAsync_WithSuccessfulProcessing_ShouldMarkAsCompleted()
    {
        // Arrange - 事件处理成功后应标记为已完成
        var eventData = new EventData
        {
            EventId = "complete_test_event",
            EventType = "test.event"
        };

        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessingAsync(eventData.EventId, It.IsAny<string?>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = false, WasProcessing = false });

        _handlerFactoryMock
            .Setup(x => x.HandleEventParallelAsync(eventData.EventType, eventData, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.HandleEventAsync(eventData);

        // Assert
        Assert.True(result.Success);
        // 验证去重状态被标记为已完成
        _deduplicatorMock.Verify(x => x.MarkAsCompletedAsync(eventData.EventId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        // 验证未调用回滚
        _deduplicatorMock.Verify(x => x.RollbackProcessingAsync(eventData.EventId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleEventAsync_WhenTimeout_ShouldReturnTimeoutResult_AndRollbackOnce()
    {
        // Arrange - WHF-08：超时路径就地收尾（不 rethrow）——仅一次回滚、返回超时结果
        var eventData = new EventData
        {
            EventId = "timeout_test_event",
            EventType = "test.event"
        };

        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessingAsync(eventData.EventId, It.IsAny<string?>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = false, WasProcessing = false });

        _handlerFactoryMock
            .Setup(x => x.HandleEventParallelAsync(eventData.EventType, eventData, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var service = CreateService();

        // Act - 未发生外部取消：工厂抛出的 OCE 属于处理超时
        var result = await service.HandleEventAsync(eventData);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Event handling timeout", result.ErrorReason);
        _deduplicatorMock.Verify(x => x.RollbackProcessingAsync(eventData.EventId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once,
            "超时路径应仅回滚一次（WHF-08 消除双重回滚）");
        _deduplicatorMock.Verify(x => x.MarkAsCompletedAsync(eventData.EventId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleEventAsync_WhenExternallyCancelled_ShouldRollbackAndThrow()
    {
        // Arrange - WHF-08：真取消（外部 token）路径保留原有语义——回滚一次并上抛
        var eventData = new EventData
        {
            EventId = "cancel_test_event",
            EventType = "test.event"
        };

        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessingAsync(eventData.EventId, It.IsAny<string?>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = false, WasProcessing = false });

        var service = CreateService();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.HandleEventAsync(eventData, cts.Token));

        // Assert
        _deduplicatorMock.Verify(x => x.RollbackProcessingAsync(eventData.EventId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _deduplicatorMock.Verify(x => x.MarkAsCompletedAsync(eventData.EventId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #region WHF-07：Mark 失败不得回滚已成功的业务

    [Fact]
    public async Task HandleEventAsync_WhenMarkCompletedFails_ShouldReturnSuccess_AndNotRollback()
    {
        // Arrange - WHF-07 核心断言：业务分发成功后 Mark 失败 → 保留 processing 态、按成功口径返回
        var eventData = new EventData { EventId = "whf07_event", EventType = "test.event" };

        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessingAsync(eventData.EventId, It.IsAny<string?>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = false, WasProcessing = false });

        _handlerFactoryMock
            .Setup(x => x.HandleEventParallelAsync(eventData.EventType, eventData, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _deduplicatorMock
            .Setup(x => x.MarkAsCompletedAsync(eventData.EventId, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FeishuRedisException(FeishuRedisFailureKind.Connection, "Redis 连接失败，完成标记写入失败"));

        var service = CreateService();

        // Act
        var result = await service.HandleEventAsync(eventData);

        // Assert
        Assert.True(result.Success, "业务已成功执行，Mark 失败不得视为失败（回滚将导致重复消费）");
        _deduplicatorMock.Verify(x => x.MarkAsCompletedAsync(eventData.EventId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _deduplicatorMock.Verify(x => x.RollbackProcessingAsync(eventData.EventId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never,
            "WHF-07 核心断言：Mark 失败禁止回滚");
    }

    [Fact]
    public async Task HandleEventAsync_WhenMarkCompletedFailsWithGenericException_ShouldReturnSuccess()
    {
        // Arrange - Mark 失败的语义与异常类型无关（业务已成功）
        var eventData = new EventData { EventId = "whf07_generic_event", EventType = "test.event" };

        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessingAsync(eventData.EventId, It.IsAny<string?>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = false, WasProcessing = false });

        _handlerFactoryMock
            .Setup(x => x.HandleEventParallelAsync(eventData.EventType, eventData, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _deduplicatorMock
            .Setup(x => x.MarkAsCompletedAsync(eventData.EventId, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("去重器内部错误"));

        var service = CreateService();

        // Act
        var result = await service.HandleEventAsync(eventData);

        // Assert
        Assert.True(result.Success);
        _deduplicatorMock.Verify(x => x.RollbackProcessingAsync(eventData.EventId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region WHF-09：未注册 eventType 门控

    [Fact]
    public async Task HandleEventAsync_WhenEventTypeUnregistered_ShouldSkipDispatch_WhenIgnoreUnknownEventTypesEnabled()
    {
        // Arrange - WHF-09：默认开启——未注册事件类型静默忽略，不回退默认处理器兜底
        _options.IgnoreUnknownEventTypes = true;
        _handlerFactoryMock
            .Setup(x => x.IsHandlerRegistered("unknown.event"))
            .Returns(false);

        var eventData = new EventData { EventId = "whf09_event", EventType = "unknown.event" };

        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessingAsync(eventData.EventId, It.IsAny<string?>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = false, WasProcessing = false });

        var service = CreateService();

        // Act
        var result = await service.HandleEventAsync(eventData);

        // Assert
        Assert.True(result.Success, "unhandled 不是业务失败，不应触发回滚/重试");
        _handlerFactoryMock.Verify(x => x.HandleEventParallelAsync(
            It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()), Times.Never,
            "未注册事件类型不应分发到默认处理器兜底");
    }

    [Fact]
    public async Task HandleEventAsync_WhenEventTypeRegistered_ShouldDispatch_WhenIgnoreUnknownEventTypesEnabled()
    {
        // Arrange - 已注册事件类型正常分发
        _options.IgnoreUnknownEventTypes = true;
        _handlerFactoryMock
            .Setup(x => x.IsHandlerRegistered("test.event"))
            .Returns(true);

        var eventData = new EventData { EventId = "whf09_registered_event", EventType = "test.event" };

        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessingAsync(eventData.EventId, It.IsAny<string?>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = false, WasProcessing = false });

        _handlerFactoryMock
            .Setup(x => x.HandleEventParallelAsync(eventData.EventType, eventData, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.HandleEventAsync(eventData);

        // Assert
        Assert.True(result.Success);
        _handlerFactoryMock.Verify(x => x.HandleEventParallelAsync(
            eventData.EventType, eventData, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleEventAsync_WhenEventTypeUnregistered_ShouldFallbackToFactory_WhenIgnoreUnknownEventTypesDisabled()
    {
        // Arrange - 开关关闭时保留旧行为（默认处理器兜底）
        _options.IgnoreUnknownEventTypes = false;
        _handlerFactoryMock
            .Setup(x => x.IsHandlerRegistered("unknown.event"))
            .Returns(false);

        var eventData = new EventData { EventId = "whf09_fallback_event", EventType = "unknown.event" };

        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessingAsync(eventData.EventId, It.IsAny<string?>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = false, WasProcessing = false });

        _handlerFactoryMock
            .Setup(x => x.HandleEventParallelAsync(eventData.EventType, eventData, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.HandleEventAsync(eventData);

        // Assert
        Assert.True(result.Success);
        _handlerFactoryMock.Verify(x => x.HandleEventParallelAsync(
            eventData.EventType, eventData, It.IsAny<CancellationToken>()), Times.Once, "开关关闭时应回退工厂兜底");
    }

    #endregion

    #region WHF-02：Server 类 FeishuRedisException 三层上抛

    [Fact]
    public async Task HandleEventAsync_ShouldRethrowServerException_WhenSignatureValidationThrowsServerException()
    {
        // Arrange - 验签路径：组合验证器上抛的 Server 类异常不得在服务层被吞成 false（403）
        _options.Apps["app1"] = new FeishuAppWebhookOptions
        {
            AppKey = "app1",
            VerificationToken = "token_1",
            EncryptKey = "12345678901234567890123456789012"
        };
        _validatorMock
        .Setup(x => x.ValidateHeaderSignatureAsync(
        It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new FeishuRedisException(FeishuRedisFailureKind.Server, "Lua 脚本执行失败"));

        var service = CreateService();
        service.SetCurrentAppKey("app1");
        var request = new FeishuWebhookRequest { Timestamp = 1, Nonce = "n", Signature = "s", Encrypt = "e" };

        // Act
        var act = async () => await service.HandleEventAsync(request, "{}");

        // Assert
        await act.Should().ThrowAsync<FeishuRedisException>().Where(ex => ex.FailureKind == FeishuRedisFailureKind.Server);
    }

    [Fact]
    public async Task HandleEventAsync_ShouldRethrowServerException_WhenDeduplicationThrowsServerException()
    {
        // Arrange - 事件处理路径：去重阶段的 Server 类异常必须上抛（由中间件转 503）
        var eventData = new EventData { EventId = "whf02_event", EventType = "test.event" };
        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessingAsync(eventData.EventId, It.IsAny<string?>(), null, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FeishuRedisException(FeishuRedisFailureKind.Server, "Redis 服务端配置错误"));

        var service = CreateService();

        // Act
        var act = async () => await service.HandleEventAsync(eventData);

        // Assert
        await act.Should().ThrowAsync<FeishuRedisException>().Where(ex => ex.FailureKind == FeishuRedisFailureKind.Server);
        // 关键次生断言：Server 类故障不写失败事件存储（避免重试服务后续重复消费）
        _deduplicatorMock.Verify(x => x.RollbackProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleEventAsync_ShouldNotWriteFailedEventStore_WhenDeduplicationThrowsServerException()
    {
        // Arrange - WHF-02 核心次生断言：失败事件存储不得在 Server 类故障路径被写入
        var eventData = new EventData { EventId = "whf02_store_event", EventType = "test.event" };
        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessingAsync(eventData.EventId, It.IsAny<string?>(), null, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FeishuRedisException(FeishuRedisFailureKind.Server, "Redis 服务端配置错误"));

        var failedEventStoreMock = new Mock<IFailedEventStore>();
        _options.Retry.EnableRetry = true;
        var service = CreateService(failedEventStoreMock.Object);

        // Act
        var act = async () => await service.HandleEventAsync(eventData);

        // Assert
        await act.Should().ThrowAsync<FeishuRedisException>();
        failedEventStoreMock.Verify(x => x.StoreFailedEventAsync(
            It.IsAny<EventData>(), It.IsAny<Exception>(), It.IsAny<string?>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleEventAsync_WhenBusinessHandlerThrowsRedisServerException_ShouldRollbackAndWriteFailedStore_AndReturn500()
    {
        // Arrange - P1-3 / 决策 C 核心回归：
        // 业务处理器内部因自身使用 Redis 而抛出的 Server 类 FeishuRedisException 属于"业务失败"，
        // 必须走通用失败路径（回滚 + ADR-2 失败存储 + 500），不得被 WHF-02 过滤器（仅认去重调用点包装的
        // FeishuDeduplicationFatalException）误判为"去重体系致命故障"（不回滚、不写存储、上抛 503）。
        var eventData = new EventData { EventId = "p1_3_business_redis", EventType = "test.event" };

        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessingAsync(eventData.EventId, It.IsAny<string?>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = false, WasProcessing = false });

        _handlerFactoryMock
            .Setup(x => x.HandleEventParallelAsync(eventData.EventType, eventData, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FeishuRedisException(FeishuRedisFailureKind.Server, "业务处理器内部的 Redis Lua 脚本失败"));

        var failedEventStoreMock = new Mock<IFailedEventStore>();
        _options.Retry.EnableRetry = true;
        var service = CreateService(failedEventStoreMock.Object);

        // Act
        var result = await service.HandleEventAsync(eventData);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Internal server error", result.ErrorReason);

        _deduplicatorMock.Verify(
            x => x.RollbackProcessingAsync(eventData.EventId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once,
            "业务侧 Redis 故障应按业务失败回滚去重");
        failedEventStoreMock.Verify(
            x => x.StoreFailedEventAsync(It.IsAny<EventData>(), It.IsAny<Exception>(), It.IsAny<string?>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once, "业务侧 Redis 故障应写入 ADR-2 失败存储以支持重试");
    }

    [Fact]
    public async Task HandleEventAsync_WhenExternallyCancelled_ShouldPassCanceledMarkerToAfterHandle()
    {
        // Arrange - P1-7/决策 D：AfterHandleAsync 需要可判别的"取消"终态。
        // 接口契约 null=成功，原始 OCE 无法表达类别，故以 EventHandlingOutcomeException("canceled") 传递；
        // 对外仍按 OCE 传播（取消语义不得被替换）。
        var eventData = new EventData { EventId = "cancel_marker_event", EventType = "test.event" };

        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessingAsync(eventData.EventId, It.IsAny<string?>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = false, WasProcessing = false });

        var interceptorMock = new Mock<IFeishuEventInterceptor>();
        Exception? captured = null;
        interceptorMock
            .Setup(x => x.BeforeHandleAsync(eventData.EventType, eventData, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        interceptorMock
            .Setup(x => x.AfterHandleAsync(eventData.EventType, eventData, It.IsAny<Exception?>(), It.IsAny<CancellationToken>()))
            .Callback<string, EventData, Exception?, CancellationToken>((_, _, ex, _) => captured = ex)
            .Returns(Task.CompletedTask);

        var service = CreateService(interceptors: new[] { interceptorMock.Object });
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.HandleEventAsync(eventData, cts.Token));

        // Assert
        captured.Should().BeOfType<Mud.Feishu.Abstractions.EventHandlers.EventHandlingOutcomeException>(
            "取消终态必须以致命/终态标记异常传给后置拦截器");
        ((Mud.Feishu.Abstractions.EventHandlers.EventHandlingOutcomeException)captured!).OutcomeKind.Should().Be("canceled");
        _deduplicatorMock.Verify(
            x => x.RollbackProcessingAsync(eventData.EventId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once,
            "取消路径必须回滚去重状态（且仅回滚一次）");
    }

    [Fact]
    public async Task HandleEventAsync_WhenInterceptorBlocks_ShouldPassInterceptedMarkerToAfterHandle()
    {
        // Arrange - P1-7：拦截终态标记（决策 D）
        var eventData = new EventData { EventId = "intercepted_marker_event", EventType = "test.event" };

        var interceptorMock = new Mock<IFeishuEventInterceptor>();
        interceptorMock
            .Setup(x => x.BeforeHandleAsync(eventData.EventType, eventData, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Exception? captured = null;
        interceptorMock
            .Setup(x => x.AfterHandleAsync(eventData.EventType, eventData, It.IsAny<Exception?>(), It.IsAny<CancellationToken>()))
            .Callback<string, EventData, Exception?, CancellationToken>((_, _, ex, _) => captured = ex)
            .Returns(Task.CompletedTask);

        var service = CreateService(interceptors: new[] { interceptorMock.Object });

        // Act
        var result = await service.HandleEventAsync(eventData);

        // Assert：R3-P0-2/D2——拦截 = **已消费**（200 ack），并补落去重标记。
        // 此前返回 (false, "Event intercepted") → 中间件一律 500 → 飞书重推 → 再次拦截，
        // 事件永不 ack 且不落任何记录（失败存储仅在业务 catch 分支写入），形成重推风暴。
        Assert.True(result.Success);
        Assert.Null(result.ErrorReason);

        // 拦截发生在去重之前，因此必须**补落**去重标记，否则重推会再次进入拦截分支。
        _deduplicatorMock.Verify(
            x => x.TryMarkAsProcessingAsync(eventData.EventId!, It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Once, "拦截后必须占位去重（内存实现对不存在的键 MarkAsCompleted 是静默 no-op）");
        _deduplicatorMock.Verify(
            x => x.MarkAsCompletedAsync(eventData.EventId!, It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once, "拦截后必须置完成，使飞书重推被 dedup_hit 跳过");

        // P1-7 契约不变：AfterHandle 仍可判别拦截终态
        captured.Should().BeOfType<Mud.Feishu.Abstractions.EventHandlers.EventHandlingOutcomeException>();
        ((Mud.Feishu.Abstractions.EventHandlers.EventHandlingOutcomeException)captured!).OutcomeKind.Should().Be("intercepted");
    }

    [Fact]
    public async Task HandleEventAsync_WhenInterceptorBlocksTwice_ShouldSkipSecondByDedup()
    {
        // Arrange - R3-P0-2 回归锁：同 EventId 第二次调用必须命中去重，拦截器不再被调用
        var eventData = new EventData { EventId = "intercepted_twice_event", EventType = "test.event" };

        var interceptorMock = new Mock<IFeishuEventInterceptor>();
        interceptorMock
            .Setup(x => x.BeforeHandleAsync(eventData.EventType, eventData, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // 第二次调用时去重器应报告“已完成”（命中）
        _deduplicatorMock
            .SetupSequence(x => x.TryMarkAsProcessingAsync(eventData.EventId!, It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DeduplicationResult.Success(eventData.EventId!))
            .ReturnsAsync(DeduplicationResult.Duplicate(eventData.EventId!));

        var service = CreateService(interceptors: new[] { interceptorMock.Object });

        // Act
        var first = await service.HandleEventAsync(eventData);
        var second = await service.HandleEventAsync(eventData);

        // Assert：两次都成功 ack（D2 核心回归）。
        // 注：拦截器**先于**去重执行是既有设计（拦截器是横切组件，可对未去重事件做准入判断），
        // 因此第二次仍会调用 BeforeHandleAsync——本用例锁的是「不再返回 500」：
        // 修好 HTTP 语义后飞书不会重推，重复投递只会在 dedup_hit 处被安静吸收。
        Assert.True(first.Success);
        Assert.True(second.Success, "第二次必须按 dedup_hit 成功 ack，不得再走拦截 → 500 路径");

        _deduplicatorMock.Verify(
            x => x.TryMarkAsProcessingAsync(eventData.EventId!, It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2), "两次调用都应尝试去重（第二次命中 Duplicate → 跳过处理）");
    }

    [Fact]
    public async Task HandleEventAsync_WhenInterceptorBlocksAndMarkFails_ShouldStillReturnSuccess()
    {
        // Arrange - WHF-07 同口径：去重标记失败不得改变“已消费”结论
        var eventData = new EventData { EventId = "intercepted_mark_failed", EventType = "test.event" };

        var interceptorMock = new Mock<IFeishuEventInterceptor>();
        interceptorMock
            .Setup(x => x.BeforeHandleAsync(eventData.EventType, eventData, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("标记失败"));

        var service = CreateService(interceptors: new[] { interceptorMock.Object });

        // Act
        var result = await service.HandleEventAsync(eventData);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorReason);
    }

    [Fact]
    public async Task HandleEventAsync_WhenInterceptorBlocksAndAckModeRetryable_ShouldReturnRetryableReason()
    {
        // Arrange - R3-FEAT-2：InterceptionAckMode=Retryable → 不落去重、要求重推（中间件映射 503）
        var eventData = new EventData { EventId = "intercepted_retryable_event", EventType = "test.event" };

        var interceptorMock = new Mock<IFeishuEventInterceptor>();
        interceptorMock
            .Setup(x => x.BeforeHandleAsync(eventData.EventType, eventData, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = CreateService(
            interceptors: new[] { interceptorMock.Object },
            configureOptions: options => options.InterceptionAckMode = InterceptionAckMode.Retryable);

        // Act
        var result = await service.HandleEventAsync(eventData);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(FeishuWebhookService.InterceptedRetryableReason, result.ErrorReason);
        _deduplicatorMock.Verify(
            x => x.TryMarkAsProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Never, "Retryable 语义下不得落去重——本事件尚未被消费，重推后必须能再次进入处理流程");
    }

    [Fact]
    public async Task HandleEventAsync_WhenMultipleAppHandlersFail_ShouldLogAllExceptions_AndRollbackOnce()
    {
        // Arrange - M3-5/P2-6：应用专属分支的逐处理器失败必须全部可见（不再只看到第一个 InnerException）
        var eventData = new EventData { EventId = "multi_app_fail", EventType = "test.event" };
        _appKeyAccessorMock.Setup(x => x.CurrentAppKey).Returns("app-fail");

        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessingAsync(eventData.EventId, "app-fail", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = false, WasProcessing = false });

        var registry = new FeishuWebhookHandlerRegistry();
        registry.Register("app-fail", typeof(FailingAppHandlerA));
        registry.Register("app-fail", typeof(FailingAppHandlerB));

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(FailingAppHandlerA)))
            .Returns(new FailingAppHandlerA());
        _serviceProviderMock
            .Setup(x => x.GetService(typeof(FailingAppHandlerB)))
            .Returns(new FailingAppHandlerB());

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
            registry,
            new FeishuWebhookInterceptorRegistry(),
            _serviceProviderMock.Object,
            _appKeyAccessorMock.Object,
            null);

        // Act
        var result = await service.HandleEventAsync(eventData);

        // Assert
        Assert.False(result.Success);
        _deduplicatorMock.Verify(
            x => x.RollbackProcessingAsync(eventData.EventId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once,
            "整体回滚一次，at-least-once 语义不变");
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("FailingAppHandler")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(2), "两个失败处理器都必须被逐条记录");
    }

    #endregion

    #region R3-P1-1：未匹配 SupportedEventType 不得静默丢失

    [Fact]
    public async Task HandleEventAsync_AppSpecificHandlersAllSkipped_ShouldRecordUnhandledMetricAndLogWarning()
    {
        // Arrange - R3-P1-1：应用专属路径此前只有 LogDebug，生产 Information 级不可见且无指标
        var appKey = "app-001";
        var eventData = new EventData
        {
            EventId = "evt_all_skipped",
            EventType = "event.a"
        };

        var handlerRegistry = new FeishuWebhookHandlerRegistry();
        handlerRegistry.Register(appKey, typeof(TestAppHandler));

        var handlerMock = new Mock<IFeishuEventHandler>();
        handlerMock.SetupGet(x => x.SupportedEventType).Returns("event.b");   // 不匹配 → 跳过

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(TestAppHandler)))
            .Returns(handlerMock.Object);

        var service = CreateServiceWithHandlerRegistry(handlerRegistry);

        // Act
        service.SetCurrentAppKey(appKey);
        var result = await service.HandleEventAsync(eventData);

        // Assert：事件仍按成功 ack（unhandled 不是失败），但必须留下 Warning
        Assert.True(result.Success);
        handlerMock.Verify(x => x.HandleAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()), Times.Never);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("均不匹配事件类型")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once, "全部处理器跳过时必须 Warning——否则拼错 SupportedEventType 的事件会静默消失");
    }

    [Fact]
    public async Task HandleEventAsync_MisspelledSupportedEventType_ShouldNotBeSilent()
    {
        // Arrange - 用户把 "im.message.receive_v1" 拼成 "im.message.recieve_v1" 的典型场景
        var appKey = "app-001";
        var eventData = new EventData
        {
            EventId = "evt_misspelled",
            EventType = "im.message.receive_v1"
        };

        var handlerRegistry = new FeishuWebhookHandlerRegistry();
        handlerRegistry.Register(appKey, typeof(TestAppHandler));

        var handlerMock = new Mock<IFeishuEventHandler>();
        handlerMock.SetupGet(x => x.SupportedEventType).Returns("im.message.recieve_v1");   // 拼写错误

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(TestAppHandler)))
            .Returns(handlerMock.Object);

        var service = CreateServiceWithHandlerRegistry(handlerRegistry);

        // Act
        service.SetCurrentAppKey(appKey);
        var result = await service.HandleEventAsync(eventData);

        // Assert：不得静默——必须有 Warning 提示核对 SupportedEventType
        Assert.True(result.Success);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("SupportedEventType")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once, "拼写错误的 SupportedEventType 必须在生产默认日志级别可见");
    }

    [Fact]
    public async Task HandleEventAsync_GlobalBranchUnregisteredType_ShouldLogWarningNotDebug()
    {
        // Arrange - 全局门控分支：LogDebug 提升为 Warning（生产 Information 级看不到 Debug）
        var eventData = new EventData
        {
            EventId = "evt_unregistered_global",
            EventType = "some.unregistered.type"
        };
        _options.IgnoreUnknownEventTypes = true;   // 构造函数默认为 false，本用例需走门控分支
        _handlerFactoryMock.Setup(x => x.IsHandlerRegistered("some.unregistered.type")).Returns(false);

        var service = CreateServiceWithHandlerRegistry(new FeishuWebhookHandlerRegistry());

        // Act
        var result = await service.HandleEventAsync(eventData);

        // Assert
        Assert.True(result.Success);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("未注册处理器")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region R3-P1-3：软超时可观测（timeout_overshoot）+ D4 回归保护

    [Fact]
    public async Task Timeout_WhenHandlerIgnoresCancellation_ShouldStillAwaitAllHandlers()
    {
        // Arrange - **D4 回归保护**：软超时不得演变成硬超时。
        // 处理器忽略取消令牌、直到自身完成才返回；断言它**确实被执行完**（无孤儿任务、无双执行）。
        var eventData = new EventData
        {
            EventId = "evt_ignore_cancel",
            EventType = "test.event"
        };

        var completed = new TaskCompletionSource();
        _handlerFactoryMock
            .Setup(x => x.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .Returns(async Task () =>
            {
                await Task.Delay(50);          // 不响应取消令牌
                completed.TrySetResult();
            });

        _options.EventHandlingTimeoutMs = 1000;
        var service = CreateServiceWithHandlerRegistry(new FeishuWebhookHandlerRegistry());

        // Act
        var result = await service.HandleEventAsync(eventData);

        // Assert：处理器跑完了（await WhenAll 语义未变）
        completed.Task.IsCompleted.Should().BeTrue("软超时不得中断处理器——D4 禁止‘状态已释放而任务仍在跑’");
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Timeout_WhenElapsedFarExceedsTimeout_ShouldRecordOvershootWarning()
    {
        // Arrange - R3-P1-3：实际耗时远超软超时 → 必须可见（否则"超时"形同虚设却无人知晓）
        var eventData = new EventData
        {
            EventId = "evt_overshoot",
            EventType = "test.event"
        };

        // 软超时 1000ms；处理器忽略令牌再跑 1600ms → elapsed > 1000 * 1.5
        _options.EventHandlingTimeoutMs = 1000;
        _handlerFactoryMock
            .Setup(x => x.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .Returns(async Task () =>
            {
                await Task.Delay(1600);
                throw new OperationCanceledException();
            });

        var service = CreateServiceWithHandlerRegistry(new FeishuWebhookHandlerRegistry());

        // Act
        var result = await service.HandleEventAsync(eventData);

        // Assert
        Assert.False(result.Success, "超时仍按失败口径返回（供飞书重推）");
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("显著超过软超时")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once, "实际耗时远超软超时时必须告警（软超时无法强制中断处理器）");
    }

    #endregion

    /// <summary>
    /// 用指定的处理器注册表构造被测服务（应用专属分支用例需要自定义 registry）。
    /// </summary>
    private FeishuWebhookService CreateServiceWithHandlerRegistry(FeishuWebhookHandlerRegistry handlerRegistry)
    {
        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(_options);

        // 去重占位：Moq 默认返回 null DeduplicationResult，会让 CheckDeduplicationAsync 空引用
        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, string? _, TimeSpan? __, TimeSpan? ___, CancellationToken ____)
                => DeduplicationResult.Success(id));

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
            handlerRegistry,
            new FeishuWebhookInterceptorRegistry(),
            _serviceProviderMock.Object,
            _appKeyAccessorMock.Object);
    }

    private class FailingAppHandlerA : IFeishuEventHandler
    {
        public string SupportedEventType => "test.event";
        public Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("A failed");
    }

    private class FailingAppHandlerB : IFeishuEventHandler
    {
        public string SupportedEventType => "test.event";
        public Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("B failed");
    }

    private class TestAppHandler : IFeishuEventHandler
    {
        public string SupportedEventType => "test.event";
        public Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>P1-2 测试用第二应用处理器类型（占位声明类型，实际行为由 Mock 提供）</summary>
    private class TestAppHandlerB : IFeishuEventHandler
    {
        public string SupportedEventType => "event.b";
        public Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private class TestAppInterceptor : IFeishuEventInterceptor
    {
        public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
