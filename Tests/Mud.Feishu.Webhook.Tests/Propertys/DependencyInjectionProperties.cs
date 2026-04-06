// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mud.Feishu.Abstractions.EventHandlers;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Models;
using Mud.Feishu.Webhook.Services;

namespace Mud.Feishu.Webhook.Tests.Propertys;

/// <summary>
/// 依赖注入属性测试
/// 使用 FsCheck 进行基于属性的测试，验证依赖注入配置的正确性属性
/// </summary>
[Trait("Feature", "feishu-validator-refactoring")]
public class DependencyInjectionProperties
{
    /// <summary>
    /// 属性 17: 独立服务注册
    /// **验证需求: 6.1**
    /// 对于任何专门验证器接口，应该能够独立注册到依赖注入容器中
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "17: 独立服务注册")]
    public Property DependencyInjection_ShouldRegisterSpecializedValidatorsIndependently()
    {
        return Prop.ForAll(
            GenerateServiceRegistrationData(),
            data =>
            {
                // Arrange
                var services = new ServiceCollection();
                var configuration = CreateTestConfiguration(data);

                // 添加基础依赖
                services.AddLogging();
                services.AddSingleton<IConfiguration>(configuration);

                // 注册专门验证器的依赖
                RegisterValidatorDependencies(services);

                // Act - 使用建造者注册专门验证器
                var builder = services.CreateFeishuWebhookServiceBuilder(configuration)
                    .AddHandler<TestEventHandler>();

                if (data.RegisterSignatureValidator)
                    builder.UseSignatureValidator<MockSignatureValidator>();
                if (data.RegisterTimestampValidator)
                    builder.UseTimestampValidator<MockTimestampValidator>();
                if (data.RegisterNonceValidator)
                    builder.UseNonceValidator<MockNonceValidator>();
                if (data.RegisterSubscriptionValidator)
                    builder.UseSubscriptionValidator<MockSubscriptionValidator>();

                builder.Build();

                // Assert
                var serviceProvider = services.BuildServiceProvider();

                try
                {
                    // 验证专门验证器可以独立解析
                    var canResolveSignature = !data.RegisterSignatureValidator ||
                        CanResolveService<ISignatureValidator>(serviceProvider);
                    var canResolveTimestamp = !data.RegisterTimestampValidator ||
                        CanResolveService<ITimestampValidator>(serviceProvider);
                    var canResolveNonce = !data.RegisterNonceValidator ||
                        CanResolveService<INonceValidator>(serviceProvider);
                    var canResolveSubscription = !data.RegisterSubscriptionValidator ||
                        CanResolveService<ISubscriptionValidator>(serviceProvider);

                    return canResolveSignature && canResolveTimestamp && canResolveNonce && canResolveSubscription;
                }
                finally
                {
                    serviceProvider.DisposeAsync().AsTask().Wait();
                }
            });
    }

    /// <summary>
    /// 属性 18: 组合验证器注册
    /// **验证需求: 6.2**
    /// 对于任何依赖注入配置，CompositeFeishuEventValidator 应该能够注册为 IFeishuEventValidator 的实现
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "18: 组合验证器注册")]
    public Property DependencyInjection_ShouldRegisterCompositeValidator()
    {
        return Prop.ForAll(
            GenerateCompositeValidatorRegistrationData(),
            data =>
            {
                // Arrange
                var services = new ServiceCollection();
                var configuration = CreateTestConfiguration(data);

                // 添加基础依赖
                services.AddLogging();
                services.AddSingleton<IConfiguration>(configuration);

                // 注册验证器依赖
                RegisterValidatorDependencies(services);

                // Act - 注册服务
                var builder = services.CreateFeishuWebhookServiceBuilder(configuration)
                    .AddHandler<TestEventHandler>();

                if (data.UseCustomCompositeValidator)
                {
                    builder.UseCompositeValidator<MockCompositeValidator>();
                }

                builder.Build();

                // Assert
                var serviceProvider = services.BuildServiceProvider();

                try
                {
                    // 验证可以解析 IFeishuEventValidator
                    if (serviceProvider.GetService<IFeishuEventValidator>() is not { } validator)
                        return false;

                    // 验证类型是否正确
                    var isCorrectType = data.UseCustomCompositeValidator ?
                        validator is MockCompositeValidator :
                        validator is CompositeFeishuEventValidator;

                    return isCorrectType;
                }
                finally
                {
                    serviceProvider.DisposeAsync().AsTask().Wait();
                }
            });
    }

    /// <summary>
    /// 属性 19: 依赖注入正确性
    /// **验证需求: 6.3**
    /// 对于任何验证器实例创建，应该正确注入所需的依赖服务
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "19: 依赖注入正确性")]
    public Property DependencyInjection_ShouldInjectDependenciesCorrectly()
    {
        return Prop.ForAll(
            GenerateDependencyInjectionData(),
            data =>
            {
                // Arrange
                var services = new ServiceCollection();
                var configuration = CreateTestConfiguration(data);

                // 添加基础依赖
                services.AddLogging();
                services.AddSingleton<IConfiguration>(configuration);

                // 注册验证器依赖
                RegisterValidatorDependencies(services);

                // Act - 注册服务
                services.CreateFeishuWebhookServiceBuilder(configuration)
                    .AddHandler<TestEventHandler>()
                    .Build();

                // Assert
                var serviceProvider = services.BuildServiceProvider();

                try
                {
                    // 验证组合验证器的依赖注入
                    if (serviceProvider.GetService<IFeishuEventValidator>() is not CompositeFeishuEventValidator compositeValidator)
                        return false;

                    // 验证专门验证器可以解析
                    var signatureValidator = serviceProvider.GetService<ISignatureValidator>();
                    var timestampValidator = serviceProvider.GetService<ITimestampValidator>();
                    var nonceValidator = serviceProvider.GetService<INonceValidator>();
                    var subscriptionValidator = serviceProvider.GetService<ISubscriptionValidator>();

                    // 验证所有依赖都能正确解析
                    var allDependenciesResolved = signatureValidator != null &&
                                                timestampValidator != null &&
                                                nonceValidator != null &&
                                                subscriptionValidator != null;

                    // 验证依赖类型正确
                    var correctTypes = signatureValidator is SignatureValidator &&
                                     timestampValidator is TimestampValidator &&
                                     nonceValidator is NonceValidator &&
                                     subscriptionValidator is SubscriptionValidator;

                    return allDependenciesResolved && correctTypes;
                }
                finally
                {
                    serviceProvider.DisposeAsync().AsTask().Wait();
                }
            });
    }

    #region 测试数据生成器

    /// <summary>
    /// 生成服务注册测试数据
    /// </summary>
    private static Arbitrary<ServiceRegistrationTestData> GenerateServiceRegistrationData()
    {
        return Arb.From(
            from registerSignature in Arb.Generate<bool>()
            from registerTimestamp in Arb.Generate<bool>()
            from registerNonce in Arb.Generate<bool>()
            from registerSubscription in Arb.Generate<bool>()
            select new ServiceRegistrationTestData
            {
                RegisterSignatureValidator = registerSignature,
                RegisterTimestampValidator = registerTimestamp,
                RegisterNonceValidator = registerNonce,
                RegisterSubscriptionValidator = registerSubscription
            });
    }

    /// <summary>
    /// 生成组合验证器注册测试数据
    /// </summary>
    private static Arbitrary<CompositeValidatorRegistrationTestData> GenerateCompositeValidatorRegistrationData()
    {
        return Arb.From(
            from useCustom in Arb.Generate<bool>()
            select new CompositeValidatorRegistrationTestData
            {
                UseCustomCompositeValidator = useCustom
            });
    }

    /// <summary>
    /// 生成依赖注入测试数据
    /// </summary>
    private static Arbitrary<DependencyInjectionTestData> GenerateDependencyInjectionData()
    {
        return Arb.From(
            from appKey in Gen.Elements("test-app", "demo-app", "prod-app")
            from encryptKey in Gen.Elements("test-key-32-bytes-long-enough!!", "demo-key-32-bytes-long-enough!!")
            select new DependencyInjectionTestData
            {
                AppKey = appKey,
                EncryptKey = encryptKey
            });
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 创建测试配置
    /// </summary>
    private static IConfiguration CreateTestConfiguration<T>(T _)
    {
        // 使用 Mock 创建配置，避免 ConfigurationBuilder 依赖问题
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

    /// <summary>
    /// 检查服务是否可以解析
    /// </summary>
    private static bool CanResolveService<T>(IServiceProvider serviceProvider)
    {
        try
        {
            var service = serviceProvider.GetService<T>();
            return service != null;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region 测试数据类

    /// <summary>
    /// 服务注册测试数据
    /// </summary>
    public class ServiceRegistrationTestData
    {
        public bool RegisterSignatureValidator { get; set; }
        public bool RegisterTimestampValidator { get; set; }
        public bool RegisterNonceValidator { get; set; }
        public bool RegisterSubscriptionValidator { get; set; }
    }

    /// <summary>
    /// 组合验证器注册测试数据
    /// </summary>
    public class CompositeValidatorRegistrationTestData
    {
        public bool UseCustomCompositeValidator { get; set; }
    }

    /// <summary>
    /// 依赖注入测试数据
    /// </summary>
    public class DependencyInjectionTestData
    {
        public string AppKey { get; set; } = "";
        public string EncryptKey { get; set; } = "";
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
    public class MockNonceDeduplicator : IFeishuNonceDistributedDeduplicator
    {
        public Task<bool> TryMarkAsUsedAsync(string nonce, string? appKey = null, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<bool> IsUsedAsync(string nonce, string? appKey = null, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// 测试事件处理器
    /// </summary>
    public class TestEventHandler(ILogger<TestEventHandler> logger) : DefaultFeishuEventHandler<TestEventResult>(logger)
    {
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