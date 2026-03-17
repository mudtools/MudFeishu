// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规和许可证的要求。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会顺序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Webhook.Configuration;

namespace Mud.Feishu.Webhook.Tests.Services;

/// <summary>
/// FailedEventRetryService 单元测试
/// </summary>
public class FailedEventRetryServiceTests
{
    private readonly Mock<ILogger<FailedEventRetryService>> _loggerMock;
    private readonly Mock<IFeishuWebhookService> _webhookServiceMock;
    private readonly FailedEventRetryOptions _options;

    public FailedEventRetryServiceTests()
    {
        _loggerMock = new Mock<ILogger<FailedEventRetryService>>();
        _webhookServiceMock = new Mock<IFeishuWebhookService>();
        _options = new FailedEventRetryOptions
        {
            EnableRetry = true,
            MaxRetryCount = 3,
            MaxRetryPerPoll = 10,
            RetryPollIntervalSeconds = 60
        };
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var optionsMock = Options.Create(_options);

        // Act
        var service = new FailedEventRetryService(
            optionsMock,
            _loggerMock.Object,
            _webhookServiceMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithRetryEnabledButNoEventStore_ShouldLogWarning()
    {
        // Arrange
        var options = new FailedEventRetryOptions
        {
            EnableRetry = true,
            MaxRetryCount = 3,
            MaxRetryPerPoll = 10,
            RetryPollIntervalSeconds = 60
        };
        var optionsMock = Options.Create(options);

        // Act
        var service = new FailedEventRetryService(
            optionsMock,
            _loggerMock.Object,
            _webhookServiceMock.Object,
            null);

        // Assert - Should not throw, but should log warning
        service.Should().NotBeNull();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("未配置失败事件存储")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithRetryDisabled_ShouldNotLogWarning()
    {
        // Arrange
        var options = new FailedEventRetryOptions
        {
            EnableRetry = false
        };
        var optionsMock = Options.Create(options);

        // Act
        var service = new FailedEventRetryService(
            optionsMock,
            _loggerMock.Object,
            _webhookServiceMock.Object,
            null);

        // Assert
        service.Should().NotBeNull();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("未配置失败事件存储")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldCreateInstance()
    {
        // Arrange
        var eventStoreMock = new Mock<IFailedEventStore>();
        var optionsMock = Options.Create(_options);

        // Act
        var service = new FailedEventRetryService(
            optionsMock,
            _loggerMock.Object,
            _webhookServiceMock.Object,
            eventStoreMock.Object);

        // Assert
        service.Should().NotBeNull();
    }
}
