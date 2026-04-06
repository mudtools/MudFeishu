// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.EventHandlers;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Webhook;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Models;
using Mud.Feishu.Webhook.Services;
using Xunit;

namespace Mud.Feishu.Webhook.Tests.Validators;

/// <summary>
/// 依赖注入单元测试
/// 测试各种注册配置、服务解析正确性和依赖关系完整性
/// **验证需求: 6.1, 6.2, 6.3**
/// </summary>
public class DependencyInjectionTests
{
    /// <summary>
    /// 测试专门验证器的独立注册
    /// </summary>
    [Fact]
    public void RegisterSpecializedValidators_ShouldResolveIndependently()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration();

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        RegisterValidatorDependencies(services);

        // Act - 注册专门验证器
        services.CreateFeishuWebhookServiceBuilder(configuration)
            .AddHandler<TestEventHandler>()
            .UseSignatureValidator<MockSignatureValidator>()
            .UseTimestampValidator<MockTimestampValidator>()
            .UseNonceValidator<MockNonceValidator>()
            .UseSubscriptionValidator<MockSubscriptionValidator>()
            .Build();

        // Assert
        using var serviceProvider = services.BuildServiceProvider();

        var signatureValidator = serviceProvider.GetService<ISignatureValidator>();
        var timestampValidator = serviceProvider.GetService<ITimestampValidator>();
        var nonceValidator = serviceProvider.GetService<INonceValidator>();
        var subscriptionValidator = serviceProvider.GetService<ISubscriptionValidator>();

        Assert.NotNull(signatureValidator);
        Assert.NotNull(timestampValidator);
        Assert.NotNull(nonceValidator);
        Assert.NotNull(subscriptionValidator);

        Assert.IsType<MockSignatureValidator>(signatureValidator);
        Assert.IsType<MockTimestampValidator>(timestampValidator);
        Assert.IsType<MockNonceValidator>(nonceValidator);
        Assert.IsType<MockSubscriptionValidator>(subscriptionValidator);
    }

    /// <summary>
    /// 测试默认验证器注册
    /// </summary>
    [Fact]
    public void RegisterDefaultValidators_ShouldResolveCorrectTypes()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration();

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        RegisterValidatorDependencies(services);

        // Act - 注册默认验证器
        services.CreateFeishuWebhookServiceBuilder(configuration)
            .AddHandler<TestEventHandler>()
            .Build();

        // Assert
        using var serviceProvider = services.BuildServiceProvider();

        var signatureValidator = serviceProvider.GetService<ISignatureValidator>();
        var timestampValidator = serviceProvider.GetService<ITimestampValidator>();
        var nonceValidator = serviceProvider.GetService<INonceValidator>();
        var subscriptionValidator = serviceProvider.GetService<ISubscriptionValidator>();

        Assert.NotNull(signatureValidator);
        Assert.NotNull(timestampValidator);
        Assert.NotNull(nonceValidator);
        Assert.NotNull(subscriptionValidator);

        Assert.IsType<SignatureValidator>(signatureValidator);
        Assert.IsType<TimestampValidator>(timestampValidator);
        Assert.IsType<NonceValidator>(nonceValidator);
        Assert.IsType<SubscriptionValidator>(subscriptionValidator);
    }

    /// <summary>
    /// 测试组合验证器注册
    /// </summary>
    [Fact]
    public void RegisterCompositeValidator_ShouldResolveAsIFeishuEventValidator()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration();

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        RegisterValidatorDependencies(services);

        // Act - 注册组合验证器
        services.CreateFeishuWebhookServiceBuilder(configuration)
            .AddHandler<TestEventHandler>()
            .Build();

        // Assert
        using var serviceProvider = services.BuildServiceProvider();

        var compositeValidator = serviceProvider.GetService<IFeishuEventValidator>();
        Assert.NotNull(compositeValidator);
        Assert.IsType<CompositeFeishuEventValidator>(compositeValidator);
    }

    /// <summary>
    /// 测试自定义组合验证器注册
    /// </summary>
    [Fact]
    public void RegisterCustomCompositeValidator_ShouldResolveCorrectType()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration();

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        RegisterValidatorDependencies(services);

        // Act - 注册自定义组合验证器
        services.CreateFeishuWebhookServiceBuilder(configuration)
            .AddHandler<TestEventHandler>()
            .UseCompositeValidator<MockCompositeValidator>()
            .Build();

        // Assert
        using var serviceProvider = services.BuildServiceProvider();

        var compositeValidator = serviceProvider.GetService<IFeishuEventValidator>();
        Assert.NotNull(compositeValidator);
        Assert.IsType<MockCompositeValidator>(compositeValidator);
    }

    /// <summary>
    /// 测试依赖注入的完整性
    /// </summary>
    [Fact]
    public void RegisterServices_ShouldInjectDependenciesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration();

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        RegisterValidatorDependencies(services);

        // Act - 注册服务
        services.CreateFeishuWebhookServiceBuilder(configuration)
            .AddHandler<TestEventHandler>()
            .Build();

        // Assert
        using var serviceProvider = services.BuildServiceProvider();

        // 验证组合验证器可以解析
        var compositeValidator = serviceProvider.GetService<IFeishuEventValidator>();
        Assert.NotNull(compositeValidator);
        Assert.IsType<CompositeFeishuEventValidator>(compositeValidator);

        // 验证专门验证器可以解析
        var signatureValidator = serviceProvider.GetService<ISignatureValidator>();
        var timestampValidator = serviceProvider.GetService<ITimestampValidator>();
        var nonceValidator = serviceProvider.GetService<INonceValidator>();
        var subscriptionValidator = serviceProvider.GetService<ISubscriptionValidator>();

        Assert.NotNull(signatureValidator);
        Assert.NotNull(timestampValidator);
        Assert.NotNull(nonceValidator);
        Assert.NotNull(subscriptionValidator);

        // 验证依赖服务可以解析
        var auditService = serviceProvider.GetService<ISecurityAuditService>();
        var nonceDeduplicator = serviceProvider.GetService<IFeishuNonceDistributedDeduplicator>();

        Assert.NotNull(auditService);
        Assert.NotNull(nonceDeduplicator);
    }

    /// <summary>
    /// 测试混合注册配置
    /// </summary>
    [Fact]
    public void RegisterMixedConfiguration_ShouldResolveCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration();

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        RegisterValidatorDependencies(services);

        // Act - 混合注册：部分自定义，部分默认
        services.CreateFeishuWebhookServiceBuilder(configuration)
            .AddHandler<TestEventHandler>()
            .UseSignatureValidator<MockSignatureValidator>()
            .UseNonceValidator<MockNonceValidator>()
            // TimestampValidator 和 SubscriptionValidator 使用默认实现
            .Build();

        // Assert
        using var serviceProvider = services.BuildServiceProvider();

        var signatureValidator = serviceProvider.GetService<ISignatureValidator>();
        var timestampValidator = serviceProvider.GetService<ITimestampValidator>();
        var nonceValidator = serviceProvider.GetService<INonceValidator>();
        var subscriptionValidator = serviceProvider.GetService<ISubscriptionValidator>();

        Assert.NotNull(signatureValidator);
        Assert.NotNull(timestampValidator);
        Assert.NotNull(nonceValidator);
        Assert.NotNull(subscriptionValidator);

        // 验证自定义类型
        Assert.IsType<MockSignatureValidator>(signatureValidator);
        Assert.IsType<MockNonceValidator>(nonceValidator);

        // 验证默认类型
        Assert.IsType<TimestampValidator>(timestampValidator);
        Assert.IsType<SubscriptionValidator>(subscriptionValidator);
    }

    /// <summary>
    /// 测试服务生命周期
    /// </summary>
    [Fact]
    public void RegisterServices_ShouldHaveCorrectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration();

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        RegisterValidatorDependencies(services);

        // Act
        services.CreateFeishuWebhookServiceBuilder(configuration)
            .AddHandler<TestEventHandler>()
            .Build();

        // Assert
        using var serviceProvider = services.BuildServiceProvider();

        // 验证验证器是 Scoped 生命周期（每次请求获取相同实例）
        using var scope1 = serviceProvider.CreateScope();
        using var scope2 = serviceProvider.CreateScope();

        var validator1a = scope1.ServiceProvider.GetService<IFeishuEventValidator>();
        var validator1b = scope1.ServiceProvider.GetService<IFeishuEventValidator>();
        var validator2 = scope2.ServiceProvider.GetService<IFeishuEventValidator>();

        // 同一作用域内应该是相同实例
        Assert.Same(validator1a, validator1b);

        // 不同作用域应该是不同实例
        Assert.NotSame(validator1a, validator2);
    }

    #region 辅助方法

    /// <summary>
    /// 创建测试配置
    /// </summary>
    private static IConfiguration CreateTestConfiguration()
    {
        var mockConfiguration = new Mock<IConfiguration>();
        var mockSection = new Mock<IConfigurationSection>();

        mockConfiguration.Setup(x => x.GetSection("FeishuWebhook")).Returns(mockSection.Object);
        mockSection.Setup(x => x["GlobalRoutePrefix"]).Returns("feishu");
        mockSection.Setup(x => x["TimestampToleranceSeconds"]).Returns("300");
        mockSection.Setup(x => x["EnforceHeaderSignatureValidation"]).Returns("true");

        return mockConfiguration.Object;
    }

    /// <summary>
    /// 注册验证器依赖服务
    /// </summary>
    private static void RegisterValidatorDependencies(IServiceCollection services)
    {
        // 注册 Mock 依赖服务
        services.AddScoped<ISecurityAuditService, MockSecurityAuditService>();
        services.AddScoped<IFeishuNonceDistributedDeduplicator, MockNonceDeduplicator>();

        // 注册配置选项
        services.Configure<FeishuWebhookOptions>(options =>
        {
            options.TimestampToleranceSeconds = 300;
            options.EnforceHeaderSignatureValidation = true;
        });
    }

    #endregion

    #region Mock 实现

    /// <summary>
    /// Mock 签名验证器
    /// </summary>
    public class MockSignatureValidator : ISignatureValidator
    {
        public void SetCurrentAppKey(string appKey) { }

        public Task<bool> ValidateSignatureAsync(long timestamp, string nonce, string encrypt, string signature, string encryptKey)
            => Task.FromResult(true);

        public Task<bool> ValidateHeaderSignatureAsync(long timestamp, string nonce, string body, string? headerSignature, string encryptKey)
            => Task.FromResult(true);
    }

    /// <summary>
    /// Mock 时间戳验证器
    /// </summary>
    public class MockTimestampValidator : ITimestampValidator
    {
        public void SetCurrentAppKey(string appKey) { }

        public bool ValidateTimestamp(long timestamp, int toleranceSeconds = 300) => true;
    }

    /// <summary>
    /// Mock Nonce 验证器
    /// </summary>
    public class MockNonceValidator : INonceValidator
    {
        public void SetCurrentAppKey(string appKey) { }

        public Task<bool> TryMarkNonceAsUsedAsync(string nonce) => Task.FromResult(false);

        public Task<bool> ValidateNonceAsync(string nonce, bool isProductionEnvironment = true) => Task.FromResult(true);
    }

    /// <summary>
    /// Mock 订阅验证器
    /// </summary>
    public class MockSubscriptionValidator : ISubscriptionValidator
    {
        public void SetCurrentAppKey(string appKey) { }

        public bool ValidateSubscriptionRequest(EventVerificationRequest request, string expectedToken) => true;

        public Task<bool> ValidateSubscriptionRequestAsync(EventVerificationRequest request, string expectedToken, CancellationToken cancellationToken = default) 
            => Task.FromResult(true);
    }

    /// <summary>
    /// Mock 组合验证器
    /// </summary>
    public class MockCompositeValidator : IFeishuEventValidator
    {
        public void SetCurrentAppKey(string appKey) { }

        public bool ValidateSubscriptionRequest(EventVerificationRequest request, string expectedToken) => true;

        public Task<bool> ValidateSubscriptionRequestAsync(EventVerificationRequest request, string expectedToken, CancellationToken cancellationToken = default) 
            => Task.FromResult(true);

        public Task<bool> ValidateSignatureAsync(long timestamp, string nonce, string encrypt, string signature, string encryptKey)
            => Task.FromResult(true);

        public Task<bool> ValidateHeaderSignatureAsync(long timestamp, string nonce, string body, string? headerSignature, string encryptKey)
            => Task.FromResult(true);

        public bool ValidateTimestamp(long timestamp, int toleranceSeconds = 300) => true;
    }

    /// <summary>
    /// Mock 安全审计服务
    /// </summary>
    public class MockSecurityAuditService : ISecurityAuditService
    {
        public Task LogSecurityFailureAsync(SecurityEventType eventType, string eventDescription, string? ipAddress = null, string? userAgent = null, string? requestId = null, string? additionalData = null)
            => Task.CompletedTask;

        public Task LogSecuritySuccessAsync(SecurityEventType eventType, string eventDescription, string? ipAddress = null, string? userAgent = null, string? requestId = null, string? additionalData = null)
            => Task.CompletedTask;
    }

    /// <summary>
    /// Mock Nonce 去重器
    /// </summary>
    public class MockNonceDeduplicator : IFeishuNonceDistributedDeduplicator, IDisposable
    {
        public Task<bool> TryMarkAsUsedAsync(string nonce, string? appKey = null, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<bool> IsUsedAsync(string nonce, string? appKey = null, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public void Dispose() { }
    }

    /// <summary>
    /// 测试事件处理器
    /// </summary>
    public class TestEventHandler : DefaultFeishuEventHandler<TestEventResult>
    {
        public TestEventHandler(Microsoft.Extensions.Logging.ILogger<TestEventHandler> logger) : base(logger) { }

        protected override Task ProcessBusinessLogicAsync(EventData eventData, TestEventResult? eventEntity, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 测试事件结果
    /// </summary>
    public class TestEventResult : IEventResult
    {
        public bool IsSuccess { get; set; } = true;
        public string? ErrorMessage { get; set; }
    }

    #endregion
}