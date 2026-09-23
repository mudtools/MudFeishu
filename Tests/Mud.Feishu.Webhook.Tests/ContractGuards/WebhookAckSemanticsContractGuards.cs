// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Models;
using Mud.Feishu.Webhook.Services;

namespace Mud.Feishu.Webhook.Tests.ContractGuards;

/// <summary>
/// R3-FEAT-1：把 §4.2 的「ACK / HTTP 决策表」从文档变成**可执行断言**。
/// </summary>
/// <remarks>
/// <para>
/// 为什么需要它：Webhook 的"响应码"就是**对飞书的 ACK 语义**——200 = 已消费（不再重推）、
/// 4xx = 终态丢弃（飞书不重推）、5xx = 请重推。三者一旦错配，轻则事件永久丢失，
/// 重则形成重推风暴。这些语义此前只存在于文档里，任何一次重构都可能悄悄改坏
/// （历史实例：拦截事件一律 500 → 飞书无限重推 → 事件永不 ack，见 R3-P0-2）。
/// </para>
/// <para>
/// 设计原则：**用真实中间件 + 替身服务**驱动，而不是断言源码字符串。
/// 只有走完 <see cref="FeishuMultiAppMiddleware"/> 的真实分派与 catch 链，
/// 才能证明"某场景最终映射到某个状态码"这一契约。
/// </para>
/// </remarks>
public class WebhookAckSemanticsContractGuards
{
    private readonly Mock<RequestDelegate> _nextMock = new();
    private readonly Mock<ILogger<FeishuMultiAppMiddleware>> _middlewareLoggerMock = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<IServiceScope> _scopeMock = new();
    private readonly Mock<IFeishuWebhookService> _webhookServiceMock = new();
    private readonly FeishuWebhookHandlerRegistry _handlerRegistry = new();
    private readonly FeishuWebhookOptions _options;

    public WebhookAckSemanticsContractGuards()
    {
        _options = new FeishuWebhookOptions
        {
            GlobalRoutePrefix = "feishu",
            Apps = new Dictionary<string, FeishuAppWebhookOptions>
            {
                ["app1"] = new FeishuAppWebhookOptions
                {
                    VerificationToken = "test_token_1",
                    EncryptKey = "0123456789abcdef0123456789abcdef"
                }
            }
        };

        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(_scopeMock.Object);
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(x => x.GetService(typeof(IFeishuWebhookService)))
            .Returns(_webhookServiceMock.Object);
        _scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);

        // 默认：验签通过 + 解密出合法事件
        _webhookServiceMock
            .Setup(x => x.HandleEventAsync(It.IsAny<FeishuWebhookRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _webhookServiceMock
            .Setup(x => x.DecryptEventAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, CancellationToken _) => new EventData
            {
                EventId = "evt_decision_table",
                EventType = "test.event"
            });
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
            _middlewareLoggerMock.Object,
            optionsMonitorMock.Object,
            _handlerRegistry);
    }

    private static HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/feishu/app1";
        context.Request.Method = "POST";
        context.Request.ContentType = "application/json";
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");

        var bytes = Encoding.UTF8.GetBytes("{\"encrypt\":\"ENCRYPTED_PAYLOAD\"}");
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;
        context.Response.Body = new MemoryStream();
        return context;
    }

    /// <summary>
    /// 决策表的一行：场景名 → 期望 HTTP 状态码。
    /// </summary>
    /// <remarks>
    /// 状态码语义（对飞书的 ACK）：
    /// 200 = 已消费（停止重推）；4xx = 终态拒绝（飞书不重推，事件丢弃）；
    /// 503 = 可恢复的基础设施故障（飞书按重推策略重投）；500 = 业务失败（写失败存储后重投）。
    /// </remarks>
    public static TheoryData<string, int> DecisionTable()
    {
        return new TheoryData<string, int>
        {
            { "SignatureFailure", 403 },   // 验签失败：终态，不重推
            { "DecryptionFailure", 400 },  // 解密失败：终态，不重推
            { "InterceptedAck", 200 },     // R3-P0-2/D2：拦截 = 已消费
            { "InterceptedRetryable", 503 }, // R3-FEAT-2：拦截 = 要求重推
            { "BusinessFailure", 500 },    // 业务异常：写失败存储 + 重投
            { "NonceInfraFailure", 503 },  // R3-P0-3：不得伪装成 403
            { "DecryptionTimeout", 503 },  // R3-P2-2：解密超时 = 可恢复，不得是 400
        };
    }

    [Theory]
    [MemberData(nameof(DecisionTable))]
    public async Task HttpStatus_ShouldMatchDecisionTable(string scenario, int expectedStatus)
    {
        // Arrange
        switch (scenario)
        {
            case "SignatureFailure":
                _webhookServiceMock
                    .Setup(x => x.HandleEventAsync(It.IsAny<FeishuWebhookRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);
                break;

            case "DecryptionFailure":
                _webhookServiceMock
                    .Setup(x => x.DecryptEventAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((EventData?)null);
                break;

            case "InterceptedAck":
                // R3-P0-2：服务返回 (true, null) —— 中间件不再有 !Success → 500 的分支
                _webhookServiceMock
                    .Setup(x => x.HandleEventAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((true, null));
                break;

            case "InterceptedRetryable":
                _webhookServiceMock
                    .Setup(x => x.HandleEventAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((false, FeishuWebhookService.InterceptedRetryableReason));
                break;

            case "BusinessFailure":
                _webhookServiceMock
                    .Setup(x => x.HandleEventAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((false, "Business handler threw"));
                break;

            case "NonceInfraFailure":
                // R3-P0-3：基础设施故障必须穿透为 503，不得被吞成 403/500
                _webhookServiceMock
                    .Setup(x => x.HandleEventAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new FeishuDeduplicationFatalException("Nonce 去重服务不可用",
                        new FeishuRedisException(FeishuRedisFailureKind.Connection, "redis down")));
                break;

            case "DecryptionTimeout":
                // R3-P2-2：解密超时抛 OCE（非客户端断开）→ 503。
                // 修复前：被 DecryptEventAsync 的 catch(Exception) 吞成 null → 400（终态，事件永久丢失）。
                _webhookServiceMock
                    .Setup(x => x.DecryptEventAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new OperationCanceledException("解密超时"));
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "未定义的决策表场景");
        }

        var middleware = CreateMiddleware();
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(expectedStatus,
            $"决策表场景「{scenario}」的 ACK 语义被改变——这会直接导致事件丢失或重推风暴");
    }

    [Fact]
    public async Task InterceptedEvent_ShouldAck200_Not500()
    {
        // Arrange - D2 核心契约的独立回归锁（不依赖 Theory 参数化）
        _webhookServiceMock
            .Setup(x => x.HandleEventAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null));

        var middleware = CreateMiddleware();
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(200,
            "拦截 = 已消费；若回到 500，飞书会无限重推且事件永不 ack（R3-P0-2）");
    }

    [Fact]
    public async Task InfrastructureFailure_ShouldNeverMapTo403()
    {
        // Arrange - R3-P0-3：403 是终态（飞书不重推、不写失败存储）→ 事件永久丢失，
        // 且日志会误报"检测到重放攻击"污染安全审计。
        var scenarios = new (string Name, Exception Ex)[]
        {
            ("Nonce 去重 Connection 故障",
                new FeishuDeduplicationFatalException("nonce down",
                    new FeishuRedisException(FeishuRedisFailureKind.Connection, "conn"))),
            ("Nonce 去重 Timeout 故障",
                new FeishuDeduplicationFatalException("nonce timeout",
                    new FeishuRedisException(FeishuRedisFailureKind.Timeout, "timeout"))),
            ("事件去重 Server 故障",
                new FeishuDeduplicationFatalException("dedup down",
                    new FeishuRedisException(FeishuRedisFailureKind.Server, "server"))),
        };

        foreach (var (name, ex) in scenarios)
        {
            _webhookServiceMock
                .Setup(x => x.HandleEventAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(ex);

            var middleware = CreateMiddleware();
            var context = CreateHttpContext();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().NotBe(403, $"{name} 不得映射为 403（终态 → 事件永久丢失）");
            context.Response.StatusCode.Should().Be(503, $"{name} 应映射为 503（可恢复 → 飞书重推）");
        }
    }

    [Fact]
    public async Task NonRetryableScenarios_ShouldNotWriteFailedEventStore()
    {
        // Arrange - ADR-2：终态拒绝（验签/解密失败）**不得**写入失败事件存储——
        // 写了但飞书不会重推，等于在失败存储里堆积永不消费的死记录。
        var store = new Mock<IFailedEventStore>();

        foreach (var signaturePasses in new[] { false, true })
        {
            store.Invocations.Clear();

            var service = CreateRealService(store.Object);
            var validator = Mock.Get(service.Validator);
            validator
                .Setup(x => x.ValidateHeaderSignatureAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(signaturePasses);

            if (!signaturePasses)
            {
                // Act：验签失败
                await service.Service.HandleEventAsync(
                    new FeishuWebhookRequest { Timestamp = 1, Nonce = "n", Signature = "s" }, "body");
            }
            else
            {
                // Act：验签通过但解密失败（DecryptEventAsync 返回 null 的路径不经过 HandleEvent 分发）
                service.Decryptor
                    .Setup(x => x.DecryptAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((EventData?)null);
                await service.Service.DecryptEventAsync("cipher");
            }

            // Assert
            store.Verify(
                x => x.StoreFailedEventAsync(It.IsAny<EventData>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()),
                Times.Never, $"验签通过={signaturePasses} 属终态拒绝，不得写失败存储");
            store.Verify(
                x => x.StoreFailedEventAsync(It.IsAny<EventData>(), It.IsAny<Exception>(), It.IsAny<string?>(),
                    It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }

    [Fact]
    public async Task RetryableScenarios_ShouldWriteFailedEventStore()
    {
        // Arrange - ADR-2 回归：业务失败属可重试，必须写入失败存储一次（供 FailedEventRetryService 重投）
        var store = new Mock<IFailedEventStore>();
        var service = CreateRealService(store.Object);

        service.HandlerFactory
            .Setup(x => x.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("业务处理器失败"));

        // Act
        var result = await service.Service.HandleEventAsync(new EventData
        {
            EventId = "evt_business_failure",
            EventType = "test.event"
        });

        // Assert
        result.Success.Should().BeFalse("业务失败必须回 500 以便飞书重推");
        store.Verify(
            x => x.StoreFailedEventAsync(It.IsAny<EventData>(), It.IsAny<Exception>(), It.IsAny<string?>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once, "可重试场景必须写入失败存储一次（ADR-2）");
    }

    // ────────────────────────────────────────────────────────────────────
    // 辅助：真实 FeishuWebhookService 的最小替身装配
    // ────────────────────────────────────────────────────────────────────

    /// <remarks>不使用 <c>required</c>：net6.0 目标缺少对应编译器特性支持（CS0656）。</remarks>
    private sealed class RealServiceHarness
    {
        public RealServiceHarness(
            FeishuWebhookService service,
            IFeishuEventValidator validator,
            Mock<IFeishuEventDecryptor> decryptor,
            Mock<IFeishuEventHandlerFactory> handlerFactory)
        {
            Service = service;
            Validator = validator;
            Decryptor = decryptor;
            HandlerFactory = handlerFactory;
        }

        public FeishuWebhookService Service { get; }
        public IFeishuEventValidator Validator { get; }
        public Mock<IFeishuEventDecryptor> Decryptor { get; }
        public Mock<IFeishuEventHandlerFactory> HandlerFactory { get; }
    }

    private RealServiceHarness CreateRealService(IFailedEventStore store)
    {
        var options = new FeishuWebhookOptions
        {
            EventHandlingTimeoutMs = 5000,
            MaxConcurrentEvents = 10,
            // ADR-2：失败存储写入以 EnableRetry 为门控（默认 false），守卫必须显式开启
            Retry = new FailedEventRetryOptions { EnableRetry = true }
        };
        var optionsMonitorMock = new Mock<IOptionsMonitor<FeishuWebhookOptions>>();
        optionsMonitorMock.Setup(x => x.CurrentValue).Returns(options);

        var validatorMock = new Mock<IFeishuEventValidator>();
        validatorMock
            .Setup(x => x.ValidateHeaderSignatureAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var decryptorMock = new Mock<IFeishuEventDecryptor>();
        var handlerFactoryMock = new Mock<IFeishuEventHandlerFactory>();
        handlerFactoryMock.Setup(x => x.IsHandlerRegistered(It.IsAny<string>())).Returns(true);
        handlerFactoryMock
            .Setup(x => x.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var deduplicatorMock = new Mock<IFeishuEventDeduplicator>();
        deduplicatorMock
            .Setup(x => x.TryMarkAsProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DeduplicationResult.Success("evt"));
        deduplicatorMock
            .Setup(x => x.MarkAsCompletedAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        deduplicatorMock
            .Setup(x => x.RollbackProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var encryptKeyProviderMock = new Mock<IEncryptKeyProvider>();
        encryptKeyProviderMock
            .Setup(x => x.GetEncryptKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("0123456789abcdef0123456789abcdef");

        var appKeyAccessorMock = new Mock<IWebhookAppKeyAccessor>();
        string? currentAppKey = null;
        appKeyAccessorMock.Setup(x => x.SetAppKey(It.IsAny<string>())).Callback<string>(k => currentAppKey = k);
        appKeyAccessorMock.Setup(x => x.CurrentAppKey).Returns(() => currentAppKey);

        var concurrencyService = new FeishuWebhookConcurrencyService(
            optionsMonitorMock.Object, new Mock<ILogger<FeishuWebhookConcurrencyService>>().Object);

        var service = new FeishuWebhookService(
            optionsMonitorMock.Object,
            validatorMock.Object,
            decryptorMock.Object,
            handlerFactoryMock.Object,
            new Mock<ILogger<FeishuWebhookService>>().Object,
            Array.Empty<IFeishuEventInterceptor>(),
            concurrencyService,
            deduplicatorMock.Object,
            encryptKeyProviderMock.Object,
            new FeishuWebhookHandlerRegistry(),
            new FeishuWebhookInterceptorRegistry(),
            new Mock<IServiceProvider>().Object,
            appKeyAccessorMock.Object,
            store);

        return new RealServiceHarness(
            service,
            validatorMock.Object,
            decryptorMock,
            handlerFactoryMock);
    }
}
