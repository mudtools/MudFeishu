// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FsCheck;
using FsCheck.Xunit;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Webhook.Services;

namespace Mud.Feishu.Webhook.Tests.Propertys;

/// <summary>
/// Nonce验证器属性测试
/// 使用 FsCheck 进行基于属性的测试，验证Nonce验证器的正确性属性
/// </summary>
[Trait("Feature", "feishu-validator-refactoring")]
public class NonceValidatorProperties
{
    private readonly Mock<ILogger<NonceValidator>> _loggerMock;
    private readonly Mock<IWebhookAppKeyAccessor> _appKeyAccessorMock;

    public NonceValidatorProperties()
    {
        _loggerMock = new Mock<ILogger<NonceValidator>>();
        _appKeyAccessorMock = new Mock<IWebhookAppKeyAccessor>();

        // 设置 _appKeyAccessorMock 使 SetAppKey 方法能够更新 CurrentAppKey 属性
        string? currentAppKey = null;
        _appKeyAccessorMock
            .Setup(x => x.SetAppKey(It.IsAny<string>()))
            .Callback<string>(appKey => currentAppKey = appKey);
        _appKeyAccessorMock
            .Setup(x => x.CurrentAppKey)
            .Returns(() => currentAppKey);
    }

    /// <summary>
    /// 属性 7: Nonce去重服务依赖
    /// **验证需求: 3.2**
    /// 对于任何NonceValidator实例，它应该正确使用IFeishuNonceDistributedDeduplicator服务进行去重检查
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "7: Nonce去重服务依赖")]
    public Property NonceValidator_ShouldUseDeduplicatorService()
    {
        return Prop.ForAll(
            GenerateValidNonceData(),
            data =>
            {
                // Arrange
                var deduplicatorMock = new Mock<IFeishuNonceDistributedDeduplicator>();
                deduplicatorMock
                    .Setup(x => x.TryMarkAsUsedAsync(data.Nonce, data.AppKey, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(data.IsAlreadyUsed);

                // 为每个测试用例创建新的 appKeyAccessorMock
                var appKeyAccessorMock = new Mock<IWebhookAppKeyAccessor>();
                string? currentAppKey = null;
                appKeyAccessorMock
                    .Setup(x => x.SetAppKey(It.IsAny<string>()))
                    .Callback<string>(appKey => currentAppKey = appKey);
                appKeyAccessorMock
                    .Setup(x => x.CurrentAppKey)
                    .Returns(() => currentAppKey);

                var validator = new NonceValidator(_loggerMock.Object, deduplicatorMock.Object, appKeyAccessorMock.Object);
                if (!string.IsNullOrEmpty(data.AppKey))
                {
                    validator.SetCurrentAppKey(data.AppKey);
                }

                // Act
                var result = validator.TryMarkNonceAsUsedAsync(data.Nonce).Result;

                // Assert
                // 验证器应该调用去重服务
                deduplicatorMock.Verify(
                    x => x.TryMarkAsUsedAsync(data.Nonce, data.AppKey, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
                    Times.Once);

                // 验证器应该返回去重服务的结果
                return result == data.IsAlreadyUsed;
            });
    }

    /// <summary>
    /// 属性 8: 多应用Nonce隔离
    /// **验证需求: 3.3**
    /// 对于任何不同应用的相同Nonce值，系统应该将它们视为独立的Nonce进行处理
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "8: 多应用Nonce隔离")]
    public Property NonceValidator_ShouldIsolateNoncesByApp()
    {
        return Prop.ForAll(
            GenerateMultiAppNonceData(),
            data =>
            {
                // Arrange
                var deduplicatorMock = new Mock<IFeishuNonceDistributedDeduplicator>();

                // 设置不同应用的相同Nonce应该被独立处理
                deduplicatorMock
                    .Setup(x => x.TryMarkAsUsedAsync(data.Nonce, data.AppKey1, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false); // 第一个应用的Nonce未被使用

                deduplicatorMock
                    .Setup(x => x.TryMarkAsUsedAsync(data.Nonce, data.AppKey2, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false); // 第二个应用的相同Nonce也未被使用

                // 为每个测试用例创建新的 appKeyAccessorMock
                var appKeyAccessorMock = new Mock<IWebhookAppKeyAccessor>();
                string? currentAppKey = null;
                appKeyAccessorMock
                    .Setup(x => x.SetAppKey(It.IsAny<string>()))
                    .Callback<string>(appKey => currentAppKey = appKey);
                appKeyAccessorMock
                    .Setup(x => x.CurrentAppKey)
                    .Returns(() => currentAppKey);

                var validator = new NonceValidator(_loggerMock.Object, deduplicatorMock.Object, appKeyAccessorMock.Object);

                // Act - 测试第一个应用
                validator.SetCurrentAppKey(data.AppKey1);
                var result1 = validator.TryMarkNonceAsUsedAsync(data.Nonce).Result;

                // Act - 测试第二个应用
                validator.SetCurrentAppKey(data.AppKey2);
                var result2 = validator.TryMarkNonceAsUsedAsync(data.Nonce).Result;

                // Assert
                // 验证去重服务被正确调用，使用不同的AppKey
                deduplicatorMock.Verify(
                    x => x.TryMarkAsUsedAsync(data.Nonce, data.AppKey1, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
                    Times.Once);

                deduplicatorMock.Verify(
                    x => x.TryMarkAsUsedAsync(data.Nonce, data.AppKey2, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
                    Times.Once);

                // 两个应用的相同Nonce应该都被视为未使用（独立处理）
                return !result1 && !result2;
            });
    }

    /// <summary>
    /// 属性 9: 重复Nonce拒绝
    /// **验证需求: 3.4**
    /// 对于任何已被使用的Nonce，系统应该拒绝请求并记录重放攻击警告
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "9: 重复Nonce拒绝")]
    public Property NonceValidator_ShouldRejectUsedNonce()
    {
        return Prop.ForAll(
            GenerateUsedNonceData(),
            data =>
            {
                // Arrange
                var deduplicatorMock = new Mock<IFeishuNonceDistributedDeduplicator>();
                deduplicatorMock
                    .Setup(x => x.TryMarkAsUsedAsync(data.Nonce, data.AppKey, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true); // Nonce已被使用

                var loggerMock = new Mock<ILogger<NonceValidator>>();

                // 为每个测试用例创建新的 appKeyAccessorMock
                var appKeyAccessorMock = new Mock<IWebhookAppKeyAccessor>();
                string? currentAppKey = null;
                appKeyAccessorMock
                    .Setup(x => x.SetAppKey(It.IsAny<string>()))
                    .Callback<string>(appKey => currentAppKey = appKey);
                appKeyAccessorMock
                    .Setup(x => x.CurrentAppKey)
                    .Returns(() => currentAppKey);

                var validator = new NonceValidator(loggerMock.Object, deduplicatorMock.Object, appKeyAccessorMock.Object);

                if (!string.IsNullOrEmpty(data.AppKey))
                {
                    validator.SetCurrentAppKey(data.AppKey);
                }

                // Act
                var validateResult = validator.ValidateNonceAsync(data.Nonce, data.IsProductionEnvironment).Result;
                var markResult = validator.TryMarkNonceAsUsedAsync(data.Nonce).Result;

                // Assert
                // 验证应该失败（已使用的Nonce）
                var validationFailed = !validateResult;
                var markReturnedTrue = markResult; // TryMarkNonceAsUsedAsync应该返回true（已使用）

                // 验证是否记录了重放攻击警告
                loggerMock.Verify(
                    x => x.Log(
                        LogLevel.Warning,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("重放攻击")),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.AtLeastOnce);

                return validationFailed && markReturnedTrue;
            });
    }

    /// <summary>
    /// 属性 10: 生产环境空Nonce检查
    /// **验证需求: 3.5**
    /// 对于任何在生产环境中的空Nonce，系统应该拒绝请求并记录安全事件
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "10: 生产环境空Nonce检查")]
    public Property NonceValidator_ShouldRejectEmptyNonceInProduction()
    {
        return Prop.ForAll(
            GenerateEmptyNonceData(),
            data =>
            {
                // Arrange
                var deduplicatorMock = new Mock<IFeishuNonceDistributedDeduplicator>();
                var loggerMock = new Mock<ILogger<NonceValidator>>();

                // 为每个测试用例创建新的 appKeyAccessorMock
                var appKeyAccessorMock = new Mock<IWebhookAppKeyAccessor>();
                string? currentAppKey = null;
                appKeyAccessorMock
                    .Setup(x => x.SetAppKey(It.IsAny<string>()))
                    .Callback<string>(appKey => currentAppKey = appKey);
                appKeyAccessorMock
                    .Setup(x => x.CurrentAppKey)
                    .Returns(() => currentAppKey);

                var validator = new NonceValidator(loggerMock.Object, deduplicatorMock.Object, appKeyAccessorMock.Object);

                if (!string.IsNullOrEmpty(data.AppKey))
                {
                    validator.SetCurrentAppKey(data.AppKey);
                }

                // Act
                var result = validator.ValidateNonceAsync(data.EmptyNonce, data.IsProductionEnvironment).Result;

                // Assert
                if (data.IsProductionEnvironment)
                {
                    // 生产环境应该拒绝空Nonce
                    var rejectedEmptyNonce = !result;

                    // 验证是否记录了安全事件（错误日志）
                    loggerMock.Verify(
                        x => x.Log(
                            LogLevel.Error,
                            It.IsAny<EventId>(),
                            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Nonce 为空") && v.ToString()!.Contains("拒绝请求")),
                            It.IsAny<Exception>(),
                            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                        Times.AtLeastOnce);

                    return rejectedEmptyNonce;
                }
                else
                {
                    // 开发环境应该允许空Nonce但记录警告
                    var allowedEmptyNonce = result;

                    // 验证是否记录了警告日志
                    loggerMock.Verify(
                        x => x.Log(
                            LogLevel.Warning,
                            It.IsAny<EventId>(),
                            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Nonce 为空") && v.ToString()!.Contains("跳过验证")),
                            It.IsAny<Exception>(),
                            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                        Times.AtLeastOnce);

                    return allowedEmptyNonce;
                }
            });
    }

    /// <summary>
    /// 生成有效的Nonce测试数据
    /// </summary>
    private static Arbitrary<ValidNonceTestData> GenerateValidNonceData()
    {
        return Arb.From(
            from nonce in Gen.Elements("test-nonce-1", "test-nonce-2", "valid-nonce", "another-nonce", "unique-nonce-123")
            from appKey in Gen.Elements("app1", "app2", "test-app", null)
            from isAlreadyUsed in Gen.Elements(true, false)
            select new ValidNonceTestData
            {
                Nonce = nonce,
                AppKey = appKey,
                IsAlreadyUsed = isAlreadyUsed
            });
    }

    /// <summary>
    /// 生成多应用Nonce测试数据
    /// </summary>
    private static Arbitrary<MultiAppNonceTestData> GenerateMultiAppNonceData()
    {
        return Arb.From(
            from nonce in Gen.Elements("shared-nonce-1", "shared-nonce-2", "common-nonce")
            from appKey1 in Gen.Elements("app1", "application-1", "test-app-1")
            from appKey2 in Gen.Elements("app2", "application-2", "test-app-2")
            where appKey1 != appKey2 // 确保是不同的应用
            select new MultiAppNonceTestData
            {
                Nonce = nonce,
                AppKey1 = appKey1,
                AppKey2 = appKey2
            });
    }

    /// <summary>
    /// 生成已使用的Nonce测试数据
    /// </summary>
    private static Arbitrary<UsedNonceTestData> GenerateUsedNonceData()
    {
        return Arb.From(
            from nonce in Gen.Elements("used-nonce-1", "used-nonce-2", "replay-nonce", "duplicate-nonce")
            from appKey in Gen.Elements("app1", "app2", "test-app", null)
            from isProductionEnvironment in Gen.Elements(true, false)
            select new UsedNonceTestData
            {
                Nonce = nonce,
                AppKey = appKey,
                IsProductionEnvironment = isProductionEnvironment
            });
    }

    /// <summary>
    /// 生成空Nonce测试数据
    /// </summary>
    private static Arbitrary<EmptyNonceTestData> GenerateEmptyNonceData()
    {
        return Arb.From(
            from emptyNonce in Gen.Elements("", null) // 只生成真正的空字符串和null，不包括空白字符
            from appKey in Gen.Elements("app1", "app2", "test-app", null)
            from isProductionEnvironment in Gen.Elements(true, false)
            select new EmptyNonceTestData
            {
                EmptyNonce = emptyNonce ?? "",
                AppKey = appKey,
                IsProductionEnvironment = isProductionEnvironment
            });
    }

    /// <summary>
    /// 有效Nonce测试数据
    /// </summary>
    public class ValidNonceTestData
    {
        public string Nonce { get; set; } = "";
        public string? AppKey { get; set; }
        public bool IsAlreadyUsed { get; set; }
    }

    /// <summary>
    /// 多应用Nonce测试数据
    /// </summary>
    public class MultiAppNonceTestData
    {
        public string Nonce { get; set; } = "";
        public string AppKey1 { get; set; } = "";
        public string AppKey2 { get; set; } = "";
    }

    /// <summary>
    /// 已使用Nonce测试数据
    /// </summary>
    public class UsedNonceTestData
    {
        public string Nonce { get; set; } = "";
        public string? AppKey { get; set; }
        public bool IsProductionEnvironment { get; set; }
    }

    /// <summary>
    /// 空Nonce测试数据
    /// </summary>
    public class EmptyNonceTestData
    {
        public string EmptyNonce { get; set; } = "";
        public string? AppKey { get; set; }
        public bool IsProductionEnvironment { get; set; }
    }
}