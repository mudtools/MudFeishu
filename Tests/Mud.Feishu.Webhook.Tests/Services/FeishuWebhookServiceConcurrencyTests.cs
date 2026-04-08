// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Models;
using Mud.Feishu.Webhook;
using Moq;
using Mud.Feishu.Abstractions.Services;

namespace Mud.Feishu.Webhook.Tests.Services;

/// <summary>
/// FeishuWebhookService 多线程并发测试
/// </summary>
public class FeishuWebhookServiceConcurrencyTests
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

    public FeishuWebhookServiceConcurrencyTests()
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
            MaxConcurrentEvents = 10,
            Apps = new Dictionary<string, FeishuAppWebhookOptions>
            {
                ["app1"] = new FeishuAppWebhookOptions
                {
                    AppKey = "app1",
                    VerificationToken = "token1",
                    EncryptKey = "encrypt_key_32_bytes_for_app1___"
                },
                ["app2"] = new FeishuAppWebhookOptions
                {
                    AppKey = "app2",
                    VerificationToken = "token2",
                    EncryptKey = "encrypt_key_32_bytes_for_app2___"
                },
                ["app3"] = new FeishuAppWebhookOptions
                {
                    AppKey = "app3",
                    VerificationToken = "token3",
                    EncryptKey = "encrypt_key_32_bytes_for_app3___"
                }
            }
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

        // 默认去重返回未处理
        _deduplicatorMock
            .Setup(x => x.TryMarkAsProcessing(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(false);

        // 默认处理成功
        _handlerFactoryMock
            .Setup(x => x.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // 默认验证通过
        _validatorMock
            .Setup(x => x.ValidateSubscriptionRequestAsync(It.IsAny<EventVerificationRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // 设置加密密钥提供程序
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

    private FeishuWebhookService CreateServiceWithRealAppKeyAccessor(IWebhookAppKeyAccessor appKeyAccessor)
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
            appKeyAccessor,
            null,
            null);
    }

    [Fact]
    public async Task HandleEventAsync_ConcurrentDifferentApps_ShouldProcessCorrectly()
    {
        // Arrange
        var service = CreateService();
        var tasks = new List<Task<(bool Success, string? ErrorReason)>>();
        var appKeys = new[] { "app1", "app2", "app3" };

        // 创建并发任务，每个任务使用不同的 AppKey
        for (int i = 0; i < 30; i++)
        {
            var appKey = appKeys[i % appKeys.Length];
            var eventId = $"event_{appKey}_{i}";

            var task = Task.Run(async () =>
            {
                service.SetCurrentAppKey(appKey);

                var eventData = new EventData
                {
                    EventId = eventId,
                    EventType = $"test.event.{appKey}",
                    CreateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                return await service.HandleEventAsync(eventData);
            });

            tasks.Add(task);
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, result => Assert.True(result.Success));
        _handlerFactoryMock.Verify(x => x.HandleEventParallelAsync(
            It.IsAny<string>(),
            It.IsAny<EventData>(),
            It.IsAny<CancellationToken>()), Times.Exactly(30));
    }

    [Fact]
    public async Task HandleEventAsync_ConcurrentSameApp_ShouldProcessCorrectly()
    {
        // Arrange
        var service = CreateService();
        service.SetCurrentAppKey("app1");

        var tasks = new List<Task<(bool Success, string? ErrorReason)>>();

        // 创建并发任务，所有任务使用相同的 AppKey
        for (int i = 0; i < 20; i++)
        {
            var eventId = $"event_app1_{i}";

            var task = Task.Run(async () =>
            {
                var eventData = new EventData
                {
                    EventId = eventId,
                    EventType = "test.event",
                    CreateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                return await service.HandleEventAsync(eventData);
            });

            tasks.Add(task);
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, result => Assert.True(result.Success));
        _handlerFactoryMock.Verify(x => x.HandleEventParallelAsync(
            It.IsAny<string>(),
            It.IsAny<EventData>(),
            It.IsAny<CancellationToken>()), Times.Exactly(20));
    }

    [Fact]
    public async Task VerifyEventSubscriptionAsync_ConcurrentDifferentApps_ShouldVerifyCorrectly()
    {
        // Arrange
        var service = CreateService();
        var tasks = new List<Task<EventVerificationResponse?>>();
        var appKeys = new[] { "app1", "app2", "app3" };

        // 创建并发任务
        for (int i = 0; i < 30; i++)
        {
            var appKey = appKeys[i % appKeys.Length];
            var task = Task.Run(async () =>
            {
                service.SetCurrentAppKey(appKey);

                var request = new EventVerificationRequest
                {
                    Type = "url_verification",
                    Token = $"token{i % 3 + 1}", // token1, token2, token3
                    Challenge = $"challenge_{i}"
                };

                return await service.VerifyEventSubscriptionAsync(request);
            });

            tasks.Add(task);
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, result => Assert.NotNull(result));
    }

    [Fact]
    public async Task DecryptEventAsync_ConcurrentDifferentApps_ShouldDecryptCorrectly()
    {
        // Arrange
        var service = CreateService();
        var tasks = new List<Task<EventData?>>();
        var appKeys = new[] { "app1", "app2", "app3" };

        // 设置解密器
        for (int i = 0; i < 30; i++)
        {
            var index = i;
            _decryptorMock
                .Setup(x => x.DecryptAsync($"encrypted_{index}", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new EventData
                {
                    EventId = $"event_{index}",
                    EventType = "test.event",
                    CreateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
        }

        // 创建并发任务
        for (int i = 0; i < 30; i++)
        {
            var index = i; // 捕获循环变量的当前值
            var appKey = appKeys[index % appKeys.Length];
            var task = Task.Run(async () =>
            {
                service.SetCurrentAppKey(appKey);
                return await service.DecryptEventAsync($"encrypted_{index}");
            });

            tasks.Add(task);
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, result => Assert.NotNull(result));
    }

    [Fact]
    public async Task ValidateRequestSignature_ConcurrentDifferentApps_ShouldValidateCorrectly()
    {
        // Arrange
        var service = CreateService();
        var tasks = new List<Task<bool>>();
        var appKeys = new[] { "app1", "app2", "app3" };

        // 设置验证器
        _validatorMock
            .Setup(x => x.ValidateSignatureAsync(
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(true);

        // 创建并发任务
        for (int i = 0; i < 30; i++)
        {
            var appKey = appKeys[i % appKeys.Length];
            var task = Task.Run(async () =>
            {
                service.SetCurrentAppKey(appKey);

                var request = new FeishuWebhookRequest
                {
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Nonce = $"nonce_{i}",
                    Encrypt = $"encrypt_{i}",
                    Signature = $"signature_{i}"
                };

                return await service.ValidateRequestSignature(request);
            });

            tasks.Add(task);
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, result => Assert.True(result));
    }

    [Fact]
    public async Task HandleEventAsync_WithEventData_ConcurrentDifferentApps_ShouldProcessCorrectly()
    {
        // Arrange
        var service = CreateService();
        var tasks = new List<Task<(bool Success, string? ErrorReason)>>();
        var appKeys = new[] { "app1", "app2", "app3" };

        // 创建并发任务
        for (int i = 0; i < 30; i++)
        {
            var appKey = appKeys[i % appKeys.Length];

            var task = Task.Run(async () =>
            {
                service.SetCurrentAppKey(appKey);

                var eventData = new EventData
                {
                    EventId = $"event_{i}",
                    EventType = "test.event",
                    CreateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                return await service.HandleEventAsync(eventData);
            });

            tasks.Add(task);
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, result => Assert.True(result.Success));
        _handlerFactoryMock.Verify(x => x.HandleEventParallelAsync(
            It.IsAny<string>(),
            It.IsAny<EventData>(),
            It.IsAny<CancellationToken>()), Times.Exactly(30));
    }

    [Fact]
    public async Task HandleEventAsync_WithFeishuWebhookRequest_ConcurrentDifferentApps_ShouldValidateSignatureCorrectly()
    {
        // Arrange - 使用真实的 AsyncLocal 实现
        var appKeyAccessor = new TestWebhookAppKeyAccessor();
        
        // 设置验证器 mock 返回 true（委托给验证器进行签名验证）
        _validatorMock
            .Setup(x => x.ValidateHeaderSignatureAsync(
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string>()))
            .ReturnsAsync(true);
        
        var service = CreateServiceWithRealAppKeyAccessor(appKeyAccessor);
        var tasks = new List<Task<bool>>();
        var appKeys = new[] { "app1", "app2", "app3" };
        var encryptKeys = new Dictionary<string, string>
        {
            ["app1"] = "encrypt_key_32_bytes_for_app1___",
            ["app2"] = "encrypt_key_32_bytes_for_app2___",
            ["app3"] = "encrypt_key_32_bytes_for_app3___"
        };

        // 创建并发任务
        for (int i = 0; i < 30; i++)
        {
            var appKey = appKeys[i % appKeys.Length];
            var encryptKey = encryptKeys[appKey];
            var nonce = $"nonce_{i}";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var body = $"{{\"encrypt\":\"encrypted_{i}\"}}";

            // 计算正确的签名
            var signString = $"{timestamp}{nonce}{encryptKey}{body}";
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signString));
            var computedSignature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            var task = Task.Run(async () =>
            {
                appKeyAccessor.SetAppKey(appKey);

                var request = new FeishuWebhookRequest
                {
                    Timestamp = timestamp,
                    Nonce = nonce,
                    Encrypt = $"encrypted_{i}",
                    Signature = computedSignature
                };

                return await service.HandleEventAsync(request, body);
            });

            tasks.Add(task);
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, result => Assert.True(result));
    }
}
