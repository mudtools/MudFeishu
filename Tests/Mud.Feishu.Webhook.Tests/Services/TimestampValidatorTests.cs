// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Services;
using Mud.Feishu.Webhook.Utilities;

namespace Mud.Feishu.Webhook.Tests.Services;

/// <summary>
/// TimestampValidator 单元测试
/// </summary>
public class TimestampValidatorTests
{
    private readonly Mock<ILogger<TimestampValidator>> _loggerMock;
    private readonly Mock<IOptionsMonitor<FeishuWebhookOptions>> _optionsMock;
    private readonly Mock<IEnvironmentService> _environmentServiceMock;
    private readonly Mock<IWebhookAppKeyAccessor> _appKeyAccessorMock;
    private readonly FeishuWebhookOptions _options;

    public TimestampValidatorTests()
    {
        _loggerMock = new Mock<ILogger<TimestampValidator>>();
        _optionsMock = new Mock<IOptionsMonitor<FeishuWebhookOptions>>();
        _environmentServiceMock = new Mock<IEnvironmentService>();
        _appKeyAccessorMock = new Mock<IWebhookAppKeyAccessor>();
        _options = new FeishuWebhookOptions
        {
            TimestampToleranceSeconds = 30
        };
        _optionsMock.Setup(x => x.CurrentValue).Returns(_options);

        // 设置 _appKeyAccessorMock 使 SetAppKey 方法能够更新 CurrentAppKey 属性
        string? currentAppKey = null;
        _appKeyAccessorMock
            .Setup(x => x.SetAppKey(It.IsAny<string>()))
            .Callback<string>(appKey => currentAppKey = appKey);
        _appKeyAccessorMock
            .Setup(x => x.CurrentAppKey)
            .Returns(() => currentAppKey);
    }

    [Fact]
    public void ValidateTimestamp_WithZeroTimestampInProduction_ShouldReturnFalse()
    {
        // Arrange
        _environmentServiceMock.Setup(x => x.IsProduction).Returns(true);
        var validator = new TimestampValidator(
            _loggerMock.Object,
            _optionsMock.Object,
            _appKeyAccessorMock.Object,
            _environmentServiceMock.Object);

        // Act
        var result = validator.ValidateTimestamp(0);

        // Assert
        result.Should().BeFalse("生产环境应拒绝时间戳为 0 的请求");
    }

    [Fact]
    public void ValidateTimestamp_WithZeroTimestampInDevelopment_ShouldReturnTrue()
    {
        // Arrange
        _environmentServiceMock.Setup(x => x.IsProduction).Returns(false);
        var validator = new TimestampValidator(
            _loggerMock.Object,
            _optionsMock.Object,
            _appKeyAccessorMock.Object,
            _environmentServiceMock.Object);

        // Act
        var result = validator.ValidateTimestamp(0);

        // Assert
        result.Should().BeTrue("开发环境应允许时间戳为 0 的请求");
    }

    [Fact]
    public void ValidateTimestamp_WithValidTimestamp_ShouldReturnTrue()
    {
        // Arrange
        _environmentServiceMock.Setup(x => x.IsProduction).Returns(true);
        var validator = new TimestampValidator(
            _loggerMock.Object,
            _optionsMock.Object,
            _appKeyAccessorMock.Object,
            _environmentServiceMock.Object);
        var validTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Act
        var result = validator.ValidateTimestamp(validTimestamp);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateTimestamp_WithExpiredTimestamp_ShouldReturnFalse()
    {
        // Arrange
        _environmentServiceMock.Setup(x => x.IsProduction).Returns(true);
        var validator = new TimestampValidator(
            _loggerMock.Object,
            _optionsMock.Object,
            _appKeyAccessorMock.Object,
            _environmentServiceMock.Object);
        var expiredTimestamp = DateTimeOffset.UtcNow.AddSeconds(-60).ToUnixTimeSeconds();

        // Act
        var result = validator.ValidateTimestamp(expiredTimestamp);

        // Assert
        result.Should().BeFalse("时间戳超出容错范围应被拒绝");
    }

    [Fact]
    public void ValidateTimestamp_WithMillisecondTimestamp_ShouldReturnTrue()
    {
        // Arrange
        _environmentServiceMock.Setup(x => x.IsProduction).Returns(true);
        var validator = new TimestampValidator(
            _loggerMock.Object,
            _optionsMock.Object,
            _appKeyAccessorMock.Object,
            _environmentServiceMock.Object);
        var millisecondTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Act
        var result = validator.ValidateTimestamp(millisecondTimestamp);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateTimestamp_WithAppSpecificConfig_ShouldUseAppConfig()
    {
        // Arrange
        _environmentServiceMock.Setup(x => x.IsProduction).Returns(true);
        var appOptions = new FeishuWebhookOptions
        {
            TimestampToleranceSeconds = 60,
            Apps = new Dictionary<string, FeishuAppWebhookOptions>
            {
                ["test_app"] = new FeishuAppWebhookOptions
                {
                    AppKey = "test_app",
                    TimestampToleranceSeconds = 120
                }
            }
        };
        _optionsMock.Setup(x => x.CurrentValue).Returns(appOptions);
        
        var validator = new TimestampValidator(
            _loggerMock.Object,
            _optionsMock.Object,
            _appKeyAccessorMock.Object,
            _environmentServiceMock.Object);
        validator.SetCurrentAppKey("test_app");

        var expiredTimestamp = DateTimeOffset.UtcNow.AddSeconds(-90).ToUnixTimeSeconds();

        // Act
        var result = validator.ValidateTimestamp(expiredTimestamp);

        // Assert
        result.Should().BeTrue("应使用应用特定的容错范围");
    }
}
